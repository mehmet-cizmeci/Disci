# Yapay zeka çalışma kaydı

Bu dosya, DentaLineSuit'ten yeni **Disci** sistemine geçiş için şimdiye kadar yapılanları, alınan kararları ve
öğrenilen tuzakları anlatır. Bu projede çalışacak bir yapay zeka asistanı veya geliştirici önce bu dosyayı okumalı.

- Son güncelleme: 2026-11-02
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
- Ekranlar (2026-11-02 yeniden tasarım; örnek alınanlar: Dentally, Software of Excellence, Carestream Sensei, dentsoft.com.tr):
  - `/` **Bugün**: resepsiyon panosu. Günün randevuları tek tıkla durum akışıyla (Randevulu → Teyitli → Geldi → Koltukta → Tamamlandı; ayrıca Gelmedi/İptal), bekleme salonu sayacı, borçlu hasta etiketi, yapılacaklar özeti
  - `/randevular`: gün (hekim sütunları) ve hafta görünümü, boş saate tıklayınca randevu, çakışma kontrolü (aynı hekim veya aynı hasta), kayıtsız (misafir) hasta, WhatsApp hatırlatma bağlantısı. Hekim rolü kendi sütunuyla açılır. `?hasta=ID` ile hasta seçili açılır
  - `/hastalar` (Aktif/Borçlu/Arşiv/Tümü süzgeci, kalan borç sütunu), `/hastalar/yeni`, `/hastalar/{id}/duzenle` (benzer kayıt uyarısı, anamnez uyarıları, tarife, sabit indirim)
  - `/hastalar/{id}`: sağlık uyarıları (kırmızı), hesap özeti (toplam ücret / alınan / kalan + ilerleme çubuğu), sekmeler: Hesap (borç-ödeme ekstresi, yürüyen bakiye, makbuz, ödeme iptali), Tedaviler (FDI diş şeması, plan → "yapıldı say", plandan çıkar), Randevular, Bilgiler, Anamnez, Notlar (not ekleme), Fotoğraflar. Arşive kaldır/çıkar
  - `/bildirimler`: hesaplanan bildirimler (bekleyen, geciken, yarın teyit edilecek, bugün gelen borçlu, görevler, doğum günü, 6 ay kontrol/recall) + görev (Reminder) ekleme/tamamlama. Üst çubuktaki zil aynı listeden "acil" olanları sayar
  - `/kasa` (Admin, Hekim): tarih aralığı, tahsilat/tedavi/gider/net, ödeme şekline ve hekime göre dağılım, günlük döküm, en yüksek alacaklar
  - `/fiyatlar` (Admin, Hekim): liste seçimi, hücrede fiyat düzenleme (KDV dahil girilir, net ve KDV payı listenin oranından), toplu % güncelleme (tam liraya yuvarlar), liste kopyalama, varsayılan liste, yeni işlem
  - `/ayarlar` (Admin): hekimler (renk, mesai saatleri, etkin) ve randevu türleri (süre, renk)
  - Reçete (2026-11-02): hasta kartında "Reçete yaz" (`Shared/PrescriptionEditor.razor`), "Reçeteler" sekmesi (yazdır, aynısını yeniden yaz, düzenle; silme yalnızca Admin/Hekim), `/receteler` (yazılan reçeteler: tarih/hekim/arama süzgeci, "yazan" ve "hekim adına" etiketi; İlaç listesi sekmesinde katalog yönetimi), `/recete/{id}` yazdırma sayfası (A5, `?yazdir=1` yüklenince yazdırma penceresini açar)
  - `/makbuz/{odemeId}`: yazdırılabilir tahsilat makbuzu, tutar yazıyla. Başlık `Clinic:Name/Address/Phone` ayarlarından
  - `/yonetim/kullanicilar` (Admin), `/files/photos/{id}`
