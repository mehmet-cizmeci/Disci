# Yeni sürüme geçiş: 1.0.x → en yeni sürüm (1.3.1)

Bu rehber, Disci'nin **1.0.0 veya 1.0.1** sürümünü kullanan klinikler içindir. Geçiş yaklaşık 15 dakika sürer;
hastalar, randevular, ödemeler, fotoğraflar, kullanıcılar ve ayarlar **olduğu gibi korunur**. Veritabanı yeni sürümün
ilk açılışında kendiliğinden güncellenir; elle bir şey yapmanız gerekmez.

> **Bu geçişten sonraki güncellemeler için bu rehber gerekmez.** 1.1.0 ile gelen **Güncelleme** sayfasından tek tıkla
> (ya da otomatik olarak) güncellenir. Bu elle geçiş yalnızca bir kez yapılır.

Kurulu sürümünüzü bilmiyorsanız: `C:\Disci\app\Disci.Web.exe` dosyasına sağ tıklayın → Özellikler → Ayrıntılar →
"Ürün sürümü". 1.1.0 ve sonrasında ayrıca menüde **Güncelleme** sayfası vardır.

---

## Başlamadan önce

1. **Kimse programı kullanmasın.** Geçiş sırasında program birkaç dakika kapanır. Öğle arası veya mesai sonu uygundur.
2. **Elle yedek alın.** Disci'nin kurulu olduğu bilgisayarda:
   - Başlat menüsünde **PowerShell**'e sağ tıklayın → **Yönetici olarak çalıştır**.
   - Şunu yazıp Enter'a basın: `C:\Disci\betikler\yedekle.ps1`
   - "Veritabanı yedeklendi" yazısını görün. Yedek `C:\DisciVeri\Yedekler` klasöründedir.
   - Mümkünse bu klasörü bir USB belleğe de kopyalayın.
