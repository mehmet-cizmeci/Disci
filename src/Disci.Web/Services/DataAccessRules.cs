using Disci.Domain;
using Disci.Infrastructure.Identity;

namespace Disci.Web.Services;

/// <summary>
/// What the signed-in user may do with each dentist's records (patients whose own dentist it is, appointments,
/// treatments, prescriptions). Records with no dentist belong to the whole clinic and are always open.
/// </summary>
public sealed class AccessScope
{
    private readonly Dictionary<int, DataAccess> _levels;

    public AccessScope(Dictionary<int, DataAccess> levels)
    {
        _levels = levels;
        VisibleIds = levels.Where(x => x.Value >= DataAccess.View).Select(x => x.Key).ToList();
        EditableIds = levels.Where(x => x.Value >= DataAccess.Edit).Select(x => x.Key).ToList();
        SeesAll = levels.Values.All(v => v >= DataAccess.View);
    }

    /// <summary>Every dentist is visible, so lists need no filtering.</summary>
    public bool SeesAll { get; }
    public List<int> VisibleIds { get; }
    public List<int> EditableIds { get; }

    /// <summary>A dentist added after the scope was computed is treated as hidden until the next page load.</summary>
    public DataAccess Of(int? dentistId) => dentistId is { } id ? _levels.GetValueOrDefault(id, DataAccess.None) : DataAccess.Edit;
    public bool CanSee(int? dentistId) => Of(dentistId) >= DataAccess.View;
    public bool CanEdit(int? dentistId) => Of(dentistId) >= DataAccess.Edit;

    public static readonly AccessScope Everything = new([]);
}

public static class DataAccessRules
{
    public sealed record DentistSharing(int Id, DataAccess ShareWithDentists, DataAccess ShareWithAssistants);

    /// <summary>
    /// Admins and the dentist's own account: full access. Otherwise a per-user exception wins; failing that the
    /// dentist's setting for the user's role (the more generous one if the user has both roles).
    /// </summary>
    public static AccessScope Compute(IEnumerable<string> roles, int? ownDentistId, IEnumerable<DentistSharing> dentists,
        IReadOnlyDictionary<int, DataAccess> grants)
    {
        var roleSet = roles.ToHashSet();
        var admin = roleSet.Contains(Roles.Admin);
        var levels = new Dictionary<int, DataAccess>();
        foreach (var d in dentists)
        {
            DataAccess level;
            if (admin || d.Id == ownDentistId) level = DataAccess.Edit;
            else if (grants.TryGetValue(d.Id, out var g)) level = g;
            else
            {
                level = DataAccess.None;
                if (roleSet.Contains(Roles.Dentist) && d.ShareWithDentists > level) level = d.ShareWithDentists;
                if (roleSet.Contains(Roles.Assistant) && d.ShareWithAssistants > level) level = d.ShareWithAssistants;
            }
            levels[d.Id] = level;
        }
        return new AccessScope(levels);
    }

    public static IQueryable<Patient> Visible(this IQueryable<Patient> q, AccessScope s) =>
        s.SeesAll ? q : q.Where(p => p.PrimaryDentistId == null || s.VisibleIds.Contains(p.PrimaryDentistId.Value));

    public static IQueryable<Appointment> Visible(this IQueryable<Appointment> q, AccessScope s) =>
        s.SeesAll ? q : q.Where(a => s.VisibleIds.Contains(a.DentistId));

    public static IQueryable<TreatmentSession> Visible(this IQueryable<TreatmentSession> q, AccessScope s) =>
        s.SeesAll ? q : q.Where(t => t.DentistId == null || s.VisibleIds.Contains(t.DentistId.Value));

    public static IQueryable<Prescription> Visible(this IQueryable<Prescription> q, AccessScope s) =>
        s.SeesAll ? q : q.Where(r => s.VisibleIds.Contains(r.DentistId));

    public static IQueryable<Dentist> Visible(this IQueryable<Dentist> q, AccessScope s) =>
        s.SeesAll ? q : q.Where(d => s.VisibleIds.Contains(d.Id));

    /// <summary>Dentists the user may book, treat or prescribe for.</summary>
    public static IQueryable<Dentist> Editable(this IQueryable<Dentist> q, AccessScope s) =>
        q.Where(d => s.EditableIds.Contains(d.Id));

    public static string Name(DataAccess a) => a switch
    {
        DataAccess.None => "Göremez",
        DataAccess.View => "Yalnızca görür",
        _ => "Görür ve işlem yapar",
    };

    public static string ShortName(DataAccess a) => a switch
    {
        DataAccess.None => "kapalı",
        DataAccess.View => "görür",
        _ => "tam",
    };
}
