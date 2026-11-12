using System.Net;
using Disci.Domain;
using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Disci.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disci.Web.Tests;

/// <summary>
/// Dentists' sharing settings: who sees whose patients, appointments and treatments. Runs on its own test database
/// because it adds a dentist who hides their records, which would change what the other test classes count.
/// </summary>
public sealed class AccessTests(DisciWebFactory factory) : IClassFixture<DisciWebFactory>
{
    private static async Task<string> Page(HttpClient client, string url) =>
        WebUtility.HtmlDecode(await client.GetStringAsync(url));

    private static DataAccessRules.DentistSharing Sharing(int id, DataAccess dentists, DataAccess assistants) => new(id, dentists, assistants);

    [Fact]
    public void Admin_and_own_dentist_always_have_full_access()
    {
        var dentists = new[] { Sharing(1, DataAccess.None, DataAccess.None), Sharing(2, DataAccess.None, DataAccess.None) };
        var admin = DataAccessRules.Compute([Roles.Admin], null, dentists, new Dictionary<int, DataAccess>());
        Assert.True(admin.SeesAll);
        Assert.True(admin.CanEdit(1) && admin.CanEdit(2));

        var own = DataAccessRules.Compute([Roles.Dentist], 2, dentists, new Dictionary<int, DataAccess>());
        Assert.False(own.CanSee(1));
        Assert.True(own.CanEdit(2));
        Assert.Equal([2], own.VisibleIds);
    }

    [Fact]
    public void Role_setting_applies_and_a_personal_exception_wins()
    {
        var dentists = new[] { Sharing(1, DataAccess.Edit, DataAccess.View) };
        var assistant = DataAccessRules.Compute([Roles.Assistant], null, dentists, new Dictionary<int, DataAccess>());
        Assert.True(assistant.CanSee(1));
        Assert.False(assistant.CanEdit(1));

        var blocked = DataAccessRules.Compute([Roles.Dentist], null, dentists, new Dictionary<int, DataAccess> { [1] = DataAccess.None });
        Assert.False(blocked.CanSee(1));
    }

    [Fact]
    public void Records_without_a_dentist_belong_to_everyone()
    {
        var scope = DataAccessRules.Compute([Roles.Assistant], null, [Sharing(1, DataAccess.None, DataAccess.None)], new Dictionary<int, DataAccess>());
        Assert.True(scope.CanEdit(null));
        Assert.False(scope.SeesAll);
    }

    [Fact]
    public async Task Private_dentists_patient_is_hidden_from_assistants_but_not_from_admins()
    {
        int patientId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DisciDbContext>();
            var dentist = new Dentist { FullName = "Gizli Hekim", ShareWithDentists = DataAccess.View, ShareWithAssistants = DataAccess.None };
            var patient = new Patient { FirstName = "Saklı", LastName = "HASTAOĞLU", PrimaryDentist = dentist, RegisteredOn = DateOnly.FromDateTime(DateTime.Today) };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            patientId = patient.Id;
        }

        var assistantCard = await Page(factory.ClientAs(Roles.Assistant), $"/hastalar/{patientId}");
        Assert.Contains("görme izniniz yok", assistantCard);
        Assert.DoesNotContain("HASTAOĞLU", assistantCard);

        var assistantCalendar = await Page(factory.ClientAs(Roles.Assistant), "/randevular");
        Assert.DoesNotContain("Gizli Hekim", assistantCalendar);

        var dentistCard = await Page(factory.ClientAs(Roles.Dentist), $"/hastalar/{patientId}");
        Assert.Contains("HASTAOĞLU", dentistCard);

