using System.Globalization;
using System.Text;

namespace Disci.Web.Services;

/// <summary>
/// Tooth sizes and outlines shared by the two dental charts: the arch (occlusal view, Shared/ToothChart.razor) and the
/// realistic side view with roots (Shared/DentalChart.razor). Sizes are average anatomical values in millimetres.
/// </summary>
public static class ToothGeometry
{
    public enum Kind { Incisor, Canine, Premolar, Molar }

    /// <summary>One tooth type: mesiodistal crown width, crown height, root length (mm) and visible root count.</summary>
    public sealed record Spec(Kind Kind, double Width, double CrownHeight, double RootLength, int Roots);

    // From the midline backwards: permanent 1 → 8, deciduous 1 → 5.
    public static readonly Spec[] Upper =
    [
        new(Kind.Incisor, 8.6, 10.5, 13, 1), new(Kind.Incisor, 6.6, 9.0, 13, 1), new(Kind.Canine, 7.6, 10.0, 17, 1),
        new(Kind.Premolar, 7.0, 8.5, 14, 2), new(Kind.Premolar, 6.7, 8.5, 14, 1),
        new(Kind.Molar, 10.3, 7.5, 13, 3), new(Kind.Molar, 9.2, 7.0, 12, 3), new(Kind.Molar, 8.6, 6.5, 11, 3),
    ];
    public static readonly Spec[] Lower =
    [
        new(Kind.Incisor, 5.4, 9.0, 12.5, 1), new(Kind.Incisor, 5.9, 9.5, 14, 1), new(Kind.Canine, 6.9, 11.0, 16, 1),
        new(Kind.Premolar, 7.0, 8.5, 14, 1), new(Kind.Premolar, 7.1, 8.0, 14.5, 1),
        new(Kind.Molar, 11.2, 7.5, 14, 2), new(Kind.Molar, 10.5, 7.0, 13, 2), new(Kind.Molar, 10.0, 7.0, 11, 2),
    ];
    public static readonly Spec[] UpperDeciduous =
    [
        new(Kind.Incisor, 6.5, 6.0, 10, 1), new(Kind.Incisor, 5.1, 5.6, 11, 1), new(Kind.Canine, 7.0, 6.5, 13, 1),
        new(Kind.Premolar, 7.3, 5.2, 10, 3), new(Kind.Molar, 8.2, 5.7, 11, 3),
    ];
    public static readonly Spec[] LowerDeciduous =
    [
        new(Kind.Incisor, 4.2, 5.0, 9, 1), new(Kind.Incisor, 4.7, 5.2, 10, 1), new(Kind.Canine, 5.9, 6.0, 11, 1),
        new(Kind.Premolar, 7.7, 6.0, 10, 2), new(Kind.Molar, 9.9, 5.5, 11, 2),
    ];

    /// <summary>Occlusal (top) depth relative to width, by type.</summary>
    public static double DepthFactor(Kind k) => k switch { Kind.Incisor => 0.78, Kind.Canine => 1.0, Kind.Premolar => 1.18, _ => 1.02 };

    /// <summary>Occlusal outline centred on (0,0), the outer (cheek/lip) side towards negative y, plus cusp lines.</summary>
    public static (string Shape, string? Detail) Occlusal(Kind kind, double w, double d)
    {
        double hw = w / 2, hd = d / 2;
        return kind switch
        {
            // Incisors: a flat-fronted blade with a straight biting edge.
            Kind.Incisor => (
                $"M{N(-hw)} {N(-hd * 0.55)} Q{N(-hw)} {N(-hd)} {N(-hw * 0.5)} {N(-hd)} L{N(hw * 0.5)} {N(-hd)} Q{N(hw)} {N(-hd)} {N(hw)} {N(-hd * 0.55)} " +
                $"Q{N(hw * 0.85)} {N(hd)} 0 {N(hd)} Q{N(-hw * 0.85)} {N(hd)} {N(-hw)} {N(-hd * 0.55)}Z",
                $"M{N(-hw * 0.6)} {N(-hd * 0.45)} L{N(hw * 0.6)} {N(-hd * 0.45)}"),
            // Canine: a rounded diamond with a single pointed cusp.
            Kind.Canine => (
                $"M0 {N(-hd)} Q{N(hw)} {N(-hd * 0.8)} {N(hw)} 0 Q{N(hw * 0.8)} {N(hd)} 0 {N(hd)} Q{N(-hw * 0.8)} {N(hd)} {N(-hw)} 0 Q{N(-hw)} {N(-hd * 0.8)} 0 {N(-hd)}Z",
                $"M0 {N(-hd * 0.55)} L0 {N(hd * 0.35)}"),
            // Premolar: an oval with the groove between its cheek and tongue cusps.
            Kind.Premolar => (
                $"M0 {N(-hd)} C{N(hw * 1.1)} {N(-hd)} {N(hw * 1.1)} {N(hd)} 0 {N(hd)} C{N(-hw * 1.1)} {N(hd)} {N(-hw * 1.1)} {N(-hd)} 0 {N(-hd)}Z",
                $"M{N(-hw * 0.55)} 0 Q0 {N(hd * 0.15)} {N(hw * 0.55)} 0"),
            // Molar: a rounded square with the cross-shaped groove between its four cusps.
            _ => (
                $"M{N(-hw * 0.7)} {N(-hd)} L{N(hw * 0.7)} {N(-hd)} Q{N(hw)} {N(-hd)} {N(hw)} {N(-hd * 0.7)} L{N(hw)} {N(hd * 0.7)} Q{N(hw)} {N(hd)} {N(hw * 0.7)} {N(hd)} " +
                $"L{N(-hw * 0.7)} {N(hd)} Q{N(-hw)} {N(hd)} {N(-hw)} {N(hd * 0.7)} L{N(-hw)} {N(-hd * 0.7)} Q{N(-hw)} {N(-hd)} {N(-hw * 0.7)} {N(-hd)}Z",
                $"M{N(-hw * 0.6)} {N(hd * 0.05)} L{N(hw * 0.6)} {N(-hd * 0.05)} M{N(-hw * 0.05)} {N(-hd * 0.6)} L{N(hw * 0.05)} {N(hd * 0.6)}"),
        };
    }

