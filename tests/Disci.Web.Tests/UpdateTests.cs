using System.Net;
using Disci.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Disci.Web.Tests;

/// <summary>In-app update: version parsing, the request file left for the update task, and the admin page.</summary>
public sealed class UpdateTests(DisciWebFactory factory) : IClassFixture<DisciWebFactory>
{
    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3+abcdef", "1.2.3")]
    [InlineData("V2.0", "2.0.0")]
    public void Versions_are_parsed_from_tags(string tag, string expected) =>
        Assert.Equal(Version.Parse(expected), UpdateService.ParseVersion(tag));

    [Fact]
    public void Garbage_tag_is_not_a_version() => Assert.Null(UpdateService.ParseVersion("son-surum"));

    [Fact]
    public void Running_version_comes_from_the_build() => Assert.True(UpdateService.CurrentVersion >= new Version(1, 1, 0));

    [Fact]
    public async Task Install_request_and_auto_setting_are_written_for_the_update_task()
    {
        var updates = factory.Services.GetRequiredService<UpdateService>();
        var release = new ReleaseInfo("v9.9.9", new Version(9, 9, 9), "Disci 9.9.9", "", "https://example.invalid", null);

        await updates.RequestInstallAsync(release, "yonetici@ornek.test");
        var json = await File.ReadAllTextAsync(Path.Combine(updates.Folder, "istek.json"));
        Assert.Contains("\"tag\": \"v9.9.9\"", json);          // guncelle.ps1 / guncelle.sh read these names
        Assert.Contains("\"requestedBy\"", json);
        Assert.Equal("v9.9.9", updates.PendingRequest()?.Tag);
        await updates.CancelRequestAsync();
        Assert.Null(updates.PendingRequest());

        await updates.SetAutoInstallAsync(true);
        Assert.Contains("\"automatic\": true", await File.ReadAllTextAsync(Path.Combine(updates.Folder, "ayar.json")));
        Assert.True(updates.AutoInstall);

        // Written by the update task (format of guncelle.sh).
        await File.WriteAllTextAsync(Path.Combine(updates.Folder, "durum.json"),
            "{\n  \"state\": \"tamam\",\n  \"message\": \"Disci 9.9.9 kuruldu\",\n  \"version\": \"9.9.9\",\n  \"time\": \"2026-11-09T11:30:50+03:00\"\n}\n");
        Assert.Equal("tamam", updates.Status()?.State);
    }

    [Fact]
    public async Task Update_page_is_for_admins()
    {
        var html = WebUtility.HtmlDecode(await factory.ClientAs(Disci.Infrastructure.Identity.Roles.Admin).GetStringAsync("/guncelleme"));
        Assert.Contains("Kurulu sürüm", html);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.ClientAs(Disci.Infrastructure.Identity.Roles.Dentist).GetAsync("/guncelleme")).StatusCode);
    }

    [Fact]
    public void Setup_form_opens_from_another_address_only_with_the_installers_key()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FirstRunSetup:Token"] = "AbCdEfGhIjKlMnOpQrStUvWx" }).Build();
        var setup = new FirstRunSetup(null!, config);

        HttpContext Request(string? query)
        {
            var http = new DefaultHttpContext();
            http.Connection.RemoteIpAddress = IPAddress.Parse("172.17.0.1");   // Docker network, not loopback
            http.Connection.LocalIpAddress = IPAddress.Parse("172.17.0.2");
            if (query is not null) http.Request.QueryString = new QueryString(query);
            return http;
        }

        Assert.False(setup.IsAllowed(Request(null)));
        Assert.False(setup.IsAllowed(Request("?anahtar=yanlis-anahtar-yanlis")));
        Assert.True(setup.IsAllowed(Request("?anahtar=AbCdEfGhIjKlMnOpQrStUvWx")));

        var local = new DefaultHttpContext();
        local.Connection.RemoteIpAddress = IPAddress.Loopback;
        Assert.True(new FirstRunSetup(null!, new ConfigurationBuilder().Build()).IsAllowed(local));
    }
}

/// <summary>English screen language: the cookie switch and the scripts that translate the page.</summary>
public sealed class LanguageTests(DisciWebFactory factory) : IClassFixture<DisciWebFactory>
{
    [Fact]
    public async Task English_cookie_loads_the_translation_scripts()
    {
        var turkish = await factory.ClientAs(null).GetStringAsync("/kilavuz");
        Assert.Contains("<html lang=\"tr\"", turkish);
        Assert.DoesNotContain("src=\"i18n/en", turkish);   // the import map lists every script; only a script tag loads it

        var client = factory.ClientAs(null);
        client.DefaultRequestHeaders.Add("Cookie", "disci-dil=en");
        var english = await client.GetStringAsync("/kilavuz");
        Assert.Contains("<html lang=\"en\"", english);
        Assert.Contains("src=\"i18n/en", english);
        Assert.Contains("src=\"i18n/i18n", english);
    }

    [Fact]
    public async Task Language_switch_sets_the_cookie_and_only_returns_to_local_pages()
    {
        var response = await factory.ClientAs(null).GetAsync("/dil/en?geri=/Account/Login");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location!.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("disci-dil=en"));

        var outside = await factory.ClientAs(null).GetAsync("/dil/en?geri=//kotu.example");
        Assert.Equal("/", outside.Headers.Location!.OriginalString);
    }
}

/// <summary>The database panel on Klinik ayarları reads the connection the installer wrote (never shows the password).</summary>
public sealed class DatabaseInfoTests
{
    private static DatabaseInfo Read(string connection) =>
        DatabaseInfo.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connection }).Build());

    [Fact]
    public void Sql_login_written_by_the_installer_is_understood()
    {
        // Exactly what kur.ps1 (Baglanti-Metni) produces for a SQL login whose password contains ; and ".
        var info = Read("Data Source=SUNUCU\\SQLEXPRESS;Initial Catalog=KlinikDB;Integrated Security=False;User ID=disci;Password='Pa;ss\"w=rd';MultipleActiveResultSets=True;Connect Timeout=30;TrustServerCertificate=True");
        Assert.Equal("SUNUCU\\SQLEXPRESS", info.Server);
        Assert.Equal("KlinikDB", info.Database);
        Assert.Equal("SQL kullanıcısı: disci", info.Authentication);
        Assert.DoesNotContain("Pa;ss", info.ToString());
    }

    [Fact]
    public void Windows_authentication_is_shown_as_such()
    {
        var info = Read("Data Source=.\\SQLEXPRESS;Initial Catalog=Disci;Integrated Security=True;TrustServerCertificate=True");
        Assert.Equal("Windows kimlik doğrulaması", info.Authentication);
        Assert.Contains("veritabani-ayarla.ps1", info.HowToChange);
    }
}
