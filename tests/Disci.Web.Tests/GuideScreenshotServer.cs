using Disci.Domain;
using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Disci.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disci.Web.Tests;

/// <summary>
/// Serves an invented demo clinic on http://localhost:5223 for the screenshots in the user guide
/// (docs/kilavuz-resimler, taken by docs/ekran-goruntuleri.mjs). Does nothing in normal test runs: start it with
/// DISCI_DEMO=1 (DISCI_DEMO=kurulum for the empty first-run state). Every name, phone and amount is made up.
/// </summary>
public sealed class GuideScreenshotServer
{
    [Fact]
    public async Task Serve()
    {
        var mode = Environment.GetEnvironmentVariable("DISCI_DEMO");
        if (string.IsNullOrEmpty(mode)) return;

        await using var factory = new DisciWebFactory { EnvironmentName = "Development", FirstRunSetupEnabled = mode == "kurulum" };
        factory.UseKestrel(5223);
        factory.StartServer();
        await factory.InitializeAsync();

        if (mode != "kurulum")
        {
            using var scope = factory.Services.CreateScope();
            var ids = await DemoClinic.SeedAsync(scope.ServiceProvider);
            await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), "disci-demo.json"),
                $$"""{"patient": {{ids.PatientId}}}""");
        }

        // Stop by creating the flag file (the screenshot script does it), or after 20 minutes.
        var stop = Path.Combine(Path.GetTempPath(), "disci-demo.stop");
        File.Delete(stop);
        var until = DateTime.Now.AddMinutes(20);
        while (!File.Exists(stop) && DateTime.Now < until) await Task.Delay(500);
        File.Delete(stop);
        await ((IAsyncLifetime)factory).DisposeAsync();
    }
}

/// <summary>A small invented clinic: two dentists, a dozen patients, today's appointments, treatments and payments.</summary>
public static class DemoClinic
{
    public sealed record Ids(int PatientId);

    public static async Task<Ids> SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<DisciDbContext>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;

        await services.GetRequiredService<ClinicInfo>().SaveAsync(new ClinicProfile
        {
            Name = "Örnek Ağız ve Diş Sağlığı Kliniği", Address = "Deneme Mah. Örnek Cad. No: 1, İstanbul", Phone = "0212 000 00 00",
        });

