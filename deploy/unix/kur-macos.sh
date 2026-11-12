#!/usr/bin/env bash
# Disci kurulumu (macOS 13 ve üstü; Intel veya Apple Silicon). Disci Docker Desktop içinde çalışır. SQL Server kurmak
# zorunlu değil: kurulum sorar (Disci ile birlikte yeni SQL Server Express / bu Mac'te kurulu / başka bilgisayardaki SQL Server).
#
#   ./kur-macos.sh                     # varsayılanlar: ~/Disci, portlar 5111/5443, yedek 14:00
#   ./kur-macos.sh --hedef ~/Klinik/Disci --yedek-saati 13:30
#   ./kur-macos.sh --sql-sunucu 192.168.1.10,1433 --sql-kullanici disci --sql-parola '...' --veritabani Disci
#
# sudo ile çalıştırmayın. Aynı betik güncelleme için de çalıştırılır: veriler ve ayarlar korunur.
set -euo pipefail

PAKET="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=ortak.sh
. "$PAKET/ortak.sh"
# shellcheck source=kur-ortak.sh
. "$PAKET/kur-ortak.sh"

HEDEF="$HOME/Disci"
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
        -h|--help) sed -n '2,9p' "$0"; exit 0 ;;
        *) hata "Bilinmeyen seçenek: $1" ;;
    esac
done
[[ "$YEDEK_SAATI" =~ ^([01][0-9]|2[0-3]):[0-5][0-9]$ ]] || hata "Yedek saati SS:DD biçiminde olmalı (ör. 14:00)."

printf '\033[32mDisci kurulumu (macOS)\033[0m\n'

baslik "1. Ön kontroller"
[ "$(uname -s)" = "Darwin" ] || hata "Bu betik macOS içindir; Linux'ta kur-linux.sh kullanın."
[ "$(id -u)" != "0" ] || hata "sudo kullanmayın; betiği kendi kullanıcınızla çalıştırın."
if [ "$(uname -m)" = "arm64" ]; then
    bilgi "Apple Silicon: SQL Server x86-64 olduğu için Rosetta ile çalışır."
    if ! /usr/bin/pgrep -q oahd 2>/dev/null; then
        bilgi "Rosetta kuruluyor (yönetici parolası istenebilir)..."
        softwareupdate --install-rosetta --agree-to-license || uyari "Rosetta kurulamadı; Docker Desktop ayarlarından da açabilirsiniz."
    fi
fi
tamam "Tamam."

baslik "2. Docker Desktop"
if ! command -v docker >/dev/null 2>&1; then
    if command -v brew >/dev/null 2>&1 && evet_mi "Docker Desktop Homebrew ile kurulsun mu? (brew install --cask docker)" e; then
        brew install --cask docker
    else
        hata "Docker Desktop'ı https://www.docker.com/products/docker-desktop/ adresinden kurup açın, sonra betiği yeniden çalıştırın."
    fi
fi
if ! docker info >/dev/null 2>&1; then
    bilgi "Docker Desktop başlatılıyor..."
    open -a Docker || true
    for _ in $(seq 1 60); do docker info >/dev/null 2>&1 && break; sleep 3; done
    docker info >/dev/null 2>&1 || hata "Docker Desktop açılmadı. Uygulamayı elle açıp betiği yeniden çalıştırın."
fi
if [ "$(uname -m)" = "arm64" ]; then
    uyari "Docker Desktop > Settings > General: 'Use Rosetta for x86_64/amd64 emulation on Apple Silicon' açık olmalı."
fi
uyari "Docker Desktop > Settings > General: 'Start Docker Desktop when you sign in' açık olmalı (program her açılışta başlasın)."
tamam "Docker hazır: $(docker --version)"

VERI_SAHIBI=0
kurulum_yap

baslik "Günlük yedek ve güncelleme görevi (launchd)"
ajanlar="$HOME/Library/LaunchAgents"
mkdir -p "$ajanlar"
saat="${YEDEK_SAATI%%:*}"; dakika="${YEDEK_SAATI##*:}"
cat >"$ajanlar/com.disci.yedek.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>Label</key><string>com.disci.yedek</string>
  <key>ProgramArguments</key><array><string>/bin/bash</string><string>$HEDEF/yedekle.sh</string></array>
  <key>StartCalendarInterval</key><dict><key>Hour</key><integer>$((10#$saat))</integer><key>Minute</key><integer>$((10#$dakika))</integer></dict>
  <key>EnvironmentVariables</key><dict><key>PATH</key><string>/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin</string></dict>
  <key>StandardOutPath</key><string>$HEDEF/veri/Yedekler/yedek.log</string>
  <key>StandardErrorPath</key><string>$HEDEF/veri/Yedekler/yedek.log</string>
</dict></plist>
EOF
cat >"$ajanlar/com.disci.guncelle.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>Label</key><string>com.disci.guncelle</string>
  <key>ProgramArguments</key><array><string>/bin/bash</string><string>$HEDEF/guncelle.sh</string></array>
  <key>StartInterval</key><integer>600</integer>
  <key>EnvironmentVariables</key><dict><key>PATH</key><string>/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin</string></dict>
  <key>StandardOutPath</key><string>$HEDEF/veri/Guncelleme/guncelle.log</string>
  <key>StandardErrorPath</key><string>$HEDEF/veri/Guncelleme/guncelle.log</string>
</dict></plist>
EOF
for a in com.disci.yedek com.disci.guncelle; do
    launchctl unload "$ajanlar/$a.plist" >/dev/null 2>&1 || true
    launchctl load "$ajanlar/$a.plist"
done
tamam "Her gün $YEDEK_SAATI'te yedek (Mac o saatte açıksa); güncelleme isteği 10 dakikada bir denetlenir."
uyari "Mac uykuya geçerse program erişilemez: Sistem Ayarları > Pil/Enerji > 'Ekran kapalıyken otomatik uyumayı engelle'."
