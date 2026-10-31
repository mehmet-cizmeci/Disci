using System.Globalization;
using System.Text;

namespace Disci.LegacyImport.Import;

public sealed class ImportReport
{
    private static readonly CultureInfo Tr = new("tr-TR");
    private readonly SortedDictionary<string, (int Read, int Header)> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Name, int Count)> _targets = [];
    private readonly List<(string Name, decimal Value)> _totals = [];
    private readonly List<string> _warnings = [];

    public void Source(string table, int read, int header) => _sources[table] = (read, header);
    public void Target(string name, int count) => _targets.Add((name, count));
    public void Total(string name, decimal value) => _totals.Add((name, value));
    public void Warn(string message) => _warnings.Add(message);

    /// <summary>True when every source table returned as many records as its header announces.</summary>
    public bool SourcesComplete => _sources.Values.All(s => s.Read == s.Header);

    public string ToMarkdown(string sourceDir, string connection, TimeSpan elapsed)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# DentaLineSuit → Disci aktarım raporu").AppendLine();
        sb.AppendLine($"- Tarih: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- Kaynak: `{sourceDir}`");
        sb.AppendLine($"- Hedef: `{connection}`");
        sb.AppendLine($"- Süre: {elapsed.TotalSeconds:N1} sn").AppendLine();

        sb.AppendLine("## Okunan eski tablolar").AppendLine();
        sb.AppendLine("| Tablo | Okunan | Dosya başlığındaki | Durum |").AppendLine("|---|---:|---:|---|");
        foreach (var (table, (read, header)) in _sources)
            sb.AppendLine($"| {table} | {read.ToString("N0", Tr)} | {header.ToString("N0", Tr)} | {(read == header ? "✅" : "⚠️ eksik/fazla")} |");

        sb.AppendLine().AppendLine("## Yeni veritabanına yazılanlar").AppendLine();
        sb.AppendLine("| Kayıt türü | Adet |").AppendLine("|---|---:|");
        foreach (var (name, count) in _targets)
            sb.AppendLine($"| {name} | {count.ToString("N0", Tr)} |");

        sb.AppendLine().AppendLine("## Tutar kontrolleri").AppendLine();
        sb.AppendLine("| Kontrol | Toplam |").AppendLine("|---|---:|");
        foreach (var (name, value) in _totals)
            sb.AppendLine($"| {name} | {value.ToString("N2", Tr)} |");

        sb.AppendLine().AppendLine($"## Uyarılar ({_warnings.Count})").AppendLine();
        foreach (var w in _warnings)
            sb.AppendLine($"- {w}");
        return sb.ToString();
    }
}
