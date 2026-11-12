# Disci web programı (Linux ve macOS'ta Docker içinde). kur-linux.sh / kur-macos.sh bu imajı kurulum klasöründe derler.
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Türkiye saati ve Türkçe sıralama için saat dilimi verisi.
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY app/ /app/

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 5111 5443

# İmajdaki kısıtlı kullanıcı (UID 1654); veri klasörü kurulumda bu kullanıcıya verilir.
USER app
ENTRYPOINT ["dotnet", "Disci.Web.dll"]
