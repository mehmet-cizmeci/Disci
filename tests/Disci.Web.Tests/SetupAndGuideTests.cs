using System.Net;
using Disci.Infrastructure;
using Disci.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disci.Web.Tests;

/// <summary>A test database without accounts, with the first-run redirect switched on.</summary>
public sealed class SetupWebFactory : DisciWebFactory
{
    public SetupWebFactory() => FirstRunSetupEnabled = true;
}

/// <summary>The first-run setup form (new clinic) and the user guide.</summary>
public sealed class SetupAndGuideTests(SetupWebFactory factory) : IClassFixture<SetupWebFactory>
{
    private static async Task<string> Page(HttpClient client, string url) => WebUtility.HtmlDecode(await client.GetStringAsync(url));

    [Fact]
    public async Task New_clinic_is_set_up_through_the_form_then_signs_in_normally()
    {
        // No accounts yet: the home and sign-in pages lead to the setup form.
        var redirect = await factory.ClientAs(null).GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.EndsWith("/kurulum", redirect.Headers.Location!.OriginalString);
        Assert.Contains("Kurulumu tamamla", await Page(factory.ClientAs(null), "/kurulum"));

        using (var scope = factory.Services.CreateScope())
        {
            var setup = scope.ServiceProvider.GetRequiredService<SetupService>();
            var weak = await setup.RunAsync(new SetupModel { ClinicName = "Uydurma Klinik", FullName = "Deneme Yönetici", Email = "yonetici@ornek.test", Password = "kisa", ConfirmPassword = "kisa" });
            Assert.NotEmpty(weak);   // Turkish password rules, nothing created

            var errors = await setup.RunAsync(new SetupModel
            {
                ClinicName = "Uydurma Diş Kliniği", Address = "Deneme Sok. 1", Phone = "0000 000 00 00",
                FullName = "Deneme Yönetici", DentistName = "Dt. Deneme Hekim", Email = "yonetici@ornek.test",
                Password = "Deneme1234", ConfirmPassword = "Deneme1234",
            });
            Assert.Empty(errors);

            var db = scope.ServiceProvider.GetRequiredService<DisciDbContext>();
            Assert.Equal("Uydurma Diş Kliniği", (await db.ClinicProfiles.SingleAsync()).Name);
            Assert.True(await db.Dentists.AnyAsync(d => d.FullName == "Dt. Deneme Hekim"));
            Assert.NotEmpty(await setup.RunAsync(new SetupModel { ClinicName = "x", FullName = "x", Email = "b@ornek.test", Password = "Deneme1234", ConfirmPassword = "Deneme1234" }));
        }

        Assert.Equal(HttpStatusCode.OK, (await factory.ClientAs(null).GetAsync("/Account/Login")).StatusCode);
        Assert.Contains("Kurulum tamamlanmış", await Page(factory.ClientAs(null), "/kurulum"));
    }

    [Fact]
    public async Task User_guide_is_readable_without_signing_in()
    {
        var html = await Page(factory.ClientAs(null), "/kilavuz");
        Assert.Contains("Kullanım kılavuzu", html);
        Assert.Contains("İçindekiler", html);
        Assert.Contains("Randevu", html);
    }

    [Fact]
    public void Turkish_password_messages()
    {
        var errors = new TurkishIdentityErrors();
        Assert.Contains("rakam", errors.PasswordRequiresDigit().Description);
    }
}
