# Yapay zeka çalışma kaydı

Bu dosya, DentaLineSuit'ten yeni **Disci** sistemine geçiş için şimdiye kadar yapılanları, alınan kararları ve
öğrenilen tuzakları anlatır. Bu projede çalışacak bir yapay zeka asistanı veya geliştirici önce bu dosyayı okumalı.

- Son güncelleme: 2026-11-04
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
| `tools/Disci.LegacyImport` | `Paradox/ParadoxTable.cs` (BDE'siz okuyucu), `Import/LegacyImporter.cs`, `Import/Dl2001Merger.cs` + `PatientMatcher.cs` (DL2001 ekleme), rapor, RTF ve anamnez yardımcıları |
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
  - `/hastalar/{id}`: sağlık uyarıları (kırmızı), hesap özeti (toplam ücret / alınan / kalan + ilerleme çubuğu), sekmeler: Hesap (borç-ödeme ekstresi, yürüyen bakiye, makbuz, ödeme iptali), Tedaviler (FDI diş şeması — 2026-11-03'ten beri ağız kavsi şeklinde SVG: dişler gerçek genişliklerine oranlı ve tipine göre biçimli, yay uzunluğuna göre dizilir, süt dişleri iç kavste; hastanın sağı ekranın solunda — plan → "yapıldı say", plandan çıkar, yapılan tedavi/bulgu silme: ücretli kalemi yalnızca Admin/Hekim), Notlar (ekleme ve silme), Randevular, Bilgiler, Anamnez, Notlar (not ekleme), Fotoğraflar. Arşive kaldır/çıkar
  - `/bildirimler`: hesaplanan bildirimler (bekleyen, geciken, yarın teyit edilecek, bugün gelen borçlu, görevler, doğum günü, 6 ay kontrol/recall) + görev (Reminder) ekleme/tamamlama. Üst çubuktaki zil aynı listeden "acil" olanları sayar
  - `/kasa` (Admin, Hekim): tarih aralığı, tahsilat/tedavi/gider/net, ödeme şekline ve hekime göre dağılım, günlük döküm, en yüksek alacaklar
  - `/fiyatlar` (Admin, Hekim): işlem adı/kod/kategori düzenleme ve listeden kaldırma (pasif; "Kaldırılanları göster" ile geri alınır), liste seçimi, hücrede fiyat düzenleme (KDV dahil girilir, net ve KDV payı listenin oranından), toplu % güncelleme (tam liraya yuvarlar), liste kopyalama, varsayılan liste, yeni işlem
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

## 4. DentaLine 2001 (2.3) kayıtları (2026-11-04)

Kullanıcıya göre DentaLineSuit sorun çıkarınca hekim `C:\Yazılım\DL2001` klasöründeki DentaLine 2001 programına
(`PRG\DentaLine2001.exe`, veri `DB\`, aynı Paradox biçimi) kayıt girmiş. **Veri bunun daha eskiye gittiğini gösteriyor:**
DL2001 veritabanı Temmuz 2020'de boş başlatılmış ve o tarihten beri asıl program olarak kullanılmış. DentaLineSuit'e
2020'den sonra çok az kayıt girilmiş (son yeni hasta Temmuz 2024'te). İki program bir süre birlikte kullanılmış.
DL2001'de giriş için kullanıcı adı ve şifre gerekmedi; dosyalar doğrudan okundu.

- Tablolar DentaLineSuit ile aynı. Farklar: `DLHASTA`'da `KIMLIK_NO` yok, `FOTO` var (boş); `DLODEDET`'te `MU_KUR` yok;
  `DLDISNOT.NOT` memo değil, 255 karakterlik metin. Anamnez, hasta notu, gider, randevu ve reçete tabloları boş.
- **Numaralar 1'den yeniden başlıyor**: DL2001 hastalarının protokol numaraları DentaLineSuit'teki numaralarla çakışıyor
  ama aynı numaradaki hastalar başka kişiler. Tedavi ve ödeme numaraları da çakışıyor.
- Doğum tarihi her hastada dolu, ama çoğu tahmini: 01.01.1980 varsayılanı ve "01.01.yıl" biçiminde yalnızca yıl.
- Hasta grubu `Banka1` (236 hasta): DL2001'de bu adla bir tarife yok. Fiyat listesine bağlanmadı, `LegacyExtra`'da saklanıyor.
- Ödeme türü `H` (2 kayıt): havale sayıldı (`BankTransfer`). Doğrulanmadı.

**Aktarım** (`--dl2001`, ayrıntılar README'de): `Import/Dl2001Merger.cs`, `Import/PatientMatcher.cs`. Şema
(migration `Dl2001Import`): `Patients.Dl2001Protocol` (tekil), tedavi seansı/kalemi, ödeme ve fotoğrafta `LegacySource`
(boş = DentaLineSuit, `DL2001`) ve `(LegacyId, LegacySource)` tekil indeksi. DentaLineSuit aktarımındaki eşleme kodu
`LegacyImporter` içindeki `internal static` fabrikalara (`NewPatient`, `NewSession`, `NewItem`, `NewPayment`) taşındı.
DentaLineSuit aktarımı bu değişiklikten sonra eski raporla birebir aynı sayı ve tutarları verdi.

**2026-11-04 sonucu** (geliştirme bilgisayarındaki `Disci` veritabanı; önceki yedek `data/db-yedek-20261104-dl2001-oncesi.bak`):
Kullanıcı "hastaları otomatik birleştir" dedi. Kurallar genişletildi: aynı adlı tek hasta varsa ve doğum tarihi uyumluysa
o da birleşiyor (ayrıntı README'de). Yedek geri yüklenip aktarım baştan yapıldı. 1.376 DL2001 hastasından 242'si birleşti
(3'ü DL2001'e iki kez girilmiş kişiler; ek numaralar `PatientLegacyNumbers`'ta), 1.134'ü yeni açıldı. 24 hasta aynı adlı
ama doğum tarihi çok farklı olduğu için ayrı bırakıldı (ör. 2002 doğumlu biri ile 1998'de tedavi görmüş 1944 doğumlu biri).
2.786 seans, 7.268 tedavi kalemi, 3.369 ödeme, 180 diş notu ve 5 fotoğraf eklendi. İki programa birden girilmiş 2 tedavi
kalemi ve 5 ödeme atlandı. Kaynak toplamı = eklenen + atlanan, kuruşu kuruşuna tuttu. İkinci çalıştırma hiçbir şey eklemiyor.
Ayrıntılı liste (hasta adlarıyla): `data/reports/dl2001-aktarim-*.md`.

**Geçiş günü:** DL2001 hâlâ kullanılıyor. `--dl2001` yeniden çalıştırılınca yalnızca **yeni** kayıtları ekler.
DL2001'de sonradan **düzeltilen veya silinen** kayıtlar Disci'ye yansımaz. Kesin geçişte `veri-aktar.ps1 -EskiVeri ... -Dl2001Veri ...` ile ikisi birlikte baştan aktarılmalı.

## 5. Adlarda Türkçe harfler (2026-11-04)

Kullanıcı, adlarda Türkçe harflerin neden görünmediğini sordu. Eski iki programda da adlar Türkçe harfsiz kaydedilmiş
(soyadlarda Ç/Ş/Ğ/Ü/Ö 6.600 hastada toplam 31 kez). DentaLine ayrıca adı Türkçe kuralla küçültmüş: yazılan "IBRAHIM"
"Ibrahım", "ALI" "Alı" olmuş. Kullanıcı düzeltmeyi istedi.

- `Import/TurkishNames.cs`: yaygın Türkçe ad ve soyadların doğru yazımı (sade hali anahtar). Sade hali iki farklı ada uyan
  kelime (Gulsen → Gülsen/Gülşen) değiştirilmez. Listede olmayan soyadında yalnızca "-oğlu" eki düzeltilir. İçinde ç, ş,
  ğ, ü, ö veya İ olan kelimeye dokunulmaz (elle düzeltilmiş adlar korunur). Soyadlar eskisi gibi büyük harf: "ÇELİK".
- Aktarım (`NewPatient`, hasta yakınları) bunu kendisi uyguluyor. Mevcut veritabanı için `--isim-duzelt [--dry-run]`.
- Sonuç: 6.362 hastadan 4.293'ünün, 632 hasta yakınının adı düzeldi. Önceki yedek `data/db-yedek-20261104-isim-oncesi.bak`.
  İlk uygulamada belirsiz kelimeler öteki listeden düzeltiliyordu (7 hasta). Hata giderildi, yedek geri yüklenip yeniden uygulandı.
- Rapor (eski → yeni, hasta adlarıyla) `data/reports/isim-duzeltme-*.md`. Raporun sonunda listede olmadığı için
  değişmeyen sık kelimeler var; sözlüğü genişletmek için kullanılabilir.

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
- `type="time"` girdisine **`@bind:format` vermeyin**: Blazor betiği değeri saniyeyle gönderir (`11:15` → `11:15:00`), `HH:mm` biçimi bunu çözemez ve değişiklik sessizce atılır (2026-11-03: randevu saati ve hekim mesai saati bu yüzden değişmiyordu). Biçimsiz bağlamada `:00` ekranda zaten gizlenir. `MarkupRuleTests` bunu denetler.
- Türkçe küçük harfe çevirme "NSAID"i "nsaıd" yapar (noktasız ı); metin karşılaştırmalarında ı ve i'yi eşitleyin (`PrescriptionSafety.Fold`).
- CSS adlandırılmış sayfa (`@page rx { size: A5 }`) yalnızca öğeye verilirse, ardından gelen öğeler varsayılan boyutta ikinci bir sayfaya taşar; `body:has(.rx-sheet) { page: rx }` ile bütün belgeye verin.
- Alt bileşen, üst bileşenden aldığı listeyi değiştirirse üst bileşen kendiliğinden yeniden çizilmez (ör. "Şablon olarak kaydet" düğmesi pasif kalıyordu); değişiklikten sonra bir `EventCallback` (`MedicationLines.OnChanged`) çağırın.
- Tarayıcı otomasyon aracı yoksa ekran görüntüsü için: `BrowserTestServer`'ı çalıştırıp Chrome'u `--headless=new --remote-debugging-port` ile açan küçük bir Node (yerleşik WebSocket, CDP) betiği yeterli; `test-role` çerezi `Network.setCookie` ile verilir. Sunucuyu zorla kapatmak `Disci_Tests_*` veritabanını arkada bırakır, elle silin.
- 2026-11-03: SqlClient, iptal edilen sorguyu `OperationCanceledException` değil `SqlException` ("Geçerli komutta önemli bir hata oluştu... İşlem kullanıcı tarafından iptal edildi") olarak fırlatır. Hasta listesinde hızlı yazınca QuickGrid önceki isteği iptal ediyor, bu hata yakalanmadığı için circuit ölüyor ve sayfadaki her şey (süzgeç, sıralama, düğmeler) çalışmaz oluyordu. İptal belirteciyle çalışan her sorguda `catch (Exception) when (token.IsCancellationRequested)` kullanın (QuickGrid için `OperationCanceledException(request.CancellationToken)` olarak yeniden fırlatın).
- `disabled="@string.IsNullOrWhiteSpace(x)"` olan bir düğme, `x` varsayılan `@bind` (onchange) ile bağlıysa yazarken pasif kalır; ilk tıklama yalnızca alanı bulanıklaştırır ve boşa gider. Bu tür alanlara `@bind:event="oninput"` verin.
- Uzun formlarda sayfa başlığındaki Kaydet düğmesi yapışkan üst çubuğun altına kayıyordu (oraya tıklayan "Hesabım"a gidip veriyi kaybediyordu); `.page-head.sticky` kullanın.
- Tarayıcı testi: `BrowserTestServer` + başsız Chrome + küçük CDP betiği (Node yerleşik WebSocket). Tıklamayı `elementFromPoint` ile doğrulayın (örtülü düğmeyi yakalar); Bootstrap `scroll-behavior: smooth` yüzünden kaydırmada `behavior:'instant'` verin; başlıklar CSS ile büyük harfe çevrildiği için `innerText` yerine `textContent` ile eşleyin; sidebar'daki Çıkış da `button[type=submit]`'tir.
- 2026-11-03 (gerçek veri kopyasıyla test): Eski programda isimler çoğunlukla Türkçe harfsiz girilmiş ("Ayse", "YILMAZ"; isimlerde ş yalnızca 3, ı 2.112 kez geçiyor). Veritabanı harmanlaması `SQL_Latin1_General_CP1_CI_AS` ı/I ve ş/s'yi farklı saydığı için "yılmaz" 9, "YILMAZ" 73 sonuç veriyordu. Bütün metin aramaları `Services/TextSearch.cs` üzerinden yapılır: SQL tarafında sütun iç içe `REPLACE` ile, arama kelimesi `Fold` ile sadeleştirilir; bellekteki listelerde `TextSearch.Contains`. `CurrentCultureIgnoreCase` kullanmayın (tr-TR'de "implant" ≠ "IMPLANT").
- Hasta aramasında sayı kuralı: 0 ile başlamayan en fazla 6 haneli sayı protokol no; 0 ile başlayan veya daha uzun sayı telefon (boşluklu yazılsa da tek numara; baştaki tek 0 atılır). Testler: `SearchTests`.
- Gerçek veriyle test: `Disci`'nin `COPY_ONLY` yedeği `data/` altında `Disci_TestKopya` olarak açılır, `DisciWebFactory` benzeri geçici bir fabrika (test kimliği, tohum veri yok) bu kopyaya bağlanır; iş bitince kopya ve yedek silinir. Betikler hasta adı yazdırmaz.
- 2026-11-03: Kullanıcının Firefox'unda hiçbir düğme/sekme çalışmıyordu (Chrome'da çalışıyordu). Neden: "WebSocketSniff" eklentisi `document_end`'de `window.WebSocket`'i eksik bir sarmalayıcıyla değiştiriyor; Blazor bağlantısı "this._webSocket.close is not a function" / "Failed to start the circuit" ile kopuyor. Önlem `App.razor`'da: `<head>`'in başında özgün `WebSocket` yakalanır (`window.__disciWebSocket`), `blazor.web.js` `autostart="false"` ile yüklenip `Blazor.start` içinde `configureSignalR: b => b.withUrl('_blazor', { WebSocket: ... })` ile bu kullanılır. Gerçek eklentiyle (geçici Firefox profiline kurularak, WebDriver BiDi ile) doğrulandı. Konsolda "Failed to start the circuit" görülürse önce tarayıcı eklentilerinden şüphelenin.
- Visual Studio programı çalıştırırken `src/Disci.Web/bin` kilitlidir; testler `dotnet build tests/Disci.Web.Tests -o <başka klasör>` ile derlenip oradan çalıştırılabilir.
- Kullanıcı İngilizce terimleri anlamıyor ("commit", "migration" gibi). Sorular ve özetler sade Türkçeyle, teknik terim kullanmadan yazılmalı.
- Python'a heredoc ile Türkçe metin verilince (Windows, `py -`) karakterler bozuluyor ve metin eşleşmiyor; Türkçe içerikli dosya düzenlemelerini Edit aracıyla yapın.
- `PatientMatcher` aday listesi olarak iç listesini döndürmemeli (kopya verin); aktarım sırasında `Add` ile liste büyüyünce rapordaki adaylar bozuluyordu.

## Açık sorular (kullanıcıya sorulacak)

1. 7 ödemedeki "C" durumu iptal mi, senet mi, başka bir şey mi?
2. Eski tedavi kayıtlarında hekim bilgisi yok; iki hekimden birine mi atanacak, boş mu kalacak?
3. Röntgen görüntüleri hangi bilgisayarda? Sensör yazılımı hangisi?
4. Ortodonti muayene kısaltmalarının (AT, SG, GC vb.) anlamları
5. Yeni sistem tek bilgisayarda mı, ağda mı, bulutta mı çalışacak?
6. DL2001'deki `Banka1` hasta grubu ve `H` ödeme türü: kullanıcı bilmiyor (2026-11-04), "uygun şekilde yap" dedi. Karar:
   `Banka1` hiçbir fiyat listesine/indirime bağlanmadı, `LegacyExtra`'da duruyor; `H` havale (`BankTransfer`) sayıldı.
   Anlamı öğrenilirse yeniden değerlendirilir.
7. DL2001 raporunda ayrı bırakılan 24 aynı adlı hasta: hekim aynı kişi olduğunu söylerse elle birleştirmek için bir **hasta birleştirme** ekranı gerekir (henüz yok).
8. Klinik 2020'den beri DL2001'i mi kullanıyordu? DentaLineSuit'e 2020'den sonra girilen az sayıdaki kayıt kimin? DL2001'deki tek hekim, DentaLineSuit'teki hekimlerden biri mi?

## Sıradaki işler

- ~~Hasta ekleme/düzenleme; tedavi ve ödeme girişi; randevu takvimi; bildirimler~~ (2026-11-02 yapıldı)
- Gerçek veriyle performans kontrolü (5.227 hasta): hasta listesindeki bakiye sütunu ve "Toplam alacak" alt sorgularla hesaplanıyor
- ~~Reçete modülü~~ (2026-11-02 yapıldı); E-Reçete entegrasyonu; ~~reçete şablonları~~ (2026-11-02 yapıldı)
- Taksit/senet planı; gider girişi ekranı
- SMS / WhatsApp Business ile otomatik hatırlatma (şimdilik yalnızca tek tıkla açılan wa.me bağlantısı var; mesajı kişi gönderir)
- Klinik adı/adresi için ayar ekranı (şimdilik `appsettings.json` → `Clinic`)
- Ağdan erişim (tablet/telefon), gerekirse JWT'li API
- Git deposu (henüz yok; `.gitignore` hazır, `data/` asla depoya girmemeli)
