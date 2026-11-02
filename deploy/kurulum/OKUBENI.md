# Disci kurulumu (klinik bilgisayarı)

Bu klasör, Disci'yi programın sürekli çalışacağı bilgisayara kurar. Hedef bilgisayara .NET kurmanız gerekmez;
program paketin içinde gelir.

## Gerekenler

- Windows 10 veya 11 (64 bit), internet bağlantısı (SQL Server Express indirilecek)
- Bilgisayarda **yönetici** yetkisi
- Eski veriler aktarılacaksa: DentaLineSuit'in `DL2500DB` klasörü (USB bellekle de getirilebilir)

## Kurulum

1. Bu klasörü (zip'i açarak) hedef bilgisayara kopyalayın, örneğin `C:\DisciKurulum`.
2. Eski verileri aktaracaksanız DentaLineSuit'i kapatın ve `DL2500DB` klasörünün nerede olduğunu not edin.
3. **KUR.bat** dosyasına çift tıklayın, Windows'un sorusuna **Evet** deyin.
4. Betiğin sorularını yanıtlayın:
   - Eski veriler aktarılsın mı? Evet derseniz `DL2500DB` klasörünün yolunu yazın.
   - SQL Server Express lisansını kabul ediyor musunuz?
   - Ağ "Ortak" ise "Özel" yapılsın mı? (Yalnızca klinik ağıysa evet deyin.)
   - İlk yöneticinin e-postası ve parolası
   - Deneme yedeği ve uyku modu
5. Sonunda programın adresleri yazılır. Bu bilgisayarda `https://localhost:5443` adresini açıp giriş yapın.

Betik yarıda kalırsa sorunu giderip tekrar çalıştırın; tamamlanmış adımlar tekrar zarar vermez.
Her çalıştırmanın kaydı bu klasörde `kurulum_log_*.txt` olarak tutulur.

## Betiğin yaptıkları

| Adım | Ne yapar |
|---|---|
| SQL Server Express | Kurulu değilse winget ile kurar (önce 2025, olmazsa 2022), otomatik başlatır |
| Program | `C:\Disci\app` klasörüne kopyalar |
| Windows hizmeti | "Disci Klinik Programı" hizmetini oluşturur: Windows açılınca kendiliğinden başlar, çökerse yeniden başlar, yalnızca kendi klasörlerine ve veritabanına erişebilen ayrı bir hesapla çalışır |
| HTTPS | Bu bilgisayar için 5 yıllık bir sertifika oluşturur; bağlantılar şifreli olur |
| Veritabanı | Eski verileri aktarır (istenirse) ve `Disci` veritabanını hazırlar |
| Güvenlik duvarı | 5111 (HTTP) ve 5443 (HTTPS) portlarını yalnızca Özel/Etki alanı ağlarına açar |
| İlk yönetici | Hiç kullanıcı yoksa sizin verdiğiniz e-posta ve parolayla açar |
| Gece yedeği | Her gece 23:30'da veritabanını ve fotoğrafları `C:\DisciVeri\Yedekler` klasörüne yedekler, son 14 günü saklar |
| Güç | İsterseniz bilgisayarın prizdeyken uykuya geçmesini kapatır |

## Klasörler

| Klasör | İçerik |
|---|---|
| `C:\Disci\app` | Program |
| `C:\Disci\import` | Eski verileri aktarma aracı |
| `C:\Disci\betikler` | Yedekleme, yeniden aktarma ve kaldırma betikleri |
| `C:\DisciVeri\files` | Hasta fotoğrafları |
| `C:\DisciVeri\Yedekler` | Gece yedekleri ve `yedek.log` |
| `C:\DisciVeri\sertifika\Disci.cer` | Diğer cihazlara kurulacak sertifika |
| `C:\DisciVeri\reports` | Veri aktarım raporları |

**Yedekleri başka bir yere de kopyalayın** (harici disk, bulut). Bilgisayar bozulursa aynı bilgisayardaki yedek de kaybolur.

## Diğer cihazlardan bağlanma

Klinik ağındaki tablet, telefon ve bilgisayarlardan tarayıcıya şu adreslerden birini yazın:

```
https://BILGISAYAR-ADI:5443
https://192.168.x.x:5443
```

(Kurulumun sonunda bu bilgisayarın gerçek adı ve adresleri yazılır.)

Sertifika kliniğin kendi sertifikası olduğu için, cihazlar ilk bağlantıda "bağlantınız gizli değil" uyarısı gösterir.
Uyarının çıkmaması için `C:\DisciVeri\sertifika\Disci.cer` dosyasını her cihaza bir kez kurun:

- **Windows:** Dosyaya çift tıklayın > *Sertifika yükle* > *Yerel Makine* > *Tüm sertifikaları aşağıdaki depolama alanına yerleştir* > *Güvenilen Kök Sertifika Yetkilileri*.
- **Android:** Dosyayı telefona gönderin > *Ayarlar > Güvenlik > Şifreleme ve kimlik bilgileri > Sertifika yükle > CA sertifikası*.
- **iPhone/iPad:** Dosyayı AirDrop veya e-postayla açın > *Ayarlar > Profil yüklendi* > Yükle; sonra *Ayarlar > Genel > Hakkında > Sertifika Güven Ayarları* bölümünden açın.

Bilgisayarın ağ adresi değişirse adres uyarısı çıkabilir. Modemden bu bilgisayara sabit adres verin (DHCP rezervasyonu)
veya bilgisayar adıyla bağlanın. Adres değiştiyse sertifikayı yenilemek için: `KUR.bat -YeniSertifika` (sonra yeni `Disci.cer` dosyasını cihazlara tekrar kurun).

## Sonraki işler

**Güncelleme:** Yeni paketi kopyalayıp `KUR.bat`'ı tekrar çalıştırın. Program güncellenir; veriler, ayarlar ve kullanıcılar korunur.

**Geçiş günü (eski verileri yeniden aktarma):** DentaLineSuit'e son kayıt girildikten sonra, yönetici PowerShell'de:

```powershell
C:\Disci\betikler\veri-aktar.ps1 -EskiVeri "D:\DentaLineSuit\DL2500DB"
```

Önce yedek alınır, sonra Disci'deki veriler **ve kullanıcı hesapları** silinip baştan aktarılır.

**Elle yedek:**

```powershell
C:\Disci\betikler\yedekle.ps1
```

**Yedekten geri dönme:** SQL Server Management Studio ile `C:\DisciVeri\Yedekler\Disci_*.bak` dosyasından *Restore Database* yapın,
fotoğrafları `Yedekler\files` klasöründen `C:\DisciVeri\files` klasörüne geri kopyalayın. Bu işlemi bilen birinden destek alın.

**Kaldırma:** `C:\Disci\betikler\kaldir.ps1`. Hizmet, güvenlik duvarı kuralları ve yedek görevi silinir; veriler ve yedekler kalır.

## Sorun giderme

| Belirti | Ne yapmalı |
|---|---|
| "SQL Server Express kurulamadı" | https://www.microsoft.com/sql-server/sql-server-downloads adresinden **Express**'i varsayılan ayarlarla kurun, `KUR.bat`'ı tekrar çalıştırın |
| "SQL Server'a bağlanılamadı" | SQL Server'ı kuran Windows kullanıcısıyla kurulumu çalıştırın (SQL yöneticisi o kullanıcıdır) |
| "Program yanıt vermiyor" | *Olay Görüntüleyicisi > Windows Günlükleri > Uygulama* altında kaynağı **Disci** olan kayıtlara bakın |
| Diğer cihazlar bağlanamıyor | Ağ "Özel" mi? Güvenlik duvarı kuralları var mı? Cihaz aynı ağda mı? Önce bu bilgisayarda adresi deneyin |
| Hizmeti yeniden başlatmak | *Hizmetler* uygulamasında "Disci Klinik Programı" > Yeniden başlat, veya yönetici PowerShell'de `Restart-Service Disci` |
