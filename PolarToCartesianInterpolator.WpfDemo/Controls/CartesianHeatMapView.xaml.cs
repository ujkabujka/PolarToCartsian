using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.WpfDemo.Controls;

public partial class CartesianHeatMapView : UserControl
{
    public static readonly DependencyProperty GridDataProperty = DependencyProperty.Register(
        nameof(GridData),
        typeof(double[,]),
        typeof(CartesianHeatMapView),
        new PropertyMetadata(null, OnVisualInputChanged));

    public static readonly DependencyProperty CutoffProperty = DependencyProperty.Register(
        nameof(Cutoff),
        typeof(double),
        typeof(CartesianHeatMapView),
        new PropertyMetadata(0.1d, OnVisualInputChanged));

    private HeatMapRenderData? _lastRenderData;

    public CartesianHeatMapView()
    {
        InitializeComponent();
        Loaded += (_, _) => RenderHeatMap();
        HeatMapViewport.SizeChanged += (_, _) => RedrawOverlays();
        XAxisCanvas.SizeChanged += (_, _) => RedrawAxes();
        YAxisCanvas.SizeChanged += (_, _) => RedrawAxes();
        LegendCanvas.SizeChanged += (_, _) => DrawLegendLabels();
    }

    public double[,]? GridData
    {
        get => (double[,]?)GetValue(GridDataProperty);
        set => SetValue(GridDataProperty, value);
    }

    public double Cutoff
    {
        get => (double)GetValue(CutoffProperty);
        set => SetValue(CutoffProperty, value);
    }

    private static void OnVisualInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((CartesianHeatMapView)d).RenderHeatMap();
    }

    private void RenderHeatMap()
    {
        if (GridData is null || GridData.Length == 0)
        {
            HeatMapImage.Source = null;
            _lastRenderData = null;
            OverlayCanvas.Children.Clear();
            XAxisCanvas.Children.Clear();
            YAxisCanvas.Children.Clear();
            LegendCanvas.Children.Clear();
            return;
        }

        var heatMap = new CartesianHeatMapControl(Cutoff);
        heatMap.SetGrid(GridData);
        _lastRenderData = heatMap.BuildRenderData(legendStepCount: 11, radialMeshCount: 20, angleMeshStepDegrees: 30);

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

        var pixelWidth = _lastRenderData.Pixels.GetLength(1);
        var pixelHeight = _lastRenderData.Pixels.GetLength(0);
        var viewportWidth = HeatMapViewport.ActualWidth;
        var viewportHeight = HeatMapViewport.ActualHeight;

        if (viewportWidth <= 0 || viewportHeight <= 0 || pixelWidth <= 1 || pixelHeight <= 1)
            return;

        var scaleX = viewportWidth / (pixelWidth - 1);
        var scaleY = viewportHeight / (pixelHeight - 1);
        var radiusScale = Math.Min(scaleX, scaleY);
        var centerX = ((pixelWidth - 1) / 2.0) * scaleX;
        var centerY = ((pixelHeight - 1) / 2.0) * scaleY;

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

        var centerMarker = new Ellipse
        {
            Width = 4,
            Height = 4,
            Fill = Brushes.Black
        };
        Canvas.SetLeft(centerMarker, centerX - 2);
        Canvas.SetTop(centerMarker, centerY - 2);
        OverlayCanvas.Children.Add(centerMarker);
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

    private void DrawXAxis()
    {
        var axisY = 12.0;
        var width = XAxisCanvas.ActualWidth;
        if (width <= 0)
            return;

        var axisLine = new Line
        {
            X1 = 0,
            Y1 = axisY,
            X2 = width - 12,
            Y2 = axisY,
            Stroke = Brushes.Black,
            StrokeThickness = 1.2
        };
        XAxisCanvas.Children.Add(axisLine);

        var arrow = new Polygon
        {
            Points = new PointCollection
            {
                new(width - 12, axisY - 4),
                new(width, axisY),
                new(width - 12, axisY + 4)
            },
            Fill = Brushes.Black
        };
        XAxisCanvas.Children.Add(arrow);

        var pixelWidth = _lastRenderData!.Pixels.GetLength(1);
        var scaleX = width / (pixelWidth - 1.0);

        foreach (var tick in _lastRenderData.XAxisTicks)
        {
            var x = tick.PixelPosition * scaleX;
            var mark = new Line
            {
                X1 = x,
                Y1 = axisY,
                X2 = x,
                Y2 = axisY + 5,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            XAxisCanvas.Children.Add(mark);

            var label = new TextBlock
            {
                Text = tick.CartesianValue.ToString("0", CultureInfo.InvariantCulture),
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(label, Math.Max(0, x - 10));
            Canvas.SetTop(label, axisY + 8);
            XAxisCanvas.Children.Add(label);
        }

        var axisName = new TextBlock { Text = "X", FontWeight = FontWeights.Bold };
        Canvas.SetLeft(axisName, width - 10);
        Canvas.SetTop(axisName, 0);
        XAxisCanvas.Children.Add(axisName);
    }

    private void DrawYAxis()
    {
        var axisX = YAxisCanvas.ActualWidth - 12;
        var height = YAxisCanvas.ActualHeight;
        if (height <= 0 || axisX <= 0)
            return;

        var axisLine = new Line
        {
            X1 = axisX,
            Y1 = height,
            X2 = axisX,
            Y2 = 12,
            Stroke = Brushes.Black,
            StrokeThickness = 1.2
        };
        YAxisCanvas.Children.Add(axisLine);

        var arrow = new Polygon
        {
            Points = new PointCollection
            {
                new(axisX - 4, 12),
                new(axisX, 0),
                new(axisX + 4, 12)
            },
            Fill = Brushes.Black
        };
        YAxisCanvas.Children.Add(arrow);

        var pixelHeight = _lastRenderData!.Pixels.GetLength(0);
        var scaleY = height / (pixelHeight - 1.0);

        foreach (var tick in _lastRenderData.YAxisTicks)
        {
            var y = tick.PixelPosition * scaleY;
            var mark = new Line
            {
                X1 = axisX,
                Y1 = y,
                X2 = axisX - 5,
                Y2 = y,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            YAxisCanvas.Children.Add(mark);

            var label = new TextBlock
            {
                Text = tick.CartesianValue.ToString("0", CultureInfo.InvariantCulture),
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetRight(label, 20);
            Canvas.SetTop(label, Math.Max(0, y - 8));
            YAxisCanvas.Children.Add(label);
        }

        var axisName = new TextBlock { Text = "Y", FontWeight = FontWeights.Bold };
        Canvas.SetLeft(axisName, axisX - 6);
        Canvas.SetTop(axisName, 0);
        YAxisCanvas.Children.Add(axisName);
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
            var mark = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = 6,
                Y2 = y,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            LegendCanvas.Children.Add(mark);

            var label = new TextBlock
            {
                Text = values[i].ToString("0.0", CultureInfo.InvariantCulture),
                FontSize = 10
            };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, Math.Max(0, y - 8));
            LegendCanvas.Children.Add(label);
        }
    }
}
