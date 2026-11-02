# Disci kullanım kılavuzu

Disci, kliniğimizin eski DentaLineSuit programının yerini alacak yeni hasta takip sistemidir. Bilgisayar,
tablet veya telefondaki internet tarayıcısından (Chrome, Edge, Safari) kullanılır; ayrı bir program kurmak gerekmez.

Eski programdaki bütün hastalar, tedaviler, ödemeler, anamnezler, notlar ve fotoğraflar Disci'ye aktarılmıştır.

> **Bu sürümde:** randevu takvimi, resepsiyon panosu (bekleme salonu), yeni hasta ekleme ve düzenleme, arşiv,
> tedavi planı ve yapılan tedavi girişi, ödeme alma ve makbuz, reçete, bildirimler ve görevler, kasa raporu ve fiyat listeleri var.
> Hasta kartından reçete yazılıp A5 kâğıda yazdırılabilir.

---

## İçindekiler

1. [Programı açma](#1-programı-açma)
2. [Giriş yapma](#2-giriş-yapma)
3. [Ekran düzeni](#3-ekran-düzeni)
4. [Ana sayfa (Bugün)](#4-ana-sayfa-bugün)
5. [Hasta arama](#5-hasta-arama)
6. [Hasta kartı](#6-hasta-kartı)
7. [Randevular](#7-randevular)
8. [Tedavi girme, ödeme alma, makbuz](#8-tedavi-girme-ödeme-alma-makbuz)
9. [Reçete](#9-reçete)
10. [Bildirimler ve görevler](#10-bildirimler-ve-görevler)
11. [Kasa ve fiyat listeleri (yönetici, hekim)](#11-kasa-ve-fiyat-listeleri-yönetici-hekim)
12. [Klinik ayarları (yalnızca yönetici)](#12-klinik-ayarları-yalnızca-yönetici)
13. [Hesabım ve parola değiştirme](#13-hesabım-ve-parola-değiştirme)
14. [Kullanıcı yönetimi (yalnızca yönetici)](#14-kullanıcı-yönetimi-yalnızca-yönetici)
15. [Eski verilerle ilgili bilmeniz gerekenler](#15-eski-verilerle-ilgili-bilmeniz-gerekenler)
16. [Sık sorulan sorular](#16-sık-sorulan-sorular)
17. [Ek: Eski DentaLineSuit programı](#ek-eski-dentalinesuit-programı)

---

## 1. Programı açma

Disci, klinikteki ana bilgisayarda çalışır. O bilgisayarda program açıkken tarayıcıya şu adresi yazın:

```
http://localhost:5111
```

Adresi tarayıcının yer imlerine (sık kullanılanlar) eklemenizi öneririz.

> Şimdilik Disci'ye yalnızca ana bilgisayardan girilebilir. Tablet ve telefonlardan erişim ağ ayarı yapıldıktan sonra açılacak.

## 2. Giriş yapma

1. **E-posta** kutusuna size verilen giriş adını yazın (örneğin `asistan@klinik.ornek`).
2. **Parola** kutusuna parolanızı yazın.
3. Kendi bilgisayarınızdaysanız **Beni hatırla** kutusunu işaretleyebilirsiniz; ortak kullanılan bir bilgisayarda işaretlemeyin.
4. **Giriş yap** düğmesine basın.

**Önemli kurallar**

- Hesapları yalnızca klinik yöneticisi açar. Kendi kendinize hesap açamazsınız.
- Parolayı **5 kez yanlış** girerseniz hesabınız bir süre kilitlenir. Biraz bekleyip tekrar deneyin veya yöneticiye başvurun.
- Parolanızı unuttuysanız yöneticiye söyleyin; size yeni bir parola verir.
- İşiniz bitince, özellikle ortak bilgisayarlarda, menüden **Çıkış** yapın.

**Passkey ile giriş:** Parmak izi, yüz tanıma veya Windows PIN'i ile parolasız giriş yapmak istiyorsanız önce
[Hesabım](#13-hesabım-ve-parola-değiştirme) sayfasından passkey ekleyin. Sonra giriş ekranında **Passkey ile giriş yap**'a basın.

## 3. Ekran düzeni

- **Sol menü:** Bugün, Randevular, Hastalar, Reçeteler, Bildirimler ve görevler; yönetici ve hekimlerde ayrıca Kasa ve tahsilat,
  Fiyat listeleri; yöneticide Klinik ayarları ve Kullanıcılar. En altta Hesabım ve Çıkış.
- **Üst çubuk:** her sayfadan hasta arama kutusu, **Randevu** düğmesi, **zil** (bekleyen işlerin sayısı) ve hesabınız.
- **Telefon ve tablette** menü gizlenir; sol üstteki **☰** düğmesine basarak açılır.
- Ekranlar kendiliğinden güncellenir: resepsiyonda hasta "Geldi" yapılınca hekimin ekranında da hemen görünür.

Ekranda "Beklenmeyen bir hata oluştu" yazan sarı bir şerit çıkarsa **Yenile**'ye basın. Sorun sürerse yöneticiye haber verin.

## 4. Ana sayfa (Bugün)

Giriş yaptığınızda ilk bu ekran açılır; resepsiyonun günlük çalışma ekranıdır.

- **Özet kartları:** bugünkü randevu sayısı, bekleme salonundaki hasta sayısı, bugünkü tahsilat; yönetici ve hekimde
  toplam alacak, asistanda kayıtlı hasta sayısı.
- **Bugünün randevuları:** saat sırasıyla. Satırdaki düğme hastayı bir sonraki adıma taşır:
  **Teyit et → Geldi → Koltuğa al → Bitir**. Gelmeyen hasta için **✕ (Gelmedi)**. Borcu olan hastanın adının yanında kırmızı
  tutar etiketi çıkar; hasta gelince tahsilatı hatırlatmak içindir.
- **Bekleme salonu ve koltuk:** gelen hastalar ve kaç dakikadır beklediği.
- **Yapılacaklar:** bildirimlerin özeti (bkz. [bölüm 10](#10-bildirimler-ve-görevler)).

## 5. Hasta arama

Menüden **Hastalar**'a girin.

- Arama kutusuna yazmaya başladığınız anda liste süzülür; düğmeye basmanız gerekmez.
- Neye göre arayabilirsiniz:
  - **Ad veya soyad:** `deneme` ya da `hastaoğlu`. İki kelime yazarsanız (`deneme hastaoğlu`) ikisini de içeren hastalar çıkar.
  - **Telefon:** En az 4 rakam yazın (`5321`). Boşluklu yazılmış numaralar da bulunur.
  - **Protokol numarası:** Eski programdaki hasta numarası (`1234`).
- Sütun başlıklarına (**Hasta**, **Yaş**, **Son tedavi**, **Kalan**) tıklayarak listeyi sıralayabilirsiniz.
- Liste sayfa sayfa gösterilir (sayfada 25 hasta); en alttaki düğmelerle sayfalar arasında gezin.
- Üstteki süzgeçler: **Aktif**, **Borçlu** (en çok borcu olan en üstte), **Arşiv** (bkz. [bölüm 15](#15-eski-verilerle-ilgili-bilmeniz-gerekenler)), **Tümü**.
- **Yeni hasta** düğmesiyle kayıt açılır. Yalnızca ad ve soyad zorunludur. Aynı TC kimlik numarası ya da aynı ad-soyad veya
  cep telefonuyla kayıtlı biri varsa program uyarır; aynı kişiyse yeni kayıt açmayın.

Hastanın adına tıklayınca hasta kartı açılır.

## 6. Hasta kartı

Kartın üstünde hastanın adı, protokol numarası, yaşı, telefonu, hekimi ve sonraki randevusu görünür. Telefondan
bakıyorsanız numaraya dokunarak doğrudan arayabilirsiniz.

- **Kırmızı etiketler** (Dikkat, Alerji, Diyabet…) hastanın sağlık uyarılarıdır. Tedaviden önce mutlaka bakın.
- **Düğmeler:** Randevu ver, Ödeme al, Tedavi ekle, Reçete yaz, Düzenle, WhatsApp (mesaj penceresini açar), Arşive kaldır.
- **Arşive kaldır:** hasta aktif listeden ve randevu aramasından kalkar; tedavi ve ödemeleri silinmez. Geri almak için
  hastayı Hastalar → Arşiv süzgecinde bulup **Arşivden çıkar**'a basın.

### Hesap özeti

| Alan | Anlamı |
|---|---|
| Toplam ücret | "Yapıldı" durumundaki tedavilerin toplam tutarı |
| Alınan | İptal edilmemiş ödemelerin toplamı |
| Kalan borç | Toplam ücret eksi alınan. Eksi çıkarsa "Fazla ödeme" yazar |
| Bekleyen plan | Planlanmış ama henüz yapılmamış tedavilerin tutarı (borca eklenmez) |

> Bakiye yalnızca Türk lirası kayıtlarla hesaplanır. Eski sistemde dolar ile girilmiş birkaç kayıt listede görünür ama bakiyeye katılmaz.

### Sekmeler

**Hesap**: Borç (yapılan tedavi) ve ödeme hareketleri tarih sırasıyla; her satırda o günkü bakiye. Ödeme satırındaki
yazıcı düğmesi makbuzu açar.

**Tedaviler**: Üstte diş şeması (yeşil: yapıldı, mavi: planlı, gri: bulgu; dişin üstüne gelince ayrıntı çıkar),
altında tedavi planı ve yapılan tedaviler.

| Durum | Anlamı |
|---|---|
| **Yapıldı** (yeşil) | Tamamlanmış ve ücretlendirilmiş tedavi |
| **Plan** (mavi) | Planlanmış, henüz yapılmamış tedavi |
| **Bulgu** (gri) | Muayenede tespit edilen durum (kayıp diş, implant, çene kırığı vb.); ücreti yoktur |

Diş numaraları uluslararası sisteme göredir:

| | Sağ üst | Sol üst | Sol alt | Sağ alt |
|---|---|---|---|---|
| Kalıcı dişler | 11–18 | 21–28 | 31–38 | 41–48 |
| Süt dişleri | 51–55 | 61–65 | 71–75 | 81–85 |

**Randevular**: Hastanın geçmiş ve gelecek randevuları, durumlarıyla.

**Bilgiler**: Kimlik ve iletişim bilgileri, adresler, kayıt tarihi, tarife, hekimi, referansı, yakınları ve hasta notu.
- *Tarife*: Hastanın bağlı olduğu fiyat listesi (Güncel tarife veya Eski tarife).
- *Hekimi*: "Ortak havuz" yazıyorsa hasta belirli bir hekime atanmamıştır; bütün hekimler görebilir.

**Anamnez**: Hastanın tıbbi geçmişi. Sarı etiketler hastada "evet" işaretlenmiş durumlardır (örneğin Diyabet,
Hipertansiyon, İlaç alerjisi). Etiketler gruplara ayrılmıştır: Tıbbi geçmiş, Kullandığı ilaçlar, Alışkanlıklar, Alerji ve ortodonti muayene bulguları.
Alerji ayrıntıları ve notlar alttadır.

> Tedaviye başlamadan önce Anamnez sekmesini kontrol etmeyi alışkanlık edinin.

**Notlar**: Hastaya ait genel notlar ve dişe özel notlar. Üstteki kutudan yeni not eklenir.

**Fotoğraflar**: Ağız içi kamera ve yüz fotoğrafları; tarih, tür ve varsa diş numarasıyla. Büyük görmek için fotoğrafa tıklayın; yeni sekmede açılır.

> Röntgen görüntüleri eski programda bu bilgisayarda kayıtlı değildi; bu yüzden Disci'de de yok.

## 7. Randevular

Menüden **Randevular**'a girin.

- **Gün** görünümünde her hekim bir sütundur; **Hafta** görünümünde her gün bir sütun. Hekim kutusundan tek hekime süzebilirsiniz.
  Hekim hesabıyla girildiğinde takvim hekimin kendi sütunuyla açılır.
- **Randevu vermek:** takvimde boş bir saate tıklayın (ya da **Yeni randevu**). Hastayı adı veya telefonuyla arayıp seçin;
  kayıtlı olmayan biri için **Kayıtsız hasta**'yı açıp ad ve telefon yazın. İşlem türü seçilince süre kendiliğinden gelir.
- Seçilen saatte hekimin ya da hastanın başka randevusu varsa program **"Bu saat dolu"** der ve kaydetmez.
- **Randevuyu açmak:** üstüne tıklayın. Saat, hekim, not ve **durum** değiştirilebilir. **Hatırlat** düğmesi hastaya hazır bir
  WhatsApp hatırlatma mesajı açar; mesajı siz gönderirsiniz.
- Renkler randevunun durumunu gösterir: gri randevulu, mavi teyitli, turuncu geldi/bekliyor, mor koltukta, yeşil tamamlandı,
  kırmızı gelmedi. Kırmızı yatay çizgi şu anki saattir.

## 8. Tedavi girme, ödeme alma, makbuz

**Tedavi eklemek** (hasta kartında **Tedavi ekle**):

1. Kayıt türünü seçin: **Plan** (yapılacak), **Yapıldı** (bugün yapıldı, borca eklenir) veya **Bulgu** (ücretsiz tespit).
2. Diş şemasından dişleri seçin (birden çok olabilir; hiç seçmezseniz tüm ağız sayılır).
3. İşlemi adıyla arayın; ücret hastanın fiyat listesinden gelir, hastanın sabit indirimi varsa uygulanır. Ücreti değiştirebilirsiniz.
4. **Ekle**'ye basın; başka işlem de ekleyebilirsiniz. Sonra kaydedin. Ücret diş başınadır.

**Planı tamamlamak:** Tedaviler sekmesinde plandaki satırları işaretleyip **… işlemi yapıldı say**'a basın. Seçilenler bugünün
tarihiyle "Yapıldı" olur ve hastanın borcuna eklenir. **Plandan çıkar** plandaki kalemi siler.

**Ödeme almak** (**Ödeme al**): tutar kendiliğinden kalan borç olarak gelir; değiştirebilirsiniz. Ödeme şeklini seçin
(Nakit, Kredi kartı, Havale/EFT, Çek/Senet), gerekirse açıklama ("implant 1. taksit") ve makbuz no yazın.

**Makbuz:** Hesap sekmesinde ödemenin yanındaki yazıcı düğmesi. Açılan sayfada **Yazdır**'a basın. Tutar yazıyla da yazılır.

**Ödeme iptali** (yalnızca yönetici ve hekim): ödemenin yanındaki ✕. Kayıt silinmez, üstü çizili kalır ve bakiyeye katılmaz.

## 9. Reçete

**Reçete yazmak** (hasta kartında **Reçete yaz**):

1. Pencerenin üstünde hastanın **ilaç alerjisi** (kırmızı) ve sağlık durumları (sarı) görünür; önce bunlara bakın.
2. **Hekim:** reçete her zaman bir hekim adına düzenlenir. Asistanlar hekim adına yazabilir; reçeteyi kimin yazdığı ayrıca saklanır.
3. **Tanı** yazın (listeden seçebilir ya da kendiniz yazabilirsiniz). Hazır bir şablon varsa **Şablondan ekle** altındaki
   düğmeye basın (örneğin *Çekim sonrası*): ilaçlar ve tanı tek tıkla gelir, sonra istediğinizi değiştirebilirsiniz.
   Hastanın alerjisiyle eşleşen ilaç içeren şablonlar **kırmızı** ve ⚠ işaretli görünür.
4. **İlaç ekle** kutusuna ilacın adını yazın ve listeden seçin; doz ve kullanım kendiliğinden gelir, değiştirebilirsiniz.
   Listede olmayan bir ilaç için **Enter**'a basın; "Bu ilacı ilaç listesine de ekle" işaretlenirse sonraki sefer listede çıkar.
5. Her ilaç için kutu sayısını girin, gerekirse hastaya not yazın.
6. **Kaydet ve yazdır**'a basın; reçete sayfası açılır ve yazdırma penceresi gelir. Reçete A5 kâğıda göre hazırlanır.

> İlaç adı hastanın alerjisiyle eşleşirse (örneğin penisilin alerjisi olan hastaya Augmentin veya amoksisilin) satır kırmızı
> çerçevelenir ve **"Hastanın alerjisiyle eşleşiyor"** uyarısı çıkar. Program reçeteyi engellemez; hekimle mutlaka teyit edin.
> Bu uyarı yardımcıdır, bütün ilaç etkileşimlerini bilmez.

**Reçete şablonları:** Sık yazdığınız reçeteleri şablon olarak saklayın. Yöneticiler ve hekimler bir reçeteyi yazarken
pencerenin altındaki **Şablon olarak kaydet**'e basıp ad vererek ya da **Reçeteler → Şablonlar** sekmesinden **Şablon ekle** ile
şablon oluşturur; aynı yerden düzenler, sırasını değiştirir (◀ ▶), pasif yapar veya siler. Asistanlar şablonları kullanabilir ama
değiştiremez. Şablon listesi boşken **Örnek şablonları ekle** düğmesi birkaç örnek (çekim sonrası, cerrahi sonrası, apse,
penisilin alerjisinde enfeksiyon, aft) ekler; örnekleri kullanmadan önce hekim olarak kontrol edip kliniğinize göre düzenleyin.

**Reçeteler sekmesi** (hasta kartı): yazılan reçeteler; **yazdır**, **aynısını yeniden yaz** (aynı ilaçlarla yeni reçete açar),
**düzenle**. Silme yalnızca yönetici ve hekimdedir.

**Reçeteler sayfası** (menü): **Yazılan reçeteler**, **Şablonlar** ve **İlaç listesi** sekmeleri. Yazılan reçeteler bütün hastalara yazılanlardır; tarih, hekim ve hasta/ilaç adıyla süzülür. "Yazan" sütununda
asistanın hekim adına yazdığı reçeteler **hekim adına** etiketiyle görünür. **İlaç listesi** sekmesinden ilaç eklenir, doz ve
varsayılan kullanımı düzenlenir; artık kullanılmayan ilaç **pasif** yapılır (eski reçetelerde görünmeye devam eder).

> E-Reçete (Medula) bağlantısı yoktur; bu ekran kâğıt reçete içindir.

## 10. Bildirimler ve görevler

Üst çubuktaki **zil**, şu an ilgilenilmesi gereken işlerin sayısını gösterir. Tamamı **Bildirimler ve görevler** sayfasındadır:

| Bölüm | Ne yapılır |
|---|---|
| Bekleme salonu | 15 dakikadan uzun bekleyen kırmızı görünür; **Koltuğa al** |
| Gecikenler | Randevu saati 10 dakikadan fazla geçmiş ama gelmemiş: arayın, **Geldi** veya **Gelmedi** |
| Yarın teyit edilecek randevular | Telefonla ya da WhatsApp düğmesiyle ulaşıp **Teyit et** veya **İptal** |
| Bugün gelen borçlu hastalar | Gelince tahsilatı hatırlayın |
| Görevler | Bugün veya geçmiş tarihli görevler; bitince **Tamamlandı** |
| Doğum günleri | İsterseniz kutlama mesajı gönderin |
| Kontrol zamanı gelenler | Son tedavisinin üzerinden 6 ay geçmiş ve randevusu olmayan hastalar; **Randevu ver** |

**Görev eklemek:** **Görev ekle** → görevi yazın ("laboratuvardan kron gelince hastayı ara"), tarihini, kimin için olduğunu
(Herkes / Asistan / Hekim) ve isterseniz hastayı seçin.

## 11. Kasa ve fiyat listeleri (yönetici, hekim)

**Kasa ve tahsilat:** Bugün, Dün, Bu hafta, Bu ay, Geçen ay, Bu yıl ya da istediğiniz tarih aralığı için tahsilat, yapılan
tedavi, gider ve net tutar; ödeme şekline ve hekime göre dağılım, günlük döküm ve en yüksek alacaklar. **Yazdır** ile çıktısı alınır.

**Fiyat listeleri:**

- Soldan listeyi seçin. Fiyatı hücreye yazıp hücreden çıkınca kaydedilir (KDV dahil fiyat). Hücreyi boşaltmak fiyatı kaldırır.
- **Toplu güncelle:** yüzde yazın (örneğin `15`; indirim için `-10`) ve onaylayın. Arama veya kategori süzgeci açıksa
  yalnızca listelenen işlemler değişir. Fiyatlar tam liraya yuvarlanır.
- **Listeyi kopyala:** kuruma veya kampanyaya özel yeni bir tarife açar. **Varsayılan yap:** tarifesi seçilmemiş hastalarda kullanılan liste.
- **Yeni işlem:** listede olmayan bir tedaviyi kod, ad ve kategoriyle ekler.
- Hastaya özel tarife ve sabit indirim, hasta kartında **Düzenle** → *Klinik ve ücret* bölümünden seçilir.

## 12. Klinik ayarları (yalnızca yönetici)

- **Hekimler:** ad, uzmanlık, takvim rengi, mesai başlangıç ve bitişi (takvimin gösterdiği saatler buna göre ayarlanır),
  etkin/pasif. Pasif hekim takvimde görünmez.
- **Randevu türleri:** ad, süre ve renk (Muayene 20 dk, Dolgu 45 dk, Kanal tedavisi 60 dk gibi).
- Makbuzdaki klinik adı, adresi ve telefonu şimdilik program ayar dosyasından (`appsettings.json` → `Clinic`) değiştirilir.

## 13. Hesabım ve parola değiştirme

Menüden **Hesabım**'a girin.

> Hesap ayarları sayfalarının bir kısmı henüz İngilizcedir; Türkçeleştirme sonraki sürümde yapılacak.

- **Parola değiştirme:** *Password* bölümünde mevcut parolanızı (*Current password*) ve iki kez yeni parolanızı
  (*New password*, *Confirm new password*) yazıp **Update password**'e basın.
  Parola en az 8 karakter olmalı; büyük harf, küçük harf ve rakam içermelidir.
- **Passkey ekleme:** *Passkeys* bölümünde **Add a new passkey**'e basın ve bilgisayarınızın veya telefonunuzun
  istediği doğrulamayı (parmak izi, yüz, PIN) yapın.
- **İki adımlı doğrulama:** *Two-factor authentication* bölümünden telefonunuzdaki bir doğrulama uygulamasıyla
  (Google Authenticator, Microsoft Authenticator) ek güvenlik açabilirsiniz.

**İlk girişte** yöneticinin size verdiği parolayı mutlaka değiştirin.

## 14. Kullanıcı yönetimi (yalnızca yönetici)

Menüden **Kullanıcılar**'a girin. Bu sayfayı yalnızca *Yönetici* rolündekiler görür.

### Roller

| Rol | Kimler için | Neler yapabilir |
|---|---|---|
| Yönetici | Klinik sahibi, sorumlu kişi | Her şey; kullanıcı açma, parola sıfırlama, hesap kapatma |
| Diş hekimi | Hekimler | Hasta kartları, tedaviler, ödemeler |
| Asistan | Asistan ve sekreterler | Hasta kartları, tedaviler, ödemeler. İleride randevu ve hekim adına reçete |

### Yeni kullanıcı açma

1. Sayfanın altındaki **Yeni kullanıcı** bölümüne gelin.
2. **Ad soyad** ve **E-posta** yazın. E-posta, kişinin giriş adı olur.
3. **Rol** seçin.
4. **Hekim kaydı** seçin:
   - Hekim hesabı açıyorsanız listeden hekimi seçin (eski programdan gelen hekimler listededir).
     Listede yoksa **Yeni hekim kaydı oluştur**'u seçin.
   - Asistan ve yöneticiler için genellikle **Yok** seçilir.
5. **İlk parola** yazın (en az 8 karakter, büyük/küçük harf ve rakam).
6. **Kullanıcı oluştur**'a basın.
7. Giriş adını ve parolayı kişiye **yüz yüze** iletin; ilk girişte parolasını değiştirmesini söyleyin.

### Parola sıfırlama

Kullanıcının satırında **Parola**'ya basın, yeni parolayı yazıp **Kaydet**'e basın.

### Hesabı devre dışı bırakma

İşten ayrılan birinin hesabını silmek yerine **Devre dışı bırak**'a basın. Kişi hemen sistemden çıkarılır ve bir daha
giriş yapamaz; geçmişteki kayıtları korunur. Gerekirse **Etkinleştir** ile hesabı geri açabilirsiniz.
Kendi hesabınızı devre dışı bırakamazsınız.

## 15. Eski verilerle ilgili bilmeniz gerekenler

- **Protokol numarası:** Her hastanın eski programdaki numarası korunmuştur; arama yaparken kullanabilirsiniz.
- **Ad ve soyad:** Eski programda tek kutuda tutulan adın son kelimesi soyad kabul edildi. Hatalı ayrılmış bir isim görürseniz yöneticiye bildirin.
- **Arşivdeki kayıt:** Eski programda kartı silinmiş ama tedavi kayıtları kalmış hastalar "(Eski kayıt *numara*)" adıyla arşivde durur. Kayıtlar kaybolmasın diye tutuldu.
- **Tedaviyi yapan hekim:** Eski program bu bilgiyi tutmadığı için aktarılan tedavilerde hekim adı yok.
- **KDV:** Eski programda bazı tedavilerin KDV bilgisi bozuktu; bunlar 0 olarak aktarıldı. **Tedavi tutarları değişmedi.**
- **Eski kullanıcılar:** Eski programın kullanıcı adları ve şifreleri güvenlik nedeniyle aktarılmadı. Herkes için yeni hesap açılır.
- **Geçiş günü:** Disci'ye tamamen geçmeden önce eski programa girilen son kayıtlar, yönetici tarafından bir kez daha aktarılacak. O güne kadar yeni kayıtları eski programa girmeye devam edin.

## 16. Sık sorulan sorular

**Sayfa açılmıyor, "bu siteye ulaşılamıyor" diyor.**
Disci ana bilgisayarda çalışmıyor olabilir. Yöneticiye haber verin.

**"E-posta veya parola yanlış" diyor.**
Büyük/küçük harfe ve klavye dilinin (Türkçe Q) doğru olduğuna dikkat edin. 5 yanlış denemeden sonra hesap kilitlenir.

**Giriş yaptım ama "Kullanıcılar" menüsünü göremiyorum.**
Bu menü yalnızca yöneticilere görünür.

**Bir hastayı bulamıyorum.**
Soyadın yalnızca bir kısmını veya telefonun son 4 rakamını yazmayı deneyin. Türkçe harflerle (ı, ş, ğ) ve harfsiz (i, s, g) yazmayı ayrı ayrı deneyin; eski kayıtlarda isimler farklı yazılmış olabilir. Listede **Arşiv** süzgecini de deneyin.

**Bakiye eski programdakinden farklı.**
Disci bakiyeyi tedavi ve ödeme kayıtlarından her seferinde yeniden hesaplar. Eski programın sakladığı borç toplamı bazen güncel değildi. Fark görürseniz hastanın Hesap sekmesindeki hareketleri kontrol edip yöneticiye bildirin.

**Telefonumdan girebilir miyim?**
Şimdilik hayır. Ağ ayarı yapıldıktan sonra klinik ağındaki tablet ve telefonlardan da girilebilecek.

**Yeni hasta veya tedavi nasıl eklerim?**
Yeni hasta: Hastalar → **Yeni hasta** (bkz. [bölüm 5](#5-hasta-arama)). Tedavi ve ödeme: hasta kartındaki **Tedavi ekle** ve **Ödeme al** (bkz. [bölüm 8](#8-tedavi-girme-ödeme-alma-makbuz)).

**Yanlış girilen ödemeyi nasıl silerim?**
Ödemeler silinmez; yönetici veya hekim hasta kartının Hesap sekmesinden **iptal eder**. İptal edilen ödeme üstü çizili kalır ve bakiyeye katılmaz.

---

## Ek: Eski DentaLineSuit programı

Disci'ye tamamen geçilene kadar eski program kullanılmaya devam edilir.

### Açma

- Masaüstündeki veya `C:\Yazılım\DentaLineSuit` klasöründeki **DentaLineSuit** ile açın.
- Her açılışta Windows "Bu uygulamanın cihazınızda değişiklik yapmasına izin veriyor musunuz?" diye sorar: **Evet** deyin.
  Program klasörlerini ağda paylaşabilmek için yönetici yetkisine ihtiyaç duyar.
- Açılışta "Posta Güvercini (Turkcell SMS Servisi) başlatılamadı" uyarısı çıkabilir; **Tamam** deyip devam edin. Yalnızca SMS özelliğini etkiler.

### Sık görülen hatalar

| Mesaj | Ne yapmalı |
|---|---|
| Gerekli dosya bulunamadı (BDE Problem) | Veritabanı motoru (BDE) eksik. Yeni bir bilgisayardaysanız aşağıdaki kurulumu yapın. |
| Mesaj No 30: Klasör paylaştırılamadı | Program yönetici olarak açılmamış. Açılıştaki Windows sorusuna **Evet** dediğinizden emin olun. |
| Onarım / "Repairing" ile ilgili uyarılar | Onarım programı (DentaLineSuitTamir) yarıda kesilmiş olabilir. Yöneticiye haber verin; kendiniz dosya silmeyin. |

### Başka bir bilgisayara kurma

1. Bu bilgisayarda programı kapatın.
2. `C:\Yazılım\DentaLineSuit` klasörünün tamamını USB belleğe kopyalayın.
3. Yeni bilgisayarda `KURULUM\KUR.bat` dosyasına çift tıklayın ve Windows'un sorusuna **Evet** deyin.
4. "Kurulum tamamlandı" yazısını görünce masaüstündeki kısayolla programı açın.

Ayrıntılar `KURULUM\OKUBENI.txt` dosyasındadır.

> **Dikkat:** Programı iki bilgisayarda ayrı ayrı kullanmayın. Hasta kayıtları ikiye bölünür ve sonradan birleştirilemez.

### Yedek

Bu bilgisayardaki veritabanının değişikliklerden önceki yedeği şurada: `C:\Yazılım\DentaLineSuit\_YEDEK\`.
Bu klasörü silmeyin.
