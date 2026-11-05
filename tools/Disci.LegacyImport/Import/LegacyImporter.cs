using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Disci.Domain;
using Disci.Infrastructure;
using Disci.LegacyImport.Paradox;
using Microsoft.EntityFrameworkCore;

namespace Disci.LegacyImport.Import;

public sealed record ImportOptions(string SourceDir, string FilesDir);

/// <summary>Copies a DentaLineSuit (Paradox) database into the new schema. Reads the source; never writes to it.</summary>
public sealed class LegacyImporter(DisciDbContext db, ImportOptions options, ImportReport report)
{
    private static readonly CultureInfo Tr = new("tr-TR");
    private static readonly JsonSerializerOptions JsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    internal static readonly HashSet<string> FindingPrefixes = ["DCE", "DEN", "DPR"];
    internal static readonly Dictionary<string, string> CategoryByPrefix = new()
    {
        ["CER"] = "Cerrahi",
        ["END"] = "Endodonti ve restoratif",
        ["PRO"] = "Protez",
        ["PER"] = "Periodontoloji",
        ["ORT"] = "Ortodonti",
        ["PED"] = "Pedodonti",
        ["DCE"] = "Bulgu",
        ["DEN"] = "Bulgu",
        ["DPR"] = "Bulgu",
    };

    private readonly Dictionary<string, ParadoxTable> _tables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, Dentist> _dentists = [];
    private readonly Dictionary<string, PriceList> _priceLists = [];
    private readonly Dictionary<string, Procedure> _procedures = [];
    private readonly Dictionary<int, Patient> _patients = [];
    private readonly Dictionary<int, TreatmentSession> _sessions = [];
    private readonly Dictionary<int, ExpenseAccount> _expenseAccounts = [];

    public async Task RunAsync(CancellationToken ct)
    {
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await Step("Hekimler", ImportDentists, ct);
        await Step("Tedavi kataloğu ve fiyat listeleri", ImportCatalog, ct);
        await Step("İlaçlar ve randevu türleri", ImportLookups, ct);
        await Step("Hastalar", ImportPatients, ct);
        await Step("Anamnez", ImportAnamnesis, ct);
        await Step("Diş notları ve hasta notları", ImportNotes, ct);
        await Step("Fotoğraflar", ImportPhotos, ct);
        await Step("Tedaviler", ImportTreatments, ct);
        await Step("Ödemeler", ImportPayments, ct);
        await Step("Giderler", ImportExpenses, ct);
        await Step("Randevular ve reçeteler", ImportAppointmentsAndPrescriptions, ct);

        await tx.CommitAsync(ct);
        report.Warn("Kullanıcılar (DLSIFRE) aktarılmadı: eski şifreler düz metin; yeni sistemde kullanıcılar yeniden oluşturulmalı.");
    }

    private async Task Step(string title, Action action, CancellationToken ct)
    {
        Console.WriteLine($"  {title}...");
        action();
        db.ChangeTracker.DetectChanges();
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        // Entities kept in the lookup dictionaries are detached now; re-attach them as unchanged so they can be referenced.
        foreach (var e in _dentists.Values) db.Attach(e);
        foreach (var e in _priceLists.Values) db.Attach(e);
        foreach (var e in _procedures.Values) db.Attach(e);
        foreach (var e in _patients.Values) db.Attach(e);
        foreach (var e in _sessions.Values) db.Attach(e);
        foreach (var e in _expenseAccounts.Values) db.Attach(e);
    }

