using System.Windows;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.WpfDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}

public sealed class MainWindowViewModel
{
    public float[,] HeatMapGrid { get; } = ExampleUsage.Create400By400SampleGrid();
}
