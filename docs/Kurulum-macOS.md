# Disci kurulumu: macOS

Disci, Mac'te **Docker Desktop** içinde çalışır: Microsoft SQL Server Express ve Disci programı iki "konteyner" olarak
kurulur. Klinikteki diğer bilgisayar, tablet ve telefonlar tarayıcıdan bağlanır.

## Gerekenler

| | |
|---|---|
| macOS | 13 (Ventura) ve üstü; Intel ya da Apple Silicon (M1/M2/M3/M4) |
| Bellek / disk | En az 8 GB bellek önerilir (Docker'a en az 2 GB ayrılmalı), 15 GB boş disk |
| Docker Desktop | <https://www.docker.com/products/docker-desktop/> (Homebrew varsa betik kurmayı önerir) |
| İnternet | Yalnızca kurulumda ve güncellemelerde |

> **Apple Silicon:** SQL Server yalnızca x86 için var; Docker Desktop onu **Rosetta** ile çalıştırır. Docker Desktop >
> Settings > General altında **"Use Rosetta for x86_64/amd64 emulation on Apple Silicon"** açık olmalı.
>
> Docker Desktop büyük şirketlerde ücretli olabilir (lisansı Docker'ın sitesinde); küçük klinikler için ücretsizdir.

## Kurulum

1. Docker Desktop'ı kurun ve bir kez açın. Settings > General: **"Start Docker Desktop when you sign in"** açık olsun.
2. GitHub'daki son sürümden `Disci-Kurulum-<sürüm>-linux-macos.tar.gz` dosyasını indirin:
   <https://github.com/mehmet-cizmeci/Disci/releases/latest>
3. Terminal'i açın (Uygulamalar > İzlenceler > Terminal):

   ```bash
   cd ~/Downloads
   tar -xzf Disci-Kurulum-*-linux-macos.tar.gz
   cd Disci-Kurulum-unix
   ./kur-macos.sh
   ```

   `sudo` kullanmayın. Seçenekler: `--hedef ~/Klinik/Disci` (varsayılan `~/Disci`), `--https-port 5443`,
   `--yedek-saati 14:00`.
   Betik veritabanının nerede olacağını sorar: **Disci ile birlikte yeni SQL Server Express** (Docker içinde, önerilen),
   **bu Mac'te kurulu SQL Server** ya da **başka bilgisayardaki SQL Server** (sunucu, SQL kullanıcı adı/parolası,
   veritabanı adı). Ayrıntılar ve sonradan değiştirme (`./veritabani-ayarla.sh`):
   [Linux rehberi, SQL Server seçimi](Kurulum-Linux.md#sql-server-seçimi).
4. Sonunda ekrana yazılan **ilk kurulum adresini** (`https://localhost:5443/kurulum?anahtar=...`) Safari ya da Chrome'da
   açın; klinik bilgilerini ve yönetici hesabını girin. Sertifika uyarısında "Ayrıntıları göster → bu web sitesini ziyaret et".

## Sertifikaya güvenmek (uyarı çıkmasın)

```bash
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/Disci/veri/sertifika/disci.crt
```

Diğer cihazlara da aynı `disci.crt` dosyası kurulur.

## Mac'in uyumaması

Program, Mac uykudayken erişilemez. Sistem Ayarları > Pil (ya da Enerji) > **"Ekran kapalıyken otomatik uyumayı
engelle"** seçeneğini açın.

## Günlük işler

| İş | Komut (`cd ~/Disci` içinde) |
|---|---|
| Durum | `docker compose ps` |
| Kayıtlar | `docker compose logs -f web` |
| Elle yedek | `./yedekle.sh` |
| Yedekten geri yükleme | `./geri-yukle.sh veri/Yedekler/Disci_YYYYMMDD_HHMM.bak` |
| Elle güncelleme | `./guncelle.sh son` |
| Kaldırma (veriler kalır) | `./kaldir.sh` |

Yedekler her gün `~/Disci/veri/Yedekler` klasörüne alınır (Mac o saatte açıksa). Haftada bir harici diske ya da
Time Machine'e kopyalanmasını sağlayın.

## Güncelleme

Programdaki **Güncelleme** sayfasından "Şimdi güncelle" denince en geç 10 dakika içinde yeni sürüm GitHub'dan indirilip
kurulur (önce yedek alınır; yeni sürüm açılmazsa eskisine dönülür). "Otomatik güncelle" açıksa gece 03:00-05:00 arası.

## Windows'tan Mac'e geçiş

[Linux rehberindeki](Kurulum-Linux.md#windowstan-linuxa-geçiş) adımların aynısıdır; klasör `~/Disci` ve komutlar
`sudo` olmadan çalıştırılır.
