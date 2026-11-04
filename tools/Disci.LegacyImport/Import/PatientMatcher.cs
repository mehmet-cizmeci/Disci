using System.Text;

namespace Disci.LegacyImport.Import;

/// <summary>What is known about a patient for duplicate detection.</summary>
public sealed record PatientKey(int Id, string Name, IReadOnlySet<string> Phones, DateOnly? BirthDate)
{
    public string NameKey { get; } = PatientMatcher.NameKey(Name);
}

public enum MatchKind
{
    /// <summary>No candidate: a new patient.</summary>
    None,
    /// <summary>Same person: same name and (same phone or same exact birth date), or a one-letter name typo with the same phone.</summary>
    Same,
    /// <summary>
    /// Same name and the only such patient whose birth date fits (unknown, same year, at most 2 years apart, or the same
    /// day and month with the year mistyped by at most 5). Merged automatically, listed in the report.
    /// </summary>
    Likely,
    /// <summary>Same name, but the birth dates are far apart (e.g. a grandchild with the grandparent's name): another person. Imported as new and listed.</summary>
    NameOnlyBirthDiffers,
    /// <summary>More than one existing patient fits as well; nothing is merged automatically.</summary>
    Ambiguous,
}

public sealed record MatchResult(MatchKind Kind, IReadOnlyList<PatientKey> Candidates, bool BirthDiffers = false);

/// <summary>
/// Finds an existing patient that is the same person as an incoming one. The old programs were mostly typed without
/// Turkish letters and with placeholder birth dates (01.01.1980, or 1 January of the year), so names are folded to
/// ASCII letters and a 1 January date only counts as a year.
/// </summary>
public sealed class PatientMatcher
{
    /// <summary>The default DentaLine fills in when the real date is not known.</summary>
    private static readonly DateOnly PlaceholderBirth = new(1980, 1, 1);

    private readonly Dictionary<string, List<PatientKey>> _byName = [];
    private readonly Dictionary<int, List<PatientKey>> _byLength = [];

    public PatientMatcher(IEnumerable<PatientKey> existing)
    {
        foreach (var p in existing) Add(p);
    }

    public MatchResult Match(PatientKey incoming)
    {
        var name = incoming.NameKey;
        if (name.Length == 0) return new(MatchKind.None, []);

        var exact = _byName.GetValueOrDefault(name) ?? [];
        bool PhoneEq(PatientKey p) => p.Phones.Overlaps(incoming.Phones);

        var same = exact.Where(p => PhoneEq(p) || BirthExactlyEqual(p.BirthDate, incoming.BirthDate)).ToList();
        if (same.Count == 0)
            same = NearNames(name).Where(p => PhoneEq(p) && BirthFits(p.BirthDate, incoming.BirthDate)).ToList();

        if (same.Count == 1)
            return new(MatchKind.Same, same, BirthDiffers(same[0].BirthDate, incoming.BirthDate));
        if (same.Count > 1)
            return new(MatchKind.Ambiguous, same);
        if (exact.Count == 0)
            return new(MatchKind.None, []);
        var fitting = exact.Where(p => BirthFits(p.BirthDate, incoming.BirthDate)).ToList();
        return fitting.Count switch
        {
            0 => new(MatchKind.NameOnlyBirthDiffers, exact.ToList()),
            1 => new(MatchKind.Likely, fitting, BirthDiffers(fitting[0].BirthDate, incoming.BirthDate)),
            _ => new(MatchKind.Ambiguous, fitting),
        };
    }

    /// <summary>A patient added during the run, so later rows (the same person entered twice) can match it.</summary>
    public void Add(PatientKey p)
    {
        if (p.NameKey.Length == 0) return;
        Add(_byName, p.NameKey, p);
        Add(_byLength, p.NameKey.Length, p);
    }

    /// <summary>
    /// Could be the same person's birth date: unknown or placeholder; year-only within a year; otherwise at most 2 years
    /// apart, or the same day and month with a mistyped year (at most 5 apart).
    /// </summary>
    public static bool BirthFits(DateOnly? a, DateOnly? b)
    {
        if (a is not { } x || b is not { } y || x == PlaceholderBirth || y == PlaceholderBirth) return true;
        int years = Math.Abs(x.Year - y.Year);
        if (IsYearOnly(x) || IsYearOnly(y)) return years <= 1;
        return years <= 2 || (x.Month == y.Month && x.Day == y.Day && years <= 5);
    }

    /// <summary>Existing names that differ by exactly one letter (typing errors).</summary>
    private IEnumerable<PatientKey> NearNames(string name)
    {
        for (int len = name.Length - 1; len <= name.Length + 1; len++)
            foreach (var p in _byLength.GetValueOrDefault(len) ?? [])
                if (p.NameKey != name && WithinOneEdit(p.NameKey, name))
                    yield return p;
    }

    public static bool BirthExactlyEqual(DateOnly? a, DateOnly? b) =>
        a is { } x && b is { } y && x == y && IsExactDate(x);

    /// <summary>False for the placeholder 01.01.1980 and for 1 January dates, which mostly mean "only the year is known".</summary>
    public static bool IsExactDate(DateOnly d) => !IsYearOnly(d);

    public static bool BirthDiffers(DateOnly? a, DateOnly? b)
    {
        if (a is not { } x || b is not { } y || x == PlaceholderBirth || y == PlaceholderBirth) return false;
        return IsYearOnly(x) || IsYearOnly(y) ? Math.Abs(x.Year - y.Year) > 1 : x != y;
    }

    private static bool IsYearOnly(DateOnly d) => d.Month == 1 && d.Day == 1;

    /// <summary>Upper-case ASCII letters only: "Ayşe  Yılmaz" and "AYSE YILMAZ" give the same key.</summary>
    public static string NameKey(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            char c = ch switch
            {
                'ı' or 'İ' or 'i' => 'I',
                'ş' or 'Ş' => 'S',
                'ğ' or 'Ğ' => 'G',
                'ü' or 'Ü' => 'U',
                'ö' or 'Ö' => 'O',
                'ç' or 'Ç' => 'C',
                _ => char.ToUpperInvariant(ch),
            };
            if (char.IsLetter(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>The last 10 digits of each number (country code and leading 0 dropped); shorter numbers are ignored.</summary>
    public static HashSet<string> PhoneKeys(params string?[] phones)
    {
        var keys = new HashSet<string>();
        foreach (var phone in phones)
        {
            var digits = new string((phone ?? "").Where(char.IsAsciiDigit).ToArray());
            if (digits.Length >= 10) keys.Add(digits[^10..]);
        }
        return keys;
    }

    private static bool WithinOneEdit(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) > 1) return false;
        int i = 0, j = 0, edits = 0;
        while (i < a.Length && j < b.Length)
        {
            if (a[i] == b[j]) { i++; j++; continue; }
            if (++edits > 1) return false;
            if (a.Length > b.Length) i++;
            else if (a.Length < b.Length) j++;
            else { i++; j++; }
        }
        return edits + (a.Length - i) + (b.Length - j) <= 1;
    }

    private static void Add<TKey>(Dictionary<TKey, List<PatientKey>> map, TKey key, PatientKey p) where TKey : notnull
    {
        if (!map.TryGetValue(key, out var list)) map[key] = list = [];
        list.Add(p);
    }
}
