namespace Disci.Web.Services;

/// <summary>
/// Tooth surfaces as letters, in the usual order: M mesial (towards the midline), O occlusal / incisal (biting),
/// D distal, B buccal / labial (cheek, lip), L lingual / palatal (tongue, palate). "MOD" = a three-surface filling.
/// </summary>
public static class Surface
{
    public const string All = "MODBL";

    public static readonly (char Code, string Name)[] Names =
    [
        ('M', "Mezial"), ('O', "Okluzal / insizal"), ('D', "Distal"), ('B', "Bukkal / labial"), ('L', "Lingual / palatinal"),
    ];

    public static IReadOnlyList<char> Parse(string? s) =>
        string.IsNullOrWhiteSpace(s) ? [] : All.Where(c => s.Contains(c, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Canonical text ("DOM" → "MOD"), or null when none.</summary>
    public static string? Format(IEnumerable<char> s)
    {
        var set = s.Select(char.ToUpperInvariant).ToHashSet();
        var text = new string(All.Where(set.Contains).ToArray());
        return text.Length == 0 ? null : text;
    }

    /// <summary>Procedures whose surfaces matter: they get the surface picker when entered.</summary>
    public static bool Applies(string symbol) => symbol is "filling" or "caries" or "sealant";
}
