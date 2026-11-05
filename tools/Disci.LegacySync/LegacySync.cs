using System.Globalization;
using System.Text;
using Disci.Domain;
using Disci.Infrastructure;
using Disci.LegacyImport.Import;
using Disci.LegacyImport.Paradox;
using Microsoft.EntityFrameworkCore;

namespace Disci.LegacySync;

/// <summary>One old program's database folder, read once before anything is written.</summary>
internal sealed class Side(string name, string dir)
{
    private readonly Dictionary<string, (ParadoxTable Table, List<ParadoxRecord> Rows)> _cache = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; } = name;
    public string Dir { get; } = Path.GetFullPath(dir);

    public string? File(string table) => Directory.EnumerateFiles(Dir, table + ".db",
        new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

    public List<ParadoxRecord> Rows(string table) => Load(table).Rows;

    public Encoding Encoding(string table) => Load(table).Table.Encoding;

    private (ParadoxTable Table, List<ParadoxRecord> Rows) Load(string table)
    {
        if (!_cache.TryGetValue(table, out var t))
        {
            var path = File(table) ?? throw new FileNotFoundException($"{Name}: {table} tablosu yok ({Dir}).");
            var pt = new ParadoxTable(path);
            _cache[table] = t = (pt, pt.ReadRecords().ToList());
        }
        return t;
    }

    /// <summary>The photo folder (IMAGE or Image).</summary>
    public string ImageDir => Directory.EnumerateDirectories(Dir, "IMAGE", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })
        .FirstOrDefault() ?? Path.Combine(Dir, "IMAGE");
}

/// <summary>A person: the DentaLineSuit card and the DentaLine 2001 card(s), as Disci matched them.</summary>
internal sealed record PatientGroup(int DisciId, int? Dls, List<int> D01);

/// <summary>
/// Fills DentaLineSuit with the DentaLine 2001 records and DentaLine 2001 with the DentaLineSuit records, so that both
/// programs hold everything. Patients are paired exactly as the Disci import paired them (Disci database), so no new
/// matching decisions are made here. Records of a paired patient that are already on the other side (same day, same
/// procedure, tooth and amount; same payment) are not copied twice. Every copied row is listed in
/// <see cref="SyncMap"/> next to the data, for later imports into Disci.
/// </summary>
internal sealed class LegacySync(Side dls, Side d01, string connection, Report report)
{
    private static readonly DateOnly PlaceholderBirth = new(1980, 1, 1);
    private static readonly HashSet<string> NeverFill = new(StringComparer.OrdinalIgnoreCase)
        { "PROTOKOL", "TOP_BORC_SUT", "TOP_BORC_DAIMI", "TOP_ODEME", "FOTO", "DIS_HEKIMI_NO" };

    private readonly SyncMap _dlsMap = new() { Source = SyncMap.FromDl2001, CreatedAt = DateTime.Now };
    private readonly SyncMap _d01Map = new() { Source = SyncMap.FromDentaLineSuit, CreatedAt = DateTime.Now };