        var adminCard = await Page(factory.ClientAs(Roles.Admin), $"/hastalar/{patientId}");
        Assert.Contains("HASTAOĞLU", adminCard);
    }

    [Fact]
    public async Task Assistant_sees_payment_edit_and_delete_buttons()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), $"/hastalar/{factory.Data.PatientId}");
        Assert.Contains("Ödemeyi düzenle", html);
        Assert.Contains("Ödemeyi sil", html);
        Assert.Contains("Ödemeyi iptal et", html);
    }

    [Fact]
    public async Task Home_page_no_longer_shows_money()
    {
        var html = await Page(factory.ClientAs(Roles.Admin), "/");
        Assert.DoesNotContain("Bugünkü tahsilat", html);
        Assert.DoesNotContain("Toplam alacak", html);
        var cash = await Page(factory.ClientAs(Roles.Admin), "/kasa");
        Assert.Contains("Bugünkü tahsilat", cash);
        Assert.Contains("Toplam alacak", cash);
    }

    [Fact]
    public async Task Account_pages_are_in_turkish()
    {
        // The test identity has no account row, so the Manage pages send it to "user not found"; that page is checked instead.
        var html = await Page(factory.ClientAs(Roles.Assistant), "/Account/AccessDenied");
        Assert.Contains("Erişim engellendi", html);
        Assert.DoesNotContain("Access denied", html);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.ClientAs(Roles.Assistant).GetAsync("/Account/Manage/PersonalData")).StatusCode);
    }
}

/// <summary>Treatment pictures on the tooth chart are guessed from the procedure name when none is chosen.</summary>
public sealed class ToothSymbolTests
{
    [Theory]
    [InlineData("Kompozit dolgu (iki yüzlü)", "filling")]
    [InlineData("Kanal tedavisi (tek kanal)", "rootcanal")]
    [InlineData("Kanal dolgusu", "rootcanal")]
    [InlineData("Zirkonyum kron", "crown")]
    [InlineData("Metal destekli porselen kuron", "crown")]
    [InlineData("DİŞ ÇEKİMİ", "extraction")]
    [InlineData("İmplant (tek diş)", "implant")]
    [InlineData("Eksik diş", "missing")]
    [InlineData("Çürük", "caries")]
    [InlineData("Köprü gövdesi", "bridge")]
    [InlineData("Fissür örtücü", "sealant")]
    [InlineData("Diş taşı temizliği", "other")]
    public void Symbol_is_guessed_from_the_name(string name, string expected) =>
        Assert.Equal(expected, Disci.Web.Services.ToothSymbols.For(null, name));

    [Fact]
    public void Chosen_symbol_wins_over_the_name() =>
        Assert.Equal("veneer", Disci.Web.Services.ToothSymbols.For("veneer", "Kompozit dolgu"));

    [Fact]
    public void Every_symbol_draws_something()
    {
        foreach (var s in Disci.Web.Services.ToothSymbols.All)
            Assert.StartsWith("<", Disci.Web.Services.ToothSymbols.Svg(s.Key, 20, 20, "M0 0Z"));
    }
}

/// <summary>Treated surfaces and the realistic chart.</summary>
public sealed class DentalChartTests
{
    [Theory]
    [InlineData("dom", "MOD")]
    [InlineData("OB", "OB")]
    [InlineData("xyz", null)]
    [InlineData("", null)]
    public void Surfaces_are_written_in_the_usual_order(string given, string? expected) =>
        Assert.Equal(expected, Disci.Web.Services.Surface.Format(given));

    [Fact]
    public void Only_fillings_caries_and_sealants_ask_for_surfaces()
    {
        Assert.True(Disci.Web.Services.Surface.Applies("filling"));
        Assert.False(Disci.Web.Services.Surface.Applies("crown"));
    }

    [Fact]
    public void Every_tooth_type_has_crown_and_roots()
    {
        foreach (var spec in Disci.Web.Services.ToothGeometry.Upper.Concat(Disci.Web.Services.ToothGeometry.Lower)
                     .Concat(Disci.Web.Services.ToothGeometry.UpperDeciduous).Concat(Disci.Web.Services.ToothGeometry.LowerDeciduous))
        {
            var side = Disci.Web.Services.ToothGeometry.Side(spec, 6);
            Assert.StartsWith("M", side.Crown);
            Assert.Equal(spec.Roots, side.Roots.Count);
            Assert.Equal(spec.Roots, side.Canals.Count);
            Assert.DoesNotContain(",", side.Crown);   // invariant culture: no decimal commas in SVG paths
        }
    }
}
