# Yapay zeka çalışma kaydı

Bu dosya, DentaLineSuit'ten yeni **Disci** sistemine geçiş için şimdiye kadar yapılanları, alınan kararları ve
öğrenilen tuzakları anlatır. Bu projede çalışacak bir yapay zeka asistanı veya geliştirici önce bu dosyayı okumalı.

- Son güncelleme: 2026-11-01
- Hedefler ve istenen modüller: [Yapilacak.md](Yapilacak.md)
- Kurulum ve komutlar: [README.md](README.md)
- Son kullanıcı belgesi: [docs/Kullanim-Kilavuzu.md](docs/Kullanim-Kilavuzu.md)

## Gizlilik kuralı

Bu depo GitHub'da yayınlanabilir durumda tutulur. **Gerçek hasta, hekim veya klinik verisi depoya girmez**:
isimler, telefonlar, tutarlar, fotoğraflar, aktarım raporları, `DB_Aktarim` dışa aktarımları. Bunlar yalnızca
`data/` klasöründe (Git'e girmez) veya proje dışında durur. Testler ve örnekler uydurma veri kullanır.

## Çalışma kuralları

- Kullanıcıyla **her zaman Türkçe** konuşulur.
- Kullanıcı bilgisayarındaki programları kullanmak için izin istemek yeterli; geniş yetki verdi. Yine de geri
  dönüşü zor işlemlerden (silme, sistem ayarı) önce sorulmalı.
- Parolalar ve gizli bilgiler sohbete yazılmaz, koda veya Git'e konmaz.

## Ortam

| Bileşen | Durum |
|---|---|
| İşletim sistemi | Windows 11 Pro, Türkçe |
| .NET | 10 SDK (10.0.403); eski sürümler de kurulu |
| SQL Server | LocalDB `MSSQLLocalDB`; SSMS 22 ve Visual Studio 2026 kurulu |
| Python | `py` (3.10). `python` komutu QGIS'in bozuk Python'una gidiyor, **kullanma** |
| Kabuk | PowerShell 5.1 ve Git Bash |

## 1. DentaLineSuit'i bu bilgisayarda çalıştırma

Program: `C:\Yazılım\DentaLineSuit\DentaLineSuit.exe`. 2000'li yıllarda yazılmış bir Delphi uygulaması (sürüm 2.5.3.21), Paradox 7 veritabanı kullanıyor.

### Bulunan sorunlar ve çözümler

1. **"Gerekli dosya bulunamadı (BDE Problem)"**: Borland Database Engine kurulu değildi.
   - Kaynak: bilgisayardaki eski Delphi kurulum dosyalarının içindeki `Common\Borland Shared\BDE` klasörü
   - Hedef: `C:\Program Files (x86)\Common Files\Borland Shared\BDE`
   - Kayıt defteri: `HKLM\SOFTWARE\WOW6432Node\Borland\Database Engine` (DLLPATH, CONFIGFILE01,
     `Settings\SYSTEM\INIT\LOCAL SHARE=TRUE`, `Settings\DRIVERS\PARADOX\INIT\NET DIR`) ve `...\Borland\BLW32`
2. **Yarım kalmış onarım işareti**: `DL2500DB\TamirActif` dosyası ("Repairing...", 25.09.2026'dan kalma) ve eski kilit dosyası. Silinmedi, adı değiştirildi:
   - `DL2500DB\TamirActif.eski_20260925`
   - `DL2500DB\NET2500\PDOXUSRS.NET.eski_20260925`
3. **NET DIR**: Program kendi `DL2500DB\NET2500` klasörünü kullanıyor. Kayıt defterindeki NET DIR buraya çevrildi:
   `C:\Yazılım\DentaLineSuit\DL2500DB\NET2500`. (İlk kurulumda verilen `C:\BDENET` artık kullanılmıyor.)
4. **"Mesaj No 30: Klasör paylaştırılamadı"**: Program açılışta kendi klasörlerini Windows ağında paylaşıyor
   (`DLDBS<bilgisayar>`, `DLDBNS<bilgisayar>`, `DLSuitEXEPath_<bilgisayar>`, Everyone:Full). Bunun için yönetici
   yetkisi gerekiyor. `HKCU\...\AppCompatFlags\Layers` altında exe için `~ RUNASADMIN` ayarlandı.
5. `dclib.dll` dosyası Visual C++ 2008 x86 çalışma zamanı istiyor; bu bilgisayarda kurulu.

Program kayıt defterine kendi ayarını yazmıyor; tüm durumu kendi klasöründe.

**Yedek** (değişikliklerden önce): `C:\Yazılım\DentaLineSuit\_YEDEK\DL2500DB_20261101_0027` (224 dosya, 26 MB).

### Başka bilgisayara kurulum paketi

`C:\Yazılım\DentaLineSuit\KURULUM\`: `KUR.bat` (kendini yönetici olarak başlatır), `kur.ps1`, `BDE\`, `OKUBENI.txt`.
Betik sırasıyla şunları yapar: program dosyalarını kopyalar (varsayılan hedef `C:\DentaLineSuit`, var olan
veritabanını önce yedekler), VC++ 2008'i kurar, BDE'yi kurup ayarlar, kilit dosyalarını temizler, izinleri verir,
HKLM'ye RUNASADMIN yazar ve masaüstüne kısayol koyar. `-Deneme` anahtarı hiçbir şeyi değiştirmeden çalışır.
Deneme modunda iki senaryo test edildi; gerçek bir ikinci bilgisayarda **henüz denenmedi**.

## 2. Verileri dışarı aktarma

### Veritabanı yapısı (DL2500DB)

35 Paradox tablosu. Kod sayfası tabloya göre 857 veya 1252; "1252" etiketli tablolar aslında Türkçe tutuyor,
**cp1254** ile okunmalı. Ana bağ: `PROTOKOL` = hasta numarası.

| Tablo | Kayıt | İçerik |
|---|---:|---|
| DLHASTA | 5.227 | Hasta kartı (74 alan) |
| DLORALTD / DLOR_DET | 12.081 / 29.915 | Tedavi başlığı / kalemi (diş no FDI) |
| DLODEME / DLODEDET | 9.254 / 12.053 | Ödeme / döviz ayrıntısı (kur) |
| DLANAMNZ | 96 | Anamnez: 179 evet/hayır sütunu + metin |
| DLDISNOT, DLNOTLAR | 885, 6 | Diş notları; RTF hasta notları |
| DLFOTO | 18 | Fotoğraf kayıtları |
| DLTRIFES, DLTDB, DLDIAGNZ | 334, 167, 60 | Tarifeler ve bulgu kodları |
| DLGIDER, DLLABOR | 474, 4 | Giderler ve gider hesapları |
| DLHEKIM, DLSIFRE | 2, 2 | Hekimler; kullanıcılar (şifreler düz metin) |
| RANDEVU, DLRECETE | 0, 0 | Hiç kullanılmamış |

### Kod anlamları (veriden çıkarıldı)

- `DLORALTD.TEDTYPE`: 1 = bulgu/teşhis (DCE, DEN, DPR kodları), 2 = plan, 3 = yapılan tedavi
- `DLODEME.TYPE`: N = nakit, K = kredi kartı. `STATE` O/C: "C"nin anlamı **doğrulanamadı** (7 kayıt)
- Tarife grupları: `T.D.B` = güncel fiyatlar (KDV %8, toplam fiyat dolu), `T.D.B.` = eski TDB taban fiyatları
- `DLLABOR` yalnızca laboratuvar değil, genel gider hesaplarını da tutuyor (kira, ev, inşaat gibi)
- `DLOR_DET.KDV` değeri tutarın içindeki KDV miktarı; 340 kayıtta bozuk (tutardan büyük, milyonlar)
- Boş tarih için Delphi sıfır tarihi `1899-12-30` yazılmış
- `DLRNCINS.RENK` Delphi TColor (0x00BBGGRR); `DLMESAI.ARALIK` = 30 dakika

### Fotoğraflar ve röntgenler

- Dosya adı `DLFOTO.RESIM_NO`'nun onaltılık hali: `000A.jpg` = 10. Program son açılışta dosyaları `IMAGE\0000\` alt klasörüne taşıdı.
- 19 dosya var: 18'i kayıtla eşleşti, `0010.jpg` kayıtsız. `RESIM_CINSI`: C = ağız içi kamera, F = yüz fotoğrafı.
- **Röntgen yok**: C: ve D: disklerinde başka resim klasörü bulunamadı. Röntgenler büyük olasılıkla sensörün bağlı olduğu başka bir bilgisayarda.

### İlk dışa aktarım (Python)

`C:\Yazılım\DentaLineSuit\DB_Aktarim\`: `dentaline.sqlite`, `csv\` (35 dosya, UTF-8 BOM, `;` ayraçlı), `SEMA.md`,
`resimler\` (hasta başına klasör) ve SQLite'ta `HASTA_RESIMLERI` tablosu. Python betikleri oturumun geçici
klasöründeydi ve silinmiş olabilir; artık C# aktarım programı bu işi yapıyor.

## 3. Disci projesi (`C:\Yazılım\Disci`)

### Yapı

| Proje | Görev |
|---|---|
| `src/Disci.Domain` | Entity sınıfları (Common, Patients, Staff, Catalog, Treatments, Finance, Scheduling) |
| `src/Disci.Infrastructure` | `DisciDbContext : IdentityDbContext<ApplicationUser>`, migration'lar, `Identity/ApplicationUser.cs` + `Roles` |
| `tools/Disci.LegacyImport` | `Paradox/ParadoxTable.cs` (BDE'siz okuyucu), `Import/LegacyImporter.cs`, rapor, RTF ve anamnez yardımcıları |
| `src/Disci.Web` | Blazor Web App (Interactive Server), Identity sayfaları, `Services/` |
| `tests/Disci.Web.Tests` | WebApplicationFactory entegrasyon testleri (10 test). Her çalışmada uydurma verili geçici bir LocalDB veritabanı kurup siler; gerçek veriye dokunmaz |

Teknoloji: .NET 10, EF Core 10.0.14 (SQL Server, Code-First), ASP.NET Core Identity (çerez, şema v3/passkey), QuickGrid.

### Veri modeli kararları

- Eski kayıtlarda orijinal anahtar `LegacyId` alanında tutuluyor (filtreli tekil indeks).
- Hasta adı "Ad(lar) SOYAD"; son kelime soyad kabul edildi. Eşlenmeyen dolu alanlar `Patient.LegacyExtra` içinde JSON olarak duruyor; hiçbir veri atılmadı.
- Anamnez sütunları satıra çevrildi (`MedicalCondition` + `PatientMedicalCondition`; yalnızca "evet"ler).
- Kartı silinmiş ama başka tablolarda kaydı kalan hasta numaraları için arşivli yer tutucu hasta oluşturuluyor (bu veride 1 tane).
- Silme kuralı: yalnızca alt kayıtlar (yakın, not, fotoğraf, anamnez, fiyat kalemi, tedavi kalemi, reçete kalemi) üst kayıtla birlikte silinir; diğer bütün ilişkiler `Restrict`. Kural yalnızca Domain tablolarına uygulanıyor, Identity tabloları kendi varsayılanlarında.
- Domain tablolarında metin alanları varsayılan 200 karakter (`OnModelCreating` sonunda); Identity tablolarına dokunulmuyor.
- Para: `decimal(18,2)`; para birimi `TRY`/`USD`/`EUR` (YTL ve TL → TRY).
- Hekim–kullanıcı bağı `ApplicationUser.DentistId` üzerinden.
- Bakiye = yapılan tedaviler (TRY) − iptal edilmemiş ödemeler (TRY). Döviz kayıtları listeleniyor ama çevrilmiyor.

### Aktarım sonucu (doğrulandı)

Bütün tablolarda kayıt sayıları kaynakla birebir aynı. Ödeme toplamı kuruşu kuruşuna, hasta bazında ödeme toplamları
da her hastada aynı. Tedavi toplamındaki birkaç kuruşluk fark, eski verideki küsuratların kuruşa yuvarlanmasından.
Ayrıntılı sayılar ve tutarlar yalnızca yerel rapordadır: `data/reports/aktarim-raporu-*.md` (Git'e girmez).
340 bozuk KDV değeri 0 yazıldı, tutarlar değişmedi.

### Web uygulaması

- Adres: `http://localhost:5111` (`dotnet run --project src/Disci.Web`)
- Roller: `Admin` (Yönetici), `Hekim`, `Asistan`. Sayfalar varsayılan olarak girişe kapalı (`Pages/_Imports.razor` içinde `[Authorize]`).
- Ekranlar: `/` ana sayfa, `/hastalar`, `/hastalar/{id}`, `/yonetim/kullanicilar` (yalnızca Admin), `/files/photos/{id}` (girişe kapalı fotoğraf adresi).
- Kayıt sayfası kaldırıldı. Hatalı parola denemeleri hesabı kilitliyor. Devre dışı bırakılan hesaba süresiz kilit konup güvenlik damgası yenileniyor.
- İlk yönetici `IdentitySeeder` tarafından yalnızca `Seed:AdminEmail` / `Seed:AdminPassword` ayarlarından açılıyor. Bu bilgisayarda bunlar **user-secrets** içinde (`admin@disci.local`, parola rastgele). Yapay zekanın bu parolayı okumasına izin verilmedi; doğrulama bu yüzden test kimliğiyle yapıldı.

## Öğrenilen tuzaklar

- **PowerShell 5.1 betikleri** Türkçe karakter içeriyorsa UTF-8 **BOM'lu** kaydedilmeli; yoksa yollar (`Yazılım`) bozuluyor.
- **Identity şema sürümü** yalnızca `IdentityOptions` ile belirleniyor (`OnModelCreatingVersion*` ve `SchemaVersion` dışarıya kapalı veya etkisiz). Web dışındaki kodlar `DisciDbContext.CreateOptions(...)` kullanmalı; yoksa passkey tablosu olmayan farklı bir model oluşur. Kontrol: `dotnet ef migrations has-pending-model-changes --startup-project src/Disci.Web`.
- **sqlcmd** (yeni go sürümü) `(localdb)\...` adını tanımıyor; `sqllocaldb info MSSQLLocalDB` çıktısındaki `np:\\.\pipe\...` adresiyle bağlanılmalı.
- Blazor, Türkçe karakterleri HTML varlığı olarak yazıyor (`ı` → `&#x131;`); testlerde `WebUtility.HtmlDecode` kullanılıyor.
- `UseStatusCodePagesWithReExecute` ile `/not-found` sayfası için `UseAuthentication/UseAuthorization` açıkça çağrılmalı.
- Aktarımı `--reset` ile çalıştırmak veritabanını, **kullanıcı hesapları dahil**, siler.
- Uygulama içi tarayıcı önizlemesi `launch.json`'u oturum klasöründe (`C:\Yazılım\DentaLineSuit\.claude\launch.json`) arıyor.

## Açık sorular (kullanıcıya sorulacak)

1. 7 ödemedeki "C" durumu iptal mi, senet mi, başka bir şey mi?
2. Eski tedavi kayıtlarında hekim bilgisi yok; iki hekimden birine mi atanacak, boş mu kalacak?
3. Röntgen görüntüleri hangi bilgisayarda? Sensör yazılımı hangisi?
4. Ortodonti muayene kısaltmalarının (AT, SG, GC vb.) anlamları
5. Yeni sistem tek bilgisayarda mı, ağda mı, bulutta mı çalışacak?

## Sıradaki işler

- Hasta ekleme/düzenleme; tedavi ve ödeme girişi
- Randevu takvimi (hekim bazlı, çakışma kontrolü), reçete modülü (asistan hekim adına yazabilir)
- Ağdan erişim (tablet/telefon), gerekirse JWT'li API
- Git deposu (henüz yok; `.gitignore` hazır, `data/` asla depoya girmemeli)
