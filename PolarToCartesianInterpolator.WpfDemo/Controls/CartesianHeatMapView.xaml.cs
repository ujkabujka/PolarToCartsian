using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.WpfDemo.Controls;

public partial class CartesianHeatMapView : UserControl
{
    public static readonly DependencyProperty GridDataProperty = DependencyProperty.Register(
        nameof(GridData),
        typeof(float[,]),
        typeof(CartesianHeatMapView),
        new PropertyMetadata(null, OnVisualInputChanged));

    public static readonly DependencyProperty CutoffProperty = DependencyProperty.Register(
        nameof(Cutoff),
        typeof(double),
        typeof(CartesianHeatMapView),
        new PropertyMetadata(0.1d, OnVisualInputChanged));

    public static readonly DependencyProperty MaxPointOverlayProperty = DependencyProperty.Register(
        nameof(MaxPointOverlay),
        typeof(GridMaxPoint?),
        typeof(CartesianHeatMapView),
        new PropertyMetadata(null, OnOverlayInputChanged));

    public static readonly DependencyProperty RectangleOverlayProperty = DependencyProperty.Register(
        nameof(RectangleOverlay),
        typeof(MaxSumRectangle?),
        typeof(CartesianHeatMapView),
        new PropertyMetadata(null, OnOverlayInputChanged));

    private HeatMapRenderData? _lastRenderData;
    private CartesianHeatMapControl? _heatMapControl;
    private Border? _activeProbeBox;
    private Shape? _activeProbeMark;
    private DispatcherTimer? _probeTimer;
    private readonly ScaleTransform _zoomTransform = new(1, 1);
    private readonly TranslateTransform _panTransform = new(0, 0);
    private bool _isPanning;
    private Point _lastPanPoint;

