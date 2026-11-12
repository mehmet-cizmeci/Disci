# Disci geliştirici notları

Kaynak koddan çalıştırma, proje yapısı ve testler. Kullanıcılar için:
[Kurulum rehberi](Kurulum-Rehberi.md), [Kullanım kılavuzu](Kullanim-Kilavuzu.md).

## Proje yapısı

| Klasör | İçerik |
|---|---|
| `src/Disci.Domain` | Entity sınıfları: hasta, anamnez, tedavi, ödeme, gider, randevu, reçete, hekim, katalog |
| `src/Disci.Infrastructure` | `DisciDbContext` (Identity dahil), tablo ayarları, EF migration'ları, `ApplicationUser` ve roller |
| `src/Disci.Web` | Blazor web uygulaması |
| `tests/Disci.Web.Tests` | Sayfaların ve erişim kurallarının entegrasyon testleri |
| `data/` | Yerel çalışma verileri (Git'e girmez) |

## Web uygulamasını çalıştırma

```
dotnet run --project src/Disci.Web
```

Ardından tarayıcıda `http://localhost:5111` adresini açın. Uygulama açılırken veritabanını günceller ve rolleri oluşturur.

**İlk giriş:** Hiç kullanıcı yoksa program **ilk kurulum formunu** (`/kurulum`) açar: klinik bilgileri ve yönetici
hesabı girilir. Form yalnızca programın çalıştığı bilgisayardan açılır. Geliştirmede istenirse ilk yönetici
`Seed:AdminEmail` / `Seed:AdminPassword` ayarlarından da (ör. .NET user-secrets) oluşturulur:

```
dotnet user-secrets list --project src/Disci.Web
```

Herkese açık "kayıt ol" sayfası yoktur; hesapları yalnızca yönetici açar.


## Kurulum paketi

```
powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1
```

Testleri çalıştırır, web uygulamasını .NET gerektirmeyecek şekilde (self-contained win-x64) derler,
kurulum betikleri, kurulum rehberi ve kılavuz PDF'iyle birlikte `dist\Disci-Kurulum-<sürüm>-win-x64.zip` yapar. Kurulumun
kendisi: [Kurulum rehberi](Kurulum-Rehberi.md). `KUR.bat -Deneme` hiçbir şeyi değiştirmeden yapılacakları gösterir.

### Yeni sürüm yayınlama

1. `Directory.Build.props` içindeki `<Version>`'ı artırın (programın Güncelleme sayfası bunu GitHub'daki etiketle karşılaştırır).
2. SQL Server çevrimdışı dosyası bir kez indirilir (Git'e girmez): `powershell -File deploy\sql-indir.ps1`
3. `powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1 -Unix` → `dist\` altında:
   `Disci-Kurulum-<sürüm>-win-x64.zip` (güncellemeler bunu indirir, adı değiştirilmemeli), `...-linux-macos.tar.gz`,
   `SHA256SUMS.txt` ve (SQL dosyası indirildiyse) `...-win-x64-SQL.zip`.
4. `gh release create v<sürüm> dist\Disci-Kurulum-<sürüm>-win-x64.zip dist\Disci-Kurulum-<sürüm>-linux-macos.tar.gz dist\SHA256SUMS.txt --title "Disci <sürüm>" --notes-file notlar.md`
   **SHA256SUMS.txt yüklenmezse kurulu programlar güncellemeyi reddeder** (paket doğrulanamaz).
   **SQL'li paket GitHub'a yüklenmez** (kullanıcı kararı, 2026-11-09: Microsoft dosyası); yalnızca USB ile kliniğe
   götürmek içindir. Sürüm notlarına Microsoft'un indirme bağlantısı yazılır.
5. Kurulu programlar en geç 12 saat içinde yeni sürümü görür; Güncelleme sayfasındaki "Şimdi denetle" hemen sorar.

Linux/macOS paketi Docker içinde çalışır (`deploy/unix`): `.sh` dosyaları LF satır sonuyla kalmalı (`.gitattributes`).

Disci **imzasız** dağıtılır (kod imzalama sertifikası ücretli olduğu için alınmadı). Paketleme yine de imzalama adımını
çalıştırır (`deploy\imzala.ps1`): sertifika yoksa uyarı verip imzasız bırakır; ileride sertifika edinilirse programlar ve
kurulum betikleri kendiliğinden imzalanır (`-ImzaZorunlu` ile sertifika yoksa hata verir). Ayrıntı: [Kod imzalama](IMZALAMA.md).

## Kullanım kılavuzu ve ekran görüntüleri

- Kılavuz tek kaynaktır: `docs/Kullanim-Kilavuzu.md`. Program `/kilavuz` adresinde gösterir (Markdig), GitHub da okur.
- PDF: `powershell -File docs\kilavuz-pdf.ps1` (Chrome veya Edge ile) → `docs/Kullanim-Kilavuzu.pdf`.
- Ekran görüntüleri tamamen uydurma bir örnek klinikten çekilir (`tests/Disci.Web.Tests/GuideScreenshotServer.cs`):
  `DISCI_DEMO=1 dotnet test --filter GuideScreenshotServer` ile örnek sunucuyu açıp `node docs/ekran-goruntuleri.mjs`
  çalıştırın (kurulum ekranı için `DISCI_DEMO=kurulum` ve `node docs/ekran-goruntuleri.mjs kurulum`).

## Testler

```
dotnet test
```

Testler her çalışmada LocalDB'de uydurma verilerle geçici bir veritabanı kurar, sonunda siler ve sahte bir oturum kullanır;
gerçek veriye, hesaba veya parolaya dokunmaz.

## Gizlilik

Bu depoda gerçek hasta, hekim veya klinik verisi bulunmaz ve bulunmamalıdır. Gerçek veriler ve fotoğraflar
yalnızca `data/` klasöründe durur ve `.gitignore` ile depo dışında tutulur. Kişisel veri içeren bir dosyayı asla commit etmeyin.

## Gereksinimler

- .NET 10 SDK
- SQL Server (geliştirmede LocalDB yeterli; Visual Studio ile gelir)

