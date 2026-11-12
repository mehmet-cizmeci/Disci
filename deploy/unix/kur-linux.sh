#!/usr/bin/env bash
# Disci kurulumu (Linux: Ubuntu, Debian, Fedora, RHEL ve benzerleri). Disci Docker içinde çalışır; bilgisayar açılınca
# kendiliğinden başlar. SQL Server kurmak zorunlu değil: kurulum sorar (Disci ile birlikte yeni SQL Server Express /
# bu bilgisayarda kurulu SQL Server / başka bilgisayardaki SQL Server).
#
#   sudo ./kur-linux.sh                       # varsayılanlar: /opt/disci, portlar 5111/5443, yedek 14:00
#   sudo ./kur-linux.sh --hedef /srv/disci --https-port 8443 --yedek-saati 13:30
#   sudo ./kur-linux.sh --sql-sunucu 192.168.1.10,1433 --sql-kullanici disci --sql-parola '...' --veritabani Disci
#   sudo ./kur-linux.sh --dahili-sql          # sormadan SQL Server'ı Disci ile birlikte kur
#
# Aynı betik güncelleme için de çalıştırılır: veriler ve ayarlar korunur.
set -euo pipefail

PAKET="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=ortak.sh
. "$PAKET/ortak.sh"
# shellcheck source=kur-ortak.sh
. "$PAKET/kur-ortak.sh"

HEDEF="/opt/disci"
HTTP_PORT=5111
HTTPS_PORT=5443
YEDEK_SAATI="14:00"
while [ $# -gt 0 ]; do
    case "$1" in
        --hedef) HEDEF="$2"; shift 2 ;;
        --http-port) HTTP_PORT="$2"; shift 2 ;;
        --https-port) HTTPS_PORT="$2"; shift 2 ;;
        --yedek-saati) YEDEK_SAATI="$2"; shift 2 ;;
        --sorma) DISCI_SORMA=1; shift ;;
        --dahili-sql) DB_ARG_DAHILI=1; shift ;;
        --sql-sunucu) DB_ARG_SUNUCU="$2"; shift 2 ;;
        --sql-kullanici) DB_ARG_KULLANICI="$2"; shift 2 ;;
        --sql-parola) DB_ARG_PAROLA="$2"; shift 2 ;;
        --veritabani) DB_ARG_VERITABANI="$2"; shift 2 ;;
        -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
        *) hata "Bilinmeyen seçenek: $1" ;;
    esac
done
[[ "$YEDEK_SAATI" =~ ^([01][0-9]|2[0-3]):[0-5][0-9]$ ]] || hata "Yedek saati SS:DD biçiminde olmalı (ör. 14:00)."

printf '\033[32mDisci kurulumu (Linux)\033[0m\n'

baslik "1. Ön kontroller"
[ "$(id -u)" = "0" ] || hata "Betiği yönetici olarak çalıştırın: sudo $0"
case "$(uname -m)" in
    x86_64|amd64) ;;
    *) SQL_DAHILI_OLUR=0; uyari "Bu bilgisayar $(uname -m): SQL Server burada çalışmaz (yalnızca x86-64). Ağdaki var olan bir SQL Server kullanılabilir." ;;
esac
for k in curl openssl; do command -v "$k" >/dev/null 2>&1 || hata "'$k' kurulu değil (ör. sudo apt install $k)."; done
toplam_mb=$(awk '/MemTotal/ {print int($2/1024)}' /proc/meminfo)
[ "$toplam_mb" -ge 2000 ] || uyari "Bellek ${toplam_mb} MB; SQL Server'ı Disci ile birlikte kuracaksanız en az 2 GB gerekir."
tamam "Tamam."

baslik "2. Docker"
if ! command -v docker >/dev/null 2>&1; then
    bilgi "Docker kurulu değil."
    if evet_mi "Docker'ın resmi kurulum betiğiyle (https://get.docker.com) şimdi kurulsun mu?" e; then
        curl -fsSL https://get.docker.com -o /tmp/get-docker.sh
        sh /tmp/get-docker.sh
        rm -f /tmp/get-docker.sh
    else
        hata "Docker'ı kurup (https://docs.docker.com/engine/install/) betiği yeniden çalıştırın."
    fi
fi
systemctl enable --now docker >/dev/null 2>&1 || true
docker info >/dev/null 2>&1 || hata "Docker çalışmıyor (sudo systemctl start docker)."
compose version >/dev/null 2>&1 || hata "Docker Compose eklentisi yok (ör. sudo apt install docker-compose-plugin)."
tamam "Docker hazır: $(docker --version)"

VERI_SAHIBI=1
kurulum_yap

baslik "Günlük yedek ve güncelleme görevi"
saat="${YEDEK_SAATI%%:*}"; dakika="${YEDEK_SAATI##*:}"
cat >/etc/cron.d/disci <<EOF
# Disci (kur-linux.sh yazdı)
SHELL=/bin/bash
PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
$((10#$dakika)) $((10#$saat)) * * * root $HEDEF/yedekle.sh >>$HEDEF/veri/Yedekler/yedek.log 2>&1
*/10 * * * * root $HEDEF/guncelle.sh >>$HEDEF/veri/Guncelleme/guncelle.log 2>&1
EOF
chmod 644 /etc/cron.d/disci
tamam "Her gün $YEDEK_SAATI'te yedek; güncelleme isteği 10 dakikada bir denetlenir (/etc/cron.d/disci)."

if command -v ufw >/dev/null 2>&1 && ufw status 2>/dev/null | grep -q active; then
    if evet_mi "Güvenlik duvarında (ufw) $HTTP_PORT ve $HTTPS_PORT portları yerel ağa açılsın mı?" e; then
        ufw allow "$HTTP_PORT/tcp" >/dev/null && ufw allow "$HTTPS_PORT/tcp" >/dev/null && tamam "Portlar açıldı."
    fi
fi
