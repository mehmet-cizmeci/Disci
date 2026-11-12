using System.Security.Claims;
using Disci.Domain;
using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Disci.Web.Services;

/// <summary>
/// Tells every open screen that clinic data changed (a patient checked in, a payment taken, a task done), so the
/// waiting room, the dashboard and the notification bell refresh on all computers at once.
/// </summary>
public sealed class ClinicEvents
{
    public event Func<Task>? Changed;

    public async Task NotifyAsync()
    {
        if (Changed is null) return;
        foreach (var handler in Changed.GetInvocationList().Cast<Func<Task>>())
        {
            // A closed browser tab must not stop the others from hearing about the change.
            try { await handler(); } catch { }
        }
    }
}

/// <summary>The signed-in user as the clinic screens need it: id, role and own dentist record.</summary>
public sealed class CurrentUser(AuthenticationStateProvider auth, IDbContextFactory<DisciDbContext> dbFactory)
{
    private ClaimsPrincipal? _user;
    private int? _dentistId;
    private bool _dentistLoaded;

    public async Task<ClaimsPrincipal> PrincipalAsync() => _user ??= (await auth.GetAuthenticationStateAsync()).User;

    public async Task<string?> IdAsync() => (await PrincipalAsync()).FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<bool> IsInAnyRoleAsync(params string[] roles)
    {
        var user = await PrincipalAsync();
        return roles.Any(user.IsInRole);
    }

    /// <summary>Admins and dentists see money totals and edit prices; assistants run the front desk.</summary>
    public Task<bool> CanSeeFinanceAsync() => IsInAnyRoleAsync(Roles.Admin, Roles.Dentist);

    private AccessScope? _access;

    /// <summary>Which dentists' records this user may see or change; computed once per page/connection.</summary>
    public async Task<AccessScope> AccessAsync()
    {
        if (_access is not null) return _access;
        var user = await PrincipalAsync();
        var id = await IdAsync();
        var own = await DentistIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var dentists = await db.Dentists.AsNoTracking()
            .Select(d => new DataAccessRules.DentistSharing(d.Id, d.ShareWithDentists, d.ShareWithAssistants)).ToListAsync();
        var grants = id is null ? [] : await db.DentistAccessGrants.AsNoTracking().Where(g => g.UserId == id)
            .ToDictionaryAsync(g => g.DentistId, g => g.Access);
        var roles = Roles.All.Where(user.IsInRole);
        return _access = DataAccessRules.Compute(roles, own, dentists, grants);
    }

    public async Task<int?> DentistIdAsync()
    {
        if (_dentistLoaded) return _dentistId;
        var id = await IdAsync();
        if (id is not null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            _dentistId = await db.Users.Where(u => u.Id == id).Select(u => u.DentistId).FirstOrDefaultAsync();
        }
        _dentistLoaded = true;
        return _dentistId;
    }
}

/// <summary>
/// Money owed by patients. Charges are completed treatments, credits are payments that were not cancelled.
/// Only TRY is summed; the few foreign-currency records are shown but not converted.
/// </summary>
public static class Ledger
{
    public static IQueryable<decimal> Charges(DisciDbContext db, int patientId) =>
        db.TreatmentItems
            .Where(i => i.TreatmentSession.PatientId == patientId && i.TreatmentSession.Kind == TreatmentKind.Completed && i.Currency == Currencies.Try)
            .Select(i => i.Price);

    public static IQueryable<decimal> Credits(DisciDbContext db, int patientId) =>
        db.Payments.Where(p => p.PatientId == patientId && !p.IsCancelled && p.Currency == Currencies.Try).Select(p => p.Amount);

    public static async Task<decimal> BalanceAsync(DisciDbContext db, int patientId, CancellationToken ct = default) =>
        (await Charges(db, patientId).SumAsync(x => (decimal?)x, ct) ?? 0) - (await Credits(db, patientId).SumAsync(x => (decimal?)x, ct) ?? 0);

    /// <summary>Balance per patient as a query, so lists can sort and filter on it in SQL.</summary>
    public static IQueryable<PatientBalance> Balances(DisciDbContext db) =>
        db.Patients.Select(p => new PatientBalance
        {
            PatientId = p.Id,
            Charged = p.TreatmentSessions.Where(s => s.Kind == TreatmentKind.Completed)
                .SelectMany(s => s.Items).Where(i => i.Currency == Currencies.Try).Sum(i => (decimal?)i.Price) ?? 0,
            Paid = p.Payments.Where(x => !x.IsCancelled && x.Currency == Currencies.Try).Sum(x => (decimal?)x.Amount) ?? 0,
        });
}

/// <summary>Built with member initialisers (not a constructor) so EF can still filter and sort on the fields.</summary>
public sealed class PatientBalance
{
    public int PatientId { get; init; }
    public decimal Charged { get; init; }
    public decimal Paid { get; init; }
    /// <summary>In memory only; queries use Charged - Paid.</summary>
    public decimal Remaining => Charged - Paid;
}

/// <summary>Booking rules shared by the calendar, the dashboard and the patient page.</summary>
public static class Scheduling
{
    public static readonly AppointmentStatus[] Inactive = [AppointmentStatus.Cancelled, AppointmentStatus.NoShow];

    public static TimeOnly DefaultDayStart => new(8, 0);
    public static TimeOnly DefaultDayEnd => new(20, 0);

    /// <summary>Appointments that overlap the given slot for the same dentist or the same patient.</summary>
    public static async Task<List<Appointment>> ConflictsAsync(DisciDbContext db, Appointment a, CancellationToken ct = default) =>
        await db.Appointments.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.Dentist)
            .Where(x => x.Id != a.Id && !Inactive.Contains(x.Status) && x.Start < a.End && a.Start < x.End
                        && (x.DentistId == a.DentistId || (a.PatientId != null && x.PatientId == a.PatientId)))
            .OrderBy(x => x.Start)
            .ToListAsync(ct);

    /// <summary>Moves an appointment along the visit workflow and stamps the arrival time.</summary>
    public static void SetStatus(Appointment a, AppointmentStatus status, DateTime now)
    {
        a.Status = status;
        if (status is AppointmentStatus.Arrived && a.ArrivedAt is null)
            a.ArrivedAt = now;
        else if (status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed)
            a.ArrivedAt = null;
    }

    /// <summary>The next sensible step from each status, as offered by the one-click button.</summary>
    public static AppointmentStatus? NextStep(AppointmentStatus s) => s switch
    {
        AppointmentStatus.Scheduled => AppointmentStatus.Confirmed,
        AppointmentStatus.Confirmed => AppointmentStatus.Arrived,
        AppointmentStatus.Arrived => AppointmentStatus.InChair,
        AppointmentStatus.InChair => AppointmentStatus.Completed,
        _ => null,
    };
}
