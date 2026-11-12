#!/usr/bin/env bash
# Disci yedeğini geri yükler (Linux/macOS). DİKKAT: veritabanındaki bugünkü kayıtların yerine yedektekiler gelir.
#
#   ./geri-yukle.sh veri/Yedekler/Disci_20261109_1400.bak
#
# Windows'taki Disci'den alınmış bir .bak dosyası da geri yüklenebilir (Windows'tan Linux/macOS'a geçiş): dosyayı
# veri/Yedekler klasörüne koyup bu betiği çalıştırın; fotoğrafları (C:\DisciVeri\files) veri/files klasörüne kopyalayın.
set -euo pipefail
HEDEF="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$HEDEF/ortak.sh"
cd "$HEDEF"

env_tamamla .env
dahili_sql_mi .env || hata "Disci var olan bir SQL Server kullanıyor; yedeği o sunucuda (ör. SQL Server Management Studio ile) geri yükleyin. Fotoğraflar için veri/Yedekler/files klasörünü veri/files'a kopyalayın."
[ $# -eq 1 ] || hata "Kullanım: $0 veri/Yedekler/Disci_YYYYMMDD_HHMM.bak"
[ -f "$1" ] || hata "Dosya yok: $1"
ad="$(basename "$1")"
case "$ad" in *[!A-Za-z0-9._-]*) hata "Dosya adında yalnızca harf, rakam, nokta, alt çizgi ve tire olabilir: $ad" ;; esac
if [ "$(cd "$(dirname "$1")" && pwd)" != "$HEDEF/veri/Yedekler" ]; then cp "$1" "veri/Yedekler/$ad"; fi
# Linux'ta SQL Server konteyneri dosyayı kendi kullanıcısıyla (UID 10001) okur.
if [ "$(id -u)" = "0" ]; then chown 10001:0 "veri/Yedekler/$ad"; fi

uyari "Veritabanındaki bütün kayıtların yerine '$ad' içindekiler gelecek."
evet_mi "Devam edilsin mi?" h || exit 1

bilgi "Önce bugünkü durumun yedeği alınıyor..."
"$HEDEF/yedekle.sh" || hata "Güvenlik yedeği alınamadı; geri yükleme yapılmadı."
compose stop web

if ! compose exec -T -e YEDEK_ADI="$ad" sql bash -s <<'KONTEYNER'
set -e
S=/opt/mssql-tools18/bin/sqlcmd; E="-C"
[ -x "$S" ] || { S=/opt/mssql-tools/bin/sqlcmd; E=""; }
q() { "$S" $E -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -b "$@"; }
# Windows yedeğindeki dosya yolları (C:\...) bu sunucuda yok: veri ve günlük dosyaları buraya yerleştirilir.
move=""
while IFS='|' read -r name _ type _; do
    [ -n "$name" ] || continue
    if [ "$type" = "L" ]; then target=/var/opt/mssql/data/Disci_log.ldf; else target=/var/opt/mssql/data/Disci.mdf; fi
    move="$move, MOVE N'$name' TO N'$target'"
done < <(q -h -1 -W -s '|' -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'/yedek/$YEDEK_ADI'")
q -Q "IF DB_ID(N'Disci') IS NOT NULL ALTER DATABASE [Disci] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
      RESTORE DATABASE [Disci] FROM DISK = N'/yedek/$YEDEK_ADI' WITH REPLACE$move;
      ALTER DATABASE [Disci] SET MULTI_USER;"
KONTEYNER
then
    compose start web
    hata "Geri yükleme başarısız; program eski veriyle yeniden başlatıldı."
fi

compose start web
tamam "Geri yüklendi: $ad. Program yeniden başlatıldı."
