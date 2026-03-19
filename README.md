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
- Radius aralığı dışındaki noktalar interpolasyonda `double.NaN` döner; senaryo testinde convolution öncesi bu değerler `0` ile normalize edilir.

## WPF Heat Map Demo

Repoya `PolarToCartsian.sln` çözümü ve `PolarToCartesianInterpolator.WpfDemo` isminde bir WPF demo projesi eklendi.

- `Controls/CartesianHeatMapView`: beyaz arka plan, solda Save Plot butonu + cutoff girişi, sağda dikey renk legend barı (1.0 -> 0.0), alt/sol eksen okları ve değer etiketleri, merkezden 30° aralıklı 12 radyal çizgi ve 20 sabit radius halkası ile çizim yapar.
- `MainWindow`: Açılışta `ExampleUsage.Create400By400SampleGrid()` ile örnek grid yükleyip kontrolü varsayılan olarak gösterir.

### VS Code ile çalıştırma

> Not: WPF yalnızca Windows'ta çalışır.

1. Çözümü build edin:

```powershell
dotnet build .\PolarToCartsian.sln
```

2. Demo uygulamayı başlatın:

```powershell
dotnet run --project .\PolarToCartesianInterpolator.WpfDemo\PolarToCartesianInterpolator.WpfDemo.csproj
```

Ayrıca VS Code için hazır:

- `.vscode/tasks.json` (`build-wpf-demo`, `run-wpf-demo`)
- `.vscode/launch.json` (`.NET Launch WPF Demo`)

### Testler

Heat map kontrolü için temel davranış testleri eklendi (`tests/PolarToCartesianInterpolator.Tests`).

```powershell
dotnet test .\tests\PolarToCartesianInterpolator.Tests\PolarToCartesianInterpolator.Tests.csproj
```


`CartesianHeatMapView` ayrıca kod tarafından çağrılabilecek bir `SavePlot(string path)` metodu sağlar (PNG kaydı). UI tarafındaki "Save Plot" butonu dosya diyalogu ile bu metodu kullanır. Kaydedilen görsel plot+axis+legend alanını birlikte içerir.

Cutoff textbox'ı Enter ile odaktan çıkar; değer geçersizse otomatik `0.1` uygulanır ve çizim yenilenir.
