# Disci

DentaLineSuit'in yerini alacak diş kliniği otomasyonu. Hedef mimari ve modüller [Yapilacak.md](Yapilacak.md) dosyasında.

Bu aşamada hazır olanlar (**Adım 1**):

- Yeni veritabanı modeli (Entity Framework Core, Code-First, MSSQL)
- DentaLineSuit'in Paradox veritabanını doğrudan okuyan C# okuyucu (BDE gerekmez)
- Eski verileri yeni veritabanına aktaran konsol uygulaması ve aktarım raporu

**Adım 2** (başladı):

- ASP.NET Core Blazor web uygulaması (bilgisayar, tablet, telefon tarayıcısı)
- Kullanıcı girişi (ASP.NET Core Identity, çerez) ve roller: Yönetici, Hekim, Asistan
- Ekranlar: **Bugün** (resepsiyon panosu, bekleme salonu), **randevu takvimi** (gün/hafta, çakışma kontrolü),
  hasta listesi/arama, **yeni hasta / düzenleme / arşiv**, hasta kartı (hesap ekstresi, diş şeması, tedavi planı,
  ödeme alma, makbuz, anamnez, notlar, fotoğraflar), **reçete** (hekim adına yazma, alerji uyarısı, şablonlar, A5 çıktı,
  ilaç listesi), **bildirimler ve görevler**, **kasa raporu**, **fiyat listeleri**,
  klinik ayarları, kullanıcı yönetimi

## Proje yapısı

