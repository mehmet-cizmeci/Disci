using Disci.Domain;
using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Disci.Web.Services;

public enum NoticeKind
{
    Waiting,
    Late,
    Unconfirmed,
    DebtorToday,
    Task,
    Birthday,
    Recall,
}

public enum NoticeTone { Danger, Warning, Info, Success }

public sealed record Notice(
    NoticeKind Kind,
    NoticeTone Tone,
    string Title,
    string Detail,
    int? PatientId = null,
    int? AppointmentId = null,
    int? ReminderId = null,
    string? Phone = null,
    string? Message = null);

/// <summary>
/// The front desk's to-do list, worked out from the data rather than stored: who is waiting, who is late, which of
/// tomorrow's bookings still need a confirmation call, which of today's patients owe money, open tasks, birthdays
/// and patients due for a check-up. Nothing here has to be cleared by hand except tasks.
/// </summary>
public sealed class NotificationService(IDbContextFactory<DisciDbContext> dbFactory, TimeProvider clock)
{
    /// <summary>Patients seen this many months ago and not booked since are due a check-up call.</summary>
    public const int RecallMonths = 6;

    public async Task<List<Notice>> GetAsync(bool includeLowPriority = true, string? role = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = clock.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(now);
        var dayStart = now.Date;
        var list = new List<Notice>();

        var todays = await db.Appointments.AsNoTracking()
            .Where(a => a.Start >= dayStart && a.Start < dayStart.AddDays(1) && !Scheduling.Inactive.Contains(a.Status))
            .Select(a => new
            {
                a.Id, a.PatientId, a.Start, a.Status, a.ArrivedAt, a.GuestName, a.GuestPhone,
                Name = a.Patient == null ? null : a.Patient.FirstName + " " + a.Patient.LastName,
                Phone = a.Patient == null ? null : a.Patient.MobilePhone,
                Dentist = a.Dentist.FullName,
            })
            .OrderBy(a => a.Start)
            .ToListAsync(ct);

        foreach (var a in todays.Where(a => a.Status == AppointmentStatus.Arrived))
        {
            var minutes = a.ArrivedAt is { } t ? (int)(now - t).TotalMinutes : 0;
            list.Add(new Notice(NoticeKind.Waiting, minutes >= 15 ? NoticeTone.Danger : NoticeTone.Warning,
                $"{a.Name ?? a.GuestName} bekliyor",
                $"{minutes} dakikadır bekleme salonunda · {a.Dentist} · randevu {a.Start:HH:mm}",
                a.PatientId, a.Id));
        }

        foreach (var a in todays.Where(a => a.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed && a.Start < now.AddMinutes(-10)))
            list.Add(new Notice(NoticeKind.Late, NoticeTone.Danger,
                $"{a.Name ?? a.GuestName} gelmedi",
                $"Randevu {a.Start:HH:mm} · {(int)(now - a.Start).TotalMinutes} dakika geçti · {a.Dentist}",
                a.PatientId, a.Id, Phone: a.Phone ?? a.GuestPhone));

        var tomorrow = dayStart.AddDays(1);
        var unconfirmed = await db.Appointments.AsNoTracking()
            .Where(a => a.Start >= tomorrow && a.Start < tomorrow.AddDays(1) && a.Status == AppointmentStatus.Scheduled)
            .OrderBy(a => a.Start)
            .Select(a => new
            {
                a.Id, a.PatientId, a.Start, a.GuestName, a.GuestPhone,
                Name = a.Patient == null ? null : a.Patient.FirstName + " " + a.Patient.LastName,
                Phone = a.Patient == null ? null : a.Patient.MobilePhone,
                Dentist = a.Dentist.FullName,
            })
            .ToListAsync(ct);
        foreach (var a in unconfirmed)
            list.Add(new Notice(NoticeKind.Unconfirmed, NoticeTone.Warning,
                $"{a.Name ?? a.GuestName} – yarın {a.Start:HH:mm}",
                $"Randevu teyit edilmedi · {a.Dentist}",
                a.PatientId, a.Id, Phone: a.Phone ?? a.GuestPhone, Message: Display.ReminderText(a.Name ?? a.GuestName ?? "", a.Start, a.Dentist)));

        // Raise the balance at check-in, when it is easiest to bring up.
        var todaysPatients = todays.Where(a => a.PatientId != null && a.Status != AppointmentStatus.Completed).Select(a => a.PatientId!.Value).Distinct().ToList();
        if (todaysPatients.Count > 0)
        {
            var debtors = await Ledger.Balances(db).Where(b => todaysPatients.Contains(b.PatientId) && b.Charged - b.Paid > 0).ToListAsync(ct);
            foreach (var d in debtors)
            {
                var a = todays.First(x => x.PatientId == d.PatientId);
                list.Add(new Notice(NoticeKind.DebtorToday, NoticeTone.Info,
                    $"{a.Name}: {Display.Money(d.Remaining)} borç",
                    $"Bugün {a.Start:HH:mm} randevusu var; tahsilat hatırlatması", d.PatientId, a.Id));
            }
        }

        var tasks = await db.Reminders.AsNoTracking()
            .Where(r => !r.IsDone && r.DueDate <= today && (role == null || r.ForRole == null || r.ForRole == role))
            .OrderBy(r => r.DueDate)
            .Select(r => new { r.Id, r.Title, r.DueDate, r.PatientId, Name = r.Patient == null ? null : r.Patient.FirstName + " " + r.Patient.LastName })
            .ToListAsync(ct);
        foreach (var r in tasks)
            list.Add(new Notice(NoticeKind.Task, r.DueDate < today ? NoticeTone.Danger : NoticeTone.Warning,
                r.Title,
                (r.DueDate < today ? $"Gecikti · {Display.Date(r.DueDate)}" : "Bugün") + (r.Name is null ? "" : $" · {r.Name}"),
                r.PatientId, ReminderId: r.Id));

        if (!includeLowPriority)
            return list;

        var birthdays = await db.Patients.AsNoTracking()
            .Where(p => !p.IsArchived && p.BirthDate != null && p.BirthDate.Value.Month == today.Month && p.BirthDate.Value.Day == today.Day)
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.MobilePhone, p.BirthDate })
            .Take(20)
            .ToListAsync(ct);
        foreach (var p in birthdays)
            list.Add(new Notice(NoticeKind.Birthday, NoticeTone.Success, $"{p.FirstName} {p.LastName}",
                $"Bugün doğum günü · {Display.Age(p.BirthDate)}", p.Id, Phone: p.MobilePhone,
                Message: $"Sayın {p.FirstName} {p.LastName}, doğum gününüzü kutlar, sağlıklı ve gülümsemeli bir yıl dileriz."));

        var recallFrom = today.AddMonths(-RecallMonths - 1);
        var recallTo = today.AddMonths(-RecallMonths);
        var recall = await db.Patients.AsNoTracking()
            .Where(p => !p.IsArchived)
            .Select(p => new
            {
                p.Id, p.FirstName, p.LastName, p.MobilePhone,
                Last = p.TreatmentSessions.Where(s => s.Kind == TreatmentKind.Completed).Max(s => (DateOnly?)s.Date),
                Booked = db.Appointments.Any(a => a.PatientId == p.Id && a.Start >= dayStart && !Scheduling.Inactive.Contains(a.Status)),
            })
            .Where(x => x.Last >= recallFrom && x.Last < recallTo && !x.Booked)
            .OrderBy(x => x.Last)
            .Take(25)
            .ToListAsync(ct);
        foreach (var p in recall)
            list.Add(new Notice(NoticeKind.Recall, NoticeTone.Info, $"{p.FirstName} {p.LastName}",
                $"Son tedavi {Display.Date(p.Last)} · kontrol randevusu verilebilir", p.Id, Phone: p.MobilePhone,
                Message: $"Sayın {p.FirstName} {p.LastName}, son diş kontrolünüzün üzerinden {RecallMonths} ay geçti. Kontrol randevusu için bize ulaşabilirsiniz."));

        return list;
    }

    /// <summary>What the bell counts: things someone should act on now.</summary>
    public static bool IsUrgent(Notice n) => n.Kind is NoticeKind.Waiting or NoticeKind.Late or NoticeKind.Unconfirmed or NoticeKind.Task;

    public static string GroupTitle(NoticeKind kind) => kind switch
    {
        NoticeKind.Waiting => "Bekleme salonu",
        NoticeKind.Late => "Gecikenler",
        NoticeKind.Unconfirmed => "Yarın teyit edilecek randevular",
        NoticeKind.DebtorToday => "Bugün gelen borçlu hastalar",
        NoticeKind.Task => "Görevler",
        NoticeKind.Birthday => "Doğum günleri",
        NoticeKind.Recall => $"Kontrol zamanı gelenler ({RecallMonths} ay)",
        _ => kind.ToString(),
    };

    /// <summary>Role used to pick the tasks meant for this user; null (administrators) sees every task.</summary>
    public static string? RoleFor(System.Security.Claims.ClaimsPrincipal user) =>
        user.IsInRole(Roles.Admin) ? null : user.IsInRole(Roles.Dentist) ? Roles.Dentist : Roles.Assistant;
}
