using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Disci.Web.Services;

/// <summary>
/// A safety net for prescriptions, not a drug database: warns when a medication name matches what the patient's
/// anamnesis lists as a drug allergy, including the common dental antibiotic and painkiller families.
/// </summary>
public static class PrescriptionSafety
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>Allergy keyword → name fragments of drugs in that family (brand and generic, as typed in Turkey).</summary>
    private static readonly (string Allergy, string[] Drugs)[] Families =
    [
        ("penisil", ["penisil", "amoksi", "amoxi", "ampisil", "ampicil", "augmentin", "klavul", "clavul", "sultamisil", "largopen", "alfoxil", "duocid", "devapen"]),
        ("amoksi", ["amoksi", "amoxi", "augmentin", "klavul", "largopen", "alfoxil"]),
        ("sefalospor", ["sef", "cef", "zinnat", "duricef", "iesef"]),
        ("aspirin", ["aspirin", "asetilsalisil", "coraspin", "ecopirin"]),
        ("nsaid", ["ibuprofen", "naproks", "naprox", "diklofenak", "diclofenac", "flurbiprofen", "deksketoprofen", "dexketoprofen", "etodolak", "nimesulid", "aspirin", "arveles", "majezik", "apranax", "dolorex", "parol plus"]),
        ("ağrı kesici", ["ibuprofen", "naproks", "diklofenak", "flurbiprofen", "deksketoprofen", "aspirin", "majezik", "arveles", "apranax"]),
        ("metamizol", ["metamizol", "novalgin", "geralgine"]),
        ("klindamisin", ["klindamisin", "clindamycin", "cleocin"]),
        ("metronidazol", ["metronidazol", "flagyl", "nidazol"]),
        ("lidokain", ["lidokain", "lidocaine", "jetokain"]),
        ("artikain", ["artikain", "articaine", "ultracain", "maxicaine"]),
        ("sülfa", ["sülfa", "sulfa", "bactrim", "septrin", "trimetoprim"]),
    ];

    private static readonly string[] Negatives = ["yok", "yoktur", "hayır", "hayir", "bilinmiyor", "bilmiyor", "-", "--", "none", "yok."];

    /// <summary>False for blanks and answers such as "Yok" that old records store in the allergy field.</summary>
    public static bool IsAllergy([NotNullWhen(true)] string? allergies) =>
        !string.IsNullOrWhiteSpace(allergies) && !Negatives.Contains(allergies.Trim().ToLower(Tr));

    /// <summary>
    /// Lower case with ı and i treated alike: Turkish lowering turns "NSAID" into "nsaıd" and people type both
    /// "Penisilin" and "penısılın".
    /// </summary>
    private static string Fold(string s) => s.ToLower(Tr).Replace('ı', 'i');

    /// <summary>The allergy words that make this medication risky for the patient; empty when nothing matches.</summary>
    public static IReadOnlyList<string> AllergyWarnings(string? allergies, string? medicationName)
    {
        if (!IsAllergy(allergies) || string.IsNullOrWhiteSpace(medicationName))
            return [];

        var allergy = Fold(allergies);
        var drug = Fold(medicationName);
        var hits = new List<string>();

        foreach (var (key, drugs) in Families)
            if (allergy.Contains(Fold(key)) && drugs.Any(d => drug.Contains(Fold(d))))
                hits.Add(key);

        // The allergy written word for word in the drug name ("Parol" allergy, "Parol 500 mg").
        foreach (var word in allergy.Split([' ', ',', ';', '/', '.', '-', '(', ')'], StringSplitOptions.RemoveEmptyEntries))
            if (word.Length >= 4 && drug.Contains(word) && !hits.Contains(word))
                hits.Add(word);

        return hits;
    }
}

/// <summary>One medicine line while a prescription or template is being edited.</summary>
public sealed class RxLine
{
    public int? MedicationId { get; set; }
    public string Name { get; set; } = "";
    public string? Dose { get; set; }
    public string? Usage { get; set; }
    public int Quantity { get; set; } = 1;
    /// <summary>For a typed-in medicine: also add it to the medication list when saving.</summary>
    public bool SaveToCatalog { get; set; }
}

public static class PrescriptionTemplates
{
    /// <summary>
    /// Starter templates offered once, on request, when a clinic has none. Generic names and common adult doses only;
    /// the dentist reviews and edits them before use.
    /// </summary>
    public static IReadOnlyList<(string Name, string Diagnosis, (string Medicine, string Dose, string Usage, int Boxes)[] Items)> Examples { get; } =
    [
        ("Çekim sonrası", "Diş çekimi sonrası",
        [
            ("Parasetamol", "500 mg tablet", "Ağrı olduğunda 1 tablet (günde en fazla 4)", 1),
            ("Klorheksidin %0,12 gargara", "200 ml", "Çekimden 24 saat sonra başlayarak günde 2 kez gargara", 1),
        ]),
        ("Gömülü 20 yaş / cerrahi sonrası", "İmpakte üçüncü molar cerrahisi sonrası",
        [
            ("Amoksisilin + klavulanik asit", "1000 mg tablet", "Günde 2 kez 1 tablet, tok karnına (7 gün)", 1),
            ("İbuprofen", "400 mg tablet", "Günde 3 kez 1 tablet, tok karnına", 1),
            ("Klorheksidin %0,12 gargara", "200 ml", "Günde 2 kez gargara", 1),
        ]),
        ("Apse / akut enfeksiyon", "Akut periapikal apse",
        [
            ("Amoksisilin + klavulanik asit", "1000 mg tablet", "Günde 2 kez 1 tablet, tok karnına (7 gün)", 1),
            ("Parasetamol", "500 mg tablet", "Ağrı olduğunda 1 tablet (günde en fazla 4)", 1),
        ]),
        ("Penisilin alerjisi olan hastada enfeksiyon", "Akut periapikal apse",
        [
            ("Klindamisin", "300 mg kapsül", "Günde 3 kez 1 kapsül (7 gün)", 2),
            ("Parasetamol", "500 mg tablet", "Ağrı olduğunda 1 tablet (günde en fazla 4)", 1),
        ]),
        ("Aft / ağız yarası", "Aftöz stomatit",
        [
            ("Benzidamin gargara", "200 ml", "Günde 3 kez gargara", 1),
        ]),
    ];
}
