using System.Globalization;
using System.Text;

namespace Disci.Web.Services;

/// <summary>
/// Pictures for treatments on the tooth chart, following the usual dental charting conventions (filled area for a
/// filling, a line for a root canal, a ring for a crown, a cross for an extraction, a screw for an implant...).
/// Drawn here as our own SVG, in the tooth's local frame: centre (0,0), width w, depth d, the cheek/lip side at -y.
/// A procedure may name its symbol (Procedure.Symbol); otherwise it is guessed from the procedure's name.
/// </summary>
public static class ToothSymbols
{
    public sealed record Info(string Key, string Name);

    public static readonly Info[] All =
    [
        new("filling", "Dolgu"),
        new("rootcanal", "Kanal tedavisi"),
        new("crown", "Kron / kuron"),
        new("bridge", "Köprü"),
        new("veneer", "Lamina / veneer"),
        new("post", "Post / pivot"),
        new("implant", "İmplant"),
        new("extraction", "Çekim"),
        new("missing", "Eksik diş"),
        new("sealant", "Fissür örtücü"),
        new("caries", "Çürük"),
        new("lesion", "Apse / lezyon / kist"),
        new("fracture", "Kırık"),
        new("impacted", "Gömülü diş"),
        new("denture", "Protez"),
        new("ortho", "Ortodonti"),
        new("other", "Diğer işlem"),
    ];

    public static string Name(string key) => All.FirstOrDefault(i => i.Key == key)?.Name ?? key;

    public static bool IsKnown(string? key) => key is not null && All.Any(i => i.Key == key);

    // Order matters: the first matching rule wins ("kanal dolgusu" is a root canal, "geçici kron" a crown).
    private static readonly (string Key, string[] Words)[] Rules =
    [
        ("implant", ["implant"]),
        ("rootcanal", ["kanal", "endodont", "pulpa", "pulpo", "amputasyon", "apeks", "rezeksiyon"]),
        ("extraction", ["cekim", "cekil", "extraksiyon", "ekstraksiyon", "extraction"]),
        ("missing", ["eksik", "kayip", "yok"]),
        ("impacted", ["gomulu", "yari gomulu"]),
        ("bridge", ["kopru", "govde", "pontik", "maryland"]),
        ("veneer", ["lamina", "laminate", "veneer", "lamine"]),
        ("post", ["post", "pivot", "core", "kor "]),
        ("crown", ["kron", "kuron", "porselen", "zirkon", "metal destekli", "e-max", "emax", "inley", "onley", "inlay", "onlay"]),
        ("sealant", ["fissur", "ortucu", "sealant"]),
        ("caries", ["curuk"]),
        ("lesion", ["apse", "lezyon", "kist", "granulom", "fistul"]),
        ("fracture", ["kirik", "fraktur"]),
        ("denture", ["protez", "total", "parsiyel", "iskelet"]),
        ("ortho", ["ortodont", "braket", "tel "]),
        ("filling", ["dolgu", "kompozit", "amalgam", "cam iyonomer", "restorasyon", "kavite"]),
    ];