    /// <summary>
    /// A tooth seen from the cheek side, in pixels: cervical line (gum line) at y = 0, crown towards +y (biting edge),
    /// roots towards −y. The upper jaw uses it as is (roots up); the lower jaw mirrors it vertically.
    /// </summary>
    public sealed record SideView(string Crown, IReadOnlyList<string> Roots, IReadOnlyList<(double X, double Y)> Apexes,
        double Width, double CervicalWidth, double CrownHeight, double RootLength, string? Groove, IReadOnlyList<string> Canals);

    public static SideView Side(Spec t, double scale)
    {
        double w = t.Width * scale, ch = t.CrownHeight * scale, rl = t.RootLength * scale;
        var cw = w * (t.Kind == Kind.Molar ? 0.8 : 0.72);
        double hc = cw / 2;

        // Crown: sides bulge out from the narrower neck, then the biting edge typical of the type.
        string side(double sx) => $"C{N(sx * w * 0.53)} {N(ch * 0.2)} {N(sx * w * 0.54)} {N(ch * 0.55)}";
        var sb = new StringBuilder($"M{N(-hc)} 0 {side(-1)} {N(-w * 0.49)} {N(ch * 0.74)} ");
        string? groove = null;
        switch (t.Kind)
        {
            case Kind.Incisor:
                sb.Append($"Q{N(-w * 0.47)} {N(ch * 1.0)} {N(-w * 0.3)} {N(ch * 1.0)} Q0 {N(ch * 1.03)} {N(w * 0.3)} {N(ch * 1.0)} Q{N(w * 0.47)} {N(ch * 1.0)} {N(w * 0.49)} {N(ch * 0.74)} ");
                break;
            case Kind.Canine:
                sb.Append($"Q{N(-w * 0.28)} {N(ch * 0.9)} 0 {N(ch * 1.1)} Q{N(w * 0.28)} {N(ch * 0.9)} {N(w * 0.49)} {N(ch * 0.74)} ");
                break;
            case Kind.Premolar:
                sb.Append($"Q{N(-w * 0.3)} {N(ch * 0.98)} 0 {N(ch * 1.05)} Q{N(w * 0.3)} {N(ch * 0.98)} {N(w * 0.49)} {N(ch * 0.74)} ");
                break;
            default:
                sb.Append($"Q{N(-w * 0.34)} {N(ch * 1.08)} {N(-w * 0.04)} {N(ch * 0.94)} Q{N(w * 0.24)} {N(ch * 1.1)} {N(w * 0.49)} {N(ch * 0.74)} ");
                groove = $"M{N(-w * 0.04)} {N(ch * 0.93)} Q{N(-w * 0.02)} {N(ch * 0.6)} {N(w * 0.04)} {N(ch * 0.35)}";
                break;
        }
        sb.Append($"C{N(w * 0.54)} {N(ch * 0.55)} {N(w * 0.53)} {N(ch * 0.2)} {N(hc)} 0Z");

        // Roots: tapered from the neck to the apex; molars split (furcation), upper molars show the palatal root behind.
        var roots = new List<string>();
        var apexes = new List<(double, double)>();
        var canals = new List<string>();
        void Root(double xa, double xb, double apexX, double len)
        {
            var spread = (xb - xa) * 0.28;
            roots.Add($"M{N(xa)} 2 C{N(xa)} {N(-len * 0.45)} {N(apexX - spread)} {N(-len * 0.9)} {N(apexX)} {N(-len)} " +
                      $"C{N(apexX + spread)} {N(-len * 0.9)} {N(xb)} {N(-len * 0.45)} {N(xb)} 2Z");
            apexes.Add((apexX, -len));
            // The canal runs down the middle of its root, from the pulp chamber in the crown to near the apex.
            var mx = (xa + xb) / 2;
            canals.Add($"M{N(mx * 0.35)} {N(ch * 0.22)} C{N(mx)} {N(-len * 0.12)} {N((mx + apexX) / 2)} {N(-len * 0.55)} {N(apexX)} {N(-len * 0.92)}");
        }
        switch (t.Roots)
        {
            case 1:
                Root(-hc, hc, t.Kind == Kind.Canine ? hc * 0.1 : 0, rl);
                break;
            case 2:
                if (t.Kind == Kind.Molar)
                {
                    Root(-hc, hc * 0.12, -hc * 0.62, rl);
                    Root(-hc * 0.12, hc, hc * 0.55, rl * 0.94);
                }
                else
                {
                    // Upper first premolar: two slender roots side by side.
                    Root(-hc, hc * 0.2, -hc * 0.3, rl);
                    Root(-hc * 0.2, hc, hc * 0.3, rl * 0.96);
                }
                break;
            default:
                Root(-hc * 0.45, hc * 0.45, 0, rl * 1.06);            // palatal, behind
                Root(-hc, hc * 0.15, -hc * 0.75, rl);                 // mesiobuccal
                Root(-hc * 0.15, hc, hc * 0.7, rl * 0.9);             // distobuccal
                break;
        }
        return new SideView(sb.ToString(), roots, apexes, w, cw, ch, rl, groove, canals);
    }

    public static string N(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
}