    public async Task RunAsync(string privateDir)
    {
        foreach (var side in new[] { dls, d01 })
            if (SyncMap.Load(side.Dir) is not null)
                throw new InvalidOperationException($"{side.Name} daha önce eşitlenmiş ({SyncMap.FileName} var). İkinci kez çalıştırılamaz.");

        // Everything is read before the first write, so each direction copies only the other program's own records.
        foreach (var t in new[] { "DLHASTA", "DLORALTD", "DLOR_DET", "DLODEME", "DLODEDET", "DLDISNOT", "DLFOTO", "DLANAMNZ",
                     "DLNOTLAR", "DLGIDER", "DLLABOR", "DLHEKIM", "DLILAC", "DLTASNIF" })
        {
            dls.Rows(t);
            d01.Rows(t);
        }
        var groups = await LoadGroupsAsync();

        var toDls = new Dictionary<int, int>();
        var toD01 = new Dictionary<int, int>();
        foreach (var g in groups.Where(g => g.Dls is not null && g.D01.Count > 0))
        {
            foreach (var q in g.D01) toDls[q] = g.Dls!.Value;
            toD01[g.Dls!.Value] = g.D01[0];
            _dlsMap.MatchedPatients[g.Dls.Value] = [.. g.D01];
            foreach (var q in g.D01) _d01Map.MatchedPatients[q] = [g.Dls.Value];
        }
        var matchedDls = toD01.Keys.ToHashSet();
        var matchedD01 = toDls.Keys.ToHashSet();
        report.Line($"Disci'deki eşleşmeye göre iki programda da kartı olan hasta: {matchedDls.Count} (DL2001 kartı {matchedD01.Count})");

        // ---------------- DentaLine 2001 → DentaLineSuit
        report.Section($"DentaLine 2001 → DentaLineSuit ({dls.Dir})");
        using (var db = new BdeDatabase(dls.Dir, privateDir))
        {
            using (var t = db.Open("DLHASTA.DB", dls.Encoding("DLHASTA")))
            {
                int created = 0;
                foreach (var g in groups.Where(g => g.Dls is null && g.D01.Count > 0))
                {
                    var src = d01.Rows("DLHASTA").Single(r => r.Int("PROTOKOL") == g.D01[0]);
                    var stored = t.Insert(Copy(src, t, new() { ["DIS_HEKIMI_NO"] = null }));
                    int prot = (int)stored["PROTOKOL"]!;
                    foreach (var q in g.D01) toDls[q] = prot;
                    _dlsMap.CopiedPatients[prot] = [.. g.D01];
                    created++;
                }
                report.Count("Yeni hasta kartı", created);
                report.Count("Boş alanları DL2001'den tamamlanan hasta kartı", FillEmpty(t, d01, toD01.ToDictionary(x => x.Key, x => x.Value), null));
            }
            CopyRecords(db, d01, dls, toDls, matchedDls, _dlsMap);
            Union(db, d01, dls, "DLILAC", "KOD");
            Union(db, d01, dls, "DLTASNIF", "ACIKLAMA");
        }

        // ---------------- DentaLineSuit → DentaLine 2001
        report.Section($"DentaLineSuit → DentaLine 2001 ({d01.Dir})");
        using (var db = new BdeDatabase(d01.Dir, privateDir))
        {
            var doctors = CopyDoctors(db);
            using (var t = db.Open("DLHASTA.DB", d01.Encoding("DLHASTA")))
            {
                int created = 0, lostId = 0;
                foreach (var g in groups.Where(g => g.Dls is not null && g.D01.Count == 0))
                {
                    var src = dls.Rows("DLHASTA").Single(r => r.Int("PROTOKOL") == g.Dls);
                    int? doctor = src.Int("DIS_HEKIMI_NO") is { } dn && doctors.TryGetValue(dn, out var nd) ? nd : null;
                    var stored = t.Insert(Copy(src, t, new() { ["DIS_HEKIMI_NO"] = doctor }));
                    int prot = (int)stored["PROTOKOL"]!;
                    toD01[g.Dls!.Value] = prot;
                    _d01Map.CopiedPatients[prot] = [g.Dls.Value];
                    if (src.Str("KIMLIK_NO") is not null) lostId++;
                    created++;
                }
                report.Count("Yeni hasta kartı", created);
                if (lostId > 0) report.Warn($"{lostId} hastanın kimlik numarası DentaLine 2001'e yazılamadı: o programda kimlik no alanı yok.");
                var fillFrom = matchedDls.ToDictionary(p => toD01[p], p => p);
                report.Count("Boş alanları DentaLineSuit'ten tamamlanan hasta kartı", FillEmpty(t, dls, fillFrom, doctors));
            }
            CopyRecords(db, dls, d01, toD01, matchedD01.Select(q => q).ToHashSet(), _d01Map);
            CopyAnamnesisAndNotes(db, toD01);
            CopyExpenses(db, toD01);
            Union(db, dls, d01, "DLILAC", "KOD");
            Union(db, dls, d01, "DLTASNIF", "ACIKLAMA");
        }

        _dlsMap.Save(dls.Dir);
        _d01Map.Save(d01.Dir);
        report.Section("Değişmeyen tablolar");
        report.Line("Fiyat listeleri (DLTRIFES, DLTDB, DLTRGRUP): iki programın fiyatları farklı ve aynı kodları kullanıyor; her program kendi fiyat listesini korudu.");
        report.Line("Kullanıcılar (DLSIFRE), sistem/lisans (DLSYSTEM), döviz kurları (DLDOVIZ), bulgu kodları (DLDIAGNZ), randevu türleri: programa ait ayarlar, kopyalanmadı.");
        report.Line("RANDEVU, DLRECETE, DLRECENO: iki programda da boş.");
    }