    public CartesianHeatMapView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            CutoffTextBox.Text = Cutoff.ToString("0.###", CultureInfo.InvariantCulture);
            RenderHeatMap();
        };

        HeatMapViewport.SizeChanged += (_, _) =>
        {
            ClampPanOffsets();
            RedrawOverlays();
            RedrawAxes();
        };
        HeatMapViewport.MouseWheel += HeatMapViewport_OnMouseWheel;
        HeatMapViewport.MouseMove += HeatMapViewport_OnMouseMove;
        HeatMapViewport.MouseDown += HeatMapViewport_OnMouseDown;
        HeatMapViewport.MouseUp += HeatMapViewport_OnMouseUp;

        XAxisCanvas.SizeChanged += (_, _) => RedrawAxes();
        YAxisCanvas.SizeChanged += (_, _) => RedrawAxes();
        LegendCanvas.SizeChanged += (_, _) => DrawLegendLabels();

        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_zoomTransform);
        transformGroup.Children.Add(_panTransform);
        PanZoomContent.RenderTransform = transformGroup;
    }

    public float[,]? GridData
    {
        get => (float[,]?)GetValue(GridDataProperty);
        set => SetValue(GridDataProperty, value);
    }

    public double Cutoff
    {
        get => (double)GetValue(CutoffProperty);
        set => SetValue(CutoffProperty, value);
    }

    public GridMaxPoint? MaxPointOverlay
    {
        get => (GridMaxPoint?)GetValue(MaxPointOverlayProperty);
        set => SetValue(MaxPointOverlayProperty, value);
    }

    public MaxSumRectangle? RectangleOverlay
    {
        get => (MaxSumRectangle?)GetValue(RectangleOverlayProperty);
        set => SetValue(RectangleOverlayProperty, value);
    }

    public void SavePlot(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (HeatMapImage.Source is null)
            throw new InvalidOperationException("Save için önce grid render edilmiş olmalı.");

        FrameworkElement exportAnchor = YAxisCanvas.ActualWidth > 0 ? YAxisCanvas : HeatMapViewport;
        var start = exportAnchor.TranslatePoint(new Point(0, 0), RootGrid);
        var exportWidth = RootGrid.ActualWidth - start.X;
        var exportHeight = RootGrid.ActualHeight - start.Y;

        if (exportWidth <= 0 || exportHeight <= 0)
            throw new InvalidOperationException("Kaydetme alanı henüz hazır değil.");

        var dpi = 96d;
        var fullBitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(RootGrid.ActualWidth),
            (int)Math.Ceiling(RootGrid.ActualHeight),
            dpi,
            dpi,
            PixelFormats.Pbgra32);
        fullBitmap.Render(RootGrid);

        var crop = new CroppedBitmap(
            fullBitmap,
            new Int32Rect(
                x: (int)Math.Floor(start.X),
                y: (int)Math.Floor(start.Y),
                width: (int)Math.Ceiling(exportWidth),
                height: (int)Math.Ceiling(exportHeight)));

        var directory = System.IO.Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        using var fs = File.Create(outputPath);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(crop));
        encoder.Save(fs);
    }

    private static void OnVisualInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CartesianHeatMapView)d;
        if (e.Property == CutoffProperty && control.CutoffTextBox is not null)
            control.CutoffTextBox.Text = control.Cutoff.ToString("0.###", CultureInfo.InvariantCulture);

        control.RenderHeatMap();
    }

    private static void OnOverlayInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CartesianHeatMapView)d;
        control.RedrawOverlays();
    }

    private void SavePlotButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            AddExtension = true,
            DefaultExt = "png",
            FileName = "heatmap.png"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            SavePlot(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dosya kaydedilemedi: {ex.Message}", "Save Plot", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CutoffTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(CutoffTextBox), SavePlotButton);
        Keyboard.Focus(SavePlotButton);
    }

    private void CutoffTextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(CutoffTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var newCutoff) ||
            newCutoff < 0 ||
            newCutoff > 1)
        {
            newCutoff = 0.1;
        }

        Cutoff = newCutoff;
        CutoffTextBox.Text = newCutoff.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void HeatMapViewport_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_heatMapControl is null || _lastRenderData is null)
            return;

        var rect = GetHeatMapDrawRect();
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var click = TransformViewportPointToContent(e.GetPosition(HeatMapViewport));
        if (!rect.Contains(click))
            return;

        var pixelWidth = _lastRenderData.Pixels.GetLength(1);
        var pixelHeight = _lastRenderData.Pixels.GetLength(0);
        var pixelX = ((click.X - rect.Left) / rect.Width) * (pixelWidth - 1);
        var pixelY = ((click.Y - rect.Top) / rect.Height) * (pixelHeight - 1);

        var probe = _heatMapControl.ProbeAtPixel(pixelX, pixelY);
        if (probe is null)
            return;

        ShowProbePopup(click, probe.Value);
    }

    private Point TransformViewportPointToContent(Point point)
    {
        if (PanZoomContent.RenderTransform.Inverse is null)
            return point;

        return PanZoomContent.RenderTransform.Inverse.Transform(point);
    }

    private void HeatMapViewport_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var zoomPoint = e.GetPosition(HeatMapViewport);
        var oldZoom = _zoomTransform.ScaleX;
        var zoomFactor = e.Delta > 0 ? 1.10 : 1 / 1.10;
        var newZoom = Math.Clamp(oldZoom * zoomFactor, 1.0, 12.0);
        if (Math.Abs(newZoom - oldZoom) < 1e-9)
            return;

        var contentX = (zoomPoint.X - _panTransform.X) / oldZoom;
        var contentY = (zoomPoint.Y - _panTransform.Y) / oldZoom;

        _zoomTransform.ScaleX = newZoom;
        _zoomTransform.ScaleY = newZoom;
        _panTransform.X = zoomPoint.X - (contentX * newZoom);
        _panTransform.Y = zoomPoint.Y - (contentY * newZoom);

        ClampPanOffsets();
        RedrawOverlays();
        RedrawAxes();
    }

    private void HeatMapViewport_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        _isPanning = true;
        _lastPanPoint = e.GetPosition(HeatMapViewport);
        HeatMapViewport.CaptureMouse();
        e.Handled = true;
    }

    private void HeatMapViewport_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
            return;

        var current = e.GetPosition(HeatMapViewport);
        var deltaX = current.X - _lastPanPoint.X;
        var deltaY = current.Y - _lastPanPoint.Y;

        _panTransform.X += deltaX;
        _panTransform.Y += deltaY;
        _lastPanPoint = current;

        ClampPanOffsets();
        RedrawOverlays();
        RedrawAxes();
    }

    private void HeatMapViewport_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        _isPanning = false;
        HeatMapViewport.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void ClampPanOffsets()
    {
        var viewportWidth = HeatMapViewport.ActualWidth;
        var viewportHeight = HeatMapViewport.ActualHeight;
        if (viewportWidth <= 0 || viewportHeight <= 0)
            return;

        var zoom = _zoomTransform.ScaleX;
        if (zoom <= 1.0)
        {
            _panTransform.X = 0;
            _panTransform.Y = 0;
            return;
        }

        var scaledWidth = viewportWidth * zoom;
        var scaledHeight = viewportHeight * zoom;

        var minPanX = viewportWidth - scaledWidth;
        var minPanY = viewportHeight - scaledHeight;

        _panTransform.X = Math.Clamp(_panTransform.X, minPanX, 0);
        _panTransform.Y = Math.Clamp(_panTransform.Y, minPanY, 0);
    }

    private Rect GetHeatMapDrawRect()
    {
        if (HeatMapImage.Source is not BitmapSource bmp || HeatMapViewport.ActualWidth <= 0 || HeatMapViewport.ActualHeight <= 0)
            return Rect.Empty;

        var scale = Math.Min(HeatMapViewport.ActualWidth / bmp.PixelWidth, HeatMapViewport.ActualHeight / bmp.PixelHeight);
        var drawWidth = bmp.PixelWidth * scale;
        var drawHeight = bmp.PixelHeight * scale;
        var left = (HeatMapViewport.ActualWidth - drawWidth) / 2.0;
        var top = (HeatMapViewport.ActualHeight - drawHeight) / 2.0;
        return new Rect(left, top, drawWidth, drawHeight);
    }

    private Rect GetVisibleContentRect()
    {
        var drawRect = GetHeatMapDrawRect();
        if (drawRect.IsEmpty || drawRect.Width <= 0 || drawRect.Height <= 0)
            return Rect.Empty;

        var viewportWidth = HeatMapViewport.ActualWidth;
        var viewportHeight = HeatMapViewport.ActualHeight;
        if (viewportWidth <= 0 || viewportHeight <= 0)
            return Rect.Empty;

        var viewportCorners = new[]
        {
            TransformViewportPointToContent(new Point(0, 0)),
            TransformViewportPointToContent(new Point(viewportWidth, 0)),
            TransformViewportPointToContent(new Point(0, viewportHeight)),
            TransformViewportPointToContent(new Point(viewportWidth, viewportHeight))
        };

        var minX = viewportCorners.Min(point => point.X);
        var maxX = viewportCorners.Max(point => point.X);
        var minY = viewportCorners.Min(point => point.Y);
        var maxY = viewportCorners.Max(point => point.Y);

        var visibleInContent = new Rect(new Point(minX, minY), new Point(maxX, maxY));
        visibleInContent.Intersect(drawRect);
        return visibleInContent;
    }

    private void ClearActiveProbe()
    {
        _probeTimer?.Stop();
        _probeTimer = null;

        if (_activeProbeBox is not null)
            OverlayCanvas.Children.Remove(_activeProbeBox);
        if (_activeProbeMark is not null)
            OverlayCanvas.Children.Remove(_activeProbeMark);

        _activeProbeBox = null;
        _activeProbeMark = null;
    }

    private void ShowProbePopup(Point location, HeatMapProbeResult probe)
    {
        ClearActiveProbe();

        var dot = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = Brushes.Transparent,
            Stroke = Brushes.Black,
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(dot, location.X - 4);
        Canvas.SetTop(dot, location.Y - 4);
        OverlayCanvas.Children.Add(dot);
        _activeProbeMark = dot;

        var box = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(240, 255, 255, 255)),
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(4),
            Child = new TextBlock
            {
                FontSize = 11,
                Text =
                    $"T: {probe.InterpolatedTemperature:F3}\n" +
                    $"X: {probe.CartesianX:F2}\n" +
                    $"Y: {probe.CartesianY:F2}\n" +
                    $"R: {probe.Radius:F2}\n" +
                    $"Theta: {probe.AngleDegrees:F1}°"
            }
        };

        Canvas.SetLeft(box, Math.Max(4, Math.Min(location.X + 10, HeatMapViewport.ActualWidth - 150)));
        Canvas.SetTop(box, Math.Max(4, Math.Min(location.Y + 10, HeatMapViewport.ActualHeight - 120)));
        OverlayCanvas.Children.Add(box);
        _activeProbeBox = box;

        _probeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _probeTimer.Tick += (_, _) => ClearActiveProbe();
        _probeTimer.Start();
    }

    private void RenderHeatMap()
    {
        ClearActiveProbe();

        if (GridData is null || GridData.Length == 0)
        {
            HeatMapImage.Source = null;
            _lastRenderData = null;
            _heatMapControl = null;
            OverlayCanvas.Children.Clear();
            XAxisCanvas.Children.Clear();
            YAxisCanvas.Children.Clear();
            LegendCanvas.Children.Clear();
            return;
        }

        _heatMapControl = new CartesianHeatMapControl(Cutoff);
        _heatMapControl.SetGrid(GridData);
        _lastRenderData = _heatMapControl.BuildRenderData(legendStepCount: 11, radialMeshCount: 20, angleMeshStepDegrees: 30);

        var height = _lastRenderData.Pixels.GetLength(0);
        var width = _lastRenderData.Pixels.GetLength(1);
        var stride = width * 4;
        var bytes = new byte[height * stride];

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var color = _lastRenderData.Pixels[row, col];
                var idx = (row * stride) + (col * 4);
                bytes[idx] = color.B;
                bytes[idx + 1] = color.G;
                bytes[idx + 2] = color.R;
                bytes[idx + 3] = color.A;
            }
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bytes, stride);
        bitmap.Freeze();
        HeatMapImage.Source = bitmap;

        RedrawOverlays();
        RedrawAxes();
        DrawLegendLabels();
    }

    private void RedrawOverlays()
    {
        OverlayCanvas.Children.Clear();

        if (_lastRenderData is null)
            return;

        var drawRect = GetHeatMapDrawRect();
        var pixelWidth = _lastRenderData.Pixels.GetLength(1);
        var pixelHeight = _lastRenderData.Pixels.GetLength(0);

        if (drawRect.Width <= 0 || drawRect.Height <= 0 || pixelWidth <= 1 || pixelHeight <= 1)
            return;

        var scaleX = drawRect.Width / (pixelWidth - 1);
        var scaleY = drawRect.Height / (pixelHeight - 1);
        var radiusScale = Math.Min(scaleX, scaleY);
        var centerX = drawRect.Left + (((pixelWidth - 1) / 2.0) * scaleX);
        var centerY = drawRect.Top + (((pixelHeight - 1) / 2.0) * scaleY);

        foreach (var radius in _lastRenderData.PolarMesh.CircleRadiiPixels)
        {
            var ellipse = new Ellipse
            {
                Width = radius * 2 * radiusScale,
                Height = radius * 2 * radiusScale,
                Stroke = new SolidColorBrush(Color.FromArgb(110, 55, 65, 81)),
                StrokeThickness = 0.8,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(ellipse, centerX - (ellipse.Width / 2));
            Canvas.SetTop(ellipse, centerY - (ellipse.Height / 2));
            OverlayCanvas.Children.Add(ellipse);
        }

        var maxRadius = _lastRenderData.PolarMesh.CircleRadiiPixels.Count > 0
            ? _lastRenderData.PolarMesh.CircleRadiiPixels[^1] * radiusScale
            : Math.Min(drawRect.Width, drawRect.Height) / 2.0;

        for (var angle = 0; angle < 360; angle += 30)
        {
            var radians = angle * (Math.PI / 180.0);
            var endX = centerX + (Math.Cos(radians) * maxRadius);
            var endY = centerY - (Math.Sin(radians) * maxRadius);

            var radialLine = new Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = endX,
                Y2 = endY,
                Stroke = new SolidColorBrush(Color.FromArgb(90, 31, 41, 55)),
                StrokeThickness = 0.8
            };
            OverlayCanvas.Children.Add(radialLine);

            var label = new TextBlock
            {
                Text = $"{angle}°",
                FontSize = 10,
                Foreground = Brushes.Black,
                Background = new SolidColorBrush(Color.FromArgb(185, 255, 255, 255))
            };
            Canvas.SetLeft(label, endX - 10);
            Canvas.SetTop(label, endY - 10);
            OverlayCanvas.Children.Add(label);
        }

        var centerMarker = new Ellipse
        {
            Width = 4,
            Height = 4,
            Fill = Brushes.Black
        };
        Canvas.SetLeft(centerMarker, centerX - 2);
        Canvas.SetTop(centerMarker, centerY - 2);
        OverlayCanvas.Children.Add(centerMarker);

        DrawRectangleOverlay(drawRect, scaleX, scaleY);
        DrawMaximumPointOverlay(drawRect, scaleX, scaleY);

        if (_activeProbeMark is not null)
            OverlayCanvas.Children.Add(_activeProbeMark);
        if (_activeProbeBox is not null)
            OverlayCanvas.Children.Add(_activeProbeBox);
    }

    private void RedrawAxes()
    {
        XAxisCanvas.Children.Clear();
        YAxisCanvas.Children.Clear();

        if (_lastRenderData is null)
            return;

        DrawXAxis();
        DrawYAxis();
    }

    private void DrawMaximumPointOverlay(Rect drawRect, double scaleX, double scaleY)
    {
        if (MaxPointOverlay is null)
            return;

        var point = MaxPointOverlay.Value;
        var x = drawRect.Left + (point.Column * scaleX);
        var y = drawRect.Top + (point.Row * scaleY);
        var star = CreateStar(x, y, outerRadius: 8, innerRadius: 4, points: 5);
        star.Stroke = Brushes.Black;
        star.StrokeThickness = 1.2;
        star.Fill = Brushes.Gold;
        OverlayCanvas.Children.Add(star);
    }

    private void DrawRectangleOverlay(Rect drawRect, double scaleX, double scaleY)
    {
        if (RectangleOverlay is null)
            return;

        var rectangle = RectangleOverlay.Value;
        var left = drawRect.Left + (rectangle.LeftColumn * scaleX);
        var top = drawRect.Top + (rectangle.TopRow * scaleY);
        var width = Math.Max(2.0, rectangle.Width * scaleX);
        var height = Math.Max(2.0, rectangle.Height * scaleY);

        var overlay = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = Brushes.LimeGreen,
            StrokeThickness = 2,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(overlay, left);
        Canvas.SetTop(overlay, top);
        OverlayCanvas.Children.Add(overlay);
    }

    private static Polygon CreateStar(double centerX, double centerY, double outerRadius, double innerRadius, int points)
    {
        var polygon = new Polygon();
        var geometryPoints = new PointCollection(points * 2);
        var angleStep = Math.PI / points;
        var startAngle = -Math.PI / 2;

        for (var i = 0; i < points * 2; i++)
        {
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            var angle = startAngle + (i * angleStep);
            geometryPoints.Add(new Point(
                centerX + (Math.Cos(angle) * radius),
                centerY + (Math.Sin(angle) * radius)));
        }

        polygon.Points = geometryPoints;
        return polygon;
    }

    private void DrawXAxis()
    {
        var width = XAxisCanvas.ActualWidth;
        if (width <= 0)
            return;

        var drawRect = GetHeatMapDrawRect();
        if (drawRect.Width <= 0)
            return;

        var visibleRect = GetVisibleContentRect();
        if (visibleRect.Width <= 0)
            return;

        var startX = drawRect.Left;
        var endX = drawRect.Right;
        var axisY = 12.0;
        var arrowBaseX = Math.Max(startX, endX - 10);
        var halfRange = GetOuterRadiusInCartesianUnits();

        var axisLine = new Line { X1 = startX, Y1 = axisY, X2 = arrowBaseX, Y2 = axisY, Stroke = Brushes.Black, StrokeThickness = 1.2 };
        XAxisCanvas.Children.Add(axisLine);
        XAxisCanvas.Children.Add(new Polygon
        {
            Points = new PointCollection { new(arrowBaseX, axisY - 4), new(endX, axisY), new(arrowBaseX, axisY + 4) },
            Fill = Brushes.Black
        });

        var tickCount = 9;
        for (var i = 0; i < tickCount; i++)
        {
            var t = i / (double)(tickCount - 1);
            var x = AxisViewportMath.Interpolate(startX, endX, t);
            var visibleContentX = AxisViewportMath.Interpolate(visibleRect.Left, visibleRect.Right, t);
            var value = AxisViewportMath.MapContentXToCartesian(drawRect.Left, drawRect.Width, halfRange, visibleContentX);

            XAxisCanvas.Children.Add(new Line { X1 = x, Y1 = axisY, X2 = x, Y2 = axisY + 5, Stroke = Brushes.Black, StrokeThickness = 1 });
            var label = new TextBlock { Text = value.ToString("0", CultureInfo.InvariantCulture), FontSize = 10, Foreground = Brushes.Black };
            Canvas.SetLeft(label, Math.Max(0, x - 10));
            Canvas.SetTop(label, axisY + 8);
            XAxisCanvas.Children.Add(label);
        }

        var axisName = new TextBlock { Text = "X", FontWeight = FontWeights.Bold };
        Canvas.SetLeft(axisName, endX + 2);
        Canvas.SetTop(axisName, 0);
        XAxisCanvas.Children.Add(axisName);
    }

    private void DrawYAxis()
    {
        var height = YAxisCanvas.ActualHeight;
        if (height <= 0)
            return;

        var drawRect = GetHeatMapDrawRect();
        if (drawRect.Height <= 0)
            return;

        var visibleRect = GetVisibleContentRect();
        if (visibleRect.Height <= 0)
            return;

        var axisX = YAxisCanvas.ActualWidth - 12;
        var startY = drawRect.Top;
        var endY = drawRect.Bottom;
        var arrowBaseY = Math.Min(endY, startY + 10);
        var halfRange = GetOuterRadiusInCartesianUnits();

        YAxisCanvas.Children.Add(new Line { X1 = axisX, Y1 = endY, X2 = axisX, Y2 = arrowBaseY, Stroke = Brushes.Black, StrokeThickness = 1.2 });
        YAxisCanvas.Children.Add(new Polygon
        {
            Points = new PointCollection { new(axisX - 4, arrowBaseY), new(axisX, startY), new(axisX + 4, arrowBaseY) },
            Fill = Brushes.Black
        });

        var tickCount = 9;

        for (var i = 0; i < tickCount; i++)
        {
            var t = i / (double)(tickCount - 1);
            var y = AxisViewportMath.Interpolate(startY, endY, t);
            var visibleContentY = AxisViewportMath.Interpolate(visibleRect.Top, visibleRect.Bottom, t);
            var value = AxisViewportMath.MapContentYToCartesian(drawRect.Top, drawRect.Height, halfRange, visibleContentY);

            YAxisCanvas.Children.Add(new Line { X1 = axisX, Y1 = y, X2 = axisX - 5, Y2 = y, Stroke = Brushes.Black, StrokeThickness = 1 });
            var label = new TextBlock { Text = value.ToString("0", CultureInfo.InvariantCulture), FontSize = 10, Foreground = Brushes.Black };
            Canvas.SetRight(label, 20);
            Canvas.SetTop(label, Math.Max(0, y - 8));
            YAxisCanvas.Children.Add(label);
        }

        var axisName = new TextBlock { Text = "Y", FontWeight = FontWeights.Bold };
        Canvas.SetLeft(axisName, axisX - 6);
        Canvas.SetTop(axisName, Math.Max(0, startY - 14));
        YAxisCanvas.Children.Add(axisName);
    }

    private double GetOuterRadiusInCartesianUnits()
    {
        if (_lastRenderData is null || _lastRenderData.PolarMesh.CircleRadiiPixels.Count == 0)
            return 0;

        return _lastRenderData.PolarMesh.CircleRadiiPixels[^1];
    }

    private void DrawLegendLabels()
    {
        LegendCanvas.Children.Clear();

        if (_lastRenderData is null)
            return;

        var h = LegendCanvas.ActualHeight;
        if (h <= 0)
            return;

        var values = Enumerable.Range(0, 11).Select(i => 1.0 - (i * 0.1)).ToArray();
        for (var i = 0; i < values.Length; i++)
        {
            var y = i * (h / (values.Length - 1));
            LegendCanvas.Children.Add(new Line { X1 = 0, Y1 = y, X2 = 6, Y2 = y, Stroke = Brushes.Black, StrokeThickness = 1 });

            var label = new TextBlock { Text = values[i].ToString("0.0", CultureInfo.InvariantCulture), FontSize = 10 };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, Math.Max(0, y - 8));
            LegendCanvas.Children.Add(label);
        }
    }
}
