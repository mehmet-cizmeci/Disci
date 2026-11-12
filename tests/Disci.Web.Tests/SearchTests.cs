using Disci.Infrastructure;
using Disci.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disci.Web.Tests;

/// <summary>Patient search ignores Turkish letters and case, and tells protocol numbers from phone numbers.</summary>
public sealed class SearchTests(DisciWebFactory factory) : IClassFixture<DisciWebFactory>
{
    private async Task<List<string>> Find(string term)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DisciDbContext>();
        return await PatientSearch.Apply(db.Patients.AsNoTracking(), term).Select(p => p.LastName).ToListAsync();
    }

    [Theory]
    [InlineData("HASTAOĞLU")]
    [InlineData("hastaoğlu")]
    [InlineData("hastaoglu")]
    [InlineData("HASTAOGLU")]
    [InlineData("deneme hastaog")]
    public async Task Name_search_ignores_turkish_letters_and_case(string term) =>
        Assert.Contains(factory.Data.LastName, await Find(term));

    [Theory]
    [InlineData("kisi")]   // stored as "KİŞİ"
    [InlineData("Kişi")]
    [InlineData("ornek")]  // stored as "Örnek"
    public async Task Turkish_letters_in_the_record_match_plain_letters(string term) =>
        Assert.Contains("KİŞİ", await Find(term));

    [Theory]
    [InlineData("0555 000 0001")]
    [InlineData("05550000001")]
    [InlineData("5550000001")]
    [InlineData("0001")]   // leading 0: a phone fragment, not protocol number 1
    public async Task Phone_search_finds_spaced_numbers(string term) =>
        Assert.Contains(factory.Data.LastName, await Find(term));

    [Fact]
    public async Task Short_number_is_a_protocol_number()
    {
        var found = await Find(factory.Data.PatientId.ToString());
        Assert.Equal([factory.Data.LastName], found);
    }

    [Fact]
    public async Task Second_card_number_is_searchable()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DisciDbContext>();
            db.Patients.Add(new Disci.Domain.Patient { FirstName = "Eski", LastName = "İKİNCİPROGRAM", SecondProtocol = 98765 });
            await db.SaveChangesAsync();
        }
        Assert.Equal(["İKİNCİPROGRAM"], await Find("98765"));
        Assert.Equal("#5 · 2. kart #98765", Display.Protocol(1, 5, 98765));
        Assert.Equal("#12", Display.Protocol(12, null, null));
    }

    [Theory]
    [InlineData("İMPLANT", "implant")]
    [InlineData("Ağrı kesici", "agri")]
    [InlineData("ÇEKİM", "çekim")]
    public void Fold_matches_in_memory(string text, string word) => Assert.True(TextSearch.Contains(text, word));
}
