#!/usr/bin/env bash
# kur-linux.sh ve kur-macos.sh'in ortak kurulum adımları. Değişkenler: PAKET HEDEF HTTP_PORT HTTPS_PORT VERI_SAHIBI
# (VERI_SAHIBI=1: Linux'ta veri klasörünü konteyner kullanıcılarına ver). Doğrudan çalıştırılmaz.

kurulum_yap() {
    local surum ilk=0
    surum="$(cat "$PAKET/SURUM" 2>/dev/null || echo yerel)"

    baslik "Program dosyaları ($HEDEF)"
    mkdir -p "$HEDEF"
    [ -f "$HEDEF/.env" ] || ilk=1
    if [ -d "$HEDEF/app" ]; then
        rm -rf "$HEDEF/app.onceki"
        mv "$HEDEF/app" "$HEDEF/app.onceki"
    fi
    cp -R "$PAKET/app" "$HEDEF/app"
    for f in Dockerfile .dockerignore docker-compose.yml docker-compose.sql.yml ortak.sh yedekle.sh guncelle.sh kaldir.sh geri-yukle.sh veritabani-ayarla.sh OKUBENI.md SURUM LISANS.txt; do
        [ -e "$PAKET/$f" ] && cp "$PAKET/$f" "$HEDEF/$f"
    done
    chmod +x "$HEDEF"/*.sh
    tamam "Disci $surum kopyalandı."

    baslik "Ayarlar"
    mkdir -p "$HEDEF/veri/files" "$HEDEF/veri/keys" "$HEDEF/veri/Guncelleme" "$HEDEF/veri/Yedekler" "$HEDEF/veri/sertifika"
    if [ "$ilk" = "1" ]; then
        # SQL Server kurmak zorunlu değil: Disci ile birlikte kurulabilir ya da var olan bir sunucu kullanılabilir.
        veritabani_sor
        if [ "$DB_TUR" = "dahili" ]; then
            bilgi "SQL Server lisans koşulları: https://go.microsoft.com/fwlink/?LinkId=2104078"
            evet_mi "Microsoft SQL Server Express lisans koşullarını kabul ediyor musunuz?" e || hata "Lisans kabul edilmedi."
        fi
        umask 077
        {
            echo "# Disci ayarları (kurulum yazdı). Veritabanı parolası içerir: kimseyle paylaşmayın, silmeyin."
            echo "KURULUM_ANAHTARI=$(rastgele_anahtar)"
        } >"$HEDEF/.env"
        umask 022
        veritabani_yaz "$HEDEF/.env"
        tamam "Ayarlar yazıldı ($HEDEF/.env): $([ "$DB_TUR" = "dahili" ] && echo "SQL Server Disci ile birlikte kurulacak" || echo "var olan SQL Server kullanılacak")."
    else
        env_tamamla "$HEDEF/.env"
        bilgi "Var olan ayarlar ve veriler korunuyor ($(dahili_sql_mi "$HEDEF/.env" && echo "Disci ile birlikte kurulan SQL Server" || echo "var olan SQL Server"))."
    fi
    env_yaz DISCI_SURUM "$surum" "$HEDEF/.env"
    env_yaz HTTP_PORT "$HTTP_PORT" "$HEDEF/.env"
    env_yaz HTTPS_PORT "$HTTPS_PORT" "$HEDEF/.env"
    [ -n "$(env_degeri TZ "$HEDEF/.env")" ] || env_yaz TZ "Europe/Istanbul" "$HEDEF/.env"

    if [ ! -f "$HEDEF/veri/sertifika/disci.crt" ]; then
        sertifika_olustur "$HEDEF/veri/sertifika"
        tamam "HTTPS sertifikası oluşturuldu: $HEDEF/veri/sertifika/disci.crt (diğer cihazlara bu dosya kurulur)"
    fi
    if [ "${VERI_SAHIBI:-0}" = "1" ]; then
        # Program konteynerde UID 1654 (app), SQL Server UID 10001 (mssql) ile çalışır.
        chown -R 1654:1654 "$HEDEF/veri"
        chown -R 10001:0 "$HEDEF/veri/Yedekler"
        chmod 600 "$HEDEF/veri/sertifika/disci.key"
        chmod 750 "$HEDEF/veri"
    fi

    baslik "Docker imajı ve programın başlatılması"
    (cd "$HEDEF" && compose build web) || hata "Disci imajı derlenemedi."
    (cd "$HEDEF" && compose up -d) || hata "Disci başlatılamadı. Ayrıntı: cd $HEDEF && docker compose logs"
    bilgi "SQL Server ilk açılışta 1-2 dakika hazırlanır; program bekleniyor..."
    if program_bekle "$HTTP_PORT"; then
        tamam "Program çalışıyor."
        rm -rf "$HEDEF/app.onceki"
    else
        hata "Program yanıt vermedi. Ayrıntı: cd $HEDEF && docker compose logs web sql"
    fi

    baslik "Kurulum tamamlandı"
    local ad
    ad="$(hostname 2>/dev/null || echo localhost)"
    bilgi "Bu bilgisayardan     : https://localhost:$HTTPS_PORT"
    bilgi "Ağdaki diğer cihazlar: https://$ad:$HTTPS_PORT  (önce sertifikayı kurun: $HEDEF/veri/sertifika/disci.crt)"
    bilgi "Yedekler             : $HEDEF/veri/Yedekler"
    if [ "$ilk" = "1" ]; then
        echo
        tamam "İlk kurulum formunu açın (klinik bilgileri ve yönetici hesabı):"
        bilgi "https://localhost:$HTTPS_PORT/kurulum?anahtar=$(env_degeri KURULUM_ANAHTARI "$HEDEF/.env")"
    fi
}