| Klasör | İçerik |
|---|---|
| `src/Disci.Domain` | Entity sınıfları: hasta, anamnez, tedavi, ödeme, gider, randevu, reçete, hekim, katalog |
| `src/Disci.Infrastructure` | `DisciDbContext` (Identity dahil), tablo ayarları, EF migration'ları, `ApplicationUser` ve roller |
| `src/Disci.Web` | Blazor web uygulaması |
| `tools/Disci.LegacyImport` | Paradox okuyucu (`Paradox/`) ve aktarım (`Import/`) |
| `tests/Disci.Web.Tests` | Sayfaların ve erişim kurallarının entegrasyon testleri |
| `data/` | Aktarılan fotoğraflar ve aktarım raporları (Git'e girmez) |

## Web uygulamasını çalıştırma

```
dotnet run --project src/Disci.Web
```

Ardından tarayıcıda `http://localhost:5111` adresini açın. Uygulama açılırken veritabanını günceller ve rolleri oluşturur.

**İlk giriş:** Hiç kullanıcı yoksa ilk yönetici, `Seed:AdminEmail` ve `Seed:AdminPassword` ayarlarından oluşturulur.
Bu bilgisayarda bu ayarlar .NET user-secrets içinde (proje klasöründe değil, Git'e girmez). Görmek için:

```
dotnet user-secrets list --project src/Disci.Web
```

Giriş yaptıktan sonra **Hesabım** sayfasından parolanızı değiştirin; diğer kullanıcıları **Kullanıcılar** sayfasından açın.
Herkese açık "kayıt ol" sayfası yoktur; hesapları yalnızca yönetici açar.

> Dikkat: aktarım programını `--reset` ile çalıştırmak veritabanını sildiği için kullanıcı hesaplarını da siler.
> Uygulama yeniden açıldığında ilk yönetici user-secrets'tan tekrar oluşturulur.

## Klinik bilgisayarına kurulum

Program, sürekli açık kalacak bir klinik bilgisayarında Windows hizmeti olarak çalışır.

1. Geliştirme bilgisayarında paketi oluşturun (testleri çalıştırır, programı .NET gerektirmeyecek şekilde derler):

   ```
   powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1
   ```

2. `dist\Disci-Kurulum-*.zip` dosyasını klinik bilgisayarına kopyalayıp açın ve **KUR.bat**'ı çalıştırın.

Kurulum betiği SQL Server Express'i kurar, programı "Disci Klinik Programı" hizmeti olarak kaydeder, HTTPS sertifikası
oluşturur, güvenlik duvarını ayarlar, eski verileri aktarır, ilk yöneticiyi açar ve gece yedeğini zamanlar.
Ayrıntılar ve sertifikanın tablet/telefonlara kurulumu: [deploy/kurulum/OKUBENI.md](deploy/kurulum/OKUBENI.md).
`KUR.bat -Deneme` hiçbir şeyi değiştirmeden yapılacakları gösterir.

| Betik (hedefte `C:\Disci\betikler`) | Görev |
|---|---|
| `veri-aktar.ps1` | Geçiş günü: yedek alır, verileri ve kullanıcıları silip DentaLineSuit'ten yeniden aktarır |
| `yedekle.ps1` | Veritabanı yedeği + fotoğraf kopyası (gece görevi bunu çalıştırır) |
| `kaldir.ps1` | Hizmeti, güvenlik duvarı kurallarını ve yedek görevini kaldırır; verilere dokunmaz |

## Testler

```
dotnet test
```

Testler her çalışmada LocalDB'de uydurma verilerle geçici bir veritabanı kurar, sonunda siler ve sahte bir oturum kullanır;
gerçek veriye, hesaba veya parolaya dokunmaz.

## Gizlilik

Bu depoda gerçek hasta, hekim veya klinik verisi bulunmaz ve bulunmamalıdır. Aktarılan veriler, fotoğraflar ve aktarım
raporları `data/` klasöründe durur ve `.gitignore` ile depo dışında tutulur. Kişisel veri içeren bir dosyayı asla commit etmeyin.

## Gereksinimler

- .NET 10 SDK
- SQL Server (geliştirmede LocalDB yeterli; Visual Studio ile gelir)

## Eski verileri aktarma

DentaLineSuit'i kapatın, ardından proje klasöründe:

```
dotnet run --project tools/Disci.LegacyImport -- --reset
```

Seçenekler:

| Seçenek | Varsayılan | Açıklama |
|---|---|---|
| `--source <klasör>` | `C:\Yazılım\DentaLineSuit\DL2500DB` | Eski veritabanı klasörü |
| `--connection <metin>` | LocalDB, `Disci` veritabanı | Hedef SQL Server bağlantısı (`DISCI_DB` ortam değişkeni de olur) |
| `--files <klasör>` | `C:\Yazılım\Disci\data\files` | Fotoğrafların kopyalanacağı yer |
| `--reset` | | Hedef veritabanını silip baştan kurar |
| `--inspect` | | Hiçbir şey yazmaz; eski tabloların kayıt sayılarını gösterir |
| `--dl2001 [klasör]` | `C:\Yazılım\DL2001\DB` | DentaLine 2001 (2.3) kayıtlarını DentaLineSuit aktarımının **üstüne ekler** (aşağıya bakın) |
| `--isim-duzelt` | | Önceden aktarılmış hasta adlarına Türkçe harfleri geri koyar (aktarım bunu artık kendisi yapıyor) |
| `--dry-run` | | `--dl2001` / `--isim-duzelt` ile: her şeyi dener, raporu yazar, veritabanına kaydetmez |

**Türkçe harfler** (`Import/TurkishNames.cs`): eski programlarda adlar Türkçe harfsiz yazılmış ("Ayse", "CELIK"),
DentaLine de yazılan "IBRAHIM"ı Türkçe kuralla küçültüp "Ibrahım" yapmış. Aktarım, listedeki yaygın ad ve soyadları
doğru yazımına çevirir ("Ayşe", "İbrahim", "ÇELİK"; listede olmayan soyadlarda yalnızca "-oğlu" eki). Listede olmayan
ya da sade hali iki ada uyan kelimeler ("Gulsen": Gülsen/Gülşen) olduğu gibi kalır. İçinde zaten ç, ş, ğ, ü, ö veya İ
olan kelimeye dokunulmaz.

Aktarım tek bir işlem (transaction) içinde çalışır: hata olursa hiçbir şey yazılmaz. Eski veritabanı yalnızca okunur.
Her çalıştırmada `data/reports/` altına kaynak/hedef sayılarını, tutar toplamlarını ve uyarıları içeren bir rapor yazılır.

Yeni sisteme geçiş günü: DentaLineSuit'e son kaydı girdikten sonra komutu `--reset` ile bir kez daha çalıştırın.

### DentaLine 2001 kayıtlarını ekleme

Klinik 2020'den beri DentaLine 2001'i (2.3) de kullanmış; onun numaraları 1'den yeniden başlar (oradaki 5 numara
DentaLineSuit'teki 5 numara değildir). DentaLineSuit aktarımından sonra:

```
dotnet run --project tools/Disci.LegacyImport -- --dl2001 --dry-run   # deneme: rapor yazar, kaydetmez
dotnet run --project tools/Disci.LegacyImport -- --dl2001
```

- **Hasta eşleştirme** (`Import/PatientMatcher.cs`): ad Türkçe harfsiz ve büyük harfe çevrilerek karşılaştırılır.
  Aynı kişi sayılır: aynı ad + ortak telefon, aynı ad + aynı (gerçek) doğum tarihi, ya da tek harf farklı ad + ortak telefon.
  01.01 doğum tarihleri yalnızca yıl sayılır (DentaLine bilinmeyen tarihe 01.01.1980 yazıyor). Aynı kişi ise DL2001
  kaydı mevcut hastaya eklenir: boş alanlar doldurulur, farklı telefon/adreste DL2001'deki (yeni olan) geçer, eskisi notlara yazılır.
  Telefon tutmasa da **aynı adlı tek hasta** varsa ve doğum tarihi uyuyorsa (bilinmiyor, aynı yıl, en çok 2 yıl fark
  ya da gün/ay aynı ve yıl en çok 5 farklı) yine birleştirilir. DL2001'e iki kez girilmiş kişi de tek karta iner; ek
  numaraları `PatientLegacyNumbers` tablosunda, aranabilir. Doğum tarihleri çok farklı olanlar (ör. dedesinin adını
  taşıyan torun) ve aynı adlı birden fazla uygun hasta olanlar **ayrı** kalır; raporda "Ayrı bırakılan" listesine yazılır.
- **Mükerrer kayıt**: eşleşen hastada aynı gün + aynı işlem + aynı diş + aynı tutarlı tedavi kalemi, aynı gün + aynı
  tutarlı ödeme, aynı diş notu zaten varsa atlanır.
- DL2001 hastasının protokol no'su `Patients.Dl2001Protocol`'de; ekranlarda "#123 · DL2001 #45" görünür ve aranabilir.
  Diğer kayıtlarda eski anahtar `LegacyId` + `LegacySource = 'DL2001'`. Komut tekrar çalıştırılırsa yalnızca yeni kayıtları ekler.
- Fiyatlar "DentaLine 2001 tarifesi" adlı ayrı bir liste olarak eklenir (varsayılan yapılmaz).

## Eski tablo → yeni tablo eşlemesi

| DentaLineSuit | Disci | Not |
|---|---|---|
| DLHASTA | Patients, PatientRelatives | Ad/soyad ayrıldı (son kelime soyad). Karşılığı olmayan dolu alanlar `LegacyExtra` (JSON) içinde |
| DLANAMNZ | Anamneses, MedicalConditions, PatientMedicalConditions | 179 evet/hayır sütunu satıra çevrildi; yalnızca "evet"ler yazıldı |
| DLDISNOT | ToothNotes | Boş notlar atlandı |
| DLNOTLAR | PatientNotes | RTF düz metne çevrildi, orijinali `LegacyRtf` içinde |
| DLFOTO + IMAGE\ | PatientPhotos | Dosyalar `data/files/patients/<id>/photos/` altına kopyalandı |
| DLORALTD | TreatmentSessions | TEDTYPE: 1 = bulgu, 2 = plan, 3 = yapılan tedavi |
| DLOR_DET | TreatmentItems | Diş no FDI (11-48 kalıcı, 51-85 süt) |
| DLTRIFES, DLTDB, DLDIAGNZ | Procedures, PriceLists, PriceListItems | "T.D.B" = güncel tarife (varsayılan), "T.D.B." = eski tarife |
| DLODEME + DLODEDET | Payments | N = nakit, K = kredi kartı; kur DLODEDET'ten |
| DLLABOR, DLGIDER | ExpenseAccounts, Expenses | |
| DLHEKIM | Dentists | |
| DLILAC | Medications | |
| DLRNCINS (+ DLMESAI aralığı) | AppointmentTypes | Renkler Delphi BGR → `#RRGGBB` |
| RANDEVU, DLRECETE, DLRECENO | Appointments, Prescriptions | Eski sistemde kayıt yok; eşleme hazır |
| DLSIFRE | — | Aktarılmaz: şifreler düz metin. Kullanıcılar yeni sistemde yeniden açılacak |

Ekran ayarı, SMS, lisans gibi programa özgü tablolar (DLSYSTEM, DLSMSSYTEM, DLMSG*, DLCOMPUTERS, DLXY, DLUNIT, DLTASNIF, DLDOVIZ, DLPKLISTE) aktarılmaz.

## Açık sorular

- **Ödeme durumu "C":** 7 ödemede var (çoğu aynı hasta, aylık 250 TL). İptal mi, senet/kapandı mı bilinmiyor; şimdilik iptal sayılmadı, `LegacyStatus` alanında duruyor.
- **Tedavi kayıtlarında hekim yok:** Eski sistem işlemi hangi hekimin yaptığını tutmamış; aktarılan kayıtlarda `DentistId` boş.
- **TBDR ve BIT alanları:** `BIT` diş yüzeyi bit maskesi olarak `SurfaceMask`'e, anlamı bilinmeyen `TBDR` ise `LegacyTbdr`'ye olduğu gibi aktarıldı.
- **Ortodonti muayene başlıkları:** Bazı kısaltmaların (AT, SG, GC) anlamı bilinmiyor; adları sonradan düzenlenebilir.
