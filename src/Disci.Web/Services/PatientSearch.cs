using Disci.Domain;

namespace Disci.Web.Services;

/// <summary>
/// One search box for patients everywhere. A short number (up to 6 digits, not starting with 0) is a protocol
/// number; a number starting with 0 or longer than 6 digits is a phone number; anything else matches name words,
/// ignoring Turkish letters and case (see <see cref="TextSearch"/>).
/// </summary>
public static class PatientSearch
{
    public static IQueryable<Patient> Apply(IQueryable<Patient> q, string? term)
    {
        term = term?.Trim();
        if (string.IsNullOrEmpty(term))
            return q;

        // A phone typed with spaces ("0532 111 22 33") is one number, not four words.
        var compact = term.Replace(" ", "");
        var words = compact.All(char.IsDigit) && (compact.StartsWith('0') || compact.Length > 6)
            ? [compact]
            : term.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (!word.All(char.IsDigit))
            {
                q = q.Where(TextSearch.AnyContains<Patient>(word, null, p => p.FirstName, p => p.LastName));
                continue;
            }

            if (!word.StartsWith('0') && word.Length <= 6 && int.TryParse(word, out var number))
            {
                q = q.Where(p => p.LegacyId == number || p.Id == number || p.SecondProtocol == number || p.LegacyNumbers.Any(n => n.Number == number));
                continue;
            }

            // Phones are stored as typed: with or without the leading 0, spaces, dashes or brackets.
            // Only the trunk 0 of "0532..." is dropped, so numbers saved as "532..." match too.
            var digits = word.StartsWith('0') ? word[1..] : word;
            if (digits.Length < 3)
                return q.Where(_ => false);
            q = q.Where(p => (p.MobilePhone != null && p.MobilePhone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(digits))
                             || (p.HomePhone != null && p.HomePhone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(digits))
                             || (p.WorkPhone != null && p.WorkPhone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(digits)));
        }
        return q;
    }
}
