#!/usr/bin/env bash
# Disci'yi kaldırır (Linux/macOS). Varsayılan olarak veriler (veritabanı, fotoğraflar, yedekler) SİLİNMEZ.
#
#   ./kaldir.sh                 # programı durdurur, zamanlanmış görevleri kaldırır; veriler kalır
#   ./kaldir.sh --veriyi-sil    # veritabanı ve veri klasörü de silinir (geri alınamaz)
set -euo pipefail
HEDEF="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$HEDEF/ortak.sh"
cd "$HEDEF"

sil=0
[ "${1:-}" = "--veriyi-sil" ] && sil=1

baslik "Disci kaldırılıyor ($HEDEF)"
if [ "$sil" = "1" ]; then
    uyari "Veritabanı, fotoğraflar ve yedekler KALICI OLARAK silinecek."
    evet_mi "Emin misiniz? Önce yedekleri başka bir diske kopyaladınız mı?" h || exit 1
    compose down -v
    rm -rf "$HEDEF/veri"
else
    compose down
    bilgi "Veriler duruyor: $HEDEF/veri ve Docker birimi 'disci_sql-veri'."
fi

if [ -f /etc/cron.d/disci ]; then rm -f /etc/cron.d/disci; fi
for a in com.disci.yedek com.disci.guncelle; do
    p="$HOME/Library/LaunchAgents/$a.plist"
    if [ -f "$p" ]; then launchctl unload "$p" >/dev/null 2>&1 || true; rm -f "$p"; fi
done
docker image rm "disci-web:$(env_degeri DISCI_SURUM .env)" >/dev/null 2>&1 || true
tamam "Disci kaldırıldı. Klasörü tamamen silmek için: rm -rf \"$HEDEF\""
