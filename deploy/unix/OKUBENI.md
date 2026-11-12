# Disci: Linux ve macOS kurulum paketi

- **Linux:** `sudo ./kur-linux.sh`   (ayrıntı: Kurulum-Linux.md)
- **macOS:** `./kur-macos.sh`        (ayrıntı: Kurulum-macOS.md)
- **Windows:** bu paket değil; `Disci-Kurulum-<sürüm>-win-x64.zip` paketindeki `KUR.bat`.

Program ve Microsoft SQL Server Express, Docker içinde çalışır. Kurulumdan sonra kurulum klasöründeki betikler:

| Betik | Ne yapar |
|---|---|
| `yedekle.sh` | Veritabanı ve fotoğrafları `veri/Yedekler` klasörüne yedekler (her gün kendiliğinden de çalışır) |
| `geri-yukle.sh <dosya.bak>` | Yedeği geri yükler (Windows'taki Disci'nin yedeği de olur) |
| `guncelle.sh [son\|v1.2.0]` | Yeni sürümü GitHub'dan indirip kurar (programın Güncelleme sayfası da tetikler) |
| `kaldir.sh [--veriyi-sil]` | Programı kaldırır; veriler varsayılan olarak kalır |
| `veritabani-ayarla.sh` | Veritabanı bağlantısını değiştirir (Disci ile birlikte kurulan / var olan SQL Server) |

SQL Server kurmak zorunlu değildir: kurulum, Disci ile birlikte yeni bir SQL Server Express kurmayı ya da bu
bilgisayardaki veya ağdaki var olan bir SQL Server'ı kullanmayı sorar (`--sql-sunucu`, `--sql-kullanici`,
`--sql-parola`, `--veritabani` ya da `--dahili-sql` ile sormadan).

`.env` dosyasında veritabanı parolası vardır: kimseyle paylaşmayın, silmeyin.
