using System.Net;
using Disci.Infrastructure.Identity;

namespace Disci.Web.Tests;

/// <summary>Pages render, the seeded data shows up and access rules hold.</summary>
public sealed class PageTests(DisciWebFactory factory) : IClassFixture<DisciWebFactory>
{
    /// <summary>Blazor writes non-ASCII text as HTML entities (ı → &amp;#x131;); compare against the decoded text.</summary>
    private static async Task<string> Page(HttpClient client, string url) =>
        WebUtility.HtmlDecode(await client.GetStringAsync(url));

    [Theory]
    [InlineData("/")]
    [InlineData("/hastalar")]
    [InlineData("/yonetim/kullanicilar")]
    public async Task Anonymous_users_are_sent_to_login(string url)
    {
        var response = await factory.ClientAs(null).GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Login_page_is_public_and_turkish()
    {
        var html = await Page(factory.ClientAs(null), "/Account/Login");
        Assert.Contains("Giriş yap", html);
        Assert.DoesNotContain("Register", html);
    }

    [Fact]
    public async Task Home_shows_patient_count_and_recent_visit()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), "/");
        Assert.Contains("Kayıtlı hasta", html);
        Assert.Contains($">{factory.Data.PatientCount}<", html);
        Assert.Contains(factory.Data.LastName, html);
    }

    [Fact]
    public async Task Patient_list_searches_and_hides_archived()
    {
        var client = factory.ClientAs(Roles.Assistant);
        var found = await Page(client, "/hastalar?ara=" + Uri.EscapeDataString(factory.Data.LastName));
        Assert.Contains(factory.Data.LastName, found);
        Assert.DoesNotContain("KİŞİ", found);

        var all = await Page(client, "/hastalar");
        Assert.DoesNotContain("KAYDI", all);
    }

    [Fact]
    public async Task Patient_list_finds_spaced_phone_numbers()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), "/hastalar?ara=5550000001");
        Assert.Contains(factory.Data.LastName, html);
    }

    [Fact]
    public async Task Patient_detail_shows_treatments_and_balance()
    {
        var html = await Page(factory.ClientAs(Roles.Dentist), $"/hastalar/{factory.Data.PatientId}");
        Assert.Contains(factory.Data.LastName, html);
        Assert.Contains("Deneme dolgu", html);
        Assert.Contains("Kalan borç", html);
        Assert.Contains("500,00 ₺", html);
    }

    [Fact]
    public async Task Photo_is_served_to_signed_in_users_only()
    {
        var ok = await factory.ClientAs(Roles.Assistant).GetAsync($"/files/photos/{factory.Data.PhotoId}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("image/jpeg", ok.Content.Headers.ContentType?.MediaType);

        var anonymous = await factory.ClientAs(null).GetAsync($"/files/photos/{factory.Data.PhotoId}");
        Assert.NotEqual(HttpStatusCode.OK, anonymous.StatusCode);
    }

    [Fact]
    public async Task User_admin_is_for_admins_only()
    {
        var admin = await Page(factory.ClientAs(Roles.Admin), "/yonetim/kullanicilar");
        Assert.Contains("Yeni kullanıcı", admin);

        var assistant = await factory.ClientAs(Roles.Assistant).GetAsync("/yonetim/kullanicilar");
        Assert.Equal(HttpStatusCode.Forbidden, assistant.StatusCode);
    }
}
