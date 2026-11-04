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

          --dl2001 [klasör]      DentaLine 2001 (2.3) verisini, DentaLineSuit aktarımı yapılmış veritabanına EKLER.
                                 Hastalar ad/telefon/doğum tarihiyle eşleştirilir, mükerrer tedavi ve ödemeler atlanır.
                                 Yeniden çalıştırılırsa yalnızca yeni kayıtları ekler.   [C:\Yazılım\DL2001\DB]
          --isim-duzelt          Eski programlardan gelen hasta adlarına Türkçe harfleri geri koyar ("Ayse" → "Ayşe").
                                 Aktarım bunu artık kendisi yapıyor; bu komut önceden aktarılmış kayıtlar içindir.
          --dry-run              (--dl2001 / --isim-duzelt ile) Her şeyi dener, raporu yazar, veritabanına hiçbir şey kaydetmez
        """);
    return 0;
}

var source = Option("--source", @"C:\Yazılım\DentaLineSuit\DL2500DB");
var connection = Option("--connection", Environment.GetEnvironmentVariable("DISCI_DB") ?? DisciDbContext.DefaultConnection);
var filesDir = Option("--files", @"C:\Yazılım\Disci\data\files");

if (Flag("--isim-duzelt"))
{
    bool dry = Flag("--dry-run");
    await using var ndb = new DisciDbContext(DisciDbContext.CreateOptions(connection, commandTimeoutSeconds: 300));
    await ndb.Database.MigrateAsync();
    var nreport = new ImportReport();
    var nwatch = Stopwatch.StartNew();
    await new NameFix(ndb, nreport, dry).RunAsync(CancellationToken.None);
    var ndir = Path.Combine(Path.GetDirectoryName(filesDir.TrimEnd('\\', '/'))!, "reports");
    Directory.CreateDirectory(ndir);
    var npath = Path.Combine(ndir, $"isim-duzeltme-{(dry ? "deneme-" : "")}{DateTime.Now:yyyyMMdd-HHmmss}.md");
    await File.WriteAllTextAsync(npath, nreport.ToMarkdown("-", connection, nwatch.Elapsed,
        dry ? "Hasta adlarında Türkçe harf düzeltmesi (DENEME, kaydedilmedi)" : "Hasta adlarında Türkçe harf düzeltmesi"), Encoding.UTF8);
    foreach (var line in nreport.TargetLines) Console.WriteLine($"  {line}");
    Console.WriteLine($"{(dry ? "Deneme bitti, kaydedilmedi" : "Düzeltme kaydedildi")}. Rapor: {npath}");
    return 0;
}

if (Flag("--dl2001"))
    return await MergeDl2001Async();

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

async Task<int> MergeDl2001Async()
{
    int i = argList.IndexOf("--dl2001");
    var dir = i + 1 < argList.Count && !argList[i + 1].StartsWith("--", StringComparison.Ordinal) ? argList[i + 1] : @"C:\Yazılım\DL2001\DB";
    bool dryRun = Flag("--dry-run");
    if (!File.Exists(Path.Combine(dir, "DLHASTA.DB")) && !Directory.EnumerateFiles(dir, "dlhasta.db", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).Any())
    {
        Console.Error.WriteLine($"DentaLine 2001 veritabanı bulunamadı (DLHASTA.DB yok): {dir}");
        return 2;
    }
    if (Process.GetProcessesByName("DentaLine2001").Length > 0)
        Console.WriteLine("UYARI: DentaLine 2001 açık görünüyor. Aktarım sırasında yeni kayıt girilmemesi için kapatmanız önerilir.");

    await using var mdb = new DisciDbContext(DisciDbContext.CreateOptions(connection, commandTimeoutSeconds: 300));
    Console.WriteLine("Veritabanı şeması hazırlanıyor (EF migrations)...");
    await mdb.Database.MigrateAsync();
    if (!await mdb.Patients.AnyAsync(p => p.LegacyId != null))
    {
        Console.Error.WriteLine("Hedef veritabanında DentaLineSuit hastaları yok. Önce DentaLineSuit aktarımını yapın, sonra --dl2001 ile ekleyin.");
        return 3;
    }

    var mreport = new ImportReport();
    var watch = Stopwatch.StartNew();
    Console.WriteLine($"DentaLine 2001 aktarımı{(dryRun ? " (DENEME, kaydedilmeyecek)" : "")}: {dir}");
    try
    {
        await new Dl2001Merger(mdb, new Dl2001Options(dir, filesDir, dryRun), mreport).RunAsync(CancellationToken.None);
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
    var path = Path.Combine(reportDir, $"dl2001-aktarim-{(dryRun ? "deneme-" : "")}{DateTime.Now:yyyyMMdd-HHmmss}.md");
    await File.WriteAllTextAsync(path, mreport.ToMarkdown(dir, connection, watch.Elapsed,
        dryRun ? "DentaLine 2001 → Disci aktarım raporu (DENEME, kaydedilmedi)" : "DentaLine 2001 → Disci aktarım raporu"), Encoding.UTF8);

    Console.WriteLine();
    foreach (var line in mreport.TargetLines) Console.WriteLine($"  {line}");
    Console.WriteLine();
    Console.WriteLine($"{(dryRun ? "Deneme bitti, hiçbir şey kaydedilmedi" : "Aktarım tamamlandı")} ({watch.Elapsed.TotalSeconds:N1} sn). Rapor: {path}");
    return 0;
}
