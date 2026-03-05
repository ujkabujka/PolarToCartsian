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
