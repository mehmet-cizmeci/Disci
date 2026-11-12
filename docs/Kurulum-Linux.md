# Disci kurulumu: Linux

Disci, Linux'ta **Docker** içinde çalışır: biri Microsoft SQL Server Express, biri Disci programı olmak üzere iki
"konteyner" kurulur. Bilgisayar açılınca ikisi de kendiliğinden başlar. Klinikteki diğer bilgisayar, tablet ve
telefonlar tarayıcıdan bağlanır.

## Gerekenler

| | |
|---|---|
| İşletim sistemi | 64 bit **x86** (Intel/AMD) Linux: Ubuntu 22.04/24.04, Debian 12, Fedora, RHEL/Rocky/Alma 9 ve benzerleri |
| Bellek / disk | En az 2 GB bellek (4 GB önerilir), 10 GB boş disk |
| Programlar | `curl`, `openssl`; Docker yoksa kurulum betiği kurmayı önerir |
| İnternet | Yalnızca kurulumda (Docker imajları indirilir) ve güncellemelerde |

> ARM işlemcili Linux'ta (Raspberry Pi vb.) SQL Server Disci ile birlikte kurulamaz (yalnızca x86 için var); ağdaki
> başka bir bilgisayarda çalışan SQL Server kullanılabilir.

## Kurulum

1. GitHub'daki son sürümden `Disci-Kurulum-<sürüm>-linux-macos.tar.gz` dosyasını indirin:
   <https://github.com/mehmet-cizmeci/Disci/releases/latest>
2. Terminalde açın ve kurulumu başlatın:

   ```bash
   tar -xzf Disci-Kurulum-*-linux-macos.tar.gz
   cd Disci-Kurulum-unix
   sudo ./kur-linux.sh
   ```

   Seçenekler: `--hedef /srv/disci` (varsayılan `/opt/disci`), `--http-port 5111`, `--https-port 5443`,
   `--yedek-saati 14:00`.
3. Betik sırasıyla şunları yapar: Docker'ı denetler (yoksa kurmayı önerir), **veritabanının nerede olacağını sorar**,
   programı `/opt/disci` altına kopyalar, HTTPS sertifikası üretir, konteynerleri başlatır, günlük yedeği ve güncelleme
   denetimini `/etc/cron.d/disci` dosyasına yazar.

### SQL Server seçimi

SQL Server kurmak zorunlu değildir:

| Seçenek | Açıklama |
|---|---|
| **1) Disci ile birlikte yeni SQL Server Express** | Docker içinde kurulur, rastgele parola üretilir (önerilen; yalnızca x86-64) |
| **2) Bu bilgisayarda kurulu SQL Server** | Port (1433) ve SQL kullanıcı adı/parolası sorulur; program `host.docker.internal` adıyla bağlanır |
| **3) Başka bilgisayardaki SQL Server** | Sunucu (`192.168.1.10,1433` ya da `SUNUCU\SQLEXPRESS`), SQL kullanıcı adı/parolası, veritabanı adı |

- Linux/macOS'tan **yalnızca SQL kullanıcı adı ve parolasıyla** bağlanılır (Windows kimlik doğrulaması olmaz).
- Kurulum bağlantıyı dener. Sunucuda TCP/IP açık ve 1433 portu erişilebilir olmalı.
- Var olan bir SQL Server kullanılıyorsa **veritabanı yedeğini o sunucuda** alın; `yedekle.sh` yalnızca fotoğrafları yedekler.
- Sormadan: `sudo ./kur-linux.sh --sql-sunucu 192.168.1.10,1433 --sql-kullanici disci --sql-parola '...' --veritabani Disci`
  ya da `--dahili-sql`.
- Sonradan değiştirmek: `sudo ./veritabani-ayarla.sh` (kurulum klasöründe). Veriler kendiliğinden taşınmaz.
- ARM işlemcili Linux'ta (Raspberry Pi vb.) seçenek 1 yoktur; ağdaki bir SQL Server kullanılabilir.
4. Sonunda ekrana bir **ilk kurulum adresi** yazılır:
   `https://localhost:5443/kurulum?anahtar=...`. Bu adresi tarayıcıda açıp klinik bilgilerini ve yönetici hesabını
   girin. (Tarayıcı sertifika uyarısı verir; "Gelişmiş → Devam et" deyin ya da aşağıdaki gibi sertifikayı kurun.)

## Diğer cihazlardan bağlanma

- Adres: `https://<bilgisayar-adı-veya-IP>:5443`
- Sertifika uyarısı çıkmasın diye `/opt/disci/veri/sertifika/disci.crt` dosyasını diğer cihazlara kurun
  (Windows: dosyaya çift tıklayın → "Sertifika yükle" → "Yerel Makine" → "Güvenilen Kök Sertifika Yetkilileri").
- Güvenlik duvarı (`ufw`) açıksa kurulum 5111 ve 5443 portlarını açmayı önerir.

## Günlük işler

| İş | Komut (`cd /opt/disci` içinde) |
|---|---|
| Durum | `sudo docker compose ps` |
| Kayıtlar | `sudo docker compose logs -f web` |
| Yeniden başlatma | `sudo docker compose restart web` |
| Elle yedek | `sudo ./yedekle.sh` |
| Yedekten geri yükleme | `sudo ./geri-yukle.sh veri/Yedekler/Disci_YYYYMMDD_HHMM.bak` |
| Elle güncelleme | `sudo ./guncelle.sh son` |
| Kaldırma (veriler kalır) | `sudo ./kaldir.sh` |

Yedekler `/opt/disci/veri/Yedekler` klasöründedir (her gün, son 14 gün). Haftada bir başka bir diske kopyalayın.

## Güncelleme

Programdaki **Güncelleme** sayfası (yönetici) yeni sürümü haber verir. "Şimdi güncelle" denince en geç 10 dakika
içinde `guncelle.sh` yeni sürümü GitHub'dan indirir, SHA256 özetini denetler, yedek alır, kurar; yeni sürüm açılmazsa
eskisine döner. "Otomatik güncelle" açıksa yeni sürümler gece 03:00-05:00 arasında kendiliğinden kurulur.

## Windows'tan Linux'a geçiş

1. Windows'taki Disci'de son bir yedek alın (`C:\Disci\betikler\yedekle.ps1` ya da `C:\DisciVeri\Yedekler` altındaki
   en yeni `.bak`).
2. Linux'ta Disci'yi kurun, ilk kurulum formunu **doldurmadan** devam edin.
3. `.bak` dosyasını `/opt/disci/veri/Yedekler/` klasörüne, `C:\DisciVeri\files` içindekileri `/opt/disci/veri/files/`
   klasörüne kopyalayın, sonra: `sudo chown -R 1654:1654 /opt/disci/veri/files && sudo ./geri-yukle.sh veri/Yedekler/<dosya>.bak`
4. Windows'taki kullanıcı adları ve parolalar aynen geçer.

## Sorun giderme

- **Program açılmıyor:** `sudo docker compose logs sql web`. SQL Server ilk açılışta 1-2 dakika hazırlanır.
- **"SQL Server en az 2 GB ister":** Daha fazla belleği olan bir bilgisayar kullanın.
- **Parola unutuldu:** Yönetici başka bir kullanıcının parolasını **Kullanıcılar** sayfasından değiştirebilir.
