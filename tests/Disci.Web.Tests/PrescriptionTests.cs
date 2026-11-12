using System.Net;
using Disci.Infrastructure.Identity;
using Disci.Web.Services;

namespace Disci.Web.Tests;

/// <summary>Prescription list, print sheet and the allergy safety net.</summary>
public sealed class PrescriptionTests(DisciWebFactory factory) : IClassFixture<DisciWebFactory>
{
    private static async Task<string> Page(HttpClient client, string url) =>
        WebUtility.HtmlDecode(await client.GetStringAsync(url));

    [Fact]
    public async Task Assistants_see_written_prescriptions_and_the_medication_list()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), "/receteler");
        Assert.Contains(factory.Data.LastName, html);
        Assert.Contains("Deneme Amoksisilin", html);
        Assert.Contains("İlaç listesi", html);
    }

    [Fact]
    public async Task Print_sheet_shows_dentist_patient_medicines_and_allergy()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), $"/recete/{factory.Data.PrescriptionId}");
        Assert.Contains("Deneme Hekim", html);
        Assert.Contains(factory.Data.LastName, html);
        Assert.Contains("Deneme Amoksisilin 1000 mg", html);
        Assert.Contains("2 kutu", html);
        Assert.Contains("Kullanım: Günde 2 kez 1 tablet", html);
        Assert.Contains("İlaç alerjisi", html);
        Assert.Contains("Deneme tanısı", html);
    }

    [Fact]
    public async Task Patient_page_offers_prescribing()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), $"/hastalar/{factory.Data.PatientId}");
        Assert.Contains("Reçete yaz", html);
        Assert.Contains("Alerji: Penisilin", html);
    }

    [Fact]
    public async Task Missing_prescription_is_not_an_error()
    {
        var response = await factory.ClientAs(Roles.Assistant).GetAsync("/recete/999999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("bulunamad", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Templates_are_listed_for_everyone_but_edited_by_dentists_only()
    {
        var assistant = await Page(factory.ClientAs(Roles.Assistant), "/receteler?sekme=sablon");
        Assert.Contains("Deneme şablonu", assistant);
        Assert.Contains("Deneme şablon tanısı", assistant);
        Assert.DoesNotContain("Şablon ekle", assistant);

        var dentist = await Page(factory.ClientAs(Roles.Dentist), "/receteler?sekme=sablon");
        Assert.Contains("Şablon ekle", dentist);
    }

    [Fact]
    public void Example_templates_respect_the_allergy_rules()
    {
        // The penicillin-free example must stay usable for a penicillin-allergic patient, and the
        // amoxicillin ones must be flagged for that patient.
        foreach (var (name, _, items) in PrescriptionTemplates.Examples)
        {
            var flagged = items.Any(i => PrescriptionSafety.AllergyWarnings("Penisilin", i.Medicine).Count > 0);
            Assert.Equal(items.Any(i => i.Medicine.StartsWith("Amoksisilin")), flagged);
            if (name.Contains("Penisilin alerjisi"))
                Assert.False(flagged);
        }
    }

    [Theory]
    [InlineData("Penisilin", "Augmentin BID 1000 mg")]
    [InlineData("penisilin alerjisi", "Amoksisilin + klavulanik asit")]
    [InlineData("PENİSİLİN", "Largopen 1 g")]
    [InlineData("NSAID", "Majezik 100 mg")]
    [InlineData("Parol", "Parol 500 mg")]
    public void Allergy_matches_are_flagged(string allergy, string medication) =>
        Assert.NotEmpty(PrescriptionSafety.AllergyWarnings(allergy, medication));

    [Theory]
    [InlineData("Penisilin", "Parol 500 mg")]
    [InlineData("Yok", "Augmentin")]
    [InlineData("", "Augmentin")]
    [InlineData(null, "Augmentin")]
    [InlineData("Lateks", "Klorheksidin gargara")]
    public void Unrelated_or_empty_allergies_are_not_flagged(string? allergy, string medication) =>
        Assert.Empty(PrescriptionSafety.AllergyWarnings(allergy, medication));

    [Theory]
    [InlineData("Yok", false)]
    [InlineData(" yok ", false)]
    [InlineData("HAYIR", false)]
    [InlineData("Penisilin", true)]
    public void Negative_answers_are_not_allergies(string text, bool expected) =>
        Assert.Equal(expected, PrescriptionSafety.IsAllergy(text));
}
