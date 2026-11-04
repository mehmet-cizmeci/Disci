using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Disci.Web.Services;

/// <summary>
/// The user guide (docs/Kullanim-Kilavuzu.md, copied next to the program) rendered as HTML for /kilavuz, with a table
/// of contents built from its second-level headings. Pictures live in docs/kilavuz-resimler; the PDF beside it.
/// </summary>
public sealed partial class UserGuide
{
    public static string Folder => Path.Combine(AppContext.BaseDirectory, "Kilavuz");
    public static string PdfPath => Path.Combine(Folder, "Kullanim-Kilavuzu.pdf");

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()   // tables, heading ids for the contents, task lists
        .Build();

    private readonly Lazy<(string Html, IReadOnlyList<(string Id, string Title)> Contents)> _page = new(Load);

    public string Html => _page.Value.Html;
    public IReadOnlyList<(string Id, string Title)> Contents => _page.Value.Contents;
    public bool HasPdf => File.Exists(PdfPath);

    private static (string, IReadOnlyList<(string, string)>) Load()
    {
        var path = Path.Combine(Folder, "Kullanim-Kilavuzu.md");
        if (!File.Exists(path))
            return ("<p>Kullanım kılavuzu dosyası bulunamadı.</p>", []);
        var markdown = File.ReadAllText(path);
        var doc = Markdown.Parse(markdown, Pipeline);
        var contents = doc.Descendants<HeadingBlock>()
            .Where(h => h.Level == 2 && h.GetAttributes().Id is not null)
            .Select(h => (h.GetAttributes().Id!, PlainText(markdown.Substring(h.Span.Start, h.Span.Length))))
            .ToList();
        // Pictures are referenced as kilavuz-resimler/x.png in the Markdown (works on GitHub); the program serves them at the same relative path.
        var html = doc.ToHtml(Pipeline).Replace("<table>", "<div class=\"table-responsive\"><table class=\"table\">").Replace("</table>", "</table></div>");
        return (html, contents);
    }

    private static string PlainText(string headingLine) => HeadingMarks().Replace(headingLine, "").Trim();

    [GeneratedRegex(@"^#+\s*|[*_`]")]
    private static partial Regex HeadingMarks();
}

public static class UserGuideEndpoints
{
    /// <summary>The PDF and the pictures of the guide; readable without signing in, like the guide itself.</summary>
    public static void MapUserGuideFiles(this IEndpointRouteBuilder app)
    {
        app.MapGet("/kilavuz.pdf", () => File.Exists(UserGuide.PdfPath)
            ? Results.File(UserGuide.PdfPath, "application/pdf", "Disci-Kullanim-Kilavuzu.pdf")
            : Results.NotFound()).AllowAnonymous();

        app.MapGet("/kilavuz-resimler/{name}", (string name) =>
        {
            if (name.Contains("..") || name.IndexOfAny(['/', '\\']) >= 0) return Results.NotFound();
            var path = Path.Combine(UserGuide.Folder, "kilavuz-resimler", name);
            return File.Exists(path) ? Results.File(path, "image/png") : Results.NotFound();
        }).AllowAnonymous();
    }
}
