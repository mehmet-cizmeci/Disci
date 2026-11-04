using Disci.LegacyImport.Import;

namespace Disci.Web.Tests;

/// <summary>Duplicate detection used when DentaLine 2001 patients are added to the DentaLineSuit import. Made-up people only.</summary>
public sealed class PatientMatcherTests
{
    private static PatientKey P(int id, string name, string? phone = null, DateOnly? birth = null) =>
        new(id, name, PatientMatcher.PhoneKeys(phone), birth);

    private static readonly PatientMatcher Matcher = new([
        P(1, "Ayse DENEMEOGLU", "0532 111 22 33", new DateOnly(1985, 4, 12)),
        P(2, "Mehmet ORNEKCI", "05441112233", new DateOnly(1970, 1, 1)),
        P(3, "Ali UYDURMA", null, new DateOnly(1990, 6, 1)),
        P(4, "Ali UYDURMA", null, new DateOnly(1962, 3, 3)),
        P(5, "Zeynep SAHTEKIN", "0555 999 88 77", null),
    ]);

    [Fact]
    public void Same_name_with_turkish_letters_and_same_phone_is_the_same_person()
    {
        var m = Matcher.Match(P(0, "Ayşe Denemeoğlu", "+90 532 111 2233"));
        Assert.Equal(MatchKind.Same, m.Kind);
        Assert.Equal(1, m.Candidates.Single().Id);
    }

    [Fact]
    public void Same_name_and_exact_birth_date_is_the_same_person() =>
        Assert.Equal(MatchKind.Same, Matcher.Match(P(0, "AYSE DENEMEOGLU", "0500 000 00 00", new DateOnly(1985, 4, 12))).Kind);

    [Fact]
    public void One_letter_typo_with_the_same_phone_is_the_same_person() =>
        Assert.Equal(5, Matcher.Match(P(0, "Zeynep SAHTEKİM", "05559998877")).Candidates.Single().Id);

    [Fact]
    public void One_letter_typo_without_a_phone_is_not_matched() =>
        Assert.Equal(MatchKind.None, Matcher.Match(P(0, "Zeynep SAHTEKİM")).Kind);

    [Fact]
    public void A_first_of_january_birth_date_only_counts_as_a_year()
    {
        // 01.01.1970 on file: the same name with another day of 1970 fits; it is not an exact-date proof.
        var m = Matcher.Match(P(0, "Mehmet Örnekçi", null, new DateOnly(1970, 8, 20)));
        Assert.Equal(MatchKind.Likely, m.Kind);
        Assert.False(PatientMatcher.BirthExactlyEqual(new DateOnly(1980, 1, 1), new DateOnly(1980, 1, 1)));
    }

    [Theory]
    [InlineData(1985, 9, 30)]   // same year, another day
    [InlineData(1987, 2, 2)]    // two years apart
    [InlineData(1981, 4, 12)]   // same day and month, year mistyped
    public void Same_name_with_a_close_birth_date_is_merged(int year, int month, int day) =>
        Assert.Equal(MatchKind.Likely, Matcher.Match(P(0, "Ayse DENEMEOGLU", null, new DateOnly(year, month, day))).Kind);

    [Theory]
    [InlineData(2001, 9, 9)]    // a grandchild with the grandmother's name
    [InlineData(1981, 3, 3)]    // four years and another day
    public void Same_name_with_a_far_birth_date_is_another_person(int year, int month, int day) =>
        Assert.Equal(MatchKind.NameOnlyBirthDiffers, Matcher.Match(P(0, "Ayse DENEMEOGLU", null, new DateOnly(year, month, day))).Kind);

    [Fact]
    public void Two_people_with_the_same_name_are_not_merged_unless_one_fits()
    {
        var m = Matcher.Match(P(0, "Ali Uydurma"));
        Assert.Equal(MatchKind.Ambiguous, m.Kind);
        Assert.Equal([3, 4], m.Candidates.Select(c => c.Id).Order());
        Assert.Equal(3, Matcher.Match(P(0, "Ali Uydurma", null, new DateOnly(1990, 6, 1))).Candidates.Single().Id);
        Assert.Equal(4, Matcher.Match(P(0, "Ali Uydurma", null, new DateOnly(1963, 7, 7))).Candidates.Single().Id);
    }

    [Fact]
    public void A_card_added_during_the_run_catches_a_second_card_of_the_same_person()
    {
        var matcher = new PatientMatcher([]);
        matcher.Add(P(-1, "Veli TEKRARLI", "05001112233"));
        Assert.Equal(-1, matcher.Match(P(0, "VELİ TEKRARLI", "0500 111 22 33")).Candidates.Single().Id);
    }

    [Fact]
    public void Same_phone_but_another_name_is_a_new_patient() =>
        Assert.Equal(MatchKind.None, Matcher.Match(P(0, "Fatma DENEMEOGLU", "05321112233")).Kind);

    [Theory]
    [InlineData("0532 111 22 33", "5321112233")]
    [InlineData("+90 (532) 111-22-33", "5321112233")]
    [InlineData("212 288", null)]
    public void Phone_keys_use_the_last_ten_digits(string phone, string? key) =>
        Assert.Equal(key is null ? [] : [key], PatientMatcher.PhoneKeys(phone));
}
