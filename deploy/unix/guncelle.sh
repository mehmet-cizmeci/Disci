#!/usr/bin/env bash
# Disci güncellemesi (Linux/macOS). Kurulum 10 dakikada bir çalıştırır (Linux: /etc/cron.d/disci, macOS: launchd).
#
# Ne zaman kurar:
#   - Program "Güncelleme" sayfasında istek bıraktıysa (veri/Guncelleme/istek.json): istenen sürümü,
#   - "otomatik güncelle" açıksa (veri/Guncelleme/ayar.json): gece 03:00-05:00 arasında en yeni sürümü,
#   - elle: ./guncelle.sh v1.2.0   (ya da ./guncelle.sh son)
# Paketi her zaman kendisi, sabit GitHub adresinden indirir ve SHA256SUMS.txt ile doğrular. Önce yedek alınır; yeni
# sürüm açılmazsa eski sürüme dönülür. Sonuç veri/Guncelleme/durum.json'a (programın Güncelleme sayfası) yazılır.
set -uo pipefail
HEDEF="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$HEDEF/ortak.sh"
cd "$HEDEF"

KLASOR="$HEDEF/veri/Guncelleme"
ISTEK="$KLASOR/istek.json"
AYAR="$KLASOR/ayar.json"
DURUM="$KLASOR/durum.json"
SON_DENETIM="$KLASOR/son-otomatik-denetim"
mkdir -p "$KLASOR"

json_degeri() { sed -n "s/.*\"$1\"[[:space:]]*:[[:space:]]*\"\{0,1\}\([^\",}]*\).*/\1/p" "$2" | head -n1; }

durum() {
    local zaman; zaman="$(date '+%Y-%m-%dT%H:%M:%S%z')"
    printf '{\n  "state": "%s",\n  "message": "%s",\n  "version": "%s",\n  "time": "%s"\n}\n' \
        "$1" "$(printf '%s' "$2" | sed 's/["\\]/ /g')" "$3" "${zaman:0:22}:${zaman:22:2}" >"$DURUM"
    chmod 644 "$DURUM"
    echo "$(date '+%F %T')  [$1] $2"
}

surum_sayisi() { printf '%s' "$1" | sed -n 's/^v\{0,1\}\([0-9]*\)\.\([0-9]*\)\.\([0-9]*\).*/\1 \2 \3/p' | awk '{printf "%d%03d%03d", $1, $2, $3}'; }

kurulu="$(cat "$HEDEF/SURUM" 2>/dev/null || echo 0.0.0)"

