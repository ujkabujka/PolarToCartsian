# PolarToCartesianInterpolator

Bu proje, 3D polar koordinat verisini (`f(r, θ) = temperature`) 2D kartezyen grid'e (`f(x, y) = temperature`) lineer interpolasyon ile dönüştürmek için hazırlanmış .NET sınıfları içerir.

## İçerik

- `RadiusValues`: Tek bir radius halkasının açı ve sıcaklık verisini tutar.
- `PolarGridInterpolator`:
  - `InterpolateTemperaturePolar(r, theta)` ile polar noktada sıcaklık üretir.
  - `InterpolateTemperatureAtCartesian(x, y)` ile kartezyen noktada sıcaklık üretir.
  - `BuildCartesianTemperatureGrid(n)` ile `n x n` integer grid üretir (1 metre çözünürlük).
- `InterpolationPerformanceTest`:
  - `2m`'den `300m`'ye kadar `2m` artımlı radius üretir.
  - Her halkada en az `60` açı üretir.
  - `400 x 400` interpolasyon süresini ölçer.
- `CartesianConvolutionFilter`:
  - Verilen kare 2D kernel ile convolution uygular.
  - Border padding varsayılanı: edge-extension (sınırdaki değeri dışa doğru uzatma).
  - Çıktı boyutu giriş grid ile aynıdır (`same` convolution).
- `Program`:
  - Senaryo testini çalıştırır.
  - Interpolasyon + convolution sürelerini ve örnek değerleri konsola basar.

## Kurulum

- .NET 8 SDK gerekir.
- Windows PowerShell örneği:

```powershell
dotnet --info
```

## Çalıştırma

Proje artık executable olarak ayarlanmıştır.

```powershell
dotnet run --project .\PolarToCartesianInterpolator.csproj
```

Bu komut aşağıdakileri yapar:
1. `2..300` (adım `2`) polar ring seti oluşturur.
2. `400x400` kartezyen grid interpolate eder.
3. 3x3 gaussian-benzeri kernel ile convolution uygular.
4. Interpolasyon ve convolution sürelerini yazdırır.

## Test / Benchmark Notları

Önceki hatadaki gibi tek bir `.cs` dosyasını `dotnet test` ile çalıştırmak **doğru değil**:

```powershell
# Yanlış kullanım
dotnet test .\src\InterpolationPerformanceTest.cs
```

`dotnet test` bir test projesi (`.csproj`) bekler. Bu repoda hızlı senaryo testi için `dotnet run` kullanılmalıdır:

```powershell
# Doğru kullanım (bu repo için)
dotnet run --project .\PolarToCartesianInterpolator.csproj
```

## Notlar

- Radiuslar sabit artışla gelmelidir (ör: 5, 7, 9, 11, 13).
- Her radius içindeki açı listesi 0'dan başlayıp eşit aralıklı olmalıdır.
- Sıcaklıklar 0 ile 1 arasında beklenir.
- Radius aralığı dışındaki noktalar interpolasyonda `double.NaN` döner; senaryo testinde convolution öncesi bu değerler `0` ile normalize edilir.
