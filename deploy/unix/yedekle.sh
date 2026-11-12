#!/usr/bin/env bash
# Disci yedeği (Linux/macOS): veritabanının tam yedeği ve fotoğrafların kopyası veri/Yedekler klasörüne; 14 günden
# eski yedekler silinir. Kurulum her gün çalıştırır (Linux: /etc/cron.d/disci, macOS: launchd); elle de çalışır.
set -euo pipefail
HEDEF="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$HEDEF/ortak.sh"
GUN="${1:-14}"
cd "$HEDEF"

env_tamamla .env
ad="Disci_$(date '+%Y%m%d_%H%M').bak"
if ! dahili_sql_mi .env; then
    # Var olan (başka) bir SQL Server kullanılıyor: veritabanı yedeği o sunucuda, kendi yedekleme düzeniyle alınmalı.
    echo "$(date '+%F %T')  Veritabanı Disci ile birlikte kurulan SQL Server'da değil; veritabanı yedeğini SQL Server'ın kendisinde alın. Yalnızca fotoğraflar yedekleniyor."
else
echo "$(date '+%F %T')  Yedek başlıyor: $ad"
# Komutlar SQL Server konteynerinin içinde çalışır; parola konteynerin kendi ortam değişkenindedir (komut satırında görünmez).
if ! compose exec -T -e YEDEK_ADI="$ad" sql bash -s <<'KONTEYNER'
S=/opt/mssql-tools18/bin/sqlcmd; E="-C"
[ -x "$S" ] || { S=/opt/mssql-tools/bin/sqlcmd; E=""; }
"$S" $E -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -b \
    -Q "BACKUP DATABASE [Disci] TO DISK = N'/yedek/$YEDEK_ADI' WITH INIT, CHECKSUM, NAME = N'Disci yedegi'"
KONTEYNER
then
    echo "$(date '+%F %T')  HATA: veritabanı yedeklenemedi."
    exit 1
fi
echo "$(date '+%F %T')  Veritabanı yedeklendi: veri/Yedekler/$ad"
fi

if [ -d veri/files ]; then
    mkdir -p veri/Yedekler/files
    if command -v rsync >/dev/null 2>&1; then rsync -a --delete veri/files/ veri/Yedekler/files/
    else rm -rf veri/Yedekler/files && cp -Rp veri/files veri/Yedekler/files; fi
    echo "$(date '+%F %T')  Fotoğraflar kopyalandı."
fi

find veri/Yedekler -maxdepth 1 -name 'Disci_*.bak' -mtime +"$GUN" -print | while read -r f; do
    rm -f "$f"
    echo "$(date '+%F %T')  Eski yedek silindi: $f"
done
