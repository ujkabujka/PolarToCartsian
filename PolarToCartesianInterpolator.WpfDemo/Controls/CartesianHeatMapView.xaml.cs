using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    public CartesianHeatMapView()
    {
        InitializeComponent();
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
            return;
        }

        var heatMap = new CartesianHeatMapControl(Cutoff);
        heatMap.SetGrid(GridData);
        var renderData = heatMap.BuildRenderData();

        var height = renderData.Pixels.GetLength(0);
        var width = renderData.Pixels.GetLength(1);
        var stride = width * 4;
        var bytes = new byte[height * stride];

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var color = renderData.Pixels[row, col];
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
    }
}
