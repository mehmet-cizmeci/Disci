#:package Markdig@0.41.3
// Kullanım kılavuzunu (Kullanim-Kilavuzu.md) yazdırılabilir HTML'e çevirir; kilavuz-pdf.ps1 bunu Chrome ile PDF yapar.
using Markdig;

var md = args.Length > 0 ? args[0] : "Kullanim-Kilavuzu.md";
var output = args.Length > 1 ? args[1] : "Kullanim-Kilavuzu.html";
var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
var body = Markdown.ToHtml(File.ReadAllText(md), pipeline);
var logo = File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(md))!, "disci-logo.png")) ? "<img class=\"logo\" src=\"disci-logo.png\" alt=\"\">" : "";
var html = $$"""
<!doctype html>
<html lang="tr"><head><meta charset="utf-8"><title>Disci kullanım kılavuzu</title>
<style>
  @page { size: A4; margin: 16mm 15mm 18mm; }
  body { font-family: "Segoe UI", Arial, sans-serif; font-size: 10.5pt; line-height: 1.55; color: #0f172a; }
  .cover { height: 250mm; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; break-after: page; }
  .cover .logo { width: 120px; height: 120px; margin-bottom: 24px; }
  .cover h1 { font-size: 30pt; margin: 0 0 8px; color: #0f766e; }
  .cover p { font-size: 13pt; color: #475569; margin: 4px 0; }
  h1 { font-size: 20pt; color: #0f766e; }
  h2 { font-size: 15pt; color: #0f766e; border-bottom: 2px solid #ccfbf1; padding-bottom: 4px; margin-top: 0; break-before: page; }
  h3 { font-size: 12pt; margin-top: 18px; }
  img { max-width: 100%; border: 1px solid #e2e8f0; border-radius: 6px; margin: 6px 0 10px; break-inside: avoid; }
  table { border-collapse: collapse; width: 100%; margin: 8px 0 12px; font-size: 9.5pt; break-inside: avoid; }
  th, td { border: 1px solid #e2e8f0; padding: 5px 7px; text-align: left; vertical-align: top; }
  th { background: #f1f5f9; }
  blockquote { border-left: 4px solid #0f766e; background: #f0fdfa; margin: 10px 0; padding: 6px 12px; break-inside: avoid; }
  code, pre { background: #f1f5f9; border-radius: 4px; font-family: Consolas, monospace; font-size: 9.5pt; }
  code { padding: 1px 4px; } pre { padding: 8px 10px; }
  a { color: #0f766e; text-decoration: none; }
  body > h1:first-of-type { display: none; }
</style></head><body>
<div class="cover">{{logo}}<h1>Disci</h1><p>Ücretsiz ve açık kaynak diş kliniği programı</p><p><b>Kullanım kılavuzu</b></p><p style="margin-top:40px;font-size:10pt">{{DateTime.Now:MMMM yyyy}}</p></div>
{{body}}
</body></html>
""";
File.WriteAllText(output, html);
Console.WriteLine(output);
