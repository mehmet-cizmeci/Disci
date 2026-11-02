using Disci.Domain;

namespace Disci.Web.Services;

/// <summary>One search box for patients everywhere: protocol number, name words or (spaced) phone digits.</summary>
public static class PatientSearch
{
    public static IQueryable<Patient> Apply(IQueryable<Patient> q, string? term)
    {
        term = term?.Trim();
        if (string.IsNullOrEmpty(term))
            return q;

        if (int.TryParse(term, out var number) && term.Length <= 6)
            return q.Where(p => p.LegacyId == number || p.Id == number);

        var digits = new string(term.Where(char.IsDigit).ToArray());
        foreach (var word in term.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            q = q.Where(p => p.FirstName.Contains(word) || p.LastName.Contains(word)
                             || (digits.Length >= 4 && (p.MobilePhone!.Replace(" ", "").Contains(digits)
                                                        || p.HomePhone!.Replace(" ", "").Contains(digits))));
        return q;
    }
}