3. Kurulum hangi Windows hesabıyla yapıldıysa (SQL Server'ı kuran hesap) o hesapla oturum açın.

## 1. Yeni paketi indirin

1. <https://github.com/mehmet-cizmeci/Disci/releases/latest> adresine gidin.
2. **Disci-Kurulum-1.3.1-win-x64.zip** dosyasını indirin.
   (Güncellemede SQL Server zaten kurulu olduğu için başka bir dosya gerekmez.)
3. Zip'e sağ tıklayın → **Tümünü ayıkla** → örneğin `C:\Disci-Kurulum-1.3.1` klasörüne çıkarın.

## 2. Kurulumu çalıştırın

1. Klasördeki **KUR.bat** dosyasına çift tıklayın.
   - "Windows bilgisayarınızı korudu" uyarısı çıkarsa **Ek bilgi → Yine de çalıştır**.
   - "Değişiklik yapmasına izin veriyor musunuz?" sorusuna **Evet**.
2. Betik var olan kurulumu tanır ve **güncelleme** yapar:
   - Önceki kurulumun SQL Server örneğini ve ayarlarını kendisi bulur; veriler yeniden aktarılmaz.
   - Sorulara Enter'la varsayılanı seçmeniz yeterli.
3. Yeni adım: **"12. Güncelleme görevi"**. Bundan sonraki sürümleri programın içinden kurabilmek için Windows'a
   "Disci Guncelleme" adlı bir görev eklenir.
4. Sonunda "Kurulum tamamlandı" yazar. Pencereyi kapatın.

## 3. Kontrol edin

1. Masaüstündeki **Disci** kısayolunu açın ve giriş yapın.
2. Bir hastanın kartını açın: tedaviler, ödemeler ve fotoğraflar yerinde mi?
3. Yönetici olarak menüde **Yönetim → Güncelleme** sayfasını açın: "Kurulu sürüm 1.3.1" yazmalı.

Bir sorun görürseniz aşağıdaki **Geri dönüş** bölümüne bakın.

## 4. Yeni özellikleri ayarlayın (yönetici)

Geçişten sonra her şey eskisi gibi çalışır. Yeni özellikleri kullanmak için:

### Her hekimin kendi kullanıcısı

1. **Kullanıcılar** sayfasında her hekim için bir hesap olduğundan emin olun. Yoksa "Yeni kullanıcı" ile açın,
   rolü **Diş hekimi**, "Hekim kaydı" olarak hekimin kendisini seçin.
2. Var olan bir hesabı hekime bağlamak için satırdaki **Düzenle** → "Hekim kaydı".
3. Bağlanan hekim giriş yapınca randevu, tedavi ve reçetede **hekim olarak kendisi seçili** gelir.
4. **Klinik ayarları → Hekimler** tablosunda "Kullanıcı" sütununda **Hesap yok** yazan hekim kalmamalı.

### Kim kimin kayıtlarını görür

Varsayılan olarak herkes her şeyi görür (1.0'daki gibi). Değiştirmek için **Klinik ayarları → Hekimler** satırındaki
**kişiler** düğmesi:

- **Diğer hekimler** ve **Asistanlar** için: *Görür ve işlem yapar*, *Yalnızca görür* ya da *Göremez*.
- *Göremez* seçilirse o hekimin kendi hastaları, randevuları, tedavileri ve reçeteleri başkalarına görünmez; adı da
  takvimde ve hekim seçiminde çıkmaz.
- **Kişiye özel izinler**: belirli bir kişi için farklı izin (ör. yalnızca kendi asistanı görsün).
- Hekimler bu ayarı **Hesabım → Kayıtlarımı kimler görür** sayfasından kendileri de yapabilir; istemezseniz
  Klinik ayarlarında "Hekimler ... kendileri belirleyebilir" seçeneğini kapatın.
- Hastanın "kendi hekimi" hasta düzenleme sayfasındaki **Hekim** alanıdır. Boş bırakılan hastalar herkese açıktır.

### Kullanıcıların kendi bilgileri

Klinik ayarlarında **"Kullanıcılar kendi bilgilerini ... değiştirebilir"** açıksa (varsayılan) herkes **Hesabım →
Profil** sayfasından ad soyad, e-posta ve telefonunu düzeltebilir. Kapatırsanız yalnızca yönetici değiştirir.

### Ödeme düzeltme

Hekim ve asistanlar artık hasta kartındaki **Hesap** sekmesinde ödemeyi **düzenleyebilir**, **iptal edebilir**
(üstü çizili kalır), iptali **geri alabilir** ve gerekirse **kalıcı olarak silebilir**.

### Tedavi simgeleri

Diş şeması artık her tedaviyi simgesiyle gösterir (dolgu, kanal, kron, çekim, implant...). Simge işlem adından
tahmin edilir. Yanlış tahmin edilen bir işlem varsa **Fiyat listeleri** → işlemin kalem düğmesi → **Diş şemasındaki
simge** alanından düzeltin.

### Otomatik güncelleme

**Güncelleme** sayfasında **"Yeni sürüm çıkınca kendiliğinden kur"** açılırsa yeni sürümler gece 03:00-05:00
arasında kurulur (bilgisayar açıksa). Kapalıyken yeni sürüm menüde "yeni" etiketiyle haber verilir ve
**Şimdi güncelle** düğmesiyle kurulur. Her güncellemeden önce yedek alınır; yeni sürüm açılmazsa eskisine dönülür.

### İngilizce

Her kullanıcı **Hesabım → Profil** sayfasından ekran dilini **English** yapabilir.

## Geri dönüş (bir sorun olursa)

1. Önceki sürümün zip paketini GitHub'dan (Releases → 1.0.1) indirip **KUR.bat**'ı çalıştırın; program eski sürüme döner.
2. Veritabanı yeni sürümle birkaç yeni alan kazanmıştır; eski sürüm bunlarla da çalışır. Yine de sorun olursa geçişten
   önce aldığınız yedeği geri yükleyin: yönetici PowerShell'de
   `C:\Disci\betikler\geri-yukle.ps1 -Yedek C:\DisciVeri\Yedekler\Disci_YYYYMMDD_HHMM.bak`
3. Sorunu <https://github.com/mehmet-cizmeci/Disci/issues> adresinden bildirin (hasta bilgisi yazmadan).

## Linux veya macOS'a geçmek isteyenler

Disci 1.1.0 ve sonrası Linux ve macOS'ta da çalışır (Docker ile). Windows'taki verilerinizi taşımak için
[Kurulum-Linux.md](Kurulum-Linux.md#windowstan-linuxa-geçiş) rehberindeki "Windows'tan Linux'a geçiş" bölümüne bakın.
