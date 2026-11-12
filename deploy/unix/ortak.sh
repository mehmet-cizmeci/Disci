#!/usr/bin/env bash
# Disci Linux/macOS betiklerinin ortak işlevleri. Doğrudan çalıştırılmaz; diğer betikler "source" ile yükler.

DISCI_DEPO="mehmet-cizmeci/Disci"

baslik() { printf '\n\033[36m== %s\033[0m\n' "$1"; }
bilgi()  { printf '   %s\n' "$1"; }
uyari()  { printf '   \033[33mUYARI: %s\033[0m\n' "$1"; }
tamam()  { printf '   \033[32m%s\033[0m\n' "$1"; }
hata()   { printf '\n\033[31mHATA: %s\033[0m\n' "$1" >&2; exit 1; }

# evet_mi "Soru" e|h  → varsayılanla birlikte sorar; DISCI_SORMA=1 ise varsayılanı seçer.
evet_mi() {
    local soru="$1" varsayilan="${2:-e}" cevap ek
    if [ "${DISCI_SORMA:-0}" = "1" ]; then [ "$varsayilan" = "e" ]; return; fi
    if [ "$varsayilan" = "e" ]; then ek="[E/h]"; else ek="[e/H]"; fi
    read -r -p "   $soru $ek " cevap || cevap=""
    cevap="$(printf '%s' "${cevap:-$varsayilan}" | tr '[:upper:]' '[:lower:]')"
    case "$cevap" in e|evet|y|yes) return 0 ;; *) return 1 ;; esac
}

# Docker Compose v2 ("docker compose") ya da eski "docker-compose".
compose() {
    if docker compose version >/dev/null 2>&1; then docker compose "$@"
    elif command -v docker-compose >/dev/null 2>&1; then docker-compose "$@"
    else hata "Docker Compose bulunamadı."; fi
}

# Harf, rakam ve işaret içeren rastgele parola (SQL Server parola kuralına uyar).
rastgele_parola() {
    local govde
    govde="$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 24)"
    printf 'Dc%s#9a' "$govde"
}

rastgele_anahtar() { LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 32; }

sha256_dosya() {
    if command -v sha256sum >/dev/null 2>&1; then sha256sum "$1" | awk '{print $1}'
    else shasum -a 256 "$1" | awk '{print $1}'; fi
}

# .env içinden bir değer: env_degeri ANAHTAR dosya
env_degeri() { grep -E "^$1=" "$2" 2>/dev/null | tail -n1 | cut -d= -f2-; }

# .env içinde bir değeri değiştirir ya da ekler (sed -i macOS ile Linux'ta farklı olduğu için dosya yeniden yazılır).
env_yaz() {
    local anahtar="$1" deger="$2" dosya="$3" gecici
    gecici="$(mktemp)"
    grep -vE "^$anahtar=" "$dosya" 2>/dev/null >"$gecici" || true
    printf '%s=%s\n' "$anahtar" "$deger" >>"$gecici"
    cat "$gecici" >"$dosya"
    rm -f "$gecici"
}

# Kendinden imzalı HTTPS sertifikası (bilgisayar adı, localhost ve IP adresleri için, 5 yıl).
sertifika_olustur() {
    local klasor="$1" ad ipler san
    command -v openssl >/dev/null 2>&1 || hata "openssl bulunamadı (sertifika için gerekli)."
    ad="$(hostname 2>/dev/null || echo disci)"
    if command -v hostname >/dev/null 2>&1 && hostname -I >/dev/null 2>&1; then
        ipler="$(hostname -I)"
    else
        ipler="$(ifconfig 2>/dev/null | awk '/inet / && $2 != "127.0.0.1" {print $2}')"
    fi
    san="DNS:$ad,DNS:localhost,IP:127.0.0.1"
    for ip in $ipler; do case "$ip" in *:*) ;; *) san="$san,IP:$ip" ;; esac; done
    mkdir -p "$klasor"
    openssl req -x509 -newkey rsa:2048 -nodes -days 1825 -subj "/CN=$ad" \
        -addext "subjectAltName=$san" -addext "extendedKeyUsage=serverAuth" \
        -keyout "$klasor/disci.key" -out "$klasor/disci.crt" >/dev/null 2>&1 \
        || hata "Sertifika oluşturulamadı (openssl 1.1.1 veya üstü gerekir)."
    chmod 644 "$klasor/disci.crt" "$klasor/disci.key"
}