    // ------------------------------------------------------------------ patients

    private async Task<List<PatientGroup>> LoadGroupsAsync()
    {
        await using var db = new DisciDbContext(DisciDbContext.CreateOptions(connection, 120));
        var patients = await db.Patients.IgnoreQueryFilters().Select(p => new { p.Id, p.LegacyId, p.Dl2001Protocol }).ToListAsync();
        var extras = (await db.PatientLegacyNumbers.Where(n => n.Source == LegacySources.Dl2001).Select(n => new { n.PatientId, n.Number }).ToListAsync())
            .ToLookup(n => n.PatientId, n => n.Number);

        var dlsCards = dls.Rows("DLHASTA").Select(r => r.Int("PROTOKOL")!.Value).ToHashSet();
        var d01Cards = d01.Rows("DLHASTA").Select(r => r.Int("PROTOKOL")!.Value).ToHashSet();
        var groups = new List<PatientGroup>();
        foreach (var p in patients)
        {
            int? s = p.LegacyId is { } l && dlsCards.Contains(l) ? l : null;
            var d = new[] { p.Dl2001Protocol }.Concat(extras[p.Id].Select(x => (int?)x)).OfType<int>().Where(d01Cards.Contains).Distinct().ToList();
            if (s is not null || d.Count > 0) groups.Add(new PatientGroup(p.Id, s, d));
        }

        // Disci must know every card of both programs; otherwise its import is older than the data and pairing would be guesswork.
        var missingDls = dlsCards.Except(groups.Where(g => g.Dls is not null).Select(g => g.Dls!.Value)).ToList();
        var missingD01 = d01Cards.Except(groups.SelectMany(g => g.D01)).ToList();
        if (missingDls.Count > 0 || missingD01.Count > 0)
            throw new InvalidOperationException($"Disci veritabanında karşılığı olmayan hasta kartı var (DentaLineSuit {missingDls.Count}, DL2001 {missingD01.Count}). " +
                "Önce Disci aktarımını güncelleyin (--dl2001), sonra yeniden deneyin.");
        if (groups.SelectMany(g => g.D01).GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Bir DL2001 kartı birden fazla Disci hastasına bağlı.");
        return groups;
    }

    /// <summary>Fills blank fields of existing cards (number here → number on the other side). Nothing is overwritten.</summary>
    private int FillEmpty(BdeTable t, Side source, Dictionary<int, int> from, Dictionary<int, int>? doctors)
    {
        var rows = source.Rows("DLHASTA").ToDictionary(r => r.Int("PROTOKOL")!.Value);
        int fields = 0;
        int cards = t.UpdateWhere(current =>
        {
            if (current["PROTOKOL"] is not int prot || !from.TryGetValue(prot, out var other)) return null;
            var src = rows[other];
            var updates = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in src.All())
            {
                if (value is null || value is byte[] || NeverFill.Contains(name) || !t.Has(name) || !current.ContainsKey(name)) continue;
                if (value is string s && string.IsNullOrWhiteSpace(s)) continue;
                if (name.Equals("DOGUM_TARIHI", StringComparison.OrdinalIgnoreCase) && value is DateOnly d && d == PlaceholderBirth) continue;
                var cur = current[name];
                if (cur is null || (cur is string cs && string.IsNullOrWhiteSpace(cs)))
                    updates[name] = value;
            }
            if (doctors is not null && current["DIS_HEKIMI_NO"] is null && src.Int("DIS_HEKIMI_NO") is { } dn && doctors.TryGetValue(dn, out var nd))
                updates["DIS_HEKIMI_NO"] = nd;
            fields += updates.Count;
            return updates;
        });
        report.Count("  Tamamlanan alan sayısı", fields);
        return cards;
    }

