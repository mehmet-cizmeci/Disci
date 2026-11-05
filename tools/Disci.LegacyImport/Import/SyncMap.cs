using System.Text.Json;
using System.Text.Json.Serialization;

namespace Disci.LegacyImport.Import;

/// <summary>
/// Written by Disci.LegacySync into each old database folder (<see cref="FileName"/>) after DentaLineSuit and
/// DentaLine 2001 were filled with each other's records. It tells which rows of this folder are copies from the other
/// program, so a later import into Disci takes every record only once: copied rows are skipped, and copied or matched
/// patient cards are linked to the patient that comes from the other program.
/// Keys are the numbers in THIS folder; values the numbers in the other program.
/// </summary>
public sealed class SyncMap
{
    public const string FileName = "DISCI-ESITLEME.json";
    public const string FromDl2001 = "DL2001";
    public const string FromDentaLineSuit = "DentaLineSuit";

    /// <summary>The program the copied rows came from: <see cref="FromDl2001"/> or <see cref="FromDentaLineSuit"/>.</summary>
    public string Source { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    /// <summary>Patient cards that existed in both programs: number here → numbers there (first is the main card).</summary>
    public Dictionary<int, List<int>> MatchedPatients { get; set; } = [];

    /// <summary>Patient cards created by the copy: number here → numbers there (first is the main card).</summary>
    public Dictionary<int, List<int>> CopiedPatients { get; set; } = [];

    /// <summary>Copied rows with an AutoInc key, per table (DLORALTD, DLOR_DET, DLODEME, DLODEDET, DLFOTO, DLGIDER, DLLABOR, DLHEKIM): key here → key there.</summary>
    public Dictionary<string, Dictionary<int, int>> Rows { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Copied tooth notes as "PROTOKOL/DISNO".</summary>
    public List<string> ToothNotes { get; set; } = [];

    /// <summary>Patients whose anamnesis / note row was copied (number here).</summary>
    public List<int> Anamnesis { get; set; } = [];
    public List<int> PatientNotes { get; set; } = [];

    [JsonIgnore] public bool IsEmpty => MatchedPatients.Count == 0 && CopiedPatients.Count == 0 && Rows.Count == 0;

    public bool IsCopied(string table, int? key) =>
        key is { } k && Rows.TryGetValue(table, out var map) && map.ContainsKey(k);

    public bool IsCopiedToothNote(int? protocol, int? tooth) => ToothNotes.Contains($"{protocol}/{tooth}");

    /// <summary>For a patient number here: the numbers of the same patient in the other program (main first), if linked.</summary>
    public IReadOnlyList<int>? OtherPatient(int protocol) =>
        CopiedPatients.TryGetValue(protocol, out var c) ? c : MatchedPatients.TryGetValue(protocol, out var m) ? m : null;

    /// <summary>
    /// True when a row of <paramref name="table"/> is a copy from the other program (patient cards are not covered:
    /// they are linked through <see cref="OtherPatient"/>).
    /// </summary>
    public bool IsCopy(string table, Paradox.ParadoxRecord r) => table.ToUpperInvariant() switch
    {
        "DLORALTD" => IsCopied(table, r.Int("TEDNO")),
        "DLOR_DET" => IsCopied(table, r.Int("TED_DETNO")),
        "DLODEME" => IsCopied(table, r.Int("ODEME_ID")),
        "DLODEDET" => IsCopied(table, r.Int("DETAIL_ID")),
        "DLFOTO" => IsCopied(table, r.Int("RESIM_NO")),
        "DLGIDER" => IsCopied(table, r.Int("Sira")),
        "DLLABOR" => IsCopied(table, r.Int("No")),
        "DLHEKIM" => IsCopied(table, r.Int("DOCTOR_NO")),
        "DLDISNOT" => IsCopiedToothNote(r.Int("PROTOKOL"), r.Int("DISNO")),
        "DLANAMNZ" => r.Int("PROTOKOL") is { } a && Anamnesis.Contains(a),
        "DLNOTLAR" => r.Int("PROTOKOL") is { } n && PatientNotes.Contains(n),
        _ => false,
    };

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>The map in <paramref name="dir"/>, or null when the folder was never synchronised.</summary>
    public static SyncMap? Load(string dir)
    {
        var path = Path.Combine(dir, FileName);
        return File.Exists(path) ? JsonSerializer.Deserialize<SyncMap>(File.ReadAllText(path), Json) : null;
    }

    public void Save(string dir) => File.WriteAllText(Path.Combine(dir, FileName), JsonSerializer.Serialize(this, Json));
}
