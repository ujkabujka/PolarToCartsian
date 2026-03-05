# PolarToCartesianInterpolator

Bu proje, 3D polar koordinat verisini (`f(r, θ) = temperature`) 2D kartezyen grid'e (`f(x, y) = temperature`) lineer interpolasyon ile dönüştürmek için hazırlanmış .NET sınıfları içerir.

## İçerik

- `RadiusValues`: Tek bir radius halkasının açı ve sıcaklık verisini tutar.
- `PolarGridInterpolator`: 
  - `InterpolateTemperaturePolar(r, theta)` ile polar noktada sıcaklık üretir.
  - `InterpolateTemperatureAtCartesian(x, y)` ile kartezyen noktada sıcaklık üretir.
  - `BuildCartesianTemperatureGrid(n)` ile `n x n` integer grid üretir (1 metre çözünürlük).
- `ExampleUsage`: 5,7,9,11,13m halka örneği ile 400x400 grid üretimi gösterir.

## Notlar

- Radiuslar sabit artışla gelmelidir (ör: 5, 7, 9, 11, 13).
- Her radius içindeki açı listesi 0'dan başlayıp eşit aralıklı olmalıdır.
- Sıcaklıklar 0 ile 1 arasında beklenir.
- Radius aralığı dışındaki noktalar için `double.NaN` döner.
