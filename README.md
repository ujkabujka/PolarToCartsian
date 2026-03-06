# PolarToCartesianInterpolator

Bu proje, 3D polar koordinat verisini (`f(r, θ) = temperature`) 2D kartezyen grid'e (`f(x, y) = temperature`) lineer interpolasyon ile dönüştürmek için hazırlanmış .NET sınıfları içerir.

## İçerik

- `RadiusValues`: Tek bir radius halkasının açı ve sıcaklık verisini tutar.
- `PolarGridInterpolator`:
  - `InterpolateTemperaturePolar(r, theta)` ile polar noktada sıcaklık üretir.
  - `InterpolateTemperatureAtCartesian(x, y)` ile kartezyen noktada sıcaklık üretir.
  - `BuildCartesianTemperatureGrid(n)` ile `n x n` integer grid üretir (1 metre çözünürlük).
- `ExampleUsage`: 5,7,9,11,13m halka örneği ile 400x400 grid üretimi gösterir.
- `InterpolationPerformanceTest`: 2m'den başlayıp 300m'ye kadar 2m artımlı, her halkada en az 60 açı olacak şekilde sentetik polar veri üretir ve 400x400 grid için süre ölçer.

- `CartesianHeatMapControl`:
  - `SetGrid(double[,])` ile herhangi bir boyuttaki kartezyen sıcaklık verisini alır.
  - `BuildRenderData(...)` ile heat-map piksel renklerini, kartezyen eksen ticklerini, polar mesh (daire+açı) çizim verisini ve legend bilgisini üretir.
  - `ProbeAtPixel(x,y)` ile tıklanan noktada x/y, radius, açı ve bilinear interpolate sıcaklık bilgisini döner.
  - `Cutoff` (default `0.1`) altındaki tüm değerleri beyaz olarak işaretler.

## Notlar

- Radiuslar sabit artışla gelmelidir (ör: 5, 7, 9, 11, 13).
- Her radius içindeki açı listesi 0'dan başlayıp eşit aralıklı olmalıdır.
- Sıcaklıklar 0 ile 1 arasında beklenir.
- Radius aralığı dışındaki noktalar için `double.NaN` döner.

## Performans Testi Kullanımı

```csharp
var result = InterpolationPerformanceTest.Run400x400Benchmark();
Console.WriteLine($"Ring sayısı: {result.RingCount}");
Console.WriteLine($"Grid: {result.GridSize}x{result.GridSize}");
Console.WriteLine($"Süre: {result.ElapsedMilliseconds:F2} ms");
```


## HeatMap Kontrol Notları

`CartesianHeatMapControl`, UI framework'ünden bağımsız bir "render-model" sağlar. Bu sayede WPF/WinForms/MAUI/Blazor gibi katmanlarda aynı veri modeli kullanılarak görselleştirme yapılabilir.

Renk geçişi: **1.0 kırmızı -> 0.75 turuncu -> 0.5 sarı -> 0.25 yeşil -> 0.0 mavi**.

`ProbeAtPixel` çıktısı, bir popup/kutu içerisinde gösterilecek tıklama bilgisini üretmek için kullanılabilir:

```csharp
var grid = ExampleUsage.Create400By400SampleGrid();
var heatMap = new CartesianHeatMapControl(cutoff: 0.1);
heatMap.SetGrid(grid);

var renderData = heatMap.BuildRenderData();
var probe = heatMap.ProbeAtPixel(240, 120);

if (probe is not null)
{
    Console.WriteLine($"x={probe.Value.CartesianX:F2}, y={probe.Value.CartesianY:F2}");
    Console.WriteLine($"r={probe.Value.Radius:F2}, angle={probe.Value.AngleDegrees:F2}");
    Console.WriteLine($"temp={probe.Value.InterpolatedTemperature:F4}");
}
```
