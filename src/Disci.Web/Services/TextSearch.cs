using System.Linq.Expressions;
using System.Text;

namespace Disci.Web.Services;

/// <summary>
/// Search that ignores Turkish letters and case: "yılmaz", "YILMAZ" and "yilmaz" find each other, so do "ayşe" and
/// "Ayse". The old clinic program was mostly typed without Turkish letters, and the database collation
/// (SQL_Latin1_General_CP1_CI_AS) treats ı/I and ş/s as different letters.
/// </summary>
public static class TextSearch
{
    private static readonly (string From, string To)[] Letters =
        [("ı", "i"), ("İ", "i"), ("ş", "s"), ("Ş", "s"), ("ğ", "g"), ("Ğ", "g"), ("ü", "u"), ("Ü", "u"), ("ö", "o"), ("Ö", "o"), ("ç", "c"), ("Ç", "c")];

    private static readonly System.Reflection.MethodInfo Replace = typeof(string).GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;
    private static readonly System.Reflection.MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    /// <summary>Lower-case ASCII-ish form: Turkish letters replaced, case removed.</summary>
    public static string Fold(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
            sb.Append(ch switch
            {
                'ı' or 'İ' or 'I' => 'i',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ü' or 'Ü' => 'u',
                'ö' or 'Ö' => 'o',
                'ç' or 'Ç' => 'c',
                _ => char.ToLowerInvariant(ch),
            });
        return sb.ToString();
    }

    /// <summary>In-memory lists (procedures, medicines, price rows).</summary>
    public static bool Contains(string? text, string word) => Fold(text).Contains(Fold(word), StringComparison.Ordinal);

    /// <summary>
    /// Database queries: true when any of the fields contains the word, both sides folded. The column side becomes
    /// nested REPLACE calls; the remaining ASCII case difference is handled by the case-insensitive collation.
    /// </summary>
    public static Expression<Func<T, bool>> AnyContains<T>(string word, Expression<Func<T, bool>>? orElse, params Expression<Func<T, string?>>[] fields)
    {
        var p = Expression.Parameter(typeof(T), "x");
        // A captured value (not a constant) so EF sends it as a parameter.
        var holder = new Holder(Fold(word));
        Expression value = Expression.Property(Expression.Constant(holder), nameof(Holder.Value));
        Expression? body = null;
        foreach (var f in fields)
        {
            var column = new Rebind(f.Parameters[0], p).Visit(f.Body)!;
            Expression folded = column;
            foreach (var (from, to) in Letters)
                folded = Expression.Call(folded, Replace, Expression.Constant(from), Expression.Constant(to));
            var test = Expression.AndAlso(Expression.NotEqual(column, Expression.Constant(null, typeof(string))),
                Expression.Call(folded, ContainsMethod, value));
            body = body is null ? test : Expression.OrElse(body, test);
        }
        if (orElse is not null)
        {
            var extra = new Rebind(orElse.Parameters[0], p).Visit(orElse.Body)!;
            body = body is null ? extra : Expression.OrElse(body, extra);
        }
        return Expression.Lambda<Func<T, bool>>(body ?? Expression.Constant(true), p);
    }

    private sealed class Holder(string value)
    {
        public string Value { get; } = value;
    }

    private sealed class Rebind(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
