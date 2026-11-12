# Disci kurulumu: kısa özet

Ayrıntılı anlatım bu klasördeki **Kurulum-Rehberi.md** dosyasında; programın kullanımı **Kullanim-Kilavuzu.pdf** dosyasında.

1. Bu klasörü klinik sunucusunda bir yere çıkarın (ör. `C:\Disci-Kurulum`).
2. **KUR.bat** dosyasına çift tıklayın, Windows'un sorusuna **Evet** deyin ve soruları cevaplayın.
3. Kurulum bitince tarayıcıda açılan **ilk kurulum formunu** doldurun (klinik bilgileri ve yönetici hesabı).
4. Diğer bilgisayarlarda bir kez `\SUNUCU-ADI\Disci-Baglanti\Baglanti-Kur.bat` dosyasını çalıştırın.

## Seçenekler (Komut İsteminden)

```
KUR.bat -Deneme                                                         hiçbir şeyi değiştirmeden ne yapılacağını gösterir
KUR.bat -Hedef D:\Disci -VeriKlasoru D:\DisciVeri                        başka klasörlere kurar
KUR.bat -YeniSertifika                                                  güvenlik sertifikasını yeniler (ör. IP adresi değişince)
KUR.bat -SqlSunucu ".\BASKAORNEK"                                       bu bilgisayardaki var olan bir SQL Server'ı kullanır
KUR.bat -SqlSunucu "192.168.1.10,1433" -SqlKullaniciAdi disci -SqlParolasi "..." -Veritabani Disci
                                                                        başka bilgisayardaki SQL Server'a SQL kullanıcısıyla bağlanır
KUR.bat -SqlKurma                                                       SQL Server kurma seçeneğini göstermez
```

SQL Server kurmak zorunlu değildir: seçenek verilmezse kurulum sorar (yeni SQL Server Express kur / bu bilgisayarda kurulu
SQL Server / başka bilgisayardaki SQL Server). İnternetsiz kurulum için SQL Server Express dosyasını Microsoft'tan indirip
(https://go.microsoft.com/fwlink/p/?linkid=2216019 → Medya indir → Express Core) bu klasörde `sql` klasörüne koyun.

## Kurulumdan sonra (C:\Disci\betikler, yönetici PowerShell)

| Betik | Ne yapar |
|---|---|
| `yedekle.ps1` | Elle yedek alır (otomatik yedek her gün 14:00) |
| `geri-yukle.ps1` | Bir yedeğe döner (önce şu anki hali yedekler) |
| `parola-sifirla.ps1` | Unutulan yönetici parolasını sıfırlar |
| `kaldir.ps1` | Hizmeti kaldırır; veriler ve yedekler silinmez |
| `veritabani-ayarla.ps1` | Veritabanı bağlantısını değiştirir (başka SQL Server, kullanıcı, veritabanı); isterseniz verileri taşır |
| `guncelle.ps1 -Surum son` | En yeni sürümü GitHub'dan indirip kurar (program içindeki Güncelleme sayfası da yapar) |

## Klasörler

| Klasör | İçerik |
|---|---|
| `C:\Disci\app` | Program |
| `C:\Disci\betikler` | Yukarıdaki betikler |
| `C:\DisciVeri\files` | Hasta fotoğrafları |
| `C:\DisciVeri\Yedekler` | Günlük yedekler (haftada bir harici diske de kopyalayın) |
| `C:\DisciVeri\Disci-Baglanti` | Diğer bilgisayarlar için bağlantı paketi |
| `C:\DisciVeri\sertifika` | Tablet ve telefonlara kurulacak sertifika (`Disci.cer`) |
| `C:\DisciVeri\reports` | Veri aktarım raporları |

Adresler: bu bilgisayardan `https://localhost:5443`, ağdan `https://SUNUCU-ADI:5443`.
