using Disci.LegacyImport.Import;

namespace Disci.Web.Tests;

/// <summary>Turkish letters put back into names typed without them in the old programs.</summary>
public sealed class TurkishNamesTests
{
    [Theory]
    [InlineData("Ayse", "Ayşe")]
    [InlineData("Ibrahım", "İbrahim")]      // DentaLine lower-cased a typed IBRAHIM with Turkish rules
    [InlineData("Alı", "Ali")]
    [InlineData("Ayse Gul", "Ayşe Gül")]
    [InlineData("YILDIZ", "YILDIZ")]
    [InlineData("Yıldız", "Yıldız")]        // a real ı stays
    [InlineData("Mehmet", "Mehmet")]
    public void First_names(string typed, string expected) => Assert.Equal(expected, TurkishNames.FixFirstName(typed));

    [Theory]
    [InlineData("CELIK", "ÇELİK")]
    [InlineData("OZTURK", "ÖZTÜRK")]
    [InlineData("KARAOGLU", "KARAOĞLU")]   // not in the list: only the -oğlu ending is certain
    [InlineData("UYDURMACI", "UYDURMACI")] // unknown: left as typed
    [InlineData("Celik", "Çelik")]
    public void Last_names(string typed, string expected) => Assert.Equal(expected, TurkishNames.FixLastName(typed));

    [Fact]
    public void A_name_already_written_with_turkish_letters_is_not_changed()
    {
        Assert.Equal("Gülsen", TurkishNames.FixFirstName("Gülsen"));
        Assert.Equal("ÇELİK", TurkishNames.FixLastName("ÇELİK"));
    }

    [Fact]
    public void A_plain_spelling_shared_by_two_names_is_left_alone() =>
        Assert.Equal("Gulsen", TurkishNames.FixFirstName("Gulsen"));   // Gülsen or Gülşen

    [Fact]
    public void Full_name_fixes_the_surname_as_a_surname() =>
        Assert.Equal("Fatma ÖZDEMİR", TurkishNames.FixFullName("Fatma OZDEMIR"));
}
