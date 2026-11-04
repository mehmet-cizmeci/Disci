using Disci.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Disci.LegacyImport.Import;

/// <summary>
/// Applies <see cref="TurkishNames"/> to patients (and their relatives) that came from the old programs. Words that
/// already have Turkish letters are not touched, so names corrected by hand in Disci stay as they are. Running it again
/// changes nothing.
/// </summary>
public sealed class NameFix(DisciDbContext db, ImportReport report, bool dryRun)
{
    public async Task RunAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var patients = await db.Patients.Include(p => p.Relatives)
            .Where(p => p.LegacyId != null || p.Dl2001Protocol != null)
            .ToListAsync(ct);

        var changes = new List<string>();
        int patientCount = 0, relativeCount = 0;
        var unknown = new Dictionary<string, int>();
        foreach (var p in patients)
        {
            var first = TurkishNames.FixFirstName(p.FirstName);
            var last = TurkishNames.FixLastName(p.LastName);
            if (first != p.FirstName || last != p.LastName)
            {
                changes.Add($"Disci #{p.Id}: {p.FirstName} {p.LastName} → {first} {last}");
                p.FirstName = first;
                p.LastName = last;
                patientCount++;
            }
            foreach (var r in p.Relatives)
            {
                var fixedName = TurkishNames.FixFullName(r.Name);
                if (fixedName != r.Name) { r.Name = fixedName; relativeCount++; }
            }
            // Plain words that might need a Turkish letter but are not in the lists: for extending them.
            foreach (var word in $"{first} {last}".Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (!TurkishNames.IsKnown(word) && word.All(c => c < 128 || c == 'ı') && word.ToUpperInvariant().IndexOfAny(['C', 'S', 'G', 'U', 'O', 'I']) >= 0)
                {
                    var key = PatientMatcher.NameKey(word);
                    unknown[key] = unknown.GetValueOrDefault(key) + 1;
                }
        }
        await db.SaveChangesAsync(ct);
        if (dryRun) await tx.RollbackAsync(ct); else await tx.CommitAsync(ct);

        report.Target("İncelenen hasta", patients.Count);
        report.Target("Adı düzeltilen hasta", patientCount);
        report.Target("Adı düzeltilen hasta yakını", relativeCount);
        report.Section("Düzeltilen adlar", changes);
        report.Section("Listede olmadığı için değiştirilmeyen sık kelimeler (Türkçe harf gerekebilir)",
            unknown.Where(x => x.Value >= 3).OrderByDescending(x => x.Value).Select(x => $"{x.Key}: {x.Value}"));
        if (dryRun) report.Warn("DENEME: hiçbir değişiklik kaydedilmedi (--dry-run).");
    }
}