        // The base test data has a dentist and a few patients; hide them so the pictures show only the demo clinic.
        await db.Dentists.ExecuteUpdateAsync(s => s.SetProperty(d => d.IsActive, false));
        await db.Patients.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsArchived, true));
        await db.Reminders.ExecuteDeleteAsync();
        await db.PriceLists.ExecuteUpdateAsync(s => s.SetProperty(l => l.IsDefault, false));

        var elif = new Dentist { FullName = "Dt. Elif Örnek", Specialty = "Endodonti", CalendarColor = "#0ea5e9", WorkStart = new(9, 0), WorkEnd = new(18, 0) };
        var kerem = new Dentist { FullName = "Dt. Kerem Uydurma", Specialty = "Protez", CalendarColor = "#a855f7", WorkStart = new(10, 0), WorkEnd = new(19, 0) };
        var types = new[]
        {
            new AppointmentType { Name = "Muayene", DurationMinutes = 20, Color = "#14b8a6" },
            new AppointmentType { Name = "Dolgu", DurationMinutes = 45, Color = "#3b82f6" },
            new AppointmentType { Name = "Kanal tedavisi", DurationMinutes = 60, Color = "#f59e0b" },
            new AppointmentType { Name = "Diş taşı temizliği", DurationMinutes = 30, Color = "#22c55e" },
            new AppointmentType { Name = "İmplant kontrol", DurationMinutes = 30, Color = "#a855f7" },
        };

        Procedure P(string code, string name, string category, ProcedureKind kind = ProcedureKind.Treatment, bool chart = true) =>
            new() { Code = code, Name = name, Category = category, Kind = kind, ShowOnToothChart = chart };
        var muayene = P("DMO01", "Muayene", "Teşhis", chart: false);
        var dolgu = P("DMO02", "Kompozit dolgu (iki yüzlü)", "Endodonti ve restoratif");
        var kanal = P("DMO03", "Kanal tedavisi (tek kanal)", "Endodonti ve restoratif");
        var cekim = P("DMO04", "Diş çekimi", "Cerrahi");
        var implant = P("DMO05", "İmplant (tek diş)", "Cerrahi");
        var kron = P("DMO06", "Zirkonyum kron", "Protez");
        var temizlik = P("DMO07", "Diş taşı temizliği", "Periodontoloji", chart: false);
        var beyazlatma = P("DMO08", "Ofis tipi beyazlatma", "Estetik", chart: false);
        var eksik = P("DMB01", "Eksik diş", "Bulgu", ProcedureKind.Finding);
        var curuk = P("DMB02", "Çürük", "Bulgu", ProcedureKind.Finding);
        var kopru = P("DMO09", "Zirkonyum köprü üyesi", "Protez");

        var list = new PriceList { Name = "Güncel tarife", IsDefault = true, VatRate = 10 };
        foreach (var (p, price) in new[] { (muayene, 750m), (dolgu, 2500m), (kanal, 4500m), (cekim, 1500m), (implant, 22000m), (kron, 9000m), (temizlik, 1800m), (beyazlatma, 6000m), (kopru, 7000m) })
            list.Items.Add(new PriceListItem { Procedure = p, Price = price, BasePrice = Math.Round(price / 1.1m, 2), VatAmount = price - Math.Round(price / 1.1m, 2) });

        var diabetes = new MedicalCondition { Code = "DEMO_DIYABET", Name = "Diyabet", Category = "Tıbbi geçmiş", SortOrder = 900 };
        var blood = new MedicalCondition { Code = "DEMO_TANSIYON", Name = "Hipertansiyon", Category = "Tıbbi geçmiş", SortOrder = 901 };

        string[] names =
        [
            "Zeynep|KARAKAYA|K", "Murat|ÖZDEN|E", "Selin|AKDENİZ|K", "Emre|YAZICI|E", "Hülya|ÇETİNKAYA|K", "Burak|TUNÇ|E",
            "Gizem|ERDEM|K", "Okan|SEZER|E", "Nur|BALCI|K", "Can|ÖZTEKİN|E", "Derya|UYSAL|K", "Kemal|ARSLAN|E",
        ];
        var patients = names.Select((n, i) =>
        {
            var parts = n.Split('|');
            return new Patient
            {
                FirstName = parts[0], LastName = parts[1], Gender = parts[2] == "K" ? Gender.Female : Gender.Male,
                MobilePhone = $"0500 000 00 {i + 10:00}", BirthDate = new DateOnly(1965 + i * 3, 1 + i % 12, 5 + i),
                RegisteredOn = today.AddDays(-400 + i * 25), PrimaryDentist = i % 2 == 0 ? elif : kerem, PriceList = list,
            };
        }).ToList();
        var zeynep = patients[0];
        zeynep.Anamnesis = new Anamnesis { DrugAllergies = "Penisilin", Notes = "Kan sulandırıcı kullanmıyor." };
        zeynep.MedicalConditions.Add(new PatientMedicalCondition { MedicalCondition = diabetes });
        zeynep.MedicalConditions.Add(new PatientMedicalCondition { MedicalCondition = blood });
        zeynep.ToothNotes.Add(new ToothNote { ToothNumber = 46, Note = "Soğuğa hassasiyet var." });
        zeynep.PatientNotes.Add(new PatientNote { Text = "Sabah randevularını tercih ediyor." });

        TreatmentItem I(Procedure p, int? tooth, decimal price) => new() { Procedure = p, Description = p.Name, ToothNumber = tooth, Price = price };
        zeynep.TreatmentSessions.Add(new TreatmentSession { Date = today.AddDays(-60), Kind = TreatmentKind.Finding, Dentist = elif, Items = [I(eksik, 36, 0), I(curuk, 16, 0), I(curuk, 25, 0)] });
        zeynep.TreatmentSessions.Add(new TreatmentSession { Date = today.AddDays(-45), Kind = TreatmentKind.Completed, Dentist = elif, Items = [I(muayene, null, 750), I(dolgu, 16, 2500)] });
        zeynep.TreatmentSessions.Add(new TreatmentSession { Date = today.AddDays(-20), Kind = TreatmentKind.Completed, Dentist = elif, Items = [I(kanal, 25, 4500), I(temizlik, null, 1800)] });
        zeynep.TreatmentSessions.Add(new TreatmentSession { Date = today.AddDays(-20), Kind = TreatmentKind.Planned, Dentist = kerem, Items = [I(implant, 36, 22000), I(kron, 25, 9000), I(cekim, 48, 1500)] });
        // A finished bridge 44–46 over a missing 45, a root-canal treated 46 and fillings by surface (side and occlusal views).
        TreatmentItem S(Procedure p, int tooth, decimal price, string surfaces) { var i = I(p, tooth, price); i.Surfaces = surfaces; return i; }
        zeynep.TreatmentSessions.Add(new TreatmentSession { Date = today.AddDays(-200), Kind = TreatmentKind.Finding, Dentist = elif, Items = [I(eksik, 45, 0)] });
        zeynep.TreatmentSessions.Add(new TreatmentSession { Date = today.AddDays(-190), Kind = TreatmentKind.Completed, Dentist = elif, Items = [I(kanal, 46, 4500), I(kopru, 44, 7000), I(kopru, 45, 7000), I(kopru, 46, 7000), S(dolgu, 26, 2500, "OB"), S(dolgu, 37, 2500, "MOD")] });
        zeynep.TreatmentSessions.Add(new TreatmentSession { Date = today.AddDays(-20), Kind = TreatmentKind.Planned, Dentist = kerem, Items = [S(dolgu, 14, 2500, "D"), S(dolgu, 11, 2500, "M")] });
        zeynep.Payments.Add(new Payment { Date = today.AddDays(-45), Method = PaymentMethod.Cash, Amount = 3250 });
        zeynep.Payments.Add(new Payment { Date = today.AddDays(-20), Method = PaymentMethod.CreditCard, Amount = 4000, Description = "Kanal tedavisi" });

        var rnd = new Random(7);
        foreach (var (p, i) in patients.Skip(1).Select((p, i) => (p, i)))
        {
            var day = today.AddDays(-rnd.Next(1, 28));
            var proc = new[] { dolgu, kanal, temizlik, cekim, kron }[i % 5];
            var price = list.Items.First(x => x.Procedure == proc).Price;
            p.TreatmentSessions.Add(new TreatmentSession { Date = day, Kind = TreatmentKind.Completed, Dentist = i % 2 == 0 ? kerem : elif, Items = [I(proc, 11 + i * 3 % 37, price)] });
            if (i % 3 != 2) p.Payments.Add(new Payment { Date = day, Method = i % 2 == 0 ? PaymentMethod.Cash : PaymentMethod.CreditCard, Amount = i % 4 == 0 ? price / 2 : price });
        }
        patients[3].Payments.Add(new Payment { Date = today, Method = PaymentMethod.Cash, Amount = 1500 });
        patients[6].Payments.Add(new Payment { Date = today, Method = PaymentMethod.CreditCard, Amount = 2500 });

        Appointment A(Patient? p, Dentist d, int hour, int minute, AppointmentType t, AppointmentStatus status, int arrivedMinutesAgo = 0) => new()
        {
            Patient = p, GuestName = p is null ? "Ali Yeni (kayıtsız)" : null, GuestPhone = p is null ? "0500 000 00 99" : null,
            Dentist = d, AppointmentType = t, Status = status,
            Start = DateTime.Today.AddHours(hour).AddMinutes(minute),
            End = DateTime.Today.AddHours(hour).AddMinutes(minute + t.DurationMinutes),
            ArrivedAt = arrivedMinutesAgo > 0 ? now.AddMinutes(-arrivedMinutesAgo) : null,
        };
        var appointments = new[]
        {
            A(patients[1], elif, 9, 0, types[0], AppointmentStatus.Completed),
            A(patients[2], elif, 9, 30, types[1], AppointmentStatus.Completed),
            A(zeynep, elif, 10, 30, types[2], AppointmentStatus.InChair, 25),
            A(patients[3], kerem, 10, 0, types[3], AppointmentStatus.Completed),
            A(patients[4], kerem, 11, 0, types[4], AppointmentStatus.Arrived, 18),
            A(patients[5], elif, 12, 0, types[1], AppointmentStatus.Arrived, 6),
            A(patients[6], kerem, 13, 30, types[1], AppointmentStatus.Confirmed),
            A(patients[7], elif, 14, 0, types[0], AppointmentStatus.Scheduled),
            A(null, kerem, 15, 0, types[0], AppointmentStatus.Scheduled),
            A(patients[8], elif, 15, 30, types[2], AppointmentStatus.Confirmed),
            A(patients[9], kerem, 16, 30, types[3], AppointmentStatus.Scheduled),
            A(patients[10], elif, 8, 30, types[0], AppointmentStatus.NoShow),
            A(patients[11], kerem, 10, 0, types[1], AppointmentStatus.Scheduled) is var t ? Tomorrow(t) : null!,
            A(patients[2], elif, 11, 0, types[3], AppointmentStatus.Scheduled) is var t2 ? Tomorrow(t2) : null!,
        };

        var reminders = new[]
        {
            new Reminder { Title = "Laboratuvardan kron gelince hastayı ara", DueDate = today, Patient = zeynep, ForRole = Roles.Assistant },
            new Reminder { Title = "Eldiven ve anestezik siparişi ver", DueDate = today.AddDays(-1) },
        };

        db.AddRange(elif, kerem, list, diabetes, blood);
        db.AddRange(types);
        db.AddRange(patients);
        db.AddRange(appointments);
        db.AddRange(reminders);
        var ibuprofen = new Medication { Name = "İbuprofen 400 mg", Dose = "400 mg", Usage = "Ağrı olduğunda, günde en çok 3 kez" };
        var gargara = new Medication { Name = "Klorheksidin gargara", Usage = "Günde 2 kez 1 dakika çalkalayın" };
        db.Medications.AddRange(ibuprofen, gargara,
            new Medication { Name = "Amoksisilin + Klavulanik asit 1000 mg", Dose = "1000 mg", Usage = "Günde 2 kez 1 tablet, tok karnına" });
        await db.PrescriptionTemplates.ExecuteDeleteAsync();
        db.PrescriptionTemplates.Add(new PrescriptionTemplate
        {
            Name = "Çekim sonrası",
            Diagnosis = "Diş çekimi sonrası",
            Items =
            [
                new PrescriptionTemplateItem { Medication = ibuprofen, MedicationName = ibuprofen.Name, Dose = ibuprofen.Dose, Usage = ibuprofen.Usage, Quantity = 1 },
                new PrescriptionTemplateItem { Medication = gargara, MedicationName = gargara.Name, Usage = gargara.Usage, Quantity = 1 },
            ],
        });
        await db.SaveChangesAsync();

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var (email, name, role, dentist) in new[]
        {
            ("yonetici@ornek.test", "Elif Örnek", Roles.Admin, (Dentist?)elif),
            ("kerem@ornek.test", "Kerem Uydurma", Roles.Dentist, kerem),
            ("asistan@ornek.test", "Seda Deneme", Roles.Assistant, null),
        })
        {
            var u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = name, DentistId = dentist?.Id };
            await users.CreateAsync(u, "Ornek12345");
            await users.AddToRoleAsync(u, role);
        }
        return new Ids(zeynep.Id);

        static Appointment Tomorrow(Appointment a)
        {
            a.Start = a.Start.AddDays(1);
            a.End = a.End.AddDays(1);
            return a;
        }
    }
}
