# Disci kurulum rehberi

Bu rehber, Disci'yi bir diş kliniğinin bilgisayarına kurmayı adım adım anlatır. Teknik bilgi gerekmez; kurulum
betiği her şeyi kendisi yapar. Programın kullanımı için [Kullanım kılavuzu](Kullanim-Kilavuzu.md)'na bakın.

## Nasıl çalışır

- Disci kliniğinizdeki **bir bilgisayara** (sunucu) kurulur ve orada **Windows hizmeti** olarak arka planda sürekli
  çalışır. Bilgisayar açılınca kendiliğinden başlar; kimsenin oturum açması gerekmez.
- Aynı ağdaki **diğer bilgisayarlar, tablet ve telefonlar** programa tarayıcıdan bağlanır; onlara program kurulmaz.
- Bilgiler sunucudaki **SQL Server Express** veritabanında durur (ücretsiz). İnternete çıkmaz.
- Her gece **otomatik yedek** alınır.

## Gerekenler

| | |
|---|---|
| Sunucu bilgisayar | Windows 10 veya 11 (Home, Pro fark etmez), 64 bit, en az 8 GB bellek, 10 GB boş disk |
| İnternet | Yalnızca kurulum sırasında (SQL Server Express indirilir) |
| Ağ | Diğer cihazların bağlanacağı aynı modem/Wi-Fi ağı |
| Yetki | Bilgisayarda yönetici (admin) yetkili bir Windows hesabı |

> Sunucunun sürekli açık kalması gerekir. Kurulum, bilgisayar prize takılıyken uyku moduna geçmesini kapatmayı önerir.
> Mümkünse sunucuyu bir kesintisiz güç kaynağına (UPS) bağlayın.

## 1. Kurulum paketini indirin

1. <https://github.com/mehmet-cizmeci/Disci/releases> adresine gidin.
2. En son sürümdeki **Disci-Kurulum-....zip** dosyasını indirin.
3. Zip dosyasına sağ tıklayın → **Tümünü ayıkla**. Örneğin `C:\Disci-Kurulum` klasörüne çıkarın.

> Kaynak koddan kendiniz derlemek isterseniz: .NET 10 SDK kurun, depoyu indirin ve
> `powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1` komutuyla aynı zip'i üretin.

## 2. Kurulumu çalıştırın

