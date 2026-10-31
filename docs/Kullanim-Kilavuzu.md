# Disci kullanım kılavuzu

Disci, kliniğimizin eski DentaLineSuit programının yerini alacak yeni hasta takip sistemidir. Bilgisayar,
tablet veya telefondaki internet tarayıcısından (Chrome, Edge, Safari) kullanılır; ayrı bir program kurmak gerekmez.

Eski programdaki bütün hastalar, tedaviler, ödemeler, anamnezler, notlar ve fotoğraflar Disci'ye aktarılmıştır.

> **Bu sürümde:** hastaları arayabilir, hasta kartlarını, tedavi ve ödeme geçmişini, kalan borcu görebilirsiniz.
> Yeni hasta ekleme, tedavi/ödeme girme, randevu ve reçete ekranları sonraki sürümlerde gelecek.
> Bu işler için şimdilik eski programı kullanmaya devam edin.

---

## İçindekiler

1. [Programı açma](#1-programı-açma)
2. [Giriş yapma](#2-giriş-yapma)
3. [Ekran düzeni](#3-ekran-düzeni)
4. [Ana sayfa](#4-ana-sayfa)
5. [Hasta arama](#5-hasta-arama)
6. [Hasta kartı](#6-hasta-kartı)
7. [Hesabım ve parola değiştirme](#7-hesabım-ve-parola-değiştirme)
8. [Kullanıcı yönetimi (yalnızca yönetici)](#8-kullanıcı-yönetimi-yalnızca-yönetici)
9. [Eski verilerle ilgili bilmeniz gerekenler](#9-eski-verilerle-ilgili-bilmeniz-gerekenler)
10. [Sık sorulan sorular](#10-sık-sorulan-sorular)
11. [Ek: Eski DentaLineSuit programı](#ek-eski-dentalinesuit-programı)

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
[Hesabım](#7-hesabım-ve-parola-değiştirme) sayfasından passkey ekleyin. Sonra giriş ekranında **Passkey ile giriş yap**'a basın.

## 3. Ekran düzeni

- **Sol menü:** Ana sayfa, Hastalar, Kullanıcılar (yalnızca yönetici), Hesabım, Çıkış
- **Sağ üst:** Giriş yaptığınız hesap ve rolünüz (Yönetici, Diş hekimi veya Asistan)
- **Telefon ve tablette** menü gizlenir; sol üstteki **☰** düğmesine basarak açılır.

Ekranda "Beklenmeyen bir hata oluştu" yazan sarı bir şerit çıkarsa **Yenile**'ye basın. Sorun sürerse yöneticiye haber verin.

## 4. Ana sayfa

Giriş yaptığınızda ilk bu ekran açılır.

- **Hasta arama kutusu:** Ad, soyad, telefon veya protokol numarası yazıp **Ara**'ya basın.
- **Özet kartları:**
  - *Kayıtlı hasta:* sistemdeki toplam hasta sayısı
  - *Bu ay yeni hasta:* bu ay kaydı açılan hastalar
  - *Bu ay tedavi:* bu ay yapılan tedavilerin toplam tutarı
  - *Bu ay tahsilat:* bu ay alınan ödemelerin toplamı
- **Son işlem yapılan hastalar:** En son tedavi gören 10 hasta. Birine tıklayınca hasta kartı açılır.

## 5. Hasta arama

Menüden **Hastalar**'a girin.

- Arama kutusuna yazmaya başladığınız anda liste süzülür; düğmeye basmanız gerekmez.
- Neye göre arayabilirsiniz:
  - **Ad veya soyad:** `deneme` ya da `hastaoğlu`. İki kelime yazarsanız (`deneme hastaoğlu`) ikisini de içeren hastalar çıkar.
  - **Telefon:** En az 4 rakam yazın (`5321`). Boşluklu yazılmış numaralar da bulunur.
  - **Protokol numarası:** Eski programdaki hasta numarası (`1234`).
- Sütun başlıklarına (**Protokol**, **Ad soyad**, **Doğum tarihi**, **Son tedavi**) tıklayarak listeyi sıralayabilirsiniz.
- Liste sayfa sayfa gösterilir (sayfada 25 hasta); en alttaki düğmelerle sayfalar arasında gezin.
- **Arşivdekileri de göster** düğmesi açılınca arşivlenmiş kayıtlar da listelenir (bkz. [bölüm 9](#9-eski-verilerle-ilgili-bilmeniz-gerekenler)).

Hastanın adına tıklayınca hasta kartı açılır.

## 6. Hasta kartı

Kartın üstünde hastanın adı, protokol numarası, doğum tarihi ve yaşı, cep telefonu görünür. Telefondan bakıyorsanız
numaraya dokunarak doğrudan arayabilirsiniz.

### Özet kartları

| Kart | Anlamı |
|---|---|
| Yapılan tedaviler | "Yapıldı" durumundaki tedavilerin toplam tutarı |
| Ödemeler | Hastanın yaptığı ödemelerin toplamı |
| Kalan borç / Bakiye | Yapılan tedaviler eksi ödemeler. Artı ise hastanın borcu var; eksi ise fazla ödeme yapmış |
| Son tedavi | En son tedavi tarihi |

> Bakiye yalnızca Türk lirası kayıtlarla hesaplanır. Eski sistemde dolar ile girilmiş birkaç kayıt listede görünür ama bakiyeye katılmaz.

### Sekmeler

**Tedaviler**: Hastanın bütün tedavi geçmişi, en yeni en üstte. Her satırda tarih, durum, diş numarası, işlem ve tutar yazar.

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

**Ödemeler**: Tarih, ödeme şekli (Nakit, Kredi kartı…), açıklama ve tutar. İptal edilmiş ödemeler üstü çizili görünür.

**Bilgiler**: Kimlik ve iletişim bilgileri, adresler, kayıt tarihi, tarife, hekimi, referansı, yakınları ve hasta notu.
- *Tarife*: Hastanın bağlı olduğu fiyat listesi (Güncel tarife veya Eski tarife).
- *Hekimi*: "Ortak havuz" yazıyorsa hasta belirli bir hekime atanmamıştır; bütün hekimler görebilir.

**Anamnez**: Hastanın tıbbi geçmişi. Sarı etiketler hastada "evet" işaretlenmiş durumlardır (örneğin Diyabet,
Hipertansiyon, İlaç alerjisi). Etiketler gruplara ayrılmıştır: Tıbbi geçmiş, Kullandığı ilaçlar, Alışkanlıklar, Alerji ve ortodonti muayene bulguları.
Alerji ayrıntıları ve notlar alttadır.

> Tedaviye başlamadan önce Anamnez sekmesini kontrol etmeyi alışkanlık edinin.

**Notlar**: Hastaya ait genel notlar ve dişe özel notlar (diş numarasıyla birlikte).

**Fotoğraflar**: Ağız içi kamera ve yüz fotoğrafları; tarih, tür ve varsa diş numarasıyla. Büyük görmek için fotoğrafa tıklayın; yeni sekmede açılır.

> Röntgen görüntüleri eski programda bu bilgisayarda kayıtlı değildi; bu yüzden Disci'de de yok.

## 7. Hesabım ve parola değiştirme

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

## 8. Kullanıcı yönetimi (yalnızca yönetici)

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

## 9. Eski verilerle ilgili bilmeniz gerekenler

- **Protokol numarası:** Her hastanın eski programdaki numarası korunmuştur; arama yaparken kullanabilirsiniz.
- **Ad ve soyad:** Eski programda tek kutuda tutulan adın son kelimesi soyad kabul edildi. Hatalı ayrılmış bir isim görürseniz yöneticiye bildirin.
- **Arşivdeki kayıt:** Eski programda kartı silinmiş ama tedavi kayıtları kalmış hastalar "(Eski kayıt *numara*)" adıyla arşivde durur. Kayıtlar kaybolmasın diye tutuldu.
- **Tedaviyi yapan hekim:** Eski program bu bilgiyi tutmadığı için aktarılan tedavilerde hekim adı yok.
- **KDV:** Eski programda bazı tedavilerin KDV bilgisi bozuktu; bunlar 0 olarak aktarıldı. **Tedavi tutarları değişmedi.**
- **Eski kullanıcılar:** Eski programın kullanıcı adları ve şifreleri güvenlik nedeniyle aktarılmadı. Herkes için yeni hesap açılır.
- **Geçiş günü:** Disci'ye tamamen geçmeden önce eski programa girilen son kayıtlar, yönetici tarafından bir kez daha aktarılacak. O güne kadar yeni kayıtları eski programa girmeye devam edin.

## 10. Sık sorulan sorular

**Sayfa açılmıyor, "bu siteye ulaşılamıyor" diyor.**
Disci ana bilgisayarda çalışmıyor olabilir. Yöneticiye haber verin.

**"E-posta veya parola yanlış" diyor.**
Büyük/küçük harfe ve klavye dilinin (Türkçe Q) doğru olduğuna dikkat edin. 5 yanlış denemeden sonra hesap kilitlenir.

**Giriş yaptım ama "Kullanıcılar" menüsünü göremiyorum.**
Bu menü yalnızca yöneticilere görünür.

**Bir hastayı bulamıyorum.**
Soyadın yalnızca bir kısmını veya telefonun son 4 rakamını yazmayı deneyin. Türkçe harflerle (ı, ş, ğ) ve harfsiz (i, s, g) yazmayı ayrı ayrı deneyin; eski kayıtlarda isimler farklı yazılmış olabilir. **Arşivdekileri de göster**'i açmayı da deneyin.

**Bakiye eski programdakinden farklı.**
Disci bakiyeyi tedavi ve ödeme kayıtlarından her seferinde yeniden hesaplar. Eski programın sakladığı borç toplamı bazen güncel değildi. Fark görürseniz hastanın Tedaviler ve Ödemeler sekmelerini karşılaştırıp yöneticiye bildirin.

**Telefonumdan girebilir miyim?**
Şimdilik hayır. Ağ ayarı yapıldıktan sonra klinik ağındaki tablet ve telefonlardan da girilebilecek.

**Yeni hasta veya tedavi nasıl eklerim?**
Bu ekranlar sonraki sürümde gelecek. Şimdilik eski programa girin.

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