    private Dictionary<int, int> CopyDoctors(BdeDatabase db)
    {
        var map = new Dictionary<int, int>();
        var existing = d01.Rows("DLHEKIM").Where(r => r.Str("ADI") is not null)
            .GroupBy(r => PatientMatcher.NameKey(r.Str("ADI"))).ToDictionary(g => g.Key, g => g.First().Int("DOCTOR_NO")!.Value);
        using var t = db.Open(d01.File("DLHEKIM")!, d01.Encoding("DLHEKIM"));
        int added = 0;
        foreach (var r in dls.Rows("DLHEKIM"))
        {
            int no = r.Int("DOCTOR_NO")!.Value;
            if (existing.TryGetValue(PatientMatcher.NameKey(r.Str("ADI")), out var same)) { map[no] = same; continue; }
            var stored = t.Insert(Copy(r, t));
            map[no] = (int)stored["DOCTOR_NO"]!;
            Rows(_d01Map, "DLHEKIM")[map[no]] = no;
            added++;
        }
        report.Count("Hekim", added);
        return map;
    }

    // ------------------------------------------------------------------ records

    private void CopyRecords(BdeDatabase db, Side src, Side dst, Dictionary<int, int> protMap, HashSet<int> matchedDst, SyncMap map)
    {
        // ---- treatments
        var dstSessions = dst.Rows("DLORALTD").Where(r => matchedDst.Contains(r.Int("PROTOKOL") ?? -1)).ToDictionary(r => r.Int("TEDNO")!.Value);
        var dstItems = dst.Rows("DLOR_DET").Where(r => dstSessions.ContainsKey(r.Int("TEDNO") ?? -1)).ToList();
        var itemPool = new Multiset<string>(dstItems.Select(i => ItemKey(dstSessions[i.Int("TEDNO")!.Value].Int("PROTOKOL")!.Value, dstSessions[i.Int("TEDNO")!.Value], i)));
        var withItems = dstItems.Select(i => i.Int("TEDNO")!.Value).ToHashSet();
        var emptyPool = new Multiset<string>(dstSessions.Values.Where(s => !withItems.Contains(s.Int("TEDNO")!.Value))
            .Select(s => SessionKey(s.Int("PROTOKOL")!.Value, s)));

        var itemsBySession = src.Rows("DLOR_DET").ToLookup(r => r.Int("TEDNO") ?? -1);
        var knownSessions = src.Rows("DLORALTD").Select(r => r.Int("TEDNO")!.Value).ToHashSet();
        int sessions = 0, items = 0, dupSessions = 0, dupItems = 0, orphanSessions = 0;
        double total = 0;
        using (var ts = db.Open(dst.File("DLORALTD")!, dst.Encoding("DLORALTD")))
        using (var ti = db.Open(dst.File("DLOR_DET")!, dst.Encoding("DLOR_DET")))
        {
            foreach (var s in src.Rows("DLORALTD").OrderBy(r => r.Int("TEDNO")))
            {
                if (!protMap.TryGetValue(s.Int("PROTOKOL") ?? -1, out var prot)) { orphanSessions++; continue; }
                var rows = itemsBySession[s.Int("TEDNO")!.Value].OrderBy(r => r.Int("TED_DETNO")).ToList();
                bool matched = matchedDst.Contains(prot);
                var copy = rows.Where(i => !(matched && itemPool.TryTake(ItemKey(prot, s, i)))).ToList();
                dupItems += rows.Count - copy.Count;
                if (matched && (rows.Count > 0 ? copy.Count == 0 : emptyPool.TryTake(SessionKey(prot, s)))) { dupSessions++; continue; }

                var stored = ts.Insert(Copy(s, ts, new() { ["PROTOKOL"] = prot }));
                int tedno = (int)stored["TEDNO"]!;
                Rows(map, "DLORALTD")[tedno] = s.Int("TEDNO")!.Value;
                sessions++;
                foreach (var i in copy)
                {
                    var si = ti.Insert(Copy(i, ti, new() { ["TEDNO"] = tedno }));
                    Rows(map, "DLOR_DET")[(int)si["TED_DETNO"]!] = i.Int("TED_DETNO")!.Value;
                    total += i.Num("TUTAR") ?? 0;
                    items++;
                }
            }
        }
        int orphanItems = src.Rows("DLOR_DET").Count(r => !knownSessions.Contains(r.Int("TEDNO") ?? -1));
        report.Count("Tedavi kaydı (seans)", sessions);
        report.Count("Tedavi kalemi", items);
        report.Money("Eklenen tedavi kalemleri toplamı", total);
        report.Count("Diğer programda zaten olduğu için atlanan tedavi kalemi", dupItems);
        report.Count("Diğer programda zaten olduğu için atlanan tedavi seansı", dupSessions);
        if (orphanSessions > 0) report.Warn($"{src.Name}: hasta kartı silinmiş {orphanSessions} tedavi kaydı kopyalanmadı (programda da görünmüyorlar).");
        if (orphanItems > 0) report.Warn($"{src.Name}: bağlı olduğu tedavi kaydı olmayan {orphanItems} tedavi kalemi kopyalanmadı.");

        // ---- payments
        var payPool = new Multiset<string>(dst.Rows("DLODEME").Where(r => matchedDst.Contains(r.Int("PROTOKOL") ?? -1))
            .Select(r => PaymentKey(r.Int("PROTOKOL")!.Value, r)));
        var details = src.Rows("DLODEDET").ToLookup(r => r.Int("ODEME_ID") ?? -1);
        int payments = 0, detailCount = 0, dupPayments = 0, orphanPayments = 0;
        var sums = new Dictionary<string, double>();
        using (var tp = db.Open(dst.File("DLODEME")!, dst.Encoding("DLODEME")))
        using (var td = db.Open(dst.File("DLODEDET")!, dst.Encoding("DLODEDET")))
        {
            foreach (var p in src.Rows("DLODEME").OrderBy(r => r.Int("ODEME_ID")))
            {
                if (!protMap.TryGetValue(p.Int("PROTOKOL") ?? -1, out var prot)) { orphanPayments++; continue; }
                if (matchedDst.Contains(prot) && payPool.TryTake(PaymentKey(prot, p))) { dupPayments++; continue; }
                var stored = tp.Insert(Copy(p, tp, new() { ["PROTOKOL"] = prot }));
                int id = (int)stored["ODEME_ID"]!;
                Rows(map, "DLODEME")[id] = p.Int("ODEME_ID")!.Value;
                var cur = LegacyImporterCurrency(p.Str("MON_UNIT"));
                sums[cur] = sums.GetValueOrDefault(cur) + (p.Num("TOTAL") ?? 0);
                payments++;
                foreach (var d in details[p.Int("ODEME_ID")!.Value].OrderBy(r => r.Int("DETAIL_ID")))
                {
                    var sd = td.Insert(Copy(d, td, new() { ["ODEME_ID"] = id }));
                    Rows(map, "DLODEDET")[(int)sd["DETAIL_ID"]!] = d.Int("DETAIL_ID")!.Value;
                    detailCount++;
                }
            }
        }
        report.Count("Ödeme", payments);
        report.Count("Ödeme ayrıntısı (döviz satırı)", detailCount);
        foreach (var (c, v) in sums.OrderBy(x => x.Key)) report.Money($"Eklenen ödemeler toplamı ({c})", v);
        report.Count("Diğer programda zaten olduğu için atlanan ödeme", dupPayments);
        if (orphanPayments > 0) report.Warn($"{src.Name}: hasta kartı silinmiş {orphanPayments} ödeme kopyalanmadı.");

        // ---- tooth notes (one note per patient and tooth)
        var dstNotes = dst.Rows("DLDISNOT").Where(r => r.Int("PROTOKOL") is not null)
            .ToDictionary(r => $"{r.Int("PROTOKOL")}/{r.Int("DISNO")}", r => r.Str("NOT"));
        bool imageColumns = dst.Rows("DLDISNOT").Count > 0 ? dst.Rows("DLDISNOT")[0].Has("IMAGENO") : false;
        int notes = 0, sameNotes = 0, conflicts = 0;
        using (var tn = db.Open(dst.File("DLDISNOT")!, dst.Encoding("DLDISNOT")))
        {
            imageColumns = tn.Has("IMAGENO");
            foreach (var n in src.Rows("DLDISNOT"))
            {
                if (!protMap.TryGetValue(n.Int("PROTOKOL") ?? -1, out var prot) || n.Str("NOT") is not { } text) continue;
                var key = $"{prot}/{n.Int("DISNO")}";
                if (dstNotes.TryGetValue(key, out var existing))
                {
                    if (PatientMatcher.NameKey(existing) == PatientMatcher.NameKey(text)) sameNotes++;
                    else { conflicts++; report.Warn($"Diş notu {key}: iki programda farklı not var, hedefteki korundu."); }
                    continue;
                }
                var values = new Dictionary<string, object?> { ["PROTOKOL"] = prot, ["DISNO"] = n.Int("DISNO"), ["NOT"] = text };
                // DentaLineSuit writes these for a note without pictures.
                if (imageColumns) { values["IMAGENO"] = int.MinValue; values["FORIMAGENOS"] = " "; }
                tn.Insert(values);
                dstNotes[key] = text;
                map.ToothNotes.Add(key);
                notes++;
            }
        }
        report.Count("Diş notu", notes);
        if (sameNotes > 0) report.Count("Aynısı zaten olduğu için atlanan diş notu", sameNotes);

        // ---- photos (file name = record number in hexadecimal)
        var files = Directory.Exists(src.ImageDir)
            ? Directory.EnumerateFiles(src.ImageDir, "*", SearchOption.AllDirectories)
                .Where(f => Path.GetFileNameWithoutExtension(f) is { Length: >= 4 and <= 8 } n && n.All(Uri.IsHexDigit))
                .GroupBy(f => Convert.ToInt32(Path.GetFileNameWithoutExtension(f), 16)).ToDictionary(g => g.Key, g => g.First())
            : [];
        int photos = 0, missing = 0;
        using (var tf = db.Open(dst.File("DLFOTO")!, dst.Encoding("DLFOTO")))
        {
            foreach (var f in src.Rows("DLFOTO").OrderBy(r => r.Int("RESIM_NO")))
            {
                if (!protMap.TryGetValue(f.Int("PROTOKOL") ?? -1, out var prot)) continue;
                int no = f.Int("RESIM_NO")!.Value;
                var stored = tf.Insert(Copy(f, tf, new() { ["PROTOKOL"] = prot }));
                int newNo = (int)stored["RESIM_NO"]!;
                Rows(map, "DLFOTO")[newNo] = no;
                photos++;
                if (!files.TryGetValue(no, out var file)) { missing++; continue; }
                var target = Path.Combine(dst.ImageDir, (newNo >> 16).ToString("X4", CultureInfo.InvariantCulture),
                    (newNo & 0xFFFF).ToString("X4", CultureInfo.InvariantCulture) + Path.GetExtension(file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (File.Exists(target)) File.Move(target, target + ".eski", overwrite: false);
                File.Copy(file, target);
            }
        }
        report.Count("Fotoğraf", photos);
        if (missing > 0) report.Warn($"{src.Name}: {missing} fotoğraf kaydının dosyası yok; yalnızca kayıt kopyalandı.");
    }

    private void CopyAnamnesisAndNotes(BdeDatabase db, Dictionary<int, int> toD01)
    {
        foreach (var (table, list, label) in new[] { ("DLANAMNZ", _d01Map.Anamnesis, "Anamnez"), ("DLNOTLAR", _d01Map.PatientNotes, "Hasta notu (RTF)") })
        {
            var have = d01.Rows(table).Select(r => r.Int("PROTOKOL")).ToHashSet();
            int n = 0, skipped = 0;
            using var t = db.Open(d01.File(table)!, d01.Encoding(table));
            foreach (var r in dls.Rows(table))
            {
                if (!toD01.TryGetValue(r.Int("PROTOKOL") ?? -1, out var prot)) continue;
                if (!have.Add(prot)) { skipped++; continue; }
                t.Insert(Copy(r, t, new() { ["PROTOKOL"] = prot }));
                list.Add(prot);
                n++;
            }
            report.Count(label, n);
            if (skipped > 0) report.Warn($"{label}: {skipped} hastada DL2001'de zaten kayıt vardı, korundu.");
        }
    }

    private void CopyExpenses(BdeDatabase db, Dictionary<int, int> toD01)
    {
        var accounts = new Dictionary<int, int>();
        var existing = d01.Rows("DLLABOR").Where(r => r.Str("Firma") is not null)
            .GroupBy(r => PatientMatcher.NameKey(r.Str("Firma"))).ToDictionary(g => g.Key, g => g.First().Int("No")!.Value);
        int added = 0;
        using (var t = db.Open(d01.File("DLLABOR")!, d01.Encoding("DLLABOR")))
            foreach (var r in dls.Rows("DLLABOR"))
            {
                int no = r.Int("No")!.Value;
                if (r.Str("Firma") is { } f && existing.TryGetValue(PatientMatcher.NameKey(f), out var same)) { accounts[no] = same; continue; }
                var stored = t.Insert(Copy(r, t));
                accounts[no] = (int)stored["No"]!;
                Rows(_d01Map, "DLLABOR")[accounts[no]] = no;
                added++;
            }
        report.Count("Gider hesabı", added);

        int n = 0;
        double total = 0;
        using (var t = db.Open(d01.File("DLGIDER")!, d01.Encoding("DLGIDER")))
            foreach (var r in dls.Rows("DLGIDER").OrderBy(r => r.Int("Sira")))
            {
                int? lab = r.Int("LabNo") is { } l ? accounts.TryGetValue(l, out var nl) ? nl : l : null;
                int? prot = r.Int("Protokol") is { } p and > 0 ? toD01.TryGetValue(p, out var np) ? np : null : r.Int("Protokol");
                var stored = t.Insert(Copy(r, t, new() { ["LabNo"] = lab, ["Protokol"] = prot }));
                Rows(_d01Map, "DLGIDER")[(int)stored["Sira"]!] = r.Int("Sira")!.Value;
                total += r.Num("Tutar") ?? 0;
                n++;
            }
        report.Count("Gider", n);
        report.Money("Eklenen giderler toplamı", total);
    }

    /// <summary>Adds the rows of a small list (medicines, classifications) that the target lacks.</summary>
    private void Union(BdeDatabase db, Side src, Side dst, string table, string keyField)
    {
        // Codes are compared as written (NameKey drops digits); descriptions folded like names.
        string Key(ParadoxRecord r) => keyField == "KOD" ? (r.Str(keyField) ?? "").Trim().ToUpperInvariant() : PatientMatcher.NameKey(r.Str(keyField));
        var have = dst.Rows(table).Select(Key).ToHashSet();
        int n = 0;
        using var t = db.Open(dst.File(table)!, dst.Encoding(table));
        foreach (var r in src.Rows(table))
        {
            if (r.Str(keyField) is null || !have.Add(Key(r))) continue;
            t.Insert(Copy(r, t));
            n++;
        }
        report.Count($"{table} listesine eklenen", n);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>The fields of a source row that the target table also has (AutoInc keys are left to BDE).</summary>
    private static Dictionary<string, object?> Copy(ParadoxRecord r, BdeTable target, Dictionary<string, object?>? overrides = null)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in r.All())
            if (value is not null && target.Has(name) && target.Field(name).SubType != Bde.fldstAUTOINC && !name.Equals("FOTO", StringComparison.OrdinalIgnoreCase))
                values[name] = value;
        if (overrides is not null)
            foreach (var (k, v) in overrides) values[k] = v;
        return values;
    }

    private static Dictionary<int, int> Rows(SyncMap map, string table)
    {
        if (!map.Rows.TryGetValue(table, out var d)) map.Rows[table] = d = [];
        return d;
    }

    private static string Money(double? v) => Math.Round(v ?? 0, 2).ToString("0.00", CultureInfo.InvariantCulture);
    private static string Day(DateOnly? d) => d?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "-";

    // Same keys as the Disci DL2001 import uses for "already there".
    private static string ItemKey(int prot, ParadoxRecord session, ParadoxRecord item) =>
        $"{prot}|{Day(session.Date("TEDTARIH"))}|{session.Int("TEDTYPE")}|{item.Str("KOD")?.Trim() ?? "~" + PatientMatcher.NameKey(item.Str("ADI"))}|{(item.Int("DISNO") is > 0 and var t ? t : 0)}|{Money(item.Num("TUTAR"))}";

    private static string SessionKey(int prot, ParadoxRecord s) =>
        $"{prot}|{Day(s.Date("TEDTARIH"))}|{s.Int("TEDTYPE")}|{PatientMatcher.NameKey(s.Str("DETAIL"))}";

    private static string PaymentKey(int prot, ParadoxRecord p) =>
        $"{prot}|{Day(p.Date("PAYDATE"))}|{Money(p.Num("TOTAL"))}|{LegacyImporterCurrency(p.Str("MON_UNIT"))}";

    private static string LegacyImporterCurrency(string? unit) => unit?.Trim().ToUpperInvariant() switch
    {
        "USD" => "USD",
        "EURO" or "EUR" => "EUR",
        _ => "TRY",
    };

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

/// <summary>Counts only: no names, so the report could be shown to anyone.</summary>
internal sealed class Report
{
    private readonly StringBuilder _md = new();

    public void Section(string title) { _md.AppendLine().AppendLine($"## {title}").AppendLine(); Console.WriteLine($"\n== {title}"); }
    public void Line(string text) { _md.AppendLine($"- {text}"); Console.WriteLine($"  {text}"); }
    public void Count(string label, int n) => Line($"{label}: {n.ToString("N0", new CultureInfo("tr-TR"))}");
    public void Money(string label, double v) => Line($"{label}: {v.ToString("N2", new CultureInfo("tr-TR"))}");
    public void Warn(string text) { _md.AppendLine($"- UYARI: {text}"); Console.WriteLine($"  UYARI: {text}"); }
    public override string ToString() => _md.ToString();
}
