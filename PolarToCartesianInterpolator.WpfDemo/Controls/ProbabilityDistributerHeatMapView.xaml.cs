using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.WpfDemo.Controls;

public partial class ProbabilityDistributerHeatMapView : UserControl
{
    public static readonly DependencyProperty GridDataProperty = DependencyProperty.Register(
        nameof(GridData),
        typeof(double[,]),
        typeof(ProbabilityDistributerHeatMapView),
        new PropertyMetadata(null, OnInputChanged));

    public static readonly DependencyProperty DisplayGridProperty = DependencyProperty.Register(
        nameof(DisplayGrid),
        typeof(double[,]),
        typeof(ProbabilityDistributerHeatMapView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CutoffProperty = DependencyProperty.Register(
        nameof(Cutoff),
        typeof(double),
        typeof(ProbabilityDistributerHeatMapView),
        new PropertyMetadata(0.1d));

    private ProbabilityDistributer? _probabilityDistributer;

    public ProbabilityDistributerHeatMapView()
    {
        InitializeComponent();
        // GridData = ExampleUsage.Create400By400SampleGrid();

        Loaded += (_, _) => RefreshGrid();
    }

    public double[,]? GridData
    {
        get => (double[,]?)GetValue(GridDataProperty);
        set => SetValue(GridDataProperty, value);
    }

    public double[,]? DisplayGrid
    {
        get => (double[,]?)GetValue(DisplayGridProperty);
        private set => SetValue(DisplayGridProperty, value);
    }

    public double Cutoff
    {
        get => (double)GetValue(CutoffProperty);
        set => SetValue(CutoffProperty, value);
    }

    private static void OnInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ProbabilityDistributerHeatMapView)d;
        control.RefreshGrid();
    }

    private void R50TextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_probabilityDistributer is null)
            return;

        if (!double.TryParse(R50TextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var r50) || r50 < 0)
            r50 = 0;
        _probabilityDistributer.SetGrid(ExampleUsage.Create400By400SampleGrid());

        _probabilityDistributer.R50 = r50;
        R50TextBox.Text = r50.ToString("0.###", CultureInfo.InvariantCulture);
        ApplyDistribution();
    }

    private void TargetProbabilityComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_probabilityDistributer is null || TargetProbabilityComboBox.SelectedItem is not ComboBoxItem selected)
            return;

        if (!double.TryParse(selected.Content?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var probability))
            probability = 0.95d;

        _probabilityDistributer.TargetProbability = probability;
        ApplyDistribution();
    }

    private void RefreshGrid()
    {
        if (GridData is null)
            return;

        _probabilityDistributer = new ProbabilityDistributer(GridData)
        {
            R50 = 0d,
            TargetProbability = 0.95d
        };

        _probabilityDistributer.SetGrid(ExampleUsage.Create400By400SampleGrid());
        R50TextBox.Text = "0";
        TargetProbabilityComboBox.SelectedIndex = 0;

        ApplyDistribution();
    }

    private void ApplyDistribution()
    {
        if (_probabilityDistributer is null)
            return;

        var kernelLength = _probabilityDistributer.KernelLength;
        DisplayGrid = _probabilityDistributer.BuildFilteredGrid();

        SigmaTextBlock.Text = $"SigmaR: {_probabilityDistributer.SigmaR:0.###}";
        KernelTextBlock.Text = $"Kernel Length: {kernelLength}";
    }
}
