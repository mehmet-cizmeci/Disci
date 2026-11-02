# Disci

- Kullanıcıyla her zaman **Türkçe** konuş.
- İşe başlamadan önce [yapayzeka.md](yapayzeka.md) dosyasını oku: yapılanlar, kararlar, tuzaklar ve açık sorular orada.
- **Gizlilik:** Depoya gerçek hasta, hekim veya klinik verisi girmez. Gerçek veriler yalnızca `data/` (Git dışı) ve proje dışındadır. Testler uydurma veri kullanır.
- Derleme ve test: `dotnet build`, `dotnet test`. Web uygulaması: `dotnet run --project src/Disci.Web` (http://localhost:5111).
- Arayüzü tarayıcıda giriş yapmadan denemek için `tests/Disci.Web.Tests/BrowserTestServer.cs` açıklamasına bakın.
- Blazor bileşenlerinde DbContext'i paylaşma: her sorgu için `IDbContextFactory` ile kısa ömürlü bağlam aç (eşzamanlı sorgu bağlantıyı çökertir).
- Önemli öğrenilenleri ve yeni kararları `yapayzeka.md` dosyasına ekle.
