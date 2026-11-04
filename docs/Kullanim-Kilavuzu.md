# Disci kullanım kılavuzu

**Disci**, diş klinikleri için ücretsiz ve açık kaynak kodlu bir hasta takip programıdır. Randevu, hasta kartı, diş
şeması, tedavi planı, ödeme ve makbuz, reçete, kasa raporu, fiyat listeleri ve kullanıcı yetkileri tek programdadır.

Program klinikteki bir bilgisayarda (sunucu) sürekli çalışır. Siz onu **internet tarayıcısından** (Chrome, Edge,
Firefox, Safari) kullanırsınız: aynı bilgisayardan, klinik ağındaki diğer bilgisayarlardan, tablet ve telefonlardan.
Bilgileriniz internete çıkmaz; kliniğinizin bilgisayarında durur.

> Bu kılavuzu programın içinden de okuyabilirsiniz: sol menünün altındaki **Kullanım kılavuzu** bağlantısı. Aynı
> sayfadaki **PDF indir** düğmesiyle yazdırılabilir bir kopyasını alabilirsiniz.

## Kısaca neler yapabilirsiniz

| Bölüm | Neler var |
|---|---|
| Bugün | Günün randevuları, tek tıkla "Geldi / Koltukta / Bitti", bekleme salonu, borçlu hasta uyarısı |
| Randevular | Hekim sütunlu gün görünümü, hafta görünümü, çakışma kontrolü, WhatsApp hatırlatma |
| Hastalar | Hızlı arama (ad, telefon, protokol no; Türkçe harf duyarsız), borçlu ve arşiv süzgeçleri |
| Hasta kartı | Sağlık uyarıları, hesap özeti, ağız şeklinde diş şeması, tedavi planı, notlar, anamnez, fotoğraflar |
| Tedavi ve ödeme | Plan → yapıldı, ödeme alma, makbuz yazdırma, ödeme iptali |
| Reçete | İlaç listesi, şablonlar, alerji uyarısı, A5 yazdırma |
| Bildirimler | Bekleyen, geciken, yarın teyit edilecek hastalar, görevler, doğum günleri, 6 ay kontrolü |
| Kasa | Tarih aralığına göre tahsilat, tedavi, gider, ödeme şekli ve hekime göre dağılım |
| Fiyat listeleri | Birden çok tarife, toplu yüzde güncelleme, KDV |
| Yönetim | Klinik bilgileri, hekimler, randevu türleri, kullanıcılar ve roller |
| Güvenlik | Kullanıcı rolleri, hatalı parolada kilit, passkey ve iki adımlı doğrulama, her gün otomatik yedek |
| Eski program | DentaLine (DentaLineSuit ve DentaLine 2001) verilerini aktarma |

## 1. İlk kurulum

Programı kliniğin bilgisayarına kurmak için [Kurulum rehberi](https://github.com/mehmet-cizmeci/Disci/blob/main/docs/Kurulum-Rehberi.md)'ndeki adımları izleyin.
Kurulum bitince tarayıcıda **ilk kurulum formu** açılır.

![İlk kurulum formu](kilavuz-resimler/01-kurulum.png)

1. **Klinik bilgileri:** klinik adı, adresi, telefonu ve e-postası. Bunlar makbuz ve reçetelerin başında yazar.
2. **Yönetici hesabı:** adınız soyadınız ve giriş adınız olacak e-posta adresiniz.
3. **Hekim adı:** hekimseniz doldurun (ör. *Dt. Ayşe Yılmaz*). Randevu takviminde bu adla bir sütun açılır.
4. **Parola:** en az 8 karakter; büyük harf, küçük harf ve rakam içermeli.
5. **Kurulumu tamamla**'ya basın, ardından aynı e-posta ve parolayla giriş yapın.

> Güvenlik için bu form **yalnızca programın kurulu olduğu bilgisayarda** açılır. Ağdaki başka bir bilgisayardan
> açmaya çalışırsanız uyarı görürsünüz. Form bir kez doldurulduktan sonra bir daha açılmaz.

Kurulumdan sonra sırasıyla şunları yapmanızı öneririz:

1. **Klinik ayarları → Hekimler:** diğer hekimleri, çalışma saatlerini ve takvim renklerini ekleyin.
2. **Klinik ayarları → Randevu türleri:** muayene, dolgu, kanal tedavisi gibi işlemleri süreleriyle ekleyin.
3. **Fiyat listeleri:** fiyatları girin veya güncelleyin.
4. **Kullanıcılar:** asistanlar ve hekimler için hesap açın.

## 2. Programı açma

**Programın kurulu olduğu bilgisayarda:** masaüstündeki **Disci** kısayoluna çift tıklayın. Program arka planda
Windows hizmeti olarak sürekli çalıştığı için ayrıca başlatmanız gerekmez; bilgisayar açıldığında kendiliğinden başlar.

**Klinik ağındaki diğer bilgisayarlarda:** kurulumu yapan kişinin verdiği **Disci-Baglanti** klasöründeki
`Baglanti-Kur.bat` dosyasını bir kez çalıştırın. Masaüstüne **Disci** kısayolu gelir ve tarayıcı güvenlik uyarısı vermez.
Elle girmek isterseniz tarayıcıya sunucu bilgisayarın adını veya IP adresini yazın:

```
https://SUNUCU-ADI:5443
```

**Tablet ve telefonda:** aynı Wi-Fi ağına bağlıyken tarayıcıya yukarıdaki adresi yazın ve ana ekrana ekleyin.
İlk açılışta "bağlantı gizli değil" uyarısı çıkarsa bunun nedeni sertifikanın kliniğe özel olmasıdır; kurulum rehberindeki
"Diğer cihazlar" bölümünde sertifikanın nasıl kurulacağı anlatılır.

## 3. Giriş yapma

![Giriş ekranı](kilavuz-resimler/02-giris.png)

1. **E-posta** kutusuna giriş adınızı yazın.
2. **Parola** kutusuna parolanızı yazın.
3. Kendi bilgisayarınızdaysanız **Beni hatırla**'yı işaretleyebilirsiniz; ortak bilgisayarlarda işaretlemeyin.
4. **Giriş yap**'a basın.

- Hesapları yalnızca **yönetici** açar; kendi kendinize hesap açamazsınız.
- Parolayı **5 kez yanlış** girerseniz hesap bir süre kilitlenir. Biraz bekleyin veya yöneticiye başvurun.
- Parolanızı unuttuysanız yönetici size yeni parola verir (bkz. [Kullanıcılar](#14-kullanıcılar-yalnızca-yönetici)).
- İşiniz bitince, özellikle ortak bilgisayarlarda, menüden **Çıkış** yapın.

**Passkey ile giriş:** parmak izi, yüz tanıma veya Windows PIN'i ile parolasız giriş için önce **Hesabım** sayfasından
passkey ekleyin; sonra giriş ekranında **Passkey ile giriş yap**'a basın.

## 4. Ekran düzeni

- **Sol menü:** Bugün, Randevular, Hastalar, Reçeteler, Bildirimler ve görevler. Yönetici ve hekimlerde ayrıca
  *Kasa ve tahsilat* ile *Fiyat listeleri*; yöneticide *Klinik ayarları* ve *Kullanıcılar*. En altta *Kullanım kılavuzu*,
  *Hesabım* ve *Çıkış*.
- **Üst çubuk:** her sayfadan hasta arama kutusu, **Randevu** düğmesi, **zil** (ilgilenilmesi gereken işlerin sayısı) ve hesabınız.
- **Telefon ve tablette** menü gizlenir; sol üstteki **☰** düğmesiyle açılır.
- **Ekranlar kendiliğinden güncellenir:** resepsiyonda bir hasta "Geldi" yapılınca hekimin ekranında da hemen görünür.

Ekranın altında "Beklenmeyen bir hata oluştu" yazan bir şerit çıkarsa **Yenile**'ye basın. Sorun sürerse
[Sorun giderme](#18-sorun-giderme) bölümüne bakın.

## 5. Bugün (ana sayfa)

![Bugün ekranı](kilavuz-resimler/03-bugun.png)

Giriş yapınca ilk bu ekran açılır; resepsiyonun günlük çalışma ekranıdır.

- **Özet kartları:** bugünkü randevu sayısı, bekleme salonundaki hasta, bugünkü tahsilat; yönetici ve hekimde toplam alacak.
- **Bugünün randevuları:** saat sırasıyla. Satırdaki düğme hastayı bir sonraki adıma taşır:
  **Teyit et → Geldi → Koltuğa al → Bitir**. Gelmeyen hasta için **✕ (Gelmedi)**.
  Borcu olan hastanın adının yanında kırmızı tutar etiketi çıkar; hasta gelince tahsilatı hatırlatmak içindir.
- **Bekleme salonu ve koltuk:** gelen hastalar ve kaç dakikadır beklediği.
- **Yapılacaklar:** bildirimlerin özeti (bkz. [Bildirimler](#10-bildirimler-ve-görevler)).

## 6. Hastalar ve arama

![Hasta listesi](kilavuz-resimler/04-hastalar.png)

Menüden **Hastalar**'a girin veya üst çubuktaki arama kutusunu kullanın.

- Yazmaya başladığınız anda liste süzülür.
- **Ad veya soyad:** Türkçe harfle de harfsiz de yazabilirsiniz; *yılmaz*, *YILMAZ* ve *yilmaz* aynı sonucu verir.
  İki kelime yazarsanız (*ayşe yılmaz*) ikisini de içeren hastalar çıkar.
- **Telefon:** başında 0 olan ya da 7 haneden uzun sayılar telefon sayılır (*0532 111*). Boşluklu yazılmış numaralar da bulunur.
- **Protokol numarası:** 0 ile başlamayan en fazla 6 haneli sayı (*1234*). Eski programdan gelen hastaların eski numaraları
  korunur; DentaLine 2001'den gelen hastalarda o programdaki numara da aranabilir ("#123 · DL2001 #45").
- Sütun başlıklarına tıklayarak sıralayabilirsiniz. Süzgeçler: **Aktif**, **Borçlu** (en çok borcu olan üstte), **Arşiv**, **Tümü**.

**Yeni hasta:** **Yeni hasta** düğmesi. Yalnızca ad ve soyad zorunludur. Aynı TC kimlik numarası, aynı ad-soyad veya
aynı cep telefonuyla kayıtlı biri varsa program uyarır; aynı kişiyse yeni kayıt açmayın. Formda iletişim bilgileri,
adres, yakınlar, sağlık bilgileri (anamnez), tarife ve sabit indirim girilir.

## 7. Hasta kartı

![Hasta kartı](kilavuz-resimler/05-hasta-karti.png)

Kartın üstünde hastanın adı, protokol numarası, yaşı, telefonu, hekimi ve sonraki randevusu görünür.
Telefondan bakıyorsanız numaraya dokunarak arayabilirsiniz.

- **Kırmızı etiketler** (Alerji, Diyabet, Kalp hastalığı…) hastanın sağlık uyarılarıdır. Tedaviden önce mutlaka bakın.
- **Düğmeler:** Randevu ver, Ödeme al, Tedavi ekle, Reçete yaz, Düzenle, WhatsApp, Arşive kaldır.
- **Arşive kaldır:** hasta aktif listeden ve randevu aramasından kalkar; kayıtları silinmez. Geri almak için
  Hastalar → **Arşiv** süzgecinde bulup **Arşivden çıkar**'a basın.

### Hesap özeti

| Alan | Anlamı |
|---|---|
| Toplam ücret | "Yapıldı" durumundaki tedavilerin toplamı |
| Alınan | İptal edilmemiş ödemelerin toplamı |
| Kalan borç | Toplam ücret eksi alınan. Eksi çıkarsa "Fazla ödeme" yazar |
| Bekleyen plan | Planlanmış ama yapılmamış tedavilerin tutarı (borca eklenmez) |

> Bakiye Türk lirası kayıtlarla hesaplanır. Döviz ile girilmiş kayıtlar listede görünür ama bakiyeye katılmaz.

### Sekmeler

- **Hesap:** borç (yapılan tedavi) ve ödeme hareketleri tarih sırasıyla; her satırda o günkü bakiye. Ödeme satırındaki
  yazıcı düğmesi makbuzu açar.
- **Tedaviler:** ağız şeklindeki diş şeması ve altında tedavi planı ile yapılan tedaviler (bkz. [bölüm 8](#8-tedavi-ödeme-ve-makbuz)).
- **Randevular:** hastanın geçmiş ve gelecek randevuları.
- **Reçeteler:** yazılan reçeteler; yazdır, aynısını yeniden yaz, düzenle.
- **Bilgiler:** kimlik, iletişim, adres, kayıt tarihi, tarife, hekimi, referansı ve yakınları.
  *Hekimi* "Ortak havuz" ise hasta belirli bir hekime atanmamıştır.
- **Anamnez:** tıbbi geçmiş; "evet" işaretlenmiş durumlar etiket olarak görünür. Alerji ayrıntıları ve notlar alttadır.
- **Notlar:** hastaya ait genel notlar ve dişe özel notlar; üstteki kutudan yeni not eklenir.
- **Fotoğraflar:** ağız içi ve yüz fotoğrafları; büyütmek için fotoğrafa tıklayın.

## 8. Tedavi, ödeme ve makbuz

![Tedaviler sekmesi ve diş şeması](kilavuz-resimler/06-tedaviler.png)

### Diş şeması

Dişler gerçek ağız kavsi şeklinde ve gerçek boyutlarına oranlı çizilir; hastanın sağı ekranın solundadır. Süt dişleri
iç kavistedir. Renkler: **yeşil** yapıldı, **mavi** planlı, **gri** bulgu. Dişin üstüne gelince ayrıntı çıkar.

Diş numaraları uluslararası (FDI) sisteme göredir:

| | Sağ üst | Sol üst | Sol alt | Sağ alt |
|---|---|---|---|---|
| Kalıcı dişler | 11–18 | 21–28 | 31–38 | 41–48 |
| Süt dişleri | 51–55 | 61–65 | 71–75 | 81–85 |

### Tedavi eklemek

Hasta kartında **Tedavi ekle**:

1. Kayıt türünü seçin: **Plan** (yapılacak), **Yapıldı** (bugün yapıldı, borca eklenir) veya **Bulgu** (ücretsiz tespit:
   kayıp diş, implant mevcut vb.).
2. Diş şemasından dişleri seçin; birden çok seçilebilir. Hiç seçmezseniz tüm ağız sayılır.
3. İşlemi adıyla arayın. Ücret hastanın fiyat listesinden gelir; hastanın sabit indirimi varsa uygulanır. Ücreti değiştirebilirsiniz.
4. **Ekle**'ye basın; başka işlem de ekleyebilirsiniz. Sonra **Kaydet**. Ücret diş başınadır.

**Planı tamamlamak:** Tedaviler sekmesinde plandaki satırları işaretleyip **… işlemi yapıldı say**'a basın. Seçilenler
bugünün tarihiyle "Yapıldı" olur ve borca eklenir. **Plandan çıkar** plandaki kalemi siler. Ücretli bir kaydı yalnızca
yönetici ve hekim silebilir.

### Ödeme almak

**Ödeme al** düğmesi. Tutar kendiliğinden kalan borç olarak gelir; değiştirebilirsiniz. Ödeme şeklini seçin
(Nakit, Kredi kartı, Havale/EFT, Çek/Senet), gerekirse açıklama ("implant 1. taksit") ve makbuz no yazın.

![Ödeme alma penceresi](kilavuz-resimler/07-odeme.png)

**Makbuz:** Hesap sekmesinde ödemenin yanındaki yazıcı düğmesi. Açılan sayfada **Yazdır**'a basın; tutar yazıyla da yazılır.
Makbuzun başındaki klinik bilgileri **Klinik ayarları**'ndan gelir.

**Ödeme iptali** (yalnızca yönetici ve hekim): ödemenin yanındaki ✕. Kayıt silinmez, üstü çizili kalır ve bakiyeye katılmaz.

## 9. Randevular

![Randevu takvimi](kilavuz-resimler/08-randevular.png)

Menüden **Randevular**'a girin.

- **Gün** görünümünde her hekim bir sütundur; **Hafta** görünümünde her gün bir sütun. Hekim kutusundan tek hekime
  süzebilirsiniz. Hekim hesabıyla girilince takvim hekimin kendi sütunuyla açılır.
- **Randevu vermek:** takvimde boş bir saate tıklayın (ya da **Yeni randevu**). Hastayı adı, telefonu veya protokol
  numarasıyla arayıp seçin. Kayıtlı olmayan biri için **Kayıtsız hasta**'yı açıp ad ve telefon yazın. İşlem türü
  seçilince süre kendiliğinden gelir.
- Seçilen saatte hekimin ya da hastanın başka randevusu varsa program **"Bu saat dolu"** der ve kaydetmez.
- **Randevuyu açmak:** üstüne tıklayın. Saat, hekim, not ve **durum** değiştirilebilir. **Hatırlat** düğmesi hastaya
  hazır bir WhatsApp hatırlatma mesajı açar; mesajı siz gönderirsiniz.
- Renkler: gri randevulu, mavi teyitli, turuncu geldi/bekliyor, mor koltukta, yeşil tamamlandı, kırmızı gelmedi.
  Kırmızı yatay çizgi şu anki saattir.

## 10. Bildirimler ve görevler

![Bildirimler](kilavuz-resimler/09-bildirimler.png)

Üst çubuktaki **zil**, şu an ilgilenilmesi gereken işlerin sayısını gösterir. Tamamı **Bildirimler ve görevler** sayfasındadır.

| Bölüm | Ne yapılır |
|---|---|
| Bekleme salonu | 15 dakikadan uzun bekleyen kırmızı görünür; **Koltuğa al** |
| Gecikenler | Randevu saati 10 dakikadan fazla geçmiş ama gelmemiş: arayın, **Geldi** veya **Gelmedi** |
| Yarın teyit edilecekler | Telefonla ya da WhatsApp ile ulaşıp **Teyit et** veya **İptal** |
| Bugün gelen borçlu hastalar | Gelince tahsilatı hatırlayın |
| Görevler | Bugün veya geçmiş tarihli görevler; bitince **Tamamlandı** |
| Doğum günleri | İsterseniz kutlama mesajı gönderin |
| Kontrol zamanı gelenler | Son tedavisinden 6 ay geçmiş, randevusu olmayan hastalar; **Randevu ver** |

**Görev eklemek:** **Görev ekle** → görevi yazın ("laboratuvardan kron gelince hastayı ara"), tarihini, kimin için
olduğunu (Herkes / Asistan / Hekim) ve isterseniz hastayı seçin.

## 11. Reçete

![Reçete yazma](kilavuz-resimler/10-recete.png)

Hasta kartında **Reçete yaz**:

1. Pencerenin üstünde hastanın **ilaç alerjisi** (kırmızı) ve sağlık durumları görünür; önce bunlara bakın.
2. **Hekim:** reçete her zaman bir hekim adına düzenlenir. Asistanlar hekim adına yazabilir; yazan kişi ayrıca saklanır.
3. **Tanı** yazın. Hazır şablon varsa **Şablondan ekle** altındaki düğmeye basın (ör. *Çekim sonrası*); ilaçlar ve tanı
   tek tıkla gelir. Hastanın alerjisiyle eşleşen ilaç içeren şablonlar **kırmızı** ve ⚠ işaretli görünür.
4. **İlaç ekle** kutusuna ilacın adını yazın ve listeden seçin; doz ve kullanım kendiliğinden gelir. Listede olmayan ilaç
   için **Enter**'a basın; "Bu ilacı ilaç listesine de ekle" işaretlenirse sonraki sefer listede çıkar.
5. Her ilaç için kutu sayısını girin, gerekirse not yazın.
6. **Kaydet ve yazdır**: reçete A5 kâğıda göre hazırlanır ve yazdırma penceresi açılır.

> İlaç adı hastanın alerjisiyle eşleşirse (ör. penisilin alerjisi olan hastaya amoksisilin) satır kırmızı çerçevelenir
> ve **"Hastanın alerjisiyle eşleşiyor"** uyarısı çıkar. Program reçeteyi engellemez; uyarı yardımcıdır, bütün ilaç
> etkileşimlerini bilmez. Son karar hekimindir.

**Reçeteler sayfası** (menü): *Yazılan reçeteler* (tarih, hekim, hasta/ilaç adıyla süzme), *Şablonlar* ve *İlaç listesi*
sekmeleri. Şablonları yönetici ve hekimler düzenler, asistanlar kullanır. Şablon listesi boşken **Örnek şablonları ekle**
birkaç örnek ekler; kullanmadan önce hekim olarak kontrol edip kliniğinize göre düzenleyin.

> E-Reçete (Medula) bağlantısı yoktur; bu ekran kâğıt reçete içindir.

## 12. Kasa ve fiyat listeleri (yönetici, hekim)

![Kasa raporu](kilavuz-resimler/11-kasa.png)

**Kasa ve tahsilat:** Bugün, Dün, Bu hafta, Bu ay, Geçen ay, Bu yıl ya da istediğiniz tarih aralığı için tahsilat, yapılan
tedavi, gider ve net tutar; ödeme şekline ve hekime göre dağılım, günlük döküm ve en yüksek alacaklar. **Yazdır** ile çıktı alınır.

**Fiyat listeleri:**

- Soldan listeyi seçin. Fiyatı hücreye yazıp hücreden çıkınca kaydedilir (KDV dahil fiyat). Hücreyi boşaltmak fiyatı kaldırır.
- **Toplu güncelle:** yüzde yazın (ör. `15`; indirim için `-10`). Arama veya kategori süzgeci açıksa yalnızca listelenenler
  değişir. Fiyatlar tam liraya yuvarlanır.
- **Listeyi kopyala:** kuruma veya kampanyaya özel yeni tarife açar. **Varsayılan yap:** tarifesi seçilmemiş hastalarda kullanılır.
- **Yeni işlem:** listede olmayan bir tedaviyi kod, ad ve kategoriyle ekler. İşlem adını düzenleyebilir veya listeden
  kaldırabilirsiniz (kaldırılanlar "Kaldırılanları göster" ile geri alınır).
- Hastaya özel tarife ve sabit indirim hasta kartında **Düzenle**'den seçilir.

## 13. Klinik ayarları (yalnızca yönetici)

![Klinik ayarları](kilavuz-resimler/12-ayarlar.png)

- **Klinik bilgileri:** klinik adı, adres, telefon, e-posta, vergi dairesi ve numarası. Makbuz ve reçetelerin başında yazar.
  Değiştirip **Klinik bilgilerini kaydet**'e basın.
- **Hekimler:** ad, uzmanlık, takvim rengi, mesai başlangıç ve bitişi (takvimin saatleri buna göre ayarlanır), etkin/pasif.
  Pasif hekim takvimde görünmez, eski kayıtlarda adı kalır.
- **Randevu türleri:** ad, süre ve renk (Muayene 20 dk, Dolgu 45 dk, Kanal tedavisi 60 dk gibi).

## 14. Kullanıcılar (yalnızca yönetici)

![Kullanıcılar](kilavuz-resimler/13-kullanicilar.png)

### Roller

| Rol | Kimler için | Neler yapabilir |
|---|---|---|
| Yönetici | Klinik sahibi, sorumlu kişi | Her şey: ayarlar, kullanıcılar, kasa, fiyatlar, ödeme iptali |
| Hekim | Diş hekimleri | Hasta, tedavi, ödeme, reçete, kasa ve fiyat listeleri, ödeme iptali |
| Asistan | Asistan ve sekreterler | Randevu, hasta kaydı, tedavi ve ödeme girişi, hekim adına reçete |

### Yeni kullanıcı açma

1. **Ad soyad** ve **E-posta** yazın; e-posta kişinin giriş adı olur.
2. **Rol** seçin.
3. **Hekim kaydı:** hekim hesabı açıyorsanız listeden hekimi seçin veya **Yeni hekim kaydı oluştur**'u seçin.
   Asistan ve yöneticiler için genellikle **Yok**.
4. **İlk parola** yazın (en az 8 karakter, büyük/küçük harf ve rakam) ve **Kullanıcı oluştur**'a basın.
5. Giriş adını ve parolayı kişiye **yüz yüze** iletin; ilk girişte parolasını değiştirmesini söyleyin.

**Parola sıfırlama:** kullanıcının satırında **Parola** → yeni parola → **Kaydet**.

**Hesabı kapatma:** işten ayrılan birinin hesabını silmek yerine **Devre dışı bırak**'a basın. Kişi hemen sistemden
çıkarılır ve bir daha giremez; geçmiş kayıtları korunur. **Etkinleştir** ile geri açılır. Kendi hesabınızı kapatamazsınız.

## 15. Hesabım ve parola değiştirme

Menünün altındaki **Hesabım**:

- **Parola değiştirme:** mevcut parolanızı ve iki kez yeni parolanızı yazıp kaydedin.
- **Passkey ekleme:** bilgisayarınızın veya telefonunuzun parmak izi, yüz tanıma ya da PIN doğrulamasıyla parolasız giriş.
- **İki adımlı doğrulama:** telefondaki bir doğrulama uygulamasıyla (Google veya Microsoft Authenticator) ek güvenlik.

> Hesap ayarları sayfalarının bir kısmı henüz İngilizcedir.

**İlk girişte** yöneticinin verdiği parolayı mutlaka değiştirin.

## 16. Yedekleme ve geri yükleme

Kurulum, **her gün saat 14:00'te** otomatik yedek alacak şekilde ayarlanır (bilgisayarın açık olduğu bir saat seçildi):

- Veritabanı ve hasta fotoğrafları `C:\DisciVeri\Yedekler` klasörüne yedeklenir; **son 14 gün** saklanır.
- Bilgisayar o saatte kapalıysa yedek, açıldığında alınır.
- Elle yedek almak için yönetici PowerShell'de: `C:\Disci\betikler\yedekle.ps1`

> **Yedekleri başka bir yere de kopyalayın.** Bilgisayar bozulur veya çalınırsa aynı bilgisayardaki yedekler de
> gider. Haftada bir `C:\DisciVeri\Yedekler` klasörünü harici bir diske veya USB belleğe kopyalayın ya da bu klasörü
> OneDrive / Google Drive ile eşitleyin.

**Geri yükleme:** bir yedeğe dönmek gerekirse [Kurulum rehberi](https://github.com/mehmet-cizmeci/Disci/blob/main/docs/Kurulum-Rehberi.md)'ndeki "Yedekten geri yükleme"
adımlarını izleyin. Geri yükleme, yedekten sonra girilen kayıtları siler; emin değilseniz önce yeni bir yedek alın.

## 17. Eski programdan veri aktarma

Disci, **DentaLine** programlarının (DentaLineSuit 2.5 ve DentaLine 2001 2.3) verilerini aktarabilir: hastalar,
tedaviler, ödemeler, anamnez, notlar, diş notları, fotoğraflar, fiyat listeleri ve ilaçlar.

- Aktarım kurulum sırasında veya sonradan yönetici tarafından yapılır (bkz. [Kurulum rehberi](https://github.com/mehmet-cizmeci/Disci/blob/main/docs/Kurulum-Rehberi.md)).
- Eski programdaki hasta numarası **protokol no** olarak korunur ve aranabilir.
- Eski programlarda adlar çoğunlukla Türkçe harfsiz yazılmıştır ("Ayse", "CELIK"). Aktarım yaygın ad ve soyadları doğru
  yazımına çevirir ("Ayşe", "ÇELİK"); listede olmayanlar olduğu gibi kalır, hasta kartındaki **Düzenle** ile düzeltilebilir.
- İki programdan birden aktarılıyorsa aynı kişi ad, telefon ve doğum tarihiyle bulunup **tek karta** birleştirilir; iki
  programa birden girilmiş tedavi ve ödemeler bir kez yazılır. Aynı adı taşıyan ama doğum tarihi çok farklı olan kişiler
  ayrı kalır.
- Eski programın kullanıcı adları ve şifreleri güvenlik nedeniyle aktarılmaz; herkes için yeni hesap açılır.
- Eski programlar tedaviyi yapan hekimi tutmadığı için aktarılan tedavilerde hekim adı yoktur.
- Kartı silinmiş ama kayıtları kalmış hastalar "(Eski kayıt *numara*)" adıyla arşivde durur.

## 18. Sorun giderme

**"Bu siteye ulaşılamıyor" diyor.**
Sunucu bilgisayar kapalı veya uykuda olabilir. Açın ve bir dakika bekleyin. Hâlâ açılmıyorsa sunucu bilgisayarda
*Hizmetler* (Windows tuşu → "Hizmetler") içinde **Disci Klinik Programı**'nın "Çalışıyor" olduğuna bakın; değilse sağ
tıklayıp **Başlat**'a basın.

**Diğer bilgisayardan açılıyor ama sunucuda açılmıyor (ya da tersi).**
Sunucunun ağ bağlantısının **Özel ağ** olduğundan emin olun (Ayarlar → Ağ ve İnternet → bağlantı → Ağ profili türü: Özel).
Ortak ağda Windows güvenlik duvarı diğer cihazları engeller.

**Sunucunun IP adresi değişti, kısayol açılmıyor.**
Modem her açılışta farklı adres verebilir. Kısayolu bilgisayar adıyla kullanın veya modemden sunucuya sabit IP verin
(bkz. kurulum rehberi).

**"E-posta veya parola yanlış" diyor.**
Büyük/küçük harfe ve klavye dilinin doğru olduğuna dikkat edin. 5 yanlış denemeden sonra hesap kilitlenir.

**Bir hastayı bulamıyorum.**
Soyadın bir kısmını veya telefonun son 4 rakamını yazın; listede **Arşiv** ve **Tümü** süzgeçlerini de deneyin.

**Ekranda "Beklenmeyen bir hata oluştu" şeridi çıkıyor, düğmeler çalışmıyor.**
Sayfayı yenileyin (F5). Yalnızca bir tarayıcıda oluyorsa o tarayıcının eklentilerinden biri (özellikle WebSocket veya
reklam engelleyici eklentiler) programla çakışıyor olabilir; başka bir tarayıcıyla deneyin.

**Yanlış girilen ödemeyi nasıl silerim?**
Ödemeler silinmez; yönetici veya hekim Hesap sekmesinden **iptal eder**. İptal edilen ödeme üstü çizili kalır ve bakiyeye katılmaz.

**Yönetici parolasını unuttum ve başka yönetici yok.**
Kurulum rehberindeki "Yönetici parolasını sıfırlama" bölümüne bakın.

---

Disci ücretsiz ve açık kaynak kodludur (MIT lisansı). Kaynak kod, güncellemeler ve sorun bildirimi:
<https://github.com/mehmet-cizmeci/Disci>
