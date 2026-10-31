Yapay zeka asistanına vereceğin istek metnini, yeni platform (tablet/telefon uyumluluğu), çoklu hekim, asistan yetkilendirmesi ve reçete özelliklerini kapsayacak şekilde güncelledim.

Bu gereksinimleri karşılamak için uygulamanın mimarisini salt masaüstü (WPF/WinForms) yerine, modern bir **Web tabanlı (ASP.NET Core / Blazor / API)** altyapıya çekmek en mantıklısıdır. Böylece uygulama tek bilgisayarda çalışırken "localhost" üzerinden, sunucuda çalışırken ise ağdaki tüm tablet, telefon ve bilgisayarlardan tarayıcı aracılığıyla erişilebilir olur.

İşte güncellenmiş metin:

---

**Rol ve Kapsam:**
Sen uzman bir .NET/C# Full-Stack yazılım geliştiricisi, veri göçü (data migration) uzmanı ve veritabanı mimarısın. Amacımız; lisanslı olarak kullandığımız ancak çok eski olduğu için yeni işletim sistemlerinde sorun çıkartan Delphi tabanlı "Dentalinesuit" yazılımındaki hasta verilerimizi modern bir sisteme aktarmaktır. Uygulamanın veritabanı ve gerekli dosya yedeklerini yerel klasörümüze aktardık ve yasal kullanıcı adı/şifrelerine sahibiz. Hedefimiz, bu yedek dosyalardaki verilerimizi analiz edip dışarı aktararak; birden fazla diş hekiminin ve asistanın kullanabileceği, cihaz bağımsız (bilgisayar, tablet, telefon) çalışabilen, modern bir C# .NET otomasyonu geliştirmektir.

**Adım 1: Yedekten Veri Okuma ve Migrasyon (Data Export & Migration)**

* Dentalinesuit yazılımından aldığımız yedek klasöründeki dosya uzantılarını inceleyerek eski veritabanı altyapısını (Paradox, Interbase, Firebird, dBase vb.) tespit etmek için bana bir analiz yönergesi hazırla.
* Sahip olduğumuz kullanıcı yetkileriyle bu yedek veritabanı dosyalarına nasıl bağlanabileceğimizi ve verileri nasıl okuyabileceğimizi gösteren C# bağlantı kodlarını sağla.
* Yedekteki eski veritabanından hasta kayıtları, randevular ve tedavi geçmişi gibi verileri okuyup modern bir MSSQL veritabanına aktaracak bir "Migration (Göç)" konsol uygulaması yaz.

**Adım 2: Yeni Yazılımın Mimarisi ve Geliştirilmesi**

* Programın hem tek bir masaüstü bilgisayarda (lokal sunucu gibi), hem yerel ağda hem de gerçek bir bulut sunucusunda çalışabilmesi; ayrıca bilgisayar, tablet ve cep telefonlarından kullanılabilmesi için projeyi **ASP.NET Core Web API + Responsive Web UI (Blazor, MVC veya React/Vue)** mimarisiyle tasarla.
* Veritabanı olarak MSSQL kullan (Entity Framework Core ile Code-First yaklaşımı).
* Sisteme çoklu cihazdan erişileceği için JWT (JSON Web Token) veya Cookie tabanlı güvenli bir kimlik doğrulama (Authentication) altyapısı kur.

**Adım 3: Çoklu Kullanıcı, Yetkilendirme ve Temel Modüller**
Aşağıdaki modüllerin veritabanı modellerini (Entity sınıflarını) ve temel iş kurallarını oluştur:

1. **Kullanıcı ve Rol Yönetimi:** "Admin", "Diş Hekimi" ve "Asistan" rolleri. Asistanlar randevu oluşturabilmeli, reçete girebilmeli ve temel kayıtları tutabilmelidir.
2. **Çoklu Hekim ve Hasta Yönetimi:** Birden fazla hekim desteği. Hastaların sistemde ortak havuzda tutulması veya hekime atanabilmesi. Hasta kimlik, iletişim ve anamnez bilgileri.
3. **Randevu Sistemi:** Tablet ve telefondan da kolayca yönetilebilecek, hekim bazlı filtrelenebilen bir takvim arayüzü ve randevu çakışma kontrolü.
4. **Tedavi, İşlem ve Reçete Takibi:** Hangi dişe, hangi hekim tarafından, ne zaman işlem yapıldığının ve ücretinin girilmesi. Asistanların da hekim adına reçete düzenleyebileceği bir reçete/ilaç modülü.

Lütfen işleme **Adım 1** ile, yani elimizdeki yedek klasöründen eski Delphi programının veritabanı türünü analiz etme stratejisini belirleyerek başla. İncelemem için yedek klasöründe öncelikle hangi dosya uzantılarını aramalıyım?

---