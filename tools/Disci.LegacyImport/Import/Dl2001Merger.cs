using System.Globalization;
using System.Text.Json;
using Disci.Domain;
using Disci.Infrastructure;
using Disci.LegacyImport.Paradox;
using Microsoft.EntityFrameworkCore;
using static Disci.LegacyImport.Import.LegacyImporter;

namespace Disci.LegacyImport.Import;

public sealed record Dl2001Options(string SourceDir, string FilesDir, bool DryRun);

/// <summary>
/// Adds a DentaLine 2001 (2.3) database to a Disci database that already holds the DentaLineSuit import.
/// DentaLine 2001 has its own numbering (protocol 5 there is another person than protocol 5 in DentaLineSuit), so
/// patients are matched by name, phone and birth date (<see cref="PatientMatcher"/>). For a matched patient, records
/// that already exist (same day and amount, same procedure on the same tooth) are skipped. Every imported record keeps
/// its old key with <see cref="LegacySources.Dl2001"/>, so running again only adds what is new.
/// </summary>
public sealed class Dl2001Merger(DisciDbContext db, Dl2001Options options, ImportReport report)
{
    private const string Source = LegacySources.Dl2001;
    private static readonly CultureInfo Tr = new("tr-TR");

    private readonly Dictionary<int, int> _patientIds = [];      // DL2001 protocol → Disci patient id
    private readonly List<string> _merged = [], _review = [];

    public async Task RunAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await Step("Fiyat listesi", ImportPriceListsAsync, ct);
        await Step("Hastalar (eşleştirme)", ImportPatientsAsync, ct);
        await Step("Tedaviler", ImportTreatmentsAsync, ct);
        await Step("Ödemeler", ImportPaymentsAsync, ct);
        await Step("Diş notları", ImportToothNotesAsync, ct);
        await Step("Fotoğraflar", ImportPhotosAsync, ct);
        WarnUnhandled();

        report.Section("Birleştirilen hastalar (DL2001 kaydı mevcut hastaya eklendi)", _merged);
        report.Section("Ayrı bırakılan aynı adlı hastalar (başka kişi sayıldı; aynı kişiyse elle birleştirilmeli)", _review);

        if (options.DryRun)
        {
            await tx.RollbackAsync(ct);
            report.Warn("DENEME: hiçbir değişiklik kaydedilmedi (--dry-run).");
        }
        else
            await tx.CommitAsync(ct);
    }

    private async Task Step(string title, Func<CancellationToken, Task> action, CancellationToken ct)
    {
        Console.WriteLine($"  {title}...");
        await action(ct);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }

    private readonly Dictionary<string, ParadoxTable> _tables = new(StringComparer.OrdinalIgnoreCase);

    private List<ParadoxRecord> Rows(string name)
    {
        if (!_tables.TryGetValue(name, out var table))
        {
            var path = Directory.EnumerateFiles(options.SourceDir, name + ".db", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })
                .FirstOrDefault();
            if (path is null) return [];
            _tables[name] = table = new ParadoxTable(path);
        }
        var rows = table.ReadRecords().ToList();
        report.Source(name, rows.Count, table.HeaderRecordCount);
        return rows;
    }

    // ---------------------------------------------------------------- price lists

    private async Task ImportPriceListsAsync(CancellationToken ct)
    {
        var procedures = await db.Procedures.ToDictionaryAsync(p => p.Code, ct);
        var existing = (await db.PriceLists.Select(l => l.LegacyCode).ToListAsync(ct)).ToHashSet();
        var vatByGroup = Rows("DLTRGRUP").Where(r => r.Str("GRUP") is not null).ToDictionary(r => r.Str("GRUP")!, r => r.Dec("KDVYZD"));
        int lists = 0, items = 0, added = 0;

        foreach (var group in Rows("DLTRIFES").Where(r => r.Str("GRUP") is not null && r.Str("KOD") is not null).GroupBy(r => r.Str("GRUP")!))
        {
            // Only the list with VAT-inclusive totals was in use; the other is the old TDB floor list already imported from DentaLineSuit.
            if (!group.Any(r => r.Num("TOPLAM") is > 0)) continue;
            var code = $"{Source}:{group.Key}";
            if (existing.Contains(code)) continue;
            var list = new PriceList
            {
                Name = $"DentaLine 2001 tarifesi ({group.Key})",
                LegacyCode = code,
                VatRate = vatByGroup.GetValueOrDefault(group.Key),
            };
            foreach (var r in group.DistinctBy(r => r.Str("KOD")))
            {
                var procedure = EnsureProcedure(procedures, r.Str("KOD")!, r.Str("ADI"), ref added);
                var basePrice = r.Dec("BIRIM") ?? 0;
                list.Items.Add(new PriceListItem { Procedure = procedure, BasePrice = basePrice, VatAmount = r.Dec("KDV"), Price = r.Dec("TOPLAM") ?? basePrice });
                items++;
            }
            db.PriceLists.Add(list);
            lists++;
        }
        foreach (var r in Rows("DLOR_DET").Where(r => r.Str("KOD") is not null))
            EnsureProcedure(procedures, r.Str("KOD")!, r.Str("ADI"), ref added);

        report.Target("Fiyat listesi (varsayılan yapılmadı)", lists);
        report.Target("Fiyat listesi kalemi", items);
        report.Target("Yeni tedavi/bulgu tanımı", added);
    }

    private Procedure EnsureProcedure(Dictionary<string, Procedure> procedures, string code, string? name, ref int added)
    {
        if (procedures.TryGetValue(code, out var p)) return p;
        var prefix = code.Length >= 3 ? code[..3] : code;
        p = new Procedure
        {
            Code = code,
            Name = Clean(name) ?? code,
            Category = CategoryByPrefix.GetValueOrDefault(prefix, "Diğer"),
            Kind = FindingPrefixes.Contains(prefix) ? ProcedureKind.Finding : ProcedureKind.Treatment,
        };
        db.Procedures.Add(p);
        procedures[code] = p;
        added++;
        report.Warn($"'{code}' kodu Disci'de yoktu; DentaLine 2001'deki adıyla eklendi.");
        return p;
    }

    // ---------------------------------------------------------------- patients

    private async Task ImportPatientsAsync(CancellationToken ct)
    {
        var existing = await db.Patients
            .Select(p => new { p.Id, p.LegacyId, p.Dl2001Protocol, p.FirstName, p.LastName, p.MobilePhone, p.HomePhone, p.WorkPhone, p.BirthDate })
            .ToListAsync(ct);
        foreach (var p in existing.Where(p => p.Dl2001Protocol is not null))
            _patientIds[p.Dl2001Protocol!.Value] = p.Id;
        foreach (var n in await db.PatientLegacyNumbers.Where(n => n.Source == Source).ToListAsync(ct))
            _patientIds[n.Number] = n.PatientId;
        var legacyNo = existing.ToDictionary(p => p.Id, p => p.LegacyId);

        var matcher = new PatientMatcher(existing.Select(p => new PatientKey(p.Id, p.FirstName + " " + p.LastName,
            PatientMatcher.PhoneKeys(p.MobilePhone, p.HomePhone, p.WorkPhone), p.BirthDate)));
        var priceLists = await db.PriceLists.Where(l => l.LegacyCode != null).ToDictionaryAsync(l => l.LegacyCode!, l => l.Id, ct);

        // Patients opened in this run get negative keys until they are saved, so a second card of the same person can merge into them.
        var pending = new Dictionary<int, (int Protocol, Patient Patient)>();
        var tracked = new Dictionary<int, Patient>();
        var newPatients = new List<(int Protocol, Patient Patient)>();   // every protocol that maps to an unsaved patient
        int already = 0, created = 0, merged = 0;

        foreach (var r in Rows("DLHASTA"))
        {
            int protocol = r.Int("PROTOKOL")!.Value;
            if (_patientIds.ContainsKey(protocol) || newPatients.Any(n => n.Protocol == protocol)) { already++; continue; }

            var incoming = NewPatient(r);
            var key = new PatientKey(0, incoming.FullName, PatientMatcher.PhoneKeys(incoming.MobilePhone, incoming.HomePhone, incoming.WorkPhone), incoming.BirthDate);
            var match = matcher.Match(key);
            string Who(PatientKey c) => c.Id < 0
                ? $"DL2001 protokol {pending[c.Id].Protocol} {c.Name} (DL2001'e iki kez girilmiş)"
                : $"Disci #{c.Id}{(legacyNo[c.Id] is { } l ? $" (DentaLineSuit protokol {l})" : "")} {c.Name}";
            string self = $"DL2001 protokol {protocol} {incoming.FullName}";

            if (match.Kind is MatchKind.Same or MatchKind.Likely)
            {
                var candidate = match.Candidates[0];
                if (!tracked.TryGetValue(candidate.Id, out var target))
                    tracked[candidate.Id] = target = candidate.Id < 0
                        ? pending[candidate.Id].Patient
                        : await db.Patients.Include(p => p.Relatives).SingleAsync(p => p.Id == candidate.Id, ct);
                MergeInto(target, incoming, r, protocol);
                if (candidate.Id < 0) newPatients.Add((protocol, target));
                else _patientIds[protocol] = candidate.Id;
                var rule = match.Kind == MatchKind.Same ? "ad + telefon/doğum tarihi" : "aynı ad, doğum tarihi uyumlu";
                _merged.Add($"{self} → {Who(candidate)} [{rule}]{(match.BirthDiffers ? " — doğum tarihleri farklı, kontrol edin" : "")}");
                merged++;
                continue;
            }

            incoming.Dl2001Protocol = protocol;
            var group = Clean(r.Str("HASTA_GRUBU"));
            if (group is not null && priceLists.TryGetValue(group, out var listId)) incoming.PriceListId = listId;
            incoming.LegacyExtra = PatientExtras(r, group is not null && incoming.PriceListId is null ? ["HASTA_GRUBU"] : []);
            db.Patients.Add(incoming);
            newPatients.Add((protocol, incoming));
            int tempId = -(pending.Count + 1);
            pending[tempId] = (protocol, incoming);
            matcher.Add(key with { Id = tempId });
            created++;

            var reason = match.Kind switch
            {
                MatchKind.Ambiguous => "aynı adlı birden fazla uygun hasta var, hangisi olduğu bilinemedi",
                MatchKind.NameOnlyBirthDiffers => "aynı ad ama doğum tarihleri çok farklı (başka kişi sayıldı)",
                _ => null,
            };
            if (reason is not null)
                _review.Add($"{self}: {reason}. Aday: {string.Join("; ", match.Candidates.Select(Who))}");
        }

        // Records that point at a deleted patient card keep a placeholder so nothing is dropped.
        var referenced = new[] { "DLORALTD", "DLODEME", "DLDISNOT", "DLFOTO" }
            .SelectMany(t => Rows(t).Select(r => r.Int("PROTOKOL")))
            .OfType<int>().Distinct()
            .Where(p => !_patientIds.ContainsKey(p) && newPatients.All(n => n.Protocol != p)).Order();
        foreach (int protocol in referenced)
        {
            var p = PlaceholderPatient(protocol, "DentaLine 2001'de");
            p.Dl2001Protocol = protocol;
            db.Patients.Add(p);
            newPatients.Add((protocol, p));
            created++;
            report.Warn($"DL2001'de hasta kartı olmayan protokol {protocol} için arşivli yer tutucu hasta oluşturuldu.");
        }

        db.ChangeTracker.DetectChanges();
        await db.SaveChangesAsync(ct);
        foreach (var (protocol, p) in newPatients) _patientIds[protocol] = p.Id;

        report.Target("Hasta: mevcut hastayla birleştirilen", merged);
        report.Target("Hasta: yeni açılan", created);
        if (already > 0) report.Target("Hasta: önceki aktarımda gelmiş", already);
        report.Target("Hasta: ayrı bırakılan aynı adlı (kontrol listesi)", _review.Count);
    }

    /// <summary>
    /// Adds what DL2001 knows to an existing patient. DL2001 was used from 2020, so its phone and address are the newer
    /// ones: they replace different old values, and the old values are written into the notes. Nothing is dropped.
    /// </summary>
    private static void MergeInto(Patient target, Patient src, ParadoxRecord r, int protocol)
    {
        var old = new List<string>();
        void Newer(string label, Func<Patient, string?> get, Action<Patient, string?> set)
        {
            var o = get(target);
            var n = get(src);
            if (n is null) return;
            if (o is not null && !PatientMatcher.PhoneKeys(o).SetEquals(PatientMatcher.PhoneKeys(n)) && !string.Equals(o, n, StringComparison.OrdinalIgnoreCase))
                old.Add($"{label}: {o}");
            set(target, n);
        }
        string? Fill(string? o, string? n) => o ?? n;

        // A number DL2001 has in another slot is not a change.
        var knownPhones = PatientMatcher.PhoneKeys(target.MobilePhone, target.HomePhone, target.WorkPhone);
        if (src.MobilePhone is not null && !knownPhones.Overlaps(PatientMatcher.PhoneKeys(src.MobilePhone)))
            Newer("Cep tel", p => p.MobilePhone, (p, v) => p.MobilePhone = v);
        if (src.HomePhone is not null && !knownPhones.Overlaps(PatientMatcher.PhoneKeys(src.HomePhone)))
            Newer("Ev tel", p => p.HomePhone, (p, v) => p.HomePhone = v);
        if (src.WorkPhone is not null && !knownPhones.Overlaps(PatientMatcher.PhoneKeys(src.WorkPhone)))
            Newer("İş tel", p => p.WorkPhone, (p, v) => p.WorkPhone = v);

        if (!IsEmpty(src.HomeAddress) && !SameAddress(target.HomeAddress, src.HomeAddress))
        {
            if (!IsEmpty(target.HomeAddress)) old.Add($"Ev adresi: {Format(target.HomeAddress)}");
            target.HomeAddress = src.HomeAddress;
        }
        if (!IsEmpty(src.WorkAddress) && !SameAddress(target.WorkAddress, src.WorkAddress))
        {
            if (!IsEmpty(target.WorkAddress)) old.Add($"İş adresi: {Format(target.WorkAddress)}");
            target.WorkAddress = src.WorkAddress;
        }

        if (src.BirthDate is { } b)
        {
            // A real date replaces a missing or placeholder one (01.01.1980, 1 January).
            bool targetExact = target.BirthDate is { } t && PatientMatcher.IsExactDate(t);
            if (target.BirthDate is null || (!targetExact && PatientMatcher.IsExactDate(b)))
                target.BirthDate = b;
            else if (target.BirthDate != b)
                old.Add($"DL2001'deki doğum tarihi: {b.ToString("dd.MM.yyyy", Tr)}");
        }

        target.NationalId = Fill(target.NationalId, src.NationalId);
        target.BirthPlace = Fill(target.BirthPlace, src.BirthPlace);
        if (target.Gender == Gender.Unknown) target.Gender = src.Gender;
        target.BloodGroup = Fill(target.BloodGroup, src.BloodGroup);
        target.Nationality = Fill(target.Nationality, src.Nationality);
        target.Occupation = Fill(target.Occupation, src.Occupation);
        target.Email = Fill(target.Email, src.Email);
        target.SecondaryEmail = Fill(target.SecondaryEmail, src.SecondaryEmail);
        target.ReferredBy = Fill(target.ReferredBy, src.ReferredBy);
        target.ReferrerPhone = Fill(target.ReferrerPhone, src.ReferrerPhone);
        target.FamilyDoctor = Fill(target.FamilyDoctor, src.FamilyDoctor);
        target.InsuranceProvider = Fill(target.InsuranceProvider, src.InsuranceProvider);
        target.InsuranceNo = Fill(target.InsuranceNo, src.InsuranceNo);
        target.TaxOffice = Fill(target.TaxOffice, src.TaxOffice);
        target.TaxNo = Fill(target.TaxNo, src.TaxNo);
        target.DiscountRate ??= src.DiscountRate;
        if (src.RegisteredOn < target.RegisteredOn) target.RegisteredOn = src.RegisteredOn;
        target.IsArchived = false;

        foreach (var rel in src.Relatives)
            if (!target.Relatives.Any(x => PatientMatcher.NameKey(x.Name) == PatientMatcher.NameKey(rel.Name)))
                target.Relatives.Add(rel);

        var lines = new List<string> { $"DentaLine 2001 kaydıyla birleştirildi (DL2001 protokol {protocol})." };
        if (src.Notes is { } n && target.Notes?.Contains(n, StringComparison.Ordinal) != true) lines.Add($"DL2001 notu: {n}");
        if (old.Count > 0) lines.Add("Değişmeden önceki bilgiler: " + string.Join("; ", old));
        var notes = Join("\n", target.Notes, string.Join("\n", lines))!;
        target.Notes = notes.Length > 4000 ? notes[..4000] : notes;

        target.LegacyExtra = MergeExtras(target.LegacyExtra, PatientExtras(r, "HASTA_GRUBU"));
        // The same person can have had two cards in DL2001: the first number is the main one, the others stay searchable.
        if (target.Dl2001Protocol is null) target.Dl2001Protocol = protocol;
        else target.LegacyNumbers.Add(new PatientLegacyNumber { Source = Source, Number = protocol });
    }

    private static string? MergeExtras(string? existing, string? dl2001)
    {
        if (dl2001 is null) return existing;
        var map = existing is null ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existing) ?? [];
        map[Source] = JsonSerializer.Deserialize<JsonElement>(dl2001);
        return JsonSerializer.Serialize(map, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    private static bool IsEmpty(Address a) => a.Line1 is null && a.Line2 is null && a.PostalCode is null && a.District is null && a.City is null;
    private static string Format(Address a) => Join(", ", a.Line1, a.Line2, a.District, a.PostalCode, a.City) ?? "";
    private static bool SameAddress(Address a, Address b) => PatientMatcher.NameKey(Format(a)) == PatientMatcher.NameKey(Format(b));

    // ---------------------------------------------------------------- treatments

    private async Task ImportTreatmentsAsync(CancellationToken ct)
    {
        var procedures = await db.Procedures.ToDictionaryAsync(p => p.Code, p => p.Id, ct);
        var sessionIds = await db.TreatmentSessions.Where(s => s.LegacySource == Source)
            .ToDictionaryAsync(s => s.LegacyId!.Value, s => s.Id, ct);
        var importedItems = (await db.TreatmentItems.Where(i => i.LegacySource == Source).Select(i => i.LegacyId!.Value).ToListAsync(ct)).ToHashSet();

        // Every DL2001 patient, also those imported by an earlier run: records not from DL2001 (DentaLineSuit or typed into Disci) count.
        var ids = _patientIds.Values.Distinct().ToList();
        var existingItems = await db.TreatmentItems
            .Where(i => ids.Contains(i.TreatmentSession.PatientId) && i.LegacySource == null)
            .Select(i => new { i.TreatmentSession.PatientId, i.TreatmentSession.Date, i.TreatmentSession.Kind, i.ProcedureId, i.Description, i.ToothNumber, i.Price })
            .ToListAsync(ct);
        var itemPool = new Multiset<(int, DateOnly, TreatmentKind, string, int?, decimal)>(
            existingItems.Select(i => (i.PatientId, i.Date, i.Kind, i.ProcedureId?.ToString(CultureInfo.InvariantCulture) ?? PatientMatcher.NameKey(i.Description), i.ToothNumber, i.Price)));
        var emptySessions = new Multiset<(int, DateOnly, TreatmentKind, string)>(
            (await db.TreatmentSessions.Where(s => ids.Contains(s.PatientId) && s.LegacySource == null && !s.Items.Any())
                .Select(s => new { s.PatientId, s.Date, s.Kind, s.Notes }).ToListAsync(ct))
            .Select(s => (s.PatientId, s.Date, s.Kind, PatientMatcher.NameKey(s.Notes))));

        var itemsBySession = Rows("DLOR_DET").Where(r => r.Int("TEDNO") is not null).ToLookup(r => r.Int("TEDNO")!.Value);
        int sessions = 0, items = 0, dupSessions = 0, dupItems = 0, badVat = 0;
        decimal total = 0;
        var known = new HashSet<int>();

        foreach (var r in Rows("DLORALTD"))
        {
            var s = NewSession(r);
            int tedno = s.LegacyId!.Value;
            known.Add(tedno);
            if (!_patientIds.TryGetValue(r.Int("PROTOKOL") ?? -1, out var patientId))
            {
                report.Warn($"DL2001 tedavi kaydı {tedno}: hasta {r.Int("PROTOKOL")} yok, atlandı.");
                continue;
            }
            var rows = itemsBySession[tedno].Where(i => !importedItems.Contains(i.Int("TED_DETNO") ?? -1)).ToList();
            var newItems = new List<TreatmentItem>();
            foreach (var row in rows)
            {
                var code = row.Str("KOD");
                int? procedureId = code is not null && procedures.TryGetValue(code, out var pid) ? pid : null;
                var item = NewItem(row, null, out bool bad);
                item.ProcedureId = procedureId;
                item.LegacySource = Source;
                if (bad) badVat++;
                bool isNewSession = !sessionIds.ContainsKey(tedno);
                if (isNewSession && itemPool.TryTake((patientId, s.Date, s.Kind,
                        procedureId?.ToString(CultureInfo.InvariantCulture) ?? PatientMatcher.NameKey(item.Description), item.ToothNumber, item.Price)))
                {
                    dupItems++;
                    continue;
                }
                newItems.Add(item);
            }

            if (sessionIds.TryGetValue(tedno, out var sessionId))
            {
                // Imported before; only items added to it since then.
                foreach (var item in newItems) { item.TreatmentSessionId = sessionId; db.TreatmentItems.Add(item); }
            }
            else
            {
                bool duplicate = rows.Count > 0
                    ? newItems.Count == 0
                    : emptySessions.TryTake((patientId, s.Date, s.Kind, PatientMatcher.NameKey(s.Notes)));
                if (duplicate) { dupSessions++; continue; }
                s.PatientId = patientId;
                s.LegacySource = Source;
                s.Items.AddRange(newItems);
                db.TreatmentSessions.Add(s);
                sessions++;
            }
            items += newItems.Count;
            total += newItems.Sum(i => i.Price);
        }
        int orphans = itemsBySession.Where(g => !known.Contains(g.Key)).Sum(g => g.Count());
        if (orphans > 0) report.Warn($"DL2001'de bağlı olduğu tedavi kaydı olmayan {orphans} tedavi kalemi atlandı.");
        if (badVat > 0) report.Warn($"DL2001: {badVat} tedavi kaleminde KDV tutardan büyüktü (bozuk veri); KDV 0 yazıldı, tutar değişmedi.");

        report.Target("Tedavi kaydı (seans)", sessions);
        report.Target("Tedavi kalemi", items);
        report.Target("Mükerrer olduğu için atlanan tedavi seansı", dupSessions);
        report.Target("Mükerrer olduğu için atlanan tedavi kalemi", dupItems);
        report.Total("Eklenen tedavi kalemleri toplam tutar", total);
    }

    // ---------------------------------------------------------------- payments

    private async Task ImportPaymentsAsync(CancellationToken ct)
    {
        var imported = (await db.Payments.Where(p => p.LegacySource == Source).Select(p => p.LegacyId!.Value).ToListAsync(ct)).ToHashSet();
        // Every DL2001 patient, also those imported by an earlier run: records not from DL2001 (DentaLineSuit or typed into Disci) count.
        var ids = _patientIds.Values.Distinct().ToList();
        var pool = new Multiset<(int, DateOnly, decimal, string)>(
            (await db.Payments.Where(p => ids.Contains(p.PatientId) && p.LegacySource == null)
                .Select(p => new { p.PatientId, p.Date, p.Amount, p.Currency }).ToListAsync(ct))
            .Select(p => (p.PatientId, p.Date, p.Amount, p.Currency)));
        var rates = ExchangeRates(Rows("DLODEDET"));

        int count = 0, duplicates = 0;
        var totals = new Dictionary<string, decimal>();
        foreach (var r in Rows("DLODEME"))
        {
            int legacy = r.Int("ODEME_ID")!.Value;
            if (imported.Contains(legacy)) continue;
            if (!_patientIds.TryGetValue(r.Int("PROTOKOL") ?? -1, out var patientId))
            {
                report.Warn($"DL2001 ödeme {legacy}: hasta {r.Int("PROTOKOL")} yok, atlandı.");
                continue;
            }
            var p = NewPayment(r, rates);
            if (r.Str("TYPE") == "H") { p.Method = PaymentMethod.BankTransfer; }
            if (pool.TryTake((patientId, p.Date, p.Amount, p.Currency))) { duplicates++; continue; }
            p.PatientId = patientId;
            p.LegacySource = Source;
            db.Payments.Add(p);
            totals[p.Currency] = totals.GetValueOrDefault(p.Currency) + p.Amount;
            count++;
        }
        report.Target("Ödeme", count);
        report.Target("Mükerrer olduğu için atlanan ödeme", duplicates);
        foreach (var (currency, sum) in totals.OrderBy(x => x.Key))
            report.Total($"Eklenen ödemeler toplamı ({currency})", sum);
    }

    // ---------------------------------------------------------------- tooth notes and photos

    private async Task ImportToothNotesAsync(CancellationToken ct)
    {
        var ids = _patientIds.Values.ToList();
        var existing = (await db.ToothNotes.Where(n => ids.Contains(n.PatientId)).Select(n => new { n.PatientId, n.ToothNumber, n.Note }).ToListAsync(ct))
            .Select(n => (n.PatientId, n.ToothNumber, PatientMatcher.NameKey(n.Note))).ToHashSet();
        int count = 0, duplicates = 0;
        foreach (var r in Rows("DLDISNOT"))
        {
            var text = Clean(r.Str("NOT"));
            if (text is null || text.Trim('?', ' ', '\n', '\r').Length == 0) continue;
            if (!_patientIds.TryGetValue(r.Int("PROTOKOL") ?? -1, out var patientId)) continue;
            var tooth = r.Int("DISNO") ?? 0;
            if (!existing.Add((patientId, tooth, PatientMatcher.NameKey(text)))) { duplicates++; continue; }
            db.ToothNotes.Add(new ToothNote { PatientId = patientId, ToothNumber = tooth, Note = text });
            count++;
        }
        report.Target("Diş notu", count);
        if (duplicates > 0) report.Target("Zaten var olduğu için atlanan diş notu", duplicates);
    }

    private async Task ImportPhotosAsync(CancellationToken ct)
    {
        var imported = (await db.PatientPhotos.Where(p => p.LegacySource == Source).Select(p => p.LegacyId!.Value).ToListAsync(ct)).ToHashSet();
        var imageDir = Path.Combine(options.SourceDir, "IMAGE");
        var files = Directory.Exists(imageDir)
            ? Directory.EnumerateFiles(imageDir, "*", SearchOption.AllDirectories)
                .Select(f => (Path: f, Name: Path.GetFileNameWithoutExtension(f)))
                .Where(f => f.Name.Length is >= 4 and <= 8 && f.Name.All(Uri.IsHexDigit))
                .GroupBy(f => Convert.ToInt32(f.Name, 16))
                .ToDictionary(g => g.Key, g => g.First().Path)
            : [];
        int count = 0;
        foreach (var r in Rows("DLFOTO"))
        {
            int no = r.Int("RESIM_NO")!.Value;
            if (imported.Contains(no) || !_patientIds.TryGetValue(r.Int("PROTOKOL") ?? -1, out var patientId)) continue;
            if (!files.TryGetValue(no, out var source))
            {
                report.Warn($"DL2001 fotoğraf {no}: dosya bulunamadı.");
                continue;
            }
            var date = r.Date("TARIH") ?? DateOnly.FromDateTime(DateTime.Today);
            var relative = Path.Combine("patients", patientId.ToString(CultureInfo.InvariantCulture), "photos",
                $"{date:yyyy-MM-dd}_dl2001_{no}{Path.GetExtension(source).ToLowerInvariant()}");
            if (!options.DryRun)
            {
                var target = Path.Combine(options.FilesDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(source, target, overwrite: true);
            }
            db.PatientPhotos.Add(new PatientPhoto
            {
                LegacyId = no,
                LegacySource = Source,
                PatientId = patientId,
                TakenOn = date,
                Kind = r.Str("RESIM_CINSI") switch { "C" => PhotoKind.IntraOral, "F" => PhotoKind.Face, _ => PhotoKind.Other },
                Jaw = r.Int("CENE_NO"),
                ToothNumber = r.Int("DIS_NO") is > 0 and var tooth ? tooth : null,
                Description = Join(" - ", Clean(r.Str("IS_CINSI")), Clean(r.Str("IS_ACIKLAMA"))),
                FilePath = relative.Replace('\\', '/'),
            });
            count++;
        }
        report.Target("Fotoğraf", count);
    }

    private void WarnUnhandled()
    {
        foreach (var table in new[] { "DLANAMNZ", "DLNOTLAR", "DLGIDER", "DLLABOR", "RANDEVU", "DLRECETE" })
            if (Rows(table).Count is > 0 and var n)
                report.Warn($"DL2001 {table} tablosunda {n} kayıt var ama bu tablo aktarılmıyor (yazıldığı sırada boştu). Aktarıma eklenmeli.");
    }

    /// <summary>A bag of keys; each existing record can cancel out only one incoming record.</summary>
    private sealed class Multiset<T>(IEnumerable<T> items) where T : notnull
    {
        private readonly Dictionary<T, int> _counts = items.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

        public bool TryTake(T key)
        {
            if (!_counts.TryGetValue(key, out var n) || n == 0) return false;
            _counts[key] = n - 1;
            return true;
        }
    }
}