- Tasarım: Bootstrap ızgarası üstüne kendi tasarım dili (`wwwroot/app.css`, renk değişkenleri `:root`'ta, durum renkleri `.st-*`). İkonlar `Shared/Icon.razor` içinde satır içi SVG (klinik ağı internetsiz olabilir diye CDN yok). Telefon/tablette menü CSS'li çekmece.
- Ortak parçalar: `Services/ClinicServices.cs` (`ClinicEvents` anlık yenileme yayını, `CurrentUser`, `Ledger` bakiye sorguları, `Scheduling` çakışma ve durum kuralları), `Services/Notifications.cs`, `Services/PatientSearch.cs`; bileşenler `Shared/` (Modal, Kpi, StatusBadge, PatientPicker, ToothChart, AppointmentEditor, TreatmentEditor, PaymentEditor, Toast).
- Anlık yenileme: bir bilgisayarda hasta "Geldi" yapılınca/ödeme alınınca `ClinicEvents.NotifyAsync()` bütün açık ekranları (pano, takvim, zil, hasta kartı) yeniler. Zil ve pano ayrıca dakikada bir kendini yeniler (gecikme/bekleme süreleri).
- Yetki: Asistan randevu, hasta kaydı, tedavi/ödeme girişi yapabilir; ödeme iptali, kasa ve fiyat listeleri yalnızca Admin/Hekim; ayarlar yalnızca Admin. Ödeme silinmez, iptal edilir (üstü çizili).
- Reçete kuralları: reçete her zaman bir hekim adına düzenlenir (`DentistId` zorunlu); yazan kullanıcı `CreatedByUserId`'de. Asistanın kendi hekim kaydı olmadığından pencerede "hekim adına" notu çıkar. Kalemler ilaç listesinden (`MedicationId` dolu) ya da serbest metinle eklenir; serbest ilaç istenirse kataloğa da eklenir. Eski sistemden 27 ilaçlık katalog (DLILAC) gelmişti, reçete yoktu.
- Alerji güvenlik ağı: `Services/Prescriptions.cs` → `PrescriptionSafety.AllergyWarnings` hastanın ilaç alerjisi metnini ilaç adıyla karşılaştırır (penisilin, sefalosporin, NSAID, aspirin, metamizol, klindamisin, metronidazol, lokal anestezik ve sülfa aileleri + birebir kelime). Bir ilaç veritabanı değildir; yalnızca uyarır, kaydı engellemez. "Yok", "Hayır" gibi değerler alerji sayılmaz (`IsAllergy`); hasta kartı başlığındaki alerji etiketleri de bunu kullanır.
- E-Reçete (Medula) entegrasyonu yok; reçete kâğıda yazdırılır.
- Reçete şablonları (2026-11-02): `PrescriptionTemplate` + `PrescriptionTemplateItem` (migration `PrescriptionTemplates`; kalem şablonla birlikte silinir, ilaç bağı Restrict). Reçete penceresinde "Şablondan ekle" düğmeleri (aynı adlı ilacı ikinci kez eklemez, tanı/not boşsa doldurur; hastanın alerjisine uyan ilaç içeren şablon kırmızı görünür) ve "Şablon olarak kaydet". `/receteler?sekme=sablon`: kartlar, sıralama (öne/sona al), düzenleme, pasif, silme. Şablonları yalnızca Admin/Hekim düzenler; asistan kullanır. Örnek şablonlar (`PrescriptionTemplates.Examples`, genel ilaç adları, yetişkin dozları) kendiliğinden yüklenmez; liste boşken "Örnek şablonları ekle" ile, onaydan sonra eklenir ve aynı adlı katalog ilacına bağlanır. İlaç satırı düzenleyicisi reçete ve şablonda ortak: `Shared/MedicationLines.razor` (`RxLine`).
- Plan → yapıldı: seçilen plan kalemleri bugünün tarihli yeni bir "Yapıldı" seansına taşınır (kalem kimliği korunur), boş kalan plan seansı silinir (eski sistemden gelenler hariç).
- Migration `ClinicWorkflow`: `Appointments.ArrivedAt`, `Reminders` tablosu; `AppointmentStatus.InChair = 7` (int, şema değişmez). Uygulama açılışta kendisi uygular.
- Kayıt sayfası kaldırıldı. Hatalı parola denemeleri hesabı kilitliyor. Devre dışı bırakılan hesaba süresiz kilit konup güvenlik damgası yenileniyor.
- İlk yönetici `IdentitySeeder` tarafından yalnızca `Seed:AdminEmail` / `Seed:AdminPassword` ayarlarından açılıyor. Bu bilgisayarda bunlar **user-secrets** içinde (`admin@disci.local`, parola rastgele). Yapay zekanın bu parolayı okumasına izin verilmedi; doğrulama bu yüzden test kimliğiyle yapıldı.

### Klinik bilgisayarına kurulum (`deploy/`)

Kullanıcının bu bilgisayarı yalnızca geliştirme içindir; program başka bir (klinik) bilgisayarda sürekli çalışacak.

- `deploy/paket-olustur.ps1`: testleri çalıştırır, web uygulamasını ve aktarım aracını **self-contained win-x64** yayınlar
  (hedefe .NET kurulmaz), `deploy/kurulum/*` ile birlikte `dist/Disci-Kurulum-*.zip` yapar (~107 MB, Git'e girmez).
- `deploy/kurulum/kur.ps1` (KUR.bat ile yönetici olarak): SQL Server Express (winget, önce 2025 sonra 2022, örnek `.\SQLEXPRESS`),
  `C:\Disci\app` + `import` + `betikler`, `C:\DisciVeri\{files,keys,Yedekler,sertifika,reports}`, Windows hizmeti `Disci`
  (hesap `NT SERVICE\Disci`, gecikmeli otomatik, SQL'e bağımlı, hata olursa yeniden başlar), kendinden imzalı sertifika
  (LocalMachine\My, FriendlyName "Disci", bilgisayar adı + IP'ler), `appsettings.Production.json` (Kestrel HTTP 5111 / HTTPS 5443),
  SQL yetkileri (hizmet hesabı yalnızca `Disci` db_owner; SYSTEM db_backupoperator), güvenlik duvarı (Özel/Etki alanı),
  ilk yönetici (hizmetin kayıt defterindeki geçici `Environment` değişkenleriyle, sonra silinir), gece yedeği görevi (23:30, SYSTEM).
- `-Deneme` her betikte değişiklik yapmadan çalışır. Tamamlanan adımlar tekrar çalıştırılınca zarar vermez (güncelleme de aynı betikle).
- Program tarafı: `UseWindowsService`, yayınlanmış kopyada içerik kökü exe klasörü (hizmet System32'de başlar),
  `DataProtection:KeysPath` ile oturum anahtarları diske (DPAPI-makine korumalı).

**Test durumu:** Paket oluşturma, yayınlanan programın System32'den Production modunda çalışması (sayfalar, CSS/JS, erişim kuralları),
betiklerin sözdizimi, `kur.ps1 -Deneme` ve SQL işlevleri/yedek alma (LocalDB'ye karşı) denendi. **Gerçek bir hedef bilgisayarda
tam kurulum henüz yapılmadı**: winget ile SQL Express kurulumu, hizmet hesabı, LocalMachine sertifikası ve HTTPS uç noktası orada ilk kez çalışacak.

## Öğrenilen tuzaklar

- **PowerShell 5.1 betikleri** Türkçe karakter içeriyorsa UTF-8 **BOM'lu** kaydedilmeli; yoksa yollar (`Yazılım`) bozuluyor.
- **Identity şema sürümü** yalnızca `IdentityOptions` ile belirleniyor (`OnModelCreatingVersion*` ve `SchemaVersion` dışarıya kapalı veya etkisiz). Web dışındaki kodlar `DisciDbContext.CreateOptions(...)` kullanmalı; yoksa passkey tablosu olmayan farklı bir model oluşur. Kontrol: `dotnet ef migrations has-pending-model-changes --startup-project src/Disci.Web`.
- **sqlcmd** (yeni go sürümü) `(localdb)\...` adını tanımıyor; `sqllocaldb info MSSQLLocalDB` çıktısındaki `np:\\.\pipe\...` adresiyle bağlanılmalı.
- Blazor, Türkçe karakterleri HTML varlığı olarak yazıyor (`ı` → `&#x131;`); testlerde `WebUtility.HtmlDecode` kullanılıyor.
- `UseStatusCodePagesWithReExecute` ile `/not-found` sayfası için `UseAuthentication/UseAuthorization` açıkça çağrılmalı.
- Aktarımı `--reset` ile çalıştırmak veritabanını, **kullanıcı hesapları dahil**, siler.
- Uygulama içi tarayıcı önizlemesi `launch.json`'u oturum klasöründe (`C:\Yazılım\DentaLineSuit\.claude\launch.json`) arıyor.
- Yayınlanmış program başka bir klasörden başlatılırsa (ör. hizmet, System32) içerik kökü yanlış olur ve CSS/JS **200 ama 0 bayt** döner; `Program.cs` bunu içerik kökünü exe klasörüne sabitleyerek çözüyor.
- 2026-11-02: Hasta listesinde arama, sıralama ve (ardından) hasta kartı sekmeleri çalışmıyordu. Neden: QuickGrid tek, uzun ömürlü bir DbContext'e bağlıydı; her harfte yeni sorgu öncekiyle çakışınca "A second operation was started on this context instance" hatası Blazor bağlantısını (circuit) sonlandırıyordu. Çözüm: `ItemsProvider` ile her istekte `IDbContextFactory`'den yeni bağlam; eski istekler iptal belirteciyle düşüyor. Geliştirmede `DetailedErrors` açık. Tarayıcıda denemek için `BrowserTestServer` yardımcısı eklendi.
- 2026-11-02: LocalDB 17.0'a güncellenince `MSSQLLocalDB` örneği `Disci` veritabanını unuttu; açılışta "Cannot create file ...Disci.mdf because it already exists" hatası çıktı. Dosyalar sağlamdı; yedeklenip (`data/db-yedek-20261102`) `CREATE DATABASE [Disci] ON (FILENAME=...mdf),(FILENAME=...ldf) FOR ATTACH` ile geri bağlandı. Dosyaları silmeyin.
- PowerShell 5.1'de `Add-Content`/`Set-Content` varsayılan olarak ANSI yazar; Türkçe içerikli dosyalara `[IO.File]::WriteAllText(..., UTF8Encoding)` ile yazın.
- Yapay zekanın programı ağa açması (0.0.0.0'a bağlama) ve gizli parolayı okuması izin sınıflandırıcısı tarafından engellendi; bu işler kullanıcıya bırakılır.
- Razor'da satır içi stile `double` yazarken Türkçe kültür ondalık virgül koyar (`top:12,5px` geçersiz CSS, öğe en üste kayar). `Calendar.razor`'daki `Px()` gibi değişmez kültürle biçimleyin.
- Etkileşimli sayfada `NavigationManager.NavigateTo` önceden işleme (prerender) sırasında çağrılırsa yönlendirmeye dönüşür; `RendererInfo.IsInteractive` ile koruyun.
- EF, kurucuyla oluşturulan (positional record) projeksiyonların alanlarına sonradan `Where`/`OrderBy` uygulayamaz; `Ledger.Balances` bu yüzden üye atamalı sınıf (`PatientBalance`) döndürüyor ve sorgularda `Charged - Paid` kullanılıyor.
- `type="time"` girdisi `TimeOnly` bağlamada saniyeyi de gösterir; `@bind:format="HH:mm"` verin.
- Türkçe küçük harfe çevirme "NSAID"i "nsaıd" yapar (noktasız ı); metin karşılaştırmalarında ı ve i'yi eşitleyin (`PrescriptionSafety.Fold`).
- CSS adlandırılmış sayfa (`@page rx { size: A5 }`) yalnızca öğeye verilirse, ardından gelen öğeler varsayılan boyutta ikinci bir sayfaya taşar; `body:has(.rx-sheet) { page: rx }` ile bütün belgeye verin.
- Alt bileşen, üst bileşenden aldığı listeyi değiştirirse üst bileşen kendiliğinden yeniden çizilmez (ör. "Şablon olarak kaydet" düğmesi pasif kalıyordu); değişiklikten sonra bir `EventCallback` (`MedicationLines.OnChanged`) çağırın.
- Tarayıcı otomasyon aracı yoksa ekran görüntüsü için: `BrowserTestServer`'ı çalıştırıp Chrome'u `--headless=new --remote-debugging-port` ile açan küçük bir Node (yerleşik WebSocket, CDP) betiği yeterli; `test-role` çerezi `Network.setCookie` ile verilir. Sunucuyu zorla kapatmak `Disci_Tests_*` veritabanını arkada bırakır, elle silin.

## Açık sorular (kullanıcıya sorulacak)

1. 7 ödemedeki "C" durumu iptal mi, senet mi, başka bir şey mi?
2. Eski tedavi kayıtlarında hekim bilgisi yok; iki hekimden birine mi atanacak, boş mu kalacak?
3. Röntgen görüntüleri hangi bilgisayarda? Sensör yazılımı hangisi?
4. Ortodonti muayene kısaltmalarının (AT, SG, GC vb.) anlamları
5. Yeni sistem tek bilgisayarda mı, ağda mı, bulutta mı çalışacak?

## Sıradaki işler

- ~~Hasta ekleme/düzenleme; tedavi ve ödeme girişi; randevu takvimi; bildirimler~~ (2026-11-02 yapıldı)
- Gerçek veriyle performans kontrolü (5.227 hasta): hasta listesindeki bakiye sütunu ve "Toplam alacak" alt sorgularla hesaplanıyor
- ~~Reçete modülü~~ (2026-11-02 yapıldı); E-Reçete entegrasyonu; ~~reçete şablonları~~ (2026-11-02 yapıldı)
- Taksit/senet planı; gider girişi ekranı
- SMS / WhatsApp Business ile otomatik hatırlatma (şimdilik yalnızca tek tıkla açılan wa.me bağlantısı var; mesajı kişi gönderir)
- Klinik adı/adresi için ayar ekranı (şimdilik `appsettings.json` → `Clinic`)
- Ağdan erişim (tablet/telefon), gerekirse JWT'li API
- Git deposu (henüz yok; `.gitignore` hazır, `data/` asla depoya girmemeli)
