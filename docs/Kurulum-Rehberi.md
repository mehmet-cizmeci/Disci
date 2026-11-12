# Disci kurulum rehberi

Bu rehber, Disci'yi bir diş kliniğinin **Windows** bilgisayarına kurmayı adım adım anlatır. Teknik bilgi gerekmez;
kurulum betiği her şeyi kendisi yapar. Programın kullanımı için [Kullanım kılavuzu](Kullanim-Kilavuzu.md)'na bakın.

- **Linux:** [Kurulum-Linux.md](Kurulum-Linux.md) · **macOS:** [Kurulum-macOS.md](Kurulum-macOS.md)
- **1.0.x sürümünü kullanıyorsanız:** [Yeni sürüme geçiş](Surum-Gecisi.md)

## Nasıl çalışır

- Disci kliniğinizdeki **bir bilgisayara** (sunucu) kurulur ve orada **Windows hizmeti** olarak arka planda sürekli
  çalışır. Bilgisayar açılınca kendiliğinden başlar; kimsenin oturum açması gerekmez.
- Aynı ağdaki **diğer bilgisayarlar, tablet ve telefonlar** programa tarayıcıdan bağlanır; onlara program kurulmaz.
- Bilgiler sunucudaki **SQL Server Express** veritabanında durur (ücretsiz). İnternete çıkmaz.
- Her gün saat 14:00'te **otomatik yedek** alınır.

## Gerekenler

| | |
|---|---|
| Sunucu bilgisayar | Windows 10 veya 11 (Home, Pro fark etmez), 64 bit, en az 8 GB bellek, 10 GB boş disk |
| İnternet | Yalnızca yeni SQL Server Express kurulacaksa ve kurulum dosyası pakete konmadıysa (bkz. bölüm 1) |
| SQL Server | Gerekmez, kurulum kurar. Bilgisayarda zaten bir SQL Server varsa o da kullanılabilir (aşağıya bakın) |
| Ağ | Diğer cihazların bağlanacağı aynı modem/Wi-Fi ağı |
| Yetki | Bilgisayarda yönetici (admin) yetkili bir Windows hesabı |

> Sunucunun sürekli açık kalması gerekir. Kurulum, bilgisayar prize takılıyken uyku moduna geçmesini kapatmayı önerir.
> Mümkünse sunucuyu bir kesintisiz güç kaynağına (UPS) bağlayın.

## 1. Kurulum paketini indirin

1. <https://github.com/mehmet-cizmeci/Disci/releases> adresine gidin.
2. En son sürümdeki **Disci-Kurulum-<sürüm>-win-x64.zip** dosyasını indirin (~110 MB).
3. Zip dosyasına sağ tıklayın → **Tümünü ayıkla**. Örneğin `C:\Disci-Kurulum` klasörüne çıkarın.

### SQL Server'ı internetsiz kurmak (isteğe bağlı)

SQL Server Express, Microsoft'un ücretsiz yazılımıdır; kurulum dosyası Disci paketinde değil, Microsoft'un sitesindedir.
Kurulum sırasında internetten indirmede sorun yaşamamak için önceden indirip pakete koyabilirsiniz:

1. SQL Server 2022 Express indirme aracını açın: <https://go.microsoft.com/fwlink/p/?linkid=2216019>
   (resmi sayfa: <https://www.microsoft.com/sql-server/sql-server-downloads>)
2. **Medya indir (Download Media)** → **Express Core** → **İndir**. `SQLEXPR_x64_ENU.exe` dosyası oluşur (~700 MB).
3. Disci paketini çıkardığınız klasörde **`sql`** adlı bir klasör açıp dosyayı içine koyun:
   `C:\Disci-Kurulum\sql\SQLEXPR_x64_ENU.exe`
4. `KUR.bat` "yeni SQL Server kur" seçildiğinde bu dosyayı internet olmadan kullanır.

Bilgisayarda ya da ağda zaten bir SQL Server varsa bu adım gerekmez; kurulum onu kullanabilir (aşağıya bakın).

> Kaynak koddan kendiniz derlemek isterseniz: .NET 10 SDK kurun, depoyu indirin ve
> `powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1` komutuyla aynı zip'i üretin.

## 2. Kurulumu çalıştırın

1. Çıkardığınız klasördeki **KUR.bat** dosyasına çift tıklayın. Disci ücretsiz olduğu için kod imzalama sertifikası
   yoktur; Windows "Windows bilgisayarınızı korudu" (SmartScreen) uyarısı gösterirse **Ek bilgi → Yine de çalıştır**'a tıklayın.
2. Windows "Bu uygulamanın değişiklik yapmasına izin veriyor musunuz?" diye sorar: **Evet**.
3. Mavi pencerede sorulara cevap verin (Enter'a basmak köşeli parantezdeki varsayılanı seçer):

| Soru | Ne cevaplanır |
|---|---|
| Disci verilerini hangi SQL Server'da tutsun? | Bkz. aşağıdaki **SQL Server seçimi**. Bilmiyorsanız **1** (yeni SQL Server Express kur) |
| SQL Server Express lisans koşullarını kabul ediyor musunuz? | **E** (yalnızca yeni SQL Server kurulacaksa sorulur) |
| Ağ "Özel" yapılsın mı? | Klinik ağıysa **e**. Ortak ağda diğer cihazlar bağlanamaz |
| Bağlantı paketi ağda paylaşılsın mı? | **E** (diğer bilgisayarların kurulumu kolaylaşır) |
| Deneme yedeği alınsın mı? | **E** |
| Bilgisayar uyku moduna geçmesin mi? | **E** |

### SQL Server seçimi

SQL Server kurmak zorunlu değildir. Kurulum şu seçenekleri sunar:

| Seçenek | Ne zaman | Sorulanlar |
|---|---|---|
| **Bu bilgisayara yeni SQL Server Express kur** | Bilgisayarda SQL Server yoksa (önerilen) | Lisans onayı. Paketin `sql` klasöründe kurulum dosyası varsa internetsiz kurulur |
| **Bu bilgisayarda kurulu SQL Server'ı kullan** (aynı sunucu) | Bilgisayarda zaten bir SQL Server varsa; kurulu örnekler listelenir | Windows kimlik doğrulaması mı (Evet) yoksa SQL kullanıcı adı/parolası mı; veritabanı adı |
| **Başka bir bilgisayardaki SQL Server'ı kullan** | Klinikte ayrı bir veritabanı sunucusu varsa | Sunucu adı (`SUNUCU\SQLEXPRESS` ya da `192.168.1.10,1433`), kimlik doğrulama (SQL kullanıcı adı ve parolası önerilir), veritabanı adı, sunucudaki yedek klasörü |

- Kurulum bağlantıyı **dener**; bağlanamazsa bilgileri yeniden sorar.
- Veritabanı yoksa oluşturulur. SQL kullanıcısının veritabanı oluşturma yetkisi yoksa, SQL Server yöneticisinden boş bir
  veritabanı açıp bu kullanıcıyı onun sahibi (**db_owner**) yapmasını isteyin.
- SQL parolası programın ayar dosyasında (`C:\Disci\app\appsettings.Production.json`) saklanır; dosyayı yalnızca
  yöneticiler ve Disci hizmeti okuyabilir.
- SQL Server başka bir bilgisayardaysa günlük **veritabanı yedeği o sunucunun diskine** yazılır (kurulum klasörü sorar);
  fotoğraflar yine bu bilgisayardaki `C:\DisciVeri\Yedekler` klasörüne kopyalanır.
- Güncellemelerde (KUR.bat yeniden çalıştırılınca) önceki bağlantı aynen kullanılır, bir şey sorulmaz.

Sormadan kurmak için komut isteminden:

```
KUR.bat -SqlSunucu ".\SQLEXPRESS"                                              bu bilgisayardaki örnek, Windows kimliği
KUR.bat -SqlSunucu "192.168.1.10,1433" -SqlKullaniciAdi disci -SqlParolasi "..." -Veritabani Disci
KUR.bat -SqlKurma                                                              "yeni SQL Server kur" seçeneğini gösterme
```

Kurulumdan sonra bağlantıyı değiştirmek için bkz. [Veritabanı bağlantısını değiştirme](#veritabanı-bağlantısını-değiştirme).

Kurulum 5-15 dakika sürer. Betik şunları kendisi yapar:

- Seçilen SQL Server'ı hazırlar (gerekirse kurar, hizmetini otomatik başlatır, veritabanını ve yetkileri ayarlar).
- Programı `C:\Disci`, verileri `C:\DisciVeri` klasörüne kopyalar.
- **Disci Klinik Programı** adlı Windows hizmetini kurar (bilgisayar açılınca kendiliğinden başlar, hata olursa yeniden başlar).
- Kliniğe özel güvenlik sertifikası oluşturur (HTTPS) ve güvenlik duvarında 5111 ve 5443 portlarını yalnızca özel ağa açar.
- Masaüstüne **Disci** kısayolu koyar.
- Diğer bilgisayarlar için **bağlantı paketi** hazırlar.
- Her gün 14:00'te çalışan **otomatik yedek** görevini kurar (`KUR.bat -YedekSaati 12:30` ile başka saat seçilebilir).

## 3. İlk kurulum formu

Kurulum bitince tarayıcıda **ilk kurulum formu** açılır (açılmazsa masaüstündeki **Disci** kısayoluna çift tıklayın).

1. Klinik adını, adresini ve telefonunu yazın.
2. Kendi adınızı, hekimseniz takvimde görünecek hekim adınızı, e-posta adresinizi (giriş adınız olur) ve parolanızı yazın.
3. **Kurulumu tamamla**'ya basın ve aynı bilgilerle giriş yapın.

Ardından **Klinik ayarları**'ndan diğer hekimleri ve randevu türlerini, **Fiyat listeleri**'nden fiyatları,
**Kullanıcılar**'dan asistan ve hekim hesaplarını ekleyin. Ayrıntılar kullanım kılavuzunda.

> Form güvenlik için yalnızca sunucu bilgisayarda açılır ve bir kez doldurulabilir.

## 4. Diğer bilgisayarları bağlama

Her bilgisayarda **bir kez**:

1. Dosya Gezgini'nin adres çubuğuna `\\SUNUCU-ADI\Disci-Baglanti` yazın (SUNUCU-ADI kurulumun sonunda yazılır;
   ya da sunucuda `C:\DisciVeri\Disci-Baglanti` klasörünü USB belleğe kopyalayın).
2. **Baglanti-Kur.bat** dosyasına çift tıklayın ve Windows'un sorusuna **Evet** deyin.
3. Masaüstüne **Disci** kısayolu gelir ve program açılır.

### Tablet ve telefon

1. Cihaz klinik Wi-Fi'sine bağlı olsun.
2. Tarayıcıya `https://SUNUCU-IP:5443` yazın (IP adresi kurulumun sonunda yazılır, ör. `https://192.168.1.20:5443`).
3. "Bağlantınız gizli değil" uyarısı çıkarsa iki yol var:
   - **Sertifikayı kurun (önerilen):** `C:\DisciVeri\sertifika\Disci.cer` dosyasını cihaza gönderin (e-posta, USB).
     *Android:* Ayarlar → Güvenlik → Şifreleme ve kimlik bilgileri → Sertifika yükle → CA sertifikası.
     *iPhone/iPad:* dosyayı açın → Ayarlar → Profil İndirildi → Yükle; sonra Ayarlar → Genel → Hakkında →
     Sertifika Güven Ayarları → Disci'yi açın.
   - Ya da uyarıda **Gelişmiş → Devam et**'e basın (her cihazda bir kez).
4. Tarayıcı menüsünden **Ana ekrana ekle**.

### Sabit IP adresi (önerilir)

Modem, sunucuya her açılışta farklı bir IP verebilir; o zaman IP ile kurulan kısayollar açılmaz. Modemin yönetim
sayfasında (genellikle `192.168.1.1`) **DHCP rezervasyonu / sabit IP** bölümünden sunucu bilgisayara sabit adres
verin. Bilgisayar adıyla kurulan kısayollar bundan etkilenmez.

## 5. Yedekleme

- Otomatik yedek: her gün 14:00, `C:\DisciVeri\Yedekler`, son 14 gün saklanır. Kayıt: `C:\DisciVeri\Yedekler\yedek.log`.
- Elle yedek: yönetici PowerShell'de `C:\Disci\betikler\yedekle.ps1`.
- **Başka bir yere kopyalayın:** haftada bir `C:\DisciVeri\Yedekler` klasörünü harici diske kopyalayın veya
  bu klasörü OneDrive / Google Drive ile eşitleyin. Bilgisayar bozulursa veya çalınırsa yalnızca dışarıdaki yedek kurtarır.

### Yedekten geri yükleme

1. Başlat menüsünde **PowerShell**'e sağ tıklayın → **Yönetici olarak çalıştır**.
2. `C:\Disci\betikler\geri-yukle.ps1` yazıp Enter'a basın.
3. Listeden yedeği seçin (en yenisi 1 numara) ve onaylayın.

Betik önce şu anki halin yedeğini alır, sonra seçilen yedeği geri yükler ve programı yeniden başlatır.
**Yedekten sonra girilen kayıtlar silinir.**

### Bilgisayar değiştirme

1. Eski bilgisayarda elle yedek alın; `C:\DisciVeri\Yedekler` klasörünü USB belleğe kopyalayın.
2. Yeni bilgisayara Disci'yi kurun (ilk kurulum formunu doldurmanıza gerek yok).
3. Yedekleri yeni bilgisayarda `C:\DisciVeri\Yedekler` klasörüne koyun ve `geri-yukle.ps1`'i çalıştırın.
4. Diğer bilgisayarlarda bağlantı paketini yeniden çalıştırın (sunucu adı değişmiş olabilir).

## 6. Yönetici parolasını sıfırlama

Yönetici parolası unutulduysa ve başka yönetici yoksa, sunucu bilgisayarda:

1. **PowerShell**'i yönetici olarak açın.
2. `C:\Disci\betikler\parola-sifirla.ps1` yazıp Enter'a basın.
3. Yöneticinin e-postasını ve yeni parolayı yazın.

Hesabın kilidi açılır ve yönetici yetkisi verilir. O e-postada hesap yoksa yeni bir yönetici hesabı açılır.

## 7. Güncelleme

**1.1.0 ve sonrası:** Yönetici olarak **Yönetim → Güncelleme** sayfasını açın. Yeni sürüm varsa **Şimdi güncelle**
deyin; en geç 10 dakika içinde "Disci Guncelleme" görevi yeni sürümü GitHub'dan indirir, doğrular (SHA256), yedek
alır ve kurar. Yeni sürüm açılmazsa eski sürüme kendiliğinden döner. **"Yeni sürüm çıkınca kendiliğinden kur"** açılırsa
güncellemeler gece 03:00-05:00 arasında yapılır. Görevin kaydı: `C:\DisciVeri\Guncelleme\guncelle.log`.

Elle güncelleme de mümkündür: yeni zip'i indirip **KUR.bat**'ı çalıştırın ya da yönetici PowerShell'de
`C:\Disci\betikler\guncelle.ps1 -Surum son`. Veriler, ayarlar, kullanıcılar ve yedekler korunur.

**1.0.x'ten 1.1.0'a** ilk geçiş bir kez elle yapılır: [Yeni sürüme geçiş](Surum-Gecisi.md).

### Veritabanı bağlantısını değiştirme

Programın hangi SQL Server'ı kullandığı **Klinik ayarları → Veritabanı** bölümünde görünür. Değiştirmek için
(ör. veriler ayrı bir sunucuya taşınacaksa) Disci'nin kurulu olduğu bilgisayarda:

1. **PowerShell**'i yönetici olarak açın.
2. `C:\Disci\betikler\veritabani-ayarla.ps1` yazıp Enter'a basın.
3. Kurulumdaki gibi seçin: bu bilgisayardaki ya da başka bilgisayardaki SQL Server, kimlik doğrulama, veritabanı adı.
4. Betik bağlantıyı dener. Yeni veritabanı boşsa ve SQL Server bu bilgisayardaysa **mevcut verileri taşımayı** önerir
   (eski veritabanı silinmez). Başka bilgisayardaki sunucuya taşımak için önce `yedekle.ps1` ile yedek alıp yedeği o
   sunucuya kopyalayın, sonra `geri-yukle.ps1` çalıştırın.
5. Ayarı kaydeder ve programı yeniden başlatır.

## 8. Kaldırma

Yönetici PowerShell'de `C:\Disci\betikler\kaldir.ps1`. Hizmet, güvenlik duvarı kuralları ve yedek görevi kaldırılır;
**veriler ve yedekler silinmez** (`C:\DisciVeri`).

## Sorun giderme

| Sorun | Çözüm |
|---|---|
| "Windows bilgisayarınızı korudu" uyarısı | Program imzasız olduğu için çıkar (sertifika ücretli, alınmadı): **Ek bilgi → Yine de çalıştır** |
| "winget bulunamadı" | Microsoft Store'dan **Uygulama Yükleyicisi**'ni (App Installer) güncelleyin veya SQL Server Express'i <https://www.microsoft.com/sql-server/sql-server-downloads> adresinden "Express" seçerek varsayılan ayarlarla kurun, sonra KUR.bat'ı yeniden çalıştırın |
| "SQL Server'a bağlanılamadı" | Bu bilgisayardaki sunucuda: kurulumu SQL Server'ı kuran Windows hesabıyla çalıştırın ya da SQL kullanıcı adı/parolası seçin. Başka bilgisayardaki sunucuda: sunucu adını/portu, SQL Server'da **TCP/IP**'nin açık olduğunu (SQL Server Configuration Manager) ve güvenlik duvarında 1433 portunu kontrol edin |
| "Veritabanı yok ve oluşturulamadı" | SQL Server yöneticisinden boş bir veritabanı açıp kullanıcıyı **db_owner** yapmasını isteyin |
| Program açılmıyor | Hizmetler → **Disci Klinik Programı** → Başlat. Hata sürerse Olay Görüntüleyicisi → Windows Günlükleri → Uygulama altındaki "Disci" kayıtlarına bakın |
| Diğer bilgisayarlar bağlanamıyor | Sunucunun ağ profili **Özel** olmalı (Ayarlar → Ağ ve İnternet). Antivirüs programının güvenlik duvarı 5443 portunu engelliyor olabilir |
| "Bağlantınız gizli değil" | Bağlantı paketini çalıştırın veya sertifikayı cihaza kurun (bölüm 4) |
| Kurulum yarıda kaldı | Sorunu giderip KUR.bat'ı yeniden çalıştırın; tamamlanan adımlar zarar görmez |

Kurulum kaydı, kurulum klasöründeki `kurulum_log_*.txt` dosyasındadır. Sorun bildirmek için:
<https://github.com/mehmet-cizmeci/Disci/issues> (kayıt dosyasında hasta bilgisi yoktur ama paylaşmadan önce göz atın).
