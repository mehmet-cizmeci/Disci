#!/usr/bin/env bash
# Disci'nin veritabanı bağlantısını değiştirir (Linux/macOS): Disci ile birlikte kurulan SQL Server, bu bilgisayarda
# kurulu bir SQL Server ya da başka bilgisayardaki bir SQL Server. Bağlantıyı dener, .env'yi yazar, programı yeniden başlatır.
#
#   sudo ./veritabani-ayarla.sh      (macOS'ta sudo olmadan)
#
# Veriler kendiliğinden taşınmaz: taşımak için önce ./yedekle.sh ile yedek alın, yedeği yeni sunucuda geri yükleyin.
set -euo pipefail
HEDEF="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$HEDEF/ortak.sh"
cd "$HEDEF"
[ -f .env ] || hata "Disci kurulu görünmüyor: $HEDEF/.env yok."
env_tamamla .env
case "$(uname -m)" in x86_64|amd64) ;; *) [ "$(uname -s)" = "Darwin" ] || SQL_DAHILI_OLUR=0 ;; esac

baslik "Şu anki veritabanı"
if dahili_sql_mi .env; then
    bilgi "Disci ile birlikte kurulan SQL Server (Docker içinde)"
else
    bilgi "$(env_degeri DB_BAGLANTI .env | sed -e "s/^'//" -e "s/'$//" -e 's/Password="[^"]*"/Password=***/')"
fi

baslik "Yeni veritabanı"
veritabani_sor
if [ "$DB_TUR" = "dahili" ] && ! dahili_sql_mi .env; then
    bilgi "SQL Server lisans koşulları: https://go.microsoft.com/fwlink/?LinkId=2104078"
    evet_mi "Microsoft SQL Server Express lisans koşullarını kabul ediyor musunuz?" e || hata "Lisans kabul edilmedi."
fi
uyari "Veriler yeni veritabanına kendiliğinden taşınmaz. Yeni veritabanı boşsa program ilk açılışta kurulum formunu gösterir."
evet_mi "Bağlantı değiştirilsin mi?" e || exit 1

eski_dahili=0; dahili_sql_mi .env && eski_dahili=1
cp .env ".env.onceki-$(date '+%Y%m%d%H%M')"
chmod 600 .env.onceki-*
veritabani_yaz .env
compose up -d --remove-orphans || hata "Program başlatılamadı. Eski ayar: .env.onceki-* dosyası."
if [ "$eski_dahili" = "1" ] && [ "$DB_TUR" != "dahili" ]; then
    bilgi "Disci ile birlikte kurulan SQL Server durduruldu; verileri 'disci_sql-veri' Docker biriminde duruyor (silinmedi)."
fi
port="$(env_degeri HTTP_PORT .env)"
program_bekle "${port:-5111}" || hata "Program yanıt vermedi: docker compose logs web"
tamam "Program yeni veritabanıyla çalışıyor."