1. Çıkardığınız klasördeki **KUR.bat** dosyasına çift tıklayın.
2. Windows "Bu uygulamanın değişiklik yapmasına izin veriyor musunuz?" diye sorar: **Evet**.
3. Mavi pencerede sorulara cevap verin (Enter'a basmak köşeli parantezdeki varsayılanı seçer):

| Soru | Ne cevaplanır |
|---|---|
| Eski DentaLineSuit verileri aktarılsın mı? | Eski program kullanmadıysanız **h** (hayır). Kullandıysanız bkz. [bölüm 6](#6-eski-programdan-veri-aktarma) |
| SQL Server Express lisans koşullarını kabul ediyor musunuz? | **E** (ücretsiz Microsoft veritabanı) |
| Ağ "Özel" yapılsın mı? | Klinik ağıysa **e**. Ortak ağda diğer cihazlar bağlanamaz |
| Bağlantı paketi ağda paylaşılsın mı? | **E** (diğer bilgisayarların kurulumu kolaylaşır) |
| Deneme yedeği alınsın mı? | **E** |
| Bilgisayar uyku moduna geçmesin mi? | **E** |

Kurulum 5-15 dakika sürer. Betik şunları kendisi yapar:

- SQL Server Express'i kurar ve otomatik başlayacak şekilde ayarlar.
- Programı `C:\Disci`, verileri `C:\DisciVeri` klasörüne kopyalar.
- **Disci Klinik Programı** adlı Windows hizmetini kurar (bilgisayar açılınca kendiliğinden başlar, hata olursa yeniden başlar).
- Kliniğe özel güvenlik sertifikası oluşturur (HTTPS) ve güvenlik duvarında 5111 ve 5443 portlarını yalnızca özel ağa açar.
- Masaüstüne **Disci** kısayolu koyar.
- Diğer bilgisayarlar için **bağlantı paketi** hazırlar.
- Her gece 23:30'da çalışan **otomatik yedek** görevini kurar.

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

- Otomatik yedek: her gece 23:30, `C:\DisciVeri\Yedekler`, son 14 gün saklanır. Kayıt: `C:\DisciVeri\Yedekler\yedek.log`.
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

## 6. Eski programdan veri aktarma

Disci, **DentaLineSuit 2.5** ve **DentaLine 2001 (2.3)** verilerini aktarır.

1. Eski programı kapatın; kimse kayıt girmesin.
2. Eski programın veri klasörünü bulun: DentaLineSuit için `DL2500DB`, DentaLine 2001 için `DB` klasörü
   (içinde `DLHASTA.DB` dosyası olan klasör). Başka bilgisayardaysa USB belleğe kopyalayın.
3. Kurulum sırasında "Eski DentaLineSuit verileri aktarılsın mı?" sorusuna **e** deyip klasör yolunu yazın;
   DentaLine 2001 verisi de varsa ikinci soruya **e** deyip onun yolunu yazın.

**Kurulumdan sonra (geçiş günü):** eski programa son kayıt girildikten sonra, yönetici PowerShell'de:

```powershell
C:\Disci\betikler\veri-aktar.ps1 -EskiVeri "D:\DentaLineSuit\DL2500DB" -Dl2001Veri "D:\DL2001\DB"
```

Bu komut önce yedek alır, sonra Disci'deki verileri **ve kullanıcı hesaplarını silip** eski programdan baştan aktarır;
sonunda ilk kurulum formu yeniden açılır. DentaLine 2001 yoksa `-Dl2001Veri` kısmını yazmayın.

Aktarım raporu `C:\DisciVeri\reports` klasörüne yazılır: kaç kayıt geldiği, tutar kontrolleri, birleştirilen hastalar
ve aynı adlı olduğu halde ayrı bırakılan hastalar.

## 7. Yönetici parolasını sıfırlama

Yönetici parolası unutulduysa ve başka yönetici yoksa, sunucu bilgisayarda:

1. **PowerShell**'i yönetici olarak açın.
2. `C:\Disci\betikler\parola-sifirla.ps1` yazıp Enter'a basın.
3. Yöneticinin e-postasını ve yeni parolayı yazın.

Hesabın kilidi açılır ve yönetici yetkisi verilir. O e-postada hesap yoksa yeni bir yönetici hesabı açılır.

## 8. Güncelleme

1. GitHub'dan yeni sürümün zip dosyasını indirip çıkarın.
2. **KUR.bat**'ı çalıştırın. Program güncellenir; veriler, ayarlar, kullanıcılar ve yedekler korunur.

Güncellemeden önce elle yedek almanızı öneririz.

## 9. Kaldırma

Yönetici PowerShell'de `C:\Disci\betikler\kaldir.ps1`. Hizmet, güvenlik duvarı kuralları ve yedek görevi kaldırılır;
**veriler ve yedekler silinmez** (`C:\DisciVeri`).

## Sorun giderme

| Sorun | Çözüm |
|---|---|
| "winget bulunamadı" | Microsoft Store'dan **Uygulama Yükleyicisi**'ni (App Installer) güncelleyin veya SQL Server Express'i <https://www.microsoft.com/sql-server/sql-server-downloads> adresinden "Express" seçerek varsayılan ayarlarla kurun, sonra KUR.bat'ı yeniden çalıştırın |
| "SQL Server'a bağlanılamadı" | Kurulumu, SQL Server'ı kuran Windows hesabıyla çalıştırın |
| Program açılmıyor | Hizmetler → **Disci Klinik Programı** → Başlat. Hata sürerse Olay Görüntüleyicisi → Windows Günlükleri → Uygulama altındaki "Disci" kayıtlarına bakın |
| Diğer bilgisayarlar bağlanamıyor | Sunucunun ağ profili **Özel** olmalı (Ayarlar → Ağ ve İnternet). Antivirüs programının güvenlik duvarı 5443 portunu engelliyor olabilir |
| "Bağlantınız gizli değil" | Bağlantı paketini çalıştırın veya sertifikayı cihaza kurun (bölüm 4) |
| Kurulum yarıda kaldı | Sorunu giderip KUR.bat'ı yeniden çalıştırın; tamamlanan adımlar zarar görmez |

Kurulum kaydı, kurulum klasöründeki `kurulum_log_*.txt` dosyasındadır. Sorun bildirmek için:
<https://github.com/mehmet-cizmeci/Disci/issues> (kayıt dosyasında hasta bilgisi yoktur ama paylaşmadan önce göz atın).