    /// <summary>The procedure's own symbol if it has one, else a guess from its name or the item's description.</summary>
    public static string For(string? symbol, params string?[] names)
    {
        if (IsKnown(symbol)) return symbol!;
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var text = " " + TextSearch.Fold(name) + " ";
            foreach (var (key, words) in Rules)
                if (words.Any(w => text.Contains(w, StringComparison.Ordinal)))
                    return key;
        }
        return "other";
    }

    /// <summary>
    /// SVG elements for one symbol in the tooth's frame. Colours come from CSS (currentColor) so the planned /
    /// done / finding state decides them. <paramref name="outline"/> is the tooth's own path, used by crown and missing.
    /// </summary>
    public static string Svg(string key, double w, double d, string? outline = null)
    {
        double hw = w / 2, hd = d / 2, r = Math.Min(hw, hd);
        var s = new StringBuilder();
        void El(string x) => s.Append(x);
        switch (key)
        {
            case "filling":
                El($"<ellipse cx=\"0\" cy=\"0\" rx=\"{N(hw * 0.5)}\" ry=\"{N(hd * 0.42)}\" fill=\"currentColor\" fill-opacity=\".85\"/>");
                break;
            case "rootcanal":
                // Seen from above the canal is a dot in the middle; the bar marks it as filled down to the root.
                El($"<line x1=\"0\" y1=\"{N(-hd * 0.75)}\" x2=\"0\" y2=\"{N(hd * 0.75)}\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.28)}\" stroke-linecap=\"round\"/>");
                El($"<circle cx=\"0\" cy=\"0\" r=\"{N(r * 0.3)}\" fill=\"currentColor\"/>");
                break;
            case "crown":
                if (outline is not null)
                    El($"<path d=\"{outline}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.3)}\"/>");
                else
                    El($"<rect x=\"{N(-hw)}\" y=\"{N(-hd)}\" width=\"{N(w)}\" height=\"{N(d)}\" rx=\"{N(r * 0.4)}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.3)}\"/>");
                break;
            case "bridge":
                // A pontic: a bar running through the tooth from one neighbour to the other.
                El($"<rect x=\"{N(-hw * 1.15)}\" y=\"{N(-hd * 0.16)}\" width=\"{N(w * 1.15)}\" height=\"{N(hd * 0.32)}\" rx=\"{N(hd * 0.1)}\" fill=\"currentColor\"/>");
                if (outline is not null)
                    El($"<path d=\"{outline}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.18)}\"/>");
                break;
            case "veneer":
                El($"<path d=\"M{N(-hw * 0.85)} {N(-hd * 0.55)} Q0 {N(-hd * 1.25)} {N(hw * 0.85)} {N(-hd * 0.55)}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.34)}\" stroke-linecap=\"round\"/>");
                break;
            case "post":
                El($"<rect x=\"{N(-r * 0.3)}\" y=\"{N(-r * 0.3)}\" width=\"{N(r * 0.6)}\" height=\"{N(r * 0.6)}\" fill=\"currentColor\"/>");
                El($"<circle cx=\"0\" cy=\"0\" r=\"{N(r * 0.62)}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.14)}\"/>");
                break;
            case "implant":
                // A screw head seen from above: a ring with the hex socket.
                El($"<circle cx=\"0\" cy=\"0\" r=\"{N(r * 0.72)}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.2)}\"/>");
                El($"<polygon points=\"{Hex(r * 0.36)}\" fill=\"currentColor\"/>");
                break;
            case "extraction":
                El($"<path d=\"M{N(-hw * 0.85)} {N(-hd * 0.85)} L{N(hw * 0.85)} {N(hd * 0.85)} M{N(hw * 0.85)} {N(-hd * 0.85)} L{N(-hw * 0.85)} {N(hd * 0.85)}\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.26)}\" stroke-linecap=\"round\"/>");
                break;
            case "missing":
                if (outline is not null)
                    El($"<path d=\"{outline}\" fill=\"var(--c-surface, #fff)\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.12)}\" stroke-dasharray=\"{N(r * 0.3)} {N(r * 0.22)}\"/>");
                El($"<line x1=\"{N(-hw * 0.6)}\" y1=\"0\" x2=\"{N(hw * 0.6)}\" y2=\"0\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.16)}\" stroke-linecap=\"round\"/>");
                break;
            case "sealant":
                foreach (var (x, y) in new[] { (-0.35, -0.25), (0.35, -0.25), (0.0, 0.3) })
                    El($"<circle cx=\"{N(hw * x)}\" cy=\"{N(hd * y)}\" r=\"{N(r * 0.16)}\" fill=\"currentColor\"/>");
                break;
            case "caries":
                El($"<path d=\"M{N(-r * 0.35)} {N(-r * 0.1)} Q{N(-r * 0.3)} {N(-r * 0.45)} 0 {N(-r * 0.38)} Q{N(r * 0.42)} {N(-r * 0.4)} {N(r * 0.35)} 0 Q{N(r * 0.4)} {N(r * 0.4)} 0 {N(r * 0.35)} Q{N(-r * 0.45)} {N(r * 0.35)} {N(-r * 0.35)} {N(-r * 0.1)}Z\" fill=\"currentColor\"/>");
                break;
            case "lesion":
                El($"<circle cx=\"0\" cy=\"0\" r=\"{N(r * 0.55)}\" fill=\"currentColor\" fill-opacity=\".25\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.14)}\"/>");
                El($"<circle cx=\"0\" cy=\"0\" r=\"{N(r * 0.14)}\" fill=\"currentColor\"/>");
                break;
            case "fracture":
                El($"<polyline points=\"{N(-hw * 0.8)},{N(-hd * 0.5)} {N(-hw * 0.25)},{N(hd * 0.1)} {N(hw * 0.1)},{N(-hd * 0.25)} {N(hw * 0.8)},{N(hd * 0.5)}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.2)}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/>");
                break;
            case "impacted":
                El($"<path d=\"M{N(-hw * 0.8)} {N(hd * 0.35)} Q0 {N(-hd * 0.7)} {N(hw * 0.8)} {N(hd * 0.35)}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.22)}\" stroke-linecap=\"round\"/>");
                El($"<line x1=\"{N(-hw * 0.8)}\" y1=\"{N(hd * 0.55)}\" x2=\"{N(hw * 0.8)}\" y2=\"{N(hd * 0.55)}\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.12)}\" stroke-dasharray=\"{N(r * 0.2)} {N(r * 0.15)}\"/>");
                break;
            case "denture":
                El($"<path d=\"M{N(-hw * 0.9)} {N(hd * 0.2)} Q{N(-hw * 0.45)} {N(-hd * 0.4)} 0 {N(hd * 0.2)} Q{N(hw * 0.45)} {N(hd * 0.8)} {N(hw * 0.9)} {N(hd * 0.2)}\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.22)}\" stroke-linecap=\"round\"/>");
                break;
            case "ortho":
                // A bracket on the cheek side with its archwire.
                El($"<line x1=\"{N(-hw * 1.05)}\" y1=\"{N(-hd * 0.55)}\" x2=\"{N(hw * 1.05)}\" y2=\"{N(-hd * 0.55)}\" stroke=\"currentColor\" stroke-width=\"{N(r * 0.1)}\"/>");
                El($"<rect x=\"{N(-r * 0.32)}\" y=\"{N(-hd * 0.55 - r * 0.32)}\" width=\"{N(r * 0.64)}\" height=\"{N(r * 0.64)}\" rx=\"{N(r * 0.1)}\" fill=\"currentColor\"/>");
                break;
            default:
                El($"<circle cx=\"0\" cy=\"0\" r=\"{N(r * 0.24)}\" fill=\"currentColor\"/>");
                break;
        }
        return s.ToString();
    }

    /// <summary>A small stand-alone picture (generic molar plus the symbol) for lists and legends, 24×24.</summary>
    public static string Icon(string key)
    {
        const double w = 18, d = 18;
        const string molar = "M-6.3 -9 L6.3 -9 Q9 -9 9 -6.3 L9 6.3 Q9 9 6.3 9 L-6.3 9 Q-9 9 -9 6.3 L-9 -6.3 Q-9 -9 -6.3 -9Z";
        return "<svg class=\"sym-icon\" viewBox=\"-12 -12 24 24\" width=\"22\" height=\"22\" aria-hidden=\"true\">" +
               $"<path d=\"{molar}\" fill=\"var(--c-surface, #fff)\" stroke=\"var(--c-border-strong, #94a3b8)\" stroke-width=\"1.2\"/>" +
               "<g class=\"sym\">" + Svg(key, w, d, molar) + "</g></svg>";
    }

    private static string Hex(double r) => string.Join(" ", Enumerable.Range(0, 6).Select(i =>
    {
        var a = Math.PI / 3 * i + Math.PI / 6;
        return $"{N(r * Math.Cos(a))},{N(r * Math.Sin(a))}";
    }));

    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}

/// <summary>One treatment drawn on a tooth: ToothSymbols key, state css (done, plan, finding) and treated surfaces ("MOD").</summary>
public sealed record ToothMark(string Key, string State, string? Surfaces = null);