# ---- Hangi sürüm? -------------------------------------------------------------
etiket=""; isteyen="elle"
if [ $# -ge 1 ]; then
    etiket="$1"
elif [ -f "$ISTEK" ]; then
    etiket="$(json_degeri tag "$ISTEK")"
    isteyen="$(json_degeri requestedBy "$ISTEK")"
    rm -f "$ISTEK"
    [ -n "$etiket" ] || { durum hata "Güncelleme isteği okunamadı." "$kurulu"; exit 1; }
else
    otomatik="$(json_degeri automatic "$AYAR" 2>/dev/null)"
    saat=$((10#$(date +%H)))
    [ "$otomatik" = "true" ] || exit 0
    [ "$saat" -ge 3 ] && [ "$saat" -lt 5 ] || exit 0
    if [ -f "$SON_DENETIM" ] && [ -n "$(find "$SON_DENETIM" -mmin -1200 2>/dev/null)" ]; then exit 0; fi
    touch "$SON_DENETIM"
    etiket="son"; isteyen="otomatik"
fi

hata_ver() { durum hata "$1" "$kurulu"; exit 1; }

# ---- Sürüm bilgisi ------------------------------------------------------------
api="https://api.github.com/repos/$DISCI_DEPO/releases"
if [ "$etiket" = "son" ]; then adres="$api/latest"; else
    [[ "$etiket" =~ ^v?[0-9]+\.[0-9]+\.[0-9]+$ ]] || hata_ver "Geçersiz sürüm adı: $etiket"
    [[ "$etiket" == v* ]] || etiket="v$etiket"
    adres="$api/tags/$etiket"
fi
GECICI="$(mktemp -d)"
trap 'rm -rf "$GECICI"' EXIT
curl -fsSL -H 'User-Agent: Disci-Updater' -H 'Accept: application/vnd.github+json' "$adres" -o "$GECICI/surum.json" \
    || hata_ver "GitHub'a ulaşılamadı."
yeni="$(json_degeri tag_name "$GECICI/surum.json")"; yeni="${yeni#v}"
[ -n "$yeni" ] || hata_ver "GitHub'daki sürüm adı anlaşılamadı."
if [ "$isteyen" = "otomatik" ] && [ "$(surum_sayisi "$yeni")" -le "$(surum_sayisi "$kurulu")" ]; then
    echo "$(date '+%F %T')  Otomatik denetim: kurulu $kurulu, en yeni $yeni; güncelleme yok."; exit 0
fi
[ "$yeni" != "$kurulu" ] || { durum tamam "Disci $kurulu zaten kurulu." "$kurulu"; exit 0; }

paket_adresi="$(grep -o '"browser_download_url"[[:space:]]*:[[:space:]]*"[^"]*linux-macos\.tar\.gz"' "$GECICI/surum.json" | sed 's/.*"\(https[^"]*\)"/\1/' | head -n1)"
ozet_adresi="$(grep -o '"browser_download_url"[[:space:]]*:[[:space:]]*"[^"]*SHA256SUMS\.txt"' "$GECICI/surum.json" | sed 's/.*"\(https[^"]*\)"/\1/' | head -n1)"
[ -n "$paket_adresi" ] || hata_ver "v$yeni sürümünde Linux/macOS paketi (*-linux-macos.tar.gz) yok."
[ -n "$ozet_adresi" ] || hata_ver "v$yeni sürümünde SHA256SUMS.txt yok; paket doğrulanamayacağı için kurulmadı."

# ---- İndir, doğrula -----------------------------------------------------------
durum indiriliyor "Disci $yeni indiriliyor ($isteyen istedi)." "$yeni"
paket_adi="$(basename "$paket_adresi")"
curl -fsSL -H 'User-Agent: Disci-Updater' "$paket_adresi" -o "$GECICI/$paket_adi" || hata_ver "Paket indirilemedi."
curl -fsSL -H 'User-Agent: Disci-Updater' "$ozet_adresi" -o "$GECICI/SHA256SUMS.txt" || hata_ver "SHA256SUMS.txt indirilemedi."
beklenen="$(awk -v ad="$paket_adi" '$2 == ad || $2 == "*"ad {print tolower($1)}' "$GECICI/SHA256SUMS.txt" | head -n1)"
gercek="$(sha256_dosya "$GECICI/$paket_adi")"
[ -n "$beklenen" ] && [ "$beklenen" = "$gercek" ] || hata_ver "İndirilen paketin SHA256 özeti tutmuyor; kurulmadı."
tar -xzf "$GECICI/$paket_adi" -C "$GECICI" || hata_ver "Paket açılamadı."
YENI_PAKET="$GECICI/Disci-Kurulum-unix"
[ -f "$YENI_PAKET/app/Disci.Web.dll" ] || hata_ver "Paketin içinde app/Disci.Web.dll yok."

# ---- Kur ----------------------------------------------------------------------
durum kuruluyor "Disci $kurulu → $yeni kuruluyor. Önce yedek alınıyor." "$yeni"
"$HEDEF/yedekle.sh" >>"$HEDEF/veri/Yedekler/yedek.log" 2>&1 || hata_ver "Yedek alınamadı; güvenlik için güncelleme yapılmadı."

eski_imaj="$(env_degeri DISCI_SURUM "$HEDEF/.env")"
rm -rf "$HEDEF/app.onceki"
mv "$HEDEF/app" "$HEDEF/app.onceki"
cp -R "$YENI_PAKET/app" "$HEDEF/app"
for f in Dockerfile .dockerignore docker-compose.yml docker-compose.sql.yml ortak.sh yedekle.sh guncelle.sh kaldir.sh geri-yukle.sh veritabani-ayarla.sh OKUBENI.md SURUM LISANS.txt; do
    [ -e "$YENI_PAKET/$f" ] && cp "$YENI_PAKET/$f" "$HEDEF/$f"
done
chmod +x "$HEDEF"/*.sh
env_yaz DISCI_SURUM "$yeni" "$HEDEF/.env"
env_tamamla "$HEDEF/.env"

port="$(env_degeri HTTP_PORT "$HEDEF/.env")"
if compose build web >/dev/null 2>&1 && compose up -d web >/dev/null 2>&1 && program_bekle "${port:-5111}"; then
    rm -rf "$HEDEF/app.onceki"
    durum tamam "Disci $yeni kuruldu (önceki sürüm $kurulu, $isteyen istedi)." "$yeni"
    exit 0
fi

# ---- Açılmadı: geri dön ---------------------------------------------------------
rm -rf "$HEDEF/app"
mv "$HEDEF/app.onceki" "$HEDEF/app"
printf '%s' "$kurulu" >"$HEDEF/SURUM"
env_yaz DISCI_SURUM "$eski_imaj" "$HEDEF/.env"
compose up -d web >/dev/null 2>&1 || compose build web >/dev/null 2>&1 && compose up -d web >/dev/null 2>&1
durum hata "Disci $yeni açılmadı; $kurulu sürümüne geri dönüldü. Güncelleme öncesi yedek: veri/Yedekler. Ayrıntı: docker compose logs web" "$kurulu"
exit 1