# Program yanıt verene kadar bekler (en fazla 3 dakika).
program_bekle() {
    local port="$1" i
    for i in $(seq 1 60); do
        if curl -fsS -o /dev/null "http://localhost:$port/kilavuz" 2>/dev/null; then return 0; fi
        sleep 3
    done
    return 1
}

# ---- Veritabanı (SQL Server) seçimi -------------------------------------------------------------
# Disci ile birlikte SQL Server kurulduysa (Docker içinde) .env'deki COMPOSE_FILE docker-compose.sql.yml'i içerir.
dahili_sql_mi() { env_degeri COMPOSE_FILE "${1:-.env}" | grep -q 'docker-compose.sql.yml'; }

dahili_baglanti() { printf 'Server=sql;Database=Disci;User Id=sa;Password="%s";TrustServerCertificate=True;MultipleActiveResultSets=true' "$1"; }

# Eski (1.1.0) .env dosyalarına yeni değerleri ekler: o sürümde SQL Server her zaman Disci ile birlikte kuruluyordu.
env_tamamla() {
    local dosya="${1:-.env}" parola
    [ -f "$dosya" ] || return 0
    if [ -z "$(env_degeri DB_BAGLANTI "$dosya")" ] && parola="$(env_degeri SQL_PAROLA "$dosya")" && [ -n "$parola" ]; then
        env_yaz DB_BAGLANTI "'$(dahili_baglanti "$parola")'" "$dosya"
        env_yaz COMPOSE_FILE "docker-compose.yml:docker-compose.sql.yml" "$dosya"
    fi
}

# SQL Server'a bağlanmayı dener (Microsoft'un sqlcmd araç imajıyla). Başarılıysa 0.
baglanti_dene() {
    local sunucu="$1" kullanici="$2" parola="$3"
    docker run --rm --platform linux/amd64 --add-host host.docker.internal:host-gateway mcr.microsoft.com/mssql-tools \
        /opt/mssql-tools/bin/sqlcmd -S "$sunucu" -U "$kullanici" -P "$parola" -l 15 -b -Q "SELECT 1" -o /dev/null >/dev/null 2>&1
}

