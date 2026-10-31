using System.Diagnostics;
using System.Text;
using Disci.Infrastructure;
using Disci.LegacyImport.Import;
using Disci.LegacyImport.Paradox;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = Encoding.UTF8;

var argList = args.ToList();
string Option(string name, string fallback)
{
    int i = argList.IndexOf(name);
    return i >= 0 && i + 1 < argList.Count ? argList[i + 1] : fallback;
}
bool Flag(string name) => argList.Contains(name);

if (Flag("--help") || Flag("-h"))
{
    Console.WriteLine("""
        DentaLineSuit → Disci veri aktarımı

        Kullanım:
          Disci.LegacyImport [seçenekler]

        Seçenekler:
          --source <klasör>      Eski veritabanı klasörü (DL2500DB)      [C:\Yazılım\DentaLineSuit\DL2500DB]
          --connection <metin>   Hedef SQL Server bağlantısı             [LocalDB, Disci veritabanı]
          --files <klasör>       Fotoğrafların kopyalanacağı klasör       [C:\Yazılım\Disci\data\files]
          --reset                Hedef veritabanını silip baştan oluşturur (önceki aktarımın üzerine yazar)
          --inspect              Hiçbir şey yazmaz; eski tabloları okuyup kayıt sayılarını gösterir
        """);
    return 0;
}

var source = Option("--source", @"C:\Yazılım\DentaLineSuit\DL2500DB");
var connection = Option("--connection", Environment.GetEnvironmentVariable("DISCI_DB") ?? DisciDbContext.DefaultConnection);
var filesDir = Option("--files", @"C:\Yazılım\Disci\data\files");

if (!Directory.Exists(source))
{
    Console.Error.WriteLine($"Kaynak klasör bulunamadı: {source}");
    return 2;
}

if (Flag("--inspect"))
{
    Console.WriteLine($"{"Tablo",-22} {"Okunan",8} {"Başlık",8}  Alan");
    foreach (var file in Directory.EnumerateFiles(source, "*.db", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).Order())
    {
        var table = new ParadoxTable(file);
        int read = table.ReadRecords().Count();
        Console.WriteLine($"{table.Name,-22} {read,8} {table.HeaderRecordCount,8}  {table.Fields.Count}{(read == table.HeaderRecordCount ? "" : "   <-- KONTROL")}");
    }
    return 0;
}

if (Process.GetProcessesByName("DentaLineSuit").Length > 0)
    Console.WriteLine("UYARI: DentaLineSuit açık. Aktarım sırasında yeni kayıt girilmemesi için programı kapatmanız önerilir.");

await using var db = new DisciDbContext(DisciDbContext.CreateOptions(connection, commandTimeoutSeconds: 300));

if (Flag("--reset"))
{
    Console.WriteLine("Hedef veritabanı siliniyor...");
    await db.Database.EnsureDeletedAsync();
}
Console.WriteLine("Veritabanı şeması hazırlanıyor (EF migrations)...");
await db.Database.MigrateAsync();

if (await db.Patients.AnyAsync())
{
    Console.Error.WriteLine("Hedef veritabanında zaten hasta kaydı var. Aktarımı yinelemek için --reset kullanın.");
    return 3;
}

var report = new ImportReport();
var watch = Stopwatch.StartNew();
Console.WriteLine($"Aktarım başlıyor: {source}");
try
{
    await new LegacyImporter(db, new ImportOptions(source, filesDir), report).RunAsync(CancellationToken.None);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"HATA: aktarım geri alındı. {ex.GetBaseException().Message}");
    Console.Error.WriteLine(ex);
    return 1;
}
watch.Stop();

var reportDir = Path.Combine(Path.GetDirectoryName(filesDir.TrimEnd('\\', '/'))!, "reports");
Directory.CreateDirectory(reportDir);
var reportPath = Path.Combine(reportDir, $"aktarim-raporu-{DateTime.Now:yyyyMMdd-HHmmss}.md");
await File.WriteAllTextAsync(reportPath, report.ToMarkdown(source, connection, watch.Elapsed), Encoding.UTF8);

Console.WriteLine();
Console.WriteLine($"Aktarım tamamlandı ({watch.Elapsed.TotalSeconds:N1} sn). Rapor: {reportPath}");
if (!report.SourcesComplete)
    Console.WriteLine("UYARI: Bazı eski tablolarda okunan kayıt sayısı dosya başlığıyla uyuşmuyor; raporu kontrol edin.");
return 0;
