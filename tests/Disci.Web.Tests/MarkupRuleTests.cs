using System.Text.RegularExpressions;

namespace Disci.Web.Tests;

/// <summary>Checks on the .razor sources for binding mistakes that compile and render fine but silently lose input.</summary>
public sealed partial class MarkupRuleTests
{
    private static IEnumerable<string> RazorFiles()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Disci.Web")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Directory.EnumerateFiles(Path.Combine(dir.FullName, "src", "Disci.Web", "Components"), "*.razor", SearchOption.AllDirectories);
    }

    /// <summary>
    /// Blazor's script sends a time input's value with seconds ("11:15:00"), so a "HH:mm" bind format can never
    /// parse it and every change is thrown away (appointment time and working hours could not be changed).
    /// Without a format the seconds are parsed, and ":00" is hidden on display anyway.
    /// </summary>
    [Fact]
    public void Time_inputs_have_no_bind_format()
    {
        var offenders = RazorFiles()
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, line, i)))
            .Where(x => TimeInput().IsMatch(x.line) && x.line.Contains("@bind:format"))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();
        Assert.Empty(offenders);
    }

    [GeneratedRegex("""type="(time|datetime-local)""")]
    private static partial Regex TimeInput();
}