# Veritabanının nerede olacağını sorar. Sonuç: DB_TUR=dahili|harici, harici ise DB_BAGLANTI.
# Komut satırı: DB_ARG_SUNUCU, DB_ARG_KULLANICI, DB_ARG_PAROLA, DB_ARG_VERITABANI, DB_ARG_DAHILI=1; SQL_DAHILI_OLUR=0 (ARM Linux).
veritabani_sor() {
    local secim sunucu port kullanici parola vt
    DB_TUR=""; DB_BAGLANTI=""
    if [ "${DB_ARG_DAHILI:-0}" = "1" ]; then DB_TUR=dahili; return; fi
    if [ -n "${DB_ARG_SUNUCU:-}" ]; then
        sunucu="$DB_ARG_SUNUCU"; kullanici="${DB_ARG_KULLANICI:-sa}"; parola="${DB_ARG_PAROLA:-}"; vt="${DB_ARG_VERITABANI:-Disci}"
    elif [ "${DISCI_SORMA:-0}" = "1" ]; then
        if [ "${SQL_DAHILI_OLUR:-1}" = "1" ]; then DB_TUR=dahili; return; fi
        hata "Bu bilgisayarda SQL Server Disci ile birlikte kurulamaz; --sql-sunucu ile var olan bir sunucu verin."
    else
        echo
        bilgi "Disci verilerini hangi SQL Server'da tutsun?"
        if [ "${SQL_DAHILI_OLUR:-1}" = "1" ]; then
            bilgi "  1) Disci ile birlikte yeni SQL Server Express kur (Docker içinde; önerilen)"
        else
            bilgi "  1) (Bu işlemcide kullanılamaz: SQL Server yalnızca x86-64 için var)"
        fi
        bilgi "  2) Bu bilgisayarda kurulu SQL Server'ı kullan"
        bilgi "  3) Başka bir bilgisayardaki SQL Server'ı kullan"
        while true; do
            read -r -p "   Seçiminiz [$([ "${SQL_DAHILI_OLUR:-1}" = "1" ] && echo 1 || echo 2)] " secim || secim=""
            secim="${secim:-$([ "${SQL_DAHILI_OLUR:-1}" = "1" ] && echo 1 || echo 2)}"
            case "$secim" in
                1) [ "${SQL_DAHILI_OLUR:-1}" = "1" ] && { DB_TUR=dahili; return; } ;;
                2) read -r -p "   SQL Server portu [1433] " port || port=""; sunucu="host.docker.internal,${port:-1433}"; break ;;
                3) while [ -z "${sunucu:-}" ]; do read -r -p "   Sunucu (ör. 192.168.1.10,1433 ya da SUNUCU\SQLEXPRESS) " sunucu || true; done; break ;;
            esac
        done
        bilgi "Linux/macOS'tan yalnızca SQL kullanıcı adı ve parolasıyla bağlanılır (Windows kimlik doğrulaması olmaz)."
        read -r -p "   SQL kullanıcı adı [sa] " kullanici || kullanici=""; kullanici="${kullanici:-sa}"
        read -r -s -p "   Parola " parola || parola=""; echo
        read -r -p "   Veritabanı adı [Disci] " vt || vt=""; vt="${vt:-Disci}"
    fi
    case "$parola" in *\'*|*\"*) hata "Parolada tırnak işareti (' veya \") olamaz." ;; esac
    [[ "$vt" =~ ^[A-Za-z0-9_]{1,100}$ ]] || hata "Veritabanı adında yalnızca harf, rakam ve _ olabilir."
    bilgi "Bağlantı deneniyor: $sunucu ..."
    if baglanti_dene "$sunucu" "$kullanici" "$parola"; then
        tamam "SQL Server bağlantısı tamam."
    else
        uyari "Bağlanılamadı (sunucu adı, port, kullanıcı/parola ya da sunucunun ağ ayarları). SQL Server'da TCP/IP açık olmalı."
        evet_mi "Yine de bu bilgilerle devam edilsin mi?" h || hata "Kurulum durduruldu; bilgileri kontrol edip yeniden çalıştırın."
    fi
    DB_TUR=harici
    DB_BAGLANTI="Server=$sunucu;Database=$vt;User Id=$kullanici;Password=\"$parola\";TrustServerCertificate=True;MultipleActiveResultSets=true"
}

# Seçimi .env'ye yazar (dahili SQL için parola yoksa üretir).
veritabani_yaz() {
    local dosya="$1" parola
    if [ "$DB_TUR" = "dahili" ]; then
        parola="$(env_degeri SQL_PAROLA "$dosya")"
        [ -n "$parola" ] || { parola="$(rastgele_parola)"; env_yaz SQL_PAROLA "$parola" "$dosya"; }
        env_yaz DB_BAGLANTI "'$(dahili_baglanti "$parola")'" "$dosya"
        env_yaz COMPOSE_FILE "docker-compose.yml:docker-compose.sql.yml" "$dosya"
    else
        env_yaz DB_BAGLANTI "'$DB_BAGLANTI'" "$dosya"
        env_yaz COMPOSE_FILE "docker-compose.yml" "$dosya"
    fi
    chmod 600 "$dosya"
}