    private ParadoxTable Table(string name)
    {
        if (!_tables.TryGetValue(name, out var table))
        {
            var path = Directory.EnumerateFiles(options.SourceDir, name + ".db", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })
                .FirstOrDefault() ?? throw new FileNotFoundException($"{name}.DB bulunamadı: {options.SourceDir}");
            _tables[name] = table = new ParadoxTable(path);
        }
        return table;
    }

    /// <summary>
    /// Set when this folder was filled with DentaLine 2001 records (Disci.LegacySync). Those copies are skipped here:
    /// the DL2001 import brings the originals, and copied/matched patient cards get their DL2001 number now.
    /// </summary>
    private readonly SyncMap? _sync = SyncMap.Load(options.SourceDir) is { Source: SyncMap.FromDl2001 } m ? m : null;
    private readonly HashSet<string> _syncReported = new(StringComparer.OrdinalIgnoreCase);

    private List<ParadoxRecord> Rows(string name) => Rows(name, count: true);

    private List<ParadoxRecord> Rows(string name, bool count)
    {
        var table = Table(name);
        var all = table.ReadRecords().ToList();
        var rows = _sync is null ? all : all.Where(r => !_sync.IsCopy(name, r)).ToList();
        int copies = all.Count - rows.Count;
        if (count)
        {
            report.Source(name, rows.Count, table.HeaderRecordCount - copies);
            if (copies > 0 && _syncReported.Add(name))
                report.Warn($"{name}: DentaLine 2001'den eşitlemeyle kopyalanmış {copies} kayıt atlandı (asılları DL2001 aktarımıyla gelir).");
        }
        return rows;
    }

    private void ImportDentists()
    {
        foreach (var r in Rows("DLHEKIM"))
        {
            var d = new Dentist
            {
                LegacyId = r.Int("DOCTOR_NO"),
                FullName = Clean(r.Str("ADI")) ?? $"Hekim {r.Int("DOCTOR_NO")}",
                Specialty = Clean(r.Str("UZMANLIK")),
                DiplomaNo = Clean(r.Str("DIPLOMA")),
                Phone = Clean(r.Str("CEP_TEL")) ?? Clean(r.Str("IS_TEL")) ?? Clean(r.Str("EV_TEL")),
                WorkStart = r.Time("CAL_SA_BAS"),
                WorkEnd = r.Time("CAL_SA_BIT"),
            };
            db.Dentists.Add(d);
            _dentists[d.LegacyId!.Value] = d;
        }
        report.Target("Hekim", _dentists.Count);
    }

    private void ImportCatalog()
    {
        var vatByGroup = Rows("DLTRGRUP").ToDictionary(r => r.Str("GRUP")!, r => r.Dec("KDVYZD"));
        var tariff = Rows("DLTRIFES");
        var findings = Rows("DLDIAGNZ");
        var usedCodes = Rows("DLOR_DET").GroupBy(r => r.Str("KOD")!).ToDictionary(g => g.Key, g => g.First().Str("ADI"));

        Procedure Ensure(string code, string? name, bool? showOnChart, bool finding)
        {
            if (!_procedures.TryGetValue(code, out var p))
            {
                var prefix = code.Length >= 3 ? code[..3] : code;
                p = new Procedure
                {
                    Code = code,
                    Name = Clean(name) ?? code,
                    Category = CategoryByPrefix.GetValueOrDefault(prefix, "Diğer"),
                    Kind = finding || FindingPrefixes.Contains(prefix) ? ProcedureKind.Finding : ProcedureKind.Treatment,
                };
                db.Procedures.Add(p);
                _procedures[code] = p;
            }
            // Different price lists cut names at different lengths; keep the most complete one.
            if (Clean(name) is { } n && n.Length > p.Name.Length) p.Name = n;
            if (showOnChart == true) p.ShowOnToothChart = true;
            return p;
        }

        foreach (var r in findings)
            Ensure(r.Str("KOD")!, r.Str("ADI"), r.Str("GRAFIK") == "E", finding: true);

        foreach (var group in tariff.GroupBy(r => r.Str("GRUP")!))
        {
            // The list with VAT-inclusive totals is the one in current use.
            bool current = group.Any(r => r.Num("TOPLAM") is > 0);
            var list = new PriceList
            {
                Name = current ? $"Güncel tarife ({group.Key})" : $"Eski tarife ({group.Key})",
                LegacyCode = group.Key,
                VatRate = vatByGroup.GetValueOrDefault(group.Key),
                IsDefault = current,
            };
            db.PriceLists.Add(list);
            _priceLists[group.Key] = list;
            foreach (var r in group)
            {
                var basePrice = r.Dec("BIRIM") ?? 0;
                list.Items.Add(new PriceListItem
                {
                    Procedure = Ensure(r.Str("KOD")!, r.Str("ADI"), r.Str("GRAFIK") == "E", finding: false),
                    BasePrice = basePrice,
                    VatAmount = r.Dec("KDV"),
                    Price = r.Dec("TOPLAM") ?? basePrice,
                });
            }
        }

        foreach (var (code, name) in usedCodes)
            if (!_procedures.ContainsKey(code))
            {
                Ensure(code, name, null, finding: false);
                report.Warn($"'{code}' kodu yalnızca tedavi kayıtlarında var, katalogda yoktu; '{name}' adıyla eklendi.");
            }

        report.Target("Tedavi/bulgu tanımı", _procedures.Count);
        report.Target("Fiyat listesi", _priceLists.Count);
        report.Target("Fiyat listesi kalemi", _priceLists.Values.Sum(l => l.Items.Count));
    }

    private void ImportLookups()
    {
        int meds = 0;
        foreach (var r in Rows("DLILAC"))
        {
            db.Medications.Add(new Medication
            {
                Code = Clean(r.Str("KOD")),
                Name = Clean(r.Str("ADI")) ?? r.Str("KOD")!,
                Dose = Clean(r.Str("DOZ")),
                Usage = Clean(r.Str("KULLANIM")),
            });
            meds++;
        }
        report.Target("İlaç", meds);

        int slotMinutes = Rows("DLMESAI").FirstOrDefault()?.Int("ARALIK") ?? 30;
        int types = 0;
        foreach (var r in Rows("DLRNCINS"))
        {
            db.AppointmentTypes.Add(new AppointmentType
            {
                LegacyId = r.Int("NU"),
                Name = Clean(r.Str("CINS")) ?? $"Tür {r.Int("NU")}",
                DurationMinutes = Math.Max(1, r.Int("SURE") ?? 1) * slotMinutes,
                Color = DelphiColor(r.Int("RENK")),
            });
            types++;
        }
        report.Target("Randevu türü", types);
    }

    internal static readonly HashSet<string> MappedPatientFields =
    [
        "PROTOKOL", "ADI", "EV_TEL", "CEP_TEL", "IS_TEL", "EV_ADR_1", "EV_ADR_2", "EV_P_KOD", "EV_SEMT", "EV_SEHIR",
        "IS_ADR_1", "IS_ADR_2", "IS_P_KOD", "IS_SEMT", "IS_SEHIR", "DOGUM_TARIHI", "KAYIT_TARIHI", "CINSIYET",
        "MESLEGI", "HASTA_GRUBU", "YAKIN_ADI", "YAKIN_TEL", "REFERANS_ADI", "REFERANS_TEL", "NOTLAR",
        "BABA_ADI", "BABA_TEL", "BABA_CEP", "BABA_MESLEK", "ANNE_ADI", "ANNE_TEL", "ANNE_CEP", "ANNE_MESLEK",
        "VERGI_DAIRESI", "VERGI_NO", "ISKONTO_ORANI", "DIS_HEKIMI_NO", "AILE_DR_ADI", "AILE_DR_TEL",
        "SOS_GUV_SISTEMI", "SOS_GUV_NO", "KAN_GRUBU", "UYRUGU", "DOGUM_YERI", "E_MAIL_ADRES1", "E_MAIL_ADRES2",
        "KIMLIK_NO",
        // Derived in the old system; the new one calculates them from treatments and payments.
        "SON_TEDAVI_TARIHI", "SON_ODEME_TARIHI", "TOP_BORC_DAIMI", "TOP_ODEME",
    ];

    private void ImportPatients()
    {
        var groupAliases = new Dictionary<string, string>();
        foreach (var r in Rows("DLHASTA"))
        {
            int protocol = r.Int("PROTOKOL")!.Value;
            var p = NewPatient(r);
            p.LegacyId = protocol;
            p.PriceList = PriceListFor(r.Str("HASTA_GRUBU"), groupAliases);
            if (r.Int("DIS_HEKIMI_NO") is { } dr)
            {
                if (_dentists.TryGetValue(dr, out var dentist)) p.PrimaryDentist = dentist;
                else report.Warn($"Hasta {protocol}: hekim no {dr} bulunamadı.");
            }
            p.LegacyExtra = PatientExtras(r);
            // A card copied from (or matched with) DentaLine 2001: the DL2001 import then adds to this patient.
            if (_sync?.OtherPatient(protocol) is { Count: > 0 } d01)
            {
                p.Dl2001Protocol = d01[0];
                foreach (var extra in d01.Skip(1))
                    p.LegacyNumbers.Add(new PatientLegacyNumber { Source = LegacySources.Dl2001, Number = extra });
            }

            db.Patients.Add(p);
            _patients[protocol] = p;
        }
        foreach (var (alias, target) in groupAliases)
            report.Warn($"Hasta grubu '{alias}' eşleşen fiyat listesine bağlandı: {target}");

        // Records in other tables that point at a deleted patient keep a placeholder so nothing is dropped.
        var referenced = new[] { ("DLORALTD", "PROTOKOL"), ("DLODEME", "PROTOKOL"), ("DLDISNOT", "PROTOKOL"),
                ("DLFOTO", "PROTOKOL"), ("DLANAMNZ", "PROTOKOL"), ("DLNOTLAR", "PROTOKOL") }
            .SelectMany(t => Rows(t.Item1, count: false).Select(r => r.Int(t.Item2)))
            .OfType<int>().Distinct().Where(id => !_patients.ContainsKey(id)).Order();
        foreach (int protocol in referenced)
        {
            var p = PlaceholderPatient(protocol);
            p.LegacyId = protocol;
            db.Patients.Add(p);
            _patients[protocol] = p;
            report.Warn($"Hasta kartı olmayan protokol {protocol} için arşivli yer tutucu hasta oluşturuldu.");
        }
        report.Target("Hasta", _patients.Count);
        report.Target("Hasta yakını", _patients.Values.Sum(p => p.Relatives.Count));
    }

    internal static Patient PlaceholderPatient(int protocol, string system = "Eski sistemde") => new()
    {
        FirstName = $"(Eski kayıt {protocol})",
        LastName = "",
        RegisteredOn = DateOnly.FromDateTime(DateTime.Today),
        Notes = $"{system} hasta kartı silinmiş, ancak bu numaraya bağlı kayıtlar vardı.",
        IsArchived = true,
    };

    /// <summary>Old fields with no column of their own, as JSON (null when there are none).</summary>
    internal static string? PatientExtras(ParadoxRecord r, params string[] alsoKeep)
    {
        var extra = r.All()
            .Where(x => x.Value is not null && (!MappedPatientFields.Contains(x.Field) || alsoKeep.Contains(x.Field)))
            .ToDictionary(x => x.Field, x => x.Value);
        return extra.Count > 0 ? JsonSerializer.Serialize(extra, JsonOptions) : null;
    }

    /// <summary>Maps a DLHASTA row (same layout in DentaLineSuit and DentaLine 2001); keys, price list and dentist are set by the caller.</summary>
    internal static Patient NewPatient(ParadoxRecord r)
    {
        var (first, last) = SplitName(Clean(r.Str("ADI")));
        first = TurkishNames.FixFirstName(first);
        last = TurkishNames.FixLastName(last);
        var p = new Patient
        {
            FirstName = first,
            LastName = last,
            NationalId = r.Has("KIMLIK_NO") ? Clean(r.Str("KIMLIK_NO")) : null,
            BirthDate = r.Date("DOGUM_TARIHI"),
            BirthPlace = Clean(r.Str("DOGUM_YERI")),
            Gender = r.Str("CINSIYET") switch { "K" => Gender.Female, "E" => Gender.Male, _ => Gender.Unknown },
            BloodGroup = Clean(r.Str("KAN_GRUBU")),
            Nationality = Clean(r.Str("UYRUGU")),
            Occupation = Clean(r.Str("MESLEGI")),
            MobilePhone = Clean(r.Str("CEP_TEL")),
            HomePhone = Clean(r.Str("EV_TEL")),
            WorkPhone = Clean(r.Str("IS_TEL")),
            Email = Clean(r.Str("E_MAIL_ADRES1")),
            SecondaryEmail = Clean(r.Str("E_MAIL_ADRES2")),
            HomeAddress = Address(r, "EV"),
            WorkAddress = Address(r, "IS"),
            RegisteredOn = r.Date("KAYIT_TARIHI") ?? DateOnly.FromDateTime(DateTime.Today),
            ReferredBy = Clean(r.Str("REFERANS_ADI")),
            ReferrerPhone = Clean(r.Str("REFERANS_TEL")),
            FamilyDoctor = Join(" ", Clean(r.Str("AILE_DR_ADI")), Clean(r.Str("AILE_DR_TEL"))),
            InsuranceProvider = Clean(r.Str("SOS_GUV_SISTEMI")),
            InsuranceNo = Clean(r.Str("SOS_GUV_NO")),
            TaxOffice = Clean(r.Str("VERGI_DAIRESI")),
            TaxNo = Clean(r.Str("VERGI_NO")),
            DiscountRate = r.Dec("ISKONTO_ORANI") is > 0 and var rate ? rate : null,
            Notes = Clean(r.Str("NOTLAR")),
        };
        AddRelative(p, RelativeType.EmergencyContact, r.Str("YAKIN_ADI"), r.Str("YAKIN_TEL"), null, null);
        AddRelative(p, RelativeType.Father, r.Str("BABA_ADI"), r.Str("BABA_TEL"), r.Str("BABA_CEP"), r.Str("BABA_MESLEK"));
        AddRelative(p, RelativeType.Mother, r.Str("ANNE_ADI"), r.Str("ANNE_TEL"), r.Str("ANNE_CEP"), r.Str("ANNE_MESLEK"));
        return p;
    }

    private PriceList? PriceListFor(string? group, Dictionary<string, string> aliases)
    {
        if (Clean(group) is not { } g) return null;
        if (_priceLists.TryGetValue(g, out var list)) return list;
        var match = _priceLists.Values.FirstOrDefault(l => string.Equals(l.LegacyCode?.TrimEnd('.'), g.TrimEnd('.'), StringComparison.OrdinalIgnoreCase));
        if (match is not null) aliases[g] = match.Name;
        return match;
    }

    private void ImportAnamnesis()
    {
        var table = Table("DLANAMNZ");
        var conditions = new Dictionary<string, MedicalCondition>();
        int order = 0;
        foreach (var field in table.Fields.Where(f => !AnamnesisCatalog.TextColumns.Contains(f.Name)))
        {
            var (name, category) = AnamnesisCatalog.Describe(field.Name);
            var c = new MedicalCondition { Code = field.Name, Name = name, Category = category, SortOrder = ++order };
            db.MedicalConditions.Add(c);
            conditions[field.Name] = c;
        }

        int anamneses = 0, findings = 0;
        foreach (var r in Rows("DLANAMNZ"))
        {
            var patient = _patients[r.Int("PROTOKOL")!.Value];
            db.Anamneses.Add(new Anamnesis
            {
                Patient = patient,
                Notes = Clean(r.Str("NOTE")),
                DrugAllergies = Clean(r.Str("M_ALJ_ILAC")),
                FoodAllergies = Clean(r.Str("M_ALJ_GIDA")),
                PregnancyDate = r.Date("HAMILE_TAR"),
                ExtraOralOther = Clean(r.Str("AGIZDISIDIGER")),
                IntraOralOther = Clean(r.Str("AGIZICIDIGER")),
            });
            anamneses++;
            foreach (var (code, condition) in conditions)
                if (r.Str(code) == "1")
                {
                    db.PatientMedicalConditions.Add(new PatientMedicalCondition { Patient = patient, MedicalCondition = condition });
                    findings++;
                }
        }
        report.Target("Anamnez sorusu", conditions.Count);
        report.Target("Anamnez", anamneses);
        report.Target("Anamnez bulgusu (evet)", findings);
    }

    private void ImportNotes()
    {
        int toothNotes = 0, skipped = 0;
        foreach (var r in Rows("DLDISNOT"))
        {
            var text = Clean(r.Str("NOT"));
            if (text is null || text.Trim('?', ' ', '\n', '\r').Length == 0) { skipped++; continue; }
            db.ToothNotes.Add(new ToothNote
            {
                Patient = _patients[r.Int("PROTOKOL")!.Value],
                ToothNumber = r.Int("DISNO") ?? 0,
                Note = text,
            });
            toothNotes++;
        }
        report.Target("Diş notu", toothNotes);
        if (skipped > 0) report.Warn($"{skipped} boş diş notu atlandı.");

        var notes = Table("DLNOTLAR");
        int patientNotes = 0;
        foreach (var r in Rows("DLNOTLAR"))
        {
            if (r.Bytes("NOTLAR") is not { } bytes) continue;
            var raw = notes.Encoding.GetString(bytes).TrimEnd('\0');
            var text = RtfText.ToPlainText(raw, notes.Encoding);
            if (text.Length == 0) continue;
            db.PatientNotes.Add(new PatientNote
            {
                Patient = _patients[r.Int("PROTOKOL")!.Value],
                Text = text,
                LegacyRtf = raw.StartsWith(@"{\rtf", StringComparison.Ordinal) ? raw : null,
            });
            patientNotes++;
        }
        report.Target("Hasta notu", patientNotes);
    }

    private void ImportPhotos()
    {
        var imageDir = Path.Combine(options.SourceDir, "IMAGE");
        // Files are named after RESIM_NO in hex (000A.jpg = 10) and may sit in bucket subfolders such as 0000\.
        var files = Directory.Exists(imageDir)
            ? Directory.EnumerateFiles(imageDir, "*", SearchOption.AllDirectories)
                .Select(f => (Path: f, Name: Path.GetFileNameWithoutExtension(f)))
                .Where(f => f.Name.Length is >= 4 and <= 8 && f.Name.All(Uri.IsHexDigit))
                .GroupBy(f => Convert.ToInt32(f.Name, 16))
                .ToDictionary(g => g.Key, g => g.First().Path)
            : [];

        int imported = 0;
        var used = new HashSet<int>();
        foreach (var r in Rows("DLFOTO"))
        {
            int no = r.Int("RESIM_NO")!.Value;
            var patient = _patients[r.Int("PROTOKOL")!.Value];
            if (!files.TryGetValue(no, out var source))
            {
                report.Warn($"Fotoğraf {no} (hasta {patient.LegacyId}): dosya bulunamadı.");
                continue;
            }
            used.Add(no);
            var date = r.Date("TARIH") ?? patient.RegisteredOn;
            var relative = Path.Combine("patients", patient.Id.ToString(CultureInfo.InvariantCulture), "photos",
                $"{date:yyyy-MM-dd}_{no}{Path.GetExtension(source).ToLowerInvariant()}");
            var target = Path.Combine(options.FilesDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, overwrite: true);

            db.PatientPhotos.Add(new PatientPhoto
            {
                LegacyId = no,
                Patient = patient,
                TakenOn = date,
                Kind = r.Str("RESIM_CINSI") switch { "C" => PhotoKind.IntraOral, "F" => PhotoKind.Face, _ => PhotoKind.Other },
                Jaw = r.Int("CENE_NO"),
                ToothNumber = r.Int("DIS_NO") is > 0 and var tooth ? tooth : null,
                Description = Join(" - ", Clean(r.Str("IS_CINSI")), Clean(r.Str("IS_ACIKLAMA")),
                    Clean(r.Str("IS_KALITE_SINIFI")) is { } q ? $"Kalite {q}" : null),
                FilePath = relative.Replace('\\', '/'),
            });
            imported++;
        }
        report.Target("Fotoğraf", imported);
        foreach (var orphan in files.Keys.Except(used).Order())
            report.Warn($"{Path.GetFileName(files[orphan])}: veritabanında kaydı olmayan resim, aktarılmadı.");
    }

    private void ImportTreatments()
    {
        foreach (var r in Rows("DLORALTD"))
        {
            var s = NewSession(r);
            int legacy = s.LegacyId!.Value;
            s.Patient = _patients[r.Int("PROTOKOL")!.Value];
            db.TreatmentSessions.Add(s);
            _sessions[legacy] = s;
        }
        report.Target("Tedavi kaydı (seans)", _sessions.Count);

        int items = 0;
        decimal total = 0;
        var badVat = new List<int>();
        foreach (var r in Rows("DLOR_DET"))
        {
            if (!_sessions.TryGetValue(r.Int("TEDNO") ?? -1, out var session))
            {
                report.Warn($"Tedavi kalemi {r.Int("TED_DETNO")}: bağlı olduğu tedavi kaydı {r.Int("TEDNO")} yok, atlandı.");
                continue;
            }
            var code = r.Str("KOD");
            var item = NewItem(r, code is not null ? _procedures.GetValueOrDefault(code) : null, out bool vatWasBad);
            if (vatWasBad) badVat.Add(r.Int("TED_DETNO") ?? 0);
            item.TreatmentSession = session;
            db.TreatmentItems.Add(item);
            items++;
            total += item.Price;
        }
        report.Target("Tedavi kalemi", items);
        report.Total("Tedavi kalemleri toplam tutar", total);
        if (badVat.Count > 0)
            report.Warn($"{badVat.Count} tedavi kaleminde eski KDV değeri tutardan büyüktü (bozuk veri); bu kalemlerin KDV'si 0 yazıldı, tutarları değişmedi. " +
                        $"Örnek eski kayıt no: {string.Join(", ", badVat.Take(10))}");
    }

    private void ImportPayments()
    {
        var rates = ExchangeRates(Rows("DLODEDET"));

        int count = 0;
        var totals = new Dictionary<string, decimal>();
        foreach (var r in Rows("DLODEME"))
        {
            var payment = NewPayment(r, rates);
            payment.Patient = _patients[r.Int("PROTOKOL")!.Value];
            db.Payments.Add(payment);
            totals[payment.Currency] = totals.GetValueOrDefault(payment.Currency) + payment.Amount;
            count++;
        }
        report.Target("Ödeme", count);
        foreach (var (currency, sum) in totals.OrderBy(x => x.Key))
            report.Total($"Ödemeler toplamı ({currency})", sum);
        int closed = Table("DLODEME").ReadRecords().Count(r => r.Str("STATE") == "C");
        if (closed > 0)
            report.Warn($"{closed} ödemenin eski durumu 'C'. Anlamı doğrulanamadı; iptal sayılmadı, LegacyStatus alanında saklandı.");
    }

    private void ImportExpenses()
    {
        foreach (var r in Rows("DLLABOR"))
        {
            int legacy = r.Int("No")!.Value;
            var a = new ExpenseAccount
            {
                LegacyId = legacy,
                Name = Clean(r.Str("Firma")) ?? $"Hesap {legacy}",
                Kind = Clean(r.Str("HesapTuru")),
                ContactName = Clean(r.Str("Yetkili")),
                Phone = Clean(r.Str("Telefonlar")) ?? Clean(r.Str("YetkiliCep")),
                Email = Clean(r.Str("Email")),
                TaxOffice = Clean(r.Str("VergiDairesi")),
                TaxNo = Clean(r.Str("VergiNo")),
                Notes = Join("\n", Clean(r.Str("CalismaNotu")), Clean(r.Str("OdemeNotu"))),
            };
            db.ExpenseAccounts.Add(a);
            _expenseAccounts[legacy] = a;
        }
        report.Target("Gider hesabı", _expenseAccounts.Count);

        int count = 0;
        decimal total = 0;
        foreach (var r in Rows("DLGIDER"))
        {
            var amount = r.Dec("Tutar") ?? 0;
            var account = r.Int("LabNo") is { } lab ? _expenseAccounts.GetValueOrDefault(lab) : null;
            db.Expenses.Add(new Expense
            {
                LegacyId = r.Int("Sira"),
                ExpenseAccount = account,
                Patient = r.Int("Protokol") is { } prot ? _patients.GetValueOrDefault(prot) : null,
                Date = r.Date("Tarih") ?? DateOnly.MinValue,
                DueDate = r.Date("VadeTarihi"),
                PaidOn = r.Date("TahsilTarihi"),
                Method = r.Str("OdemeTuru") == "Nakit" ? PaymentMethod.Cash : PaymentMethod.Other,
                Amount = amount,
                BankFee = r.Dec("BankaMasrafi") is > 0 and var fee ? fee : null,
                Currency = CurrencyCode(r.Str("PBr")),
                Description = Clean(r.Str("Aciklama")),
                LegacyStatus = Clean(r.Str("Durumu")),
            });
            total += amount;
            count++;
        }
        report.Target("Gider", count);
        report.Total("Giderler toplamı", total);
    }

    private void ImportAppointmentsAndPrescriptions()
    {
        int appointments = 0;
        foreach (var r in Rows("RANDEVU"))
        {
            var dentist = r.Int("DR_NO") is { } dr ? _dentists.GetValueOrDefault(dr) : _dentists.Values.FirstOrDefault();
            if (dentist is null || r.Date("TARIH") is not { } day) continue;
            var start = day.ToDateTime(r.Time("SAAT") ?? TimeOnly.MinValue);
            var patient = r.Int("PROTOKOL") is { } prot ? _patients.GetValueOrDefault(prot) : null;
            db.Appointments.Add(new Appointment
            {
                LegacyId = r.Int("CAL_INDEX"),
                Patient = patient,
                GuestName = patient is null ? Clean(r.Str("ADI")) : null,
                Dentist = dentist,
                Start = start,
                End = start.AddMinutes(Math.Max(1, r.Int("SURE") ?? 1) * 30),
                Chair = r.Int("KOLTUK"),
                Status = r.Int("GELDIMI") is 1 ? AppointmentStatus.Completed : AppointmentStatus.Scheduled,
                Notes = Clean(r.Str("NOTLAR")),
            });
            appointments++;
        }
        report.Target("Randevu", appointments);

        var doctorByPrescription = Rows("DLRECENO")
            .ToDictionary(r => (r.Int("PROTOKOL"), r.Date("TARIH"), r.Int("RNO")), r => r.Int("DRNO"));
        int prescriptions = 0;
        foreach (var g in Rows("DLRECETE").GroupBy(r => (Prot: r.Int("PROTOKOL"), Date: r.Date("TARIH"), No: r.Int("RNO"))))
        {
            var dentist = doctorByPrescription.GetValueOrDefault(g.Key) is { } dr && _dentists.TryGetValue(dr, out var d)
                ? d : _dentists.Values.First();
            var prescription = new Prescription
            {
                Patient = _patients[g.Key.Prot!.Value],
                Dentist = dentist,
                Date = g.Key.Date ?? DateOnly.MinValue,
            };
            foreach (var r in g)
                prescription.Items.Add(new PrescriptionItem
                {
                    MedicationName = Clean(r.Str("ADI")) ?? r.Str("KOD") ?? "",
                    Dose = Clean(r.Str("DOZ")),
                    Usage = Clean(r.Str("KULLANIM")),
                });
            db.Prescriptions.Add(prescription);
            prescriptions++;
        }
        report.Target("Reçete", prescriptions);
    }

    internal static TreatmentSession NewSession(ParadoxRecord r) => new()
    {
        LegacyId = r.Int("TEDNO")!.Value,
        Date = r.Date("TEDTARIH") ?? DateOnly.MinValue,
        Kind = r.Int("TEDTYPE") switch { 1 => TreatmentKind.Finding, 2 => TreatmentKind.Planned, _ => TreatmentKind.Completed },
        Notes = Clean(r.Str("DETAIL")),
        LegacyStatus = Clean(r.Str("STATE")),
    };

    /// <summary>A DLOR_DET row. A VAT larger than the amount is corrupt old data: it becomes 0 and <paramref name="badVat"/> is set.</summary>
    internal static TreatmentItem NewItem(ParadoxRecord r, Procedure? procedure, out bool badVat)
    {
        var code = r.Str("KOD");
        var price = r.Dec("TUTAR") ?? 0;
        var vat = r.Dec("KDV") ?? 0;
        badVat = vat > price && price >= 0 && vat != 0;
        return new TreatmentItem
        {
            LegacyId = r.Int("TED_DETNO"),
            Procedure = procedure,
            Description = Clean(r.Str("ADI")) ?? code ?? "",
            ToothNumber = r.Int("DISNO") is > 0 and var tooth ? tooth : null,
            SurfaceMask = r.Int("BIT") ?? 0,
            Price = price,
            VatAmount = badVat ? 0 : vat,
            Currency = CurrencyCode(r.Str("PRBR")),
            LegacyTbdr = r.Int("TBDR"),
        };
    }

    /// <summary>Exchange rate per payment from DLODEDET (DentaLine 2001 has no rate column: 1).</summary>
    internal static Dictionary<int, decimal> ExchangeRates(IEnumerable<ParadoxRecord> details) => details
        .Where(r => r.Int("ODEME_ID") is not null)
        .GroupBy(r => r.Int("ODEME_ID")!.Value)
        .ToDictionary(g => g.Key, g => g.First().Has("MU_KUR") && g.First().Dec("MU_KUR", 6) is > 0 and var k ? k : 1m);

    internal static Payment NewPayment(ParadoxRecord r, Dictionary<int, decimal> rates)
    {
        int legacy = r.Int("ODEME_ID")!.Value;
        return new Payment
        {
            LegacyId = legacy,
            Date = r.Date("PAYDATE") ?? DateOnly.MinValue,
            DueDate = r.Date("DUEDATE"),
            Method = r.Str("TYPE") switch { "N" => PaymentMethod.Cash, "K" => PaymentMethod.CreditCard, _ => PaymentMethod.Other },
            Amount = r.Dec("TOTAL") ?? 0,
            Currency = CurrencyCode(r.Str("MON_UNIT")),
            ExchangeRate = rates.GetValueOrDefault(legacy, 1m),
            BankName = Clean(r.Str("BANKNAME")),
            ReceiptNo = Clean(r.Str("SLIP_CHEQUE")),
            Description = Clean(r.Str("DETAIL")),
            LegacyStatus = Clean(r.Str("STATE")),
        };
    }

    private static void AddRelative(Patient p, RelativeType type, string? name, string? phone, string? mobile, string? occupation)
    {
        if (Clean(name) is not { } n) return;
        p.Relatives.Add(new PatientRelative
        {
            Type = type,
            Name = TurkishNames.FixFullName(n),
            Phone = Clean(phone),
            MobilePhone = Clean(mobile),
            Occupation = Clean(occupation),
        });
    }

    internal static Address Address(ParadoxRecord r, string prefix) => new()
    {
        Line1 = Clean(r.Str($"{prefix}_ADR_1")),
        Line2 = Clean(r.Str($"{prefix}_ADR_2")),
        PostalCode = Clean(r.Str($"{prefix}_P_KOD")),
        District = Clean(r.Str($"{prefix}_SEMT")),
        City = Clean(r.Str($"{prefix}_SEHIR")),
    };

    /// <summary>The old system stores "Ad(lar) SOYAD": the last word is the surname.</summary>
    internal static (string First, string Last) SplitName(string? fullName)
    {
        if (fullName is null) return ("(İsimsiz)", "");
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? (parts[0], "") : (string.Join(' ', parts[..^1]), parts[^1]);
    }

    internal static string CurrencyCode(string? legacy) => Clean(legacy)?.ToUpper(Tr) switch
    {
        "USD" => Currencies.Usd,
        "EURO" or "EUR" => Currencies.Eur,
        _ => Currencies.Try,
    };

    /// <summary>Delphi TColor is 0x00BBGGRR.</summary>
    private static string? DelphiColor(int? color) =>
        color is { } c and >= 0 ? $"#{c & 0xFF:X2}{(c >> 8) & 0xFF:X2}{(c >> 16) & 0xFF:X2}" : null;

    internal static string? Clean(string? s)
    {
        var t = s?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    internal static string? Join(string separator, params string?[] parts)
    {
        var present = parts.Where(p => !string.IsNullOrEmpty(p)).ToArray();
        return present.Length == 0 ? null : string.Join(separator, present);
    }
}
