using System.Diagnostics;
using System.Text;
using Disci.Infrastructure;
using Disci.LegacySync;

Console.OutputEncoding = Encoding.UTF8;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var argList = args.ToList();
string Option(string name, string fallback)
{
    int i = argList.IndexOf(name);
    return i >= 0 && i + 1 < argList.Count ? argList[i + 1] : fallback;
}

if (argList.Contains("--help") || argList.Contains("-h"))
{
    Console.WriteLine("""
        DentaLineSuit ↔ DentaLine 2001 eşitleme

        İki eski programın veritabanını birbirinin kayıtlarıyla tamamlar: DentaLine 2001'deki hasta, tedavi, ödeme,
        diş notu ve fotoğraflar DentaLineSuit'e; DentaLineSuit'tekiler (ayrıca anamnez, hasta notu, giderler)
        DentaLine 2001'e yazılır. Hastalar Disci aktarımının eşleştirmesine göre birleştirilir. Yazma, programların
        kendi veritabanı motoruyla (BDE) yapılır; bu yüzden araç 32 bit çalışır ve BDE kurulu olmalıdır.

        ÖNCE İKİ KLASÖRÜN DE YEDEĞİNİ ALIN. İki program da kapalı olmalı. Yalnızca bir kez çalıştırılabilir
        (her klasöre DISCI-ESITLEME.json yazılır; Disci aktarımı bu dosyayla kopyaları tanır).

        Seçenekler:
          --dls <klasör>         DentaLineSuit veritabanı (DL2500DB)     [C:\Yazılım\DentaLineSuit\DL2500DB]
          --dl2001 <klasör>      DentaLine 2001 veritabanı (DB)          [C:\Yazılım\DL2001\DB]
          --connection <metin>   Eşleştirmenin okunacağı Disci veritabanı [LocalDB, Disci]
          --report <klasör>      Raporun yazılacağı klasör               [C:\Yazılım\Disci\data\reports]
        """);
    return 0;
}

if (argList.Contains("--dogrula"))
{
    // Every table read through BDE (primary index order) must hold as many records as the file itself.
    var dir = Option("--dogrula", ".");
    var priv = Path.Combine(Path.GetTempPath(), $"disci-dogrula-{Environment.ProcessId}");
    Directory.CreateDirectory(priv);
    int bad = 0;
    using (var db = new BdeDatabase(dir, priv))
        foreach (var file in Directory.GetFiles(Path.GetFullPath(dir), "*.db").Where(f => !Path.GetFileName(f).StartsWith('_')).Order())
        {
            var pt = new Disci.LegacyImport.Paradox.ParadoxTable(file);
            int fileCount = pt.ReadRecords().Count();
            try
            {
                using var t = db.Open(file, pt.Encoding);
                int bdeCount = t.Count();
                bool ok = bdeCount == fileCount && fileCount == pt.HeaderRecordCount;
                if (!ok) bad++;
                Console.WriteLine($"{(ok ? "tamam" : "SORUN"),-6} {Path.GetFileName(file),-22} BDE {bdeCount,7}  dosya {fileCount,7}  başlık {pt.HeaderRecordCount,7}");
            }
            catch (BdeException e) { bad++; Console.WriteLine($"SORUN  {Path.GetFileName(file),-22} {e.Message}"); }
        }
    Directory.Delete(priv, true);
    Console.WriteLine(bad == 0 ? "Bütün tablolar sağlam." : $"{bad} tabloda sorun var.");
    return bad == 0 ? 0 : 1;
}

var dlsDir = Option("--dls", @"C:\Yazılım\DentaLineSuit\DL2500DB");
var d01Dir = Option("--dl2001", @"C:\Yazılım\DL2001\DB");
var connection = Option("--connection", Environment.GetEnvironmentVariable("DISCI_DB") ?? DisciDbContext.DefaultConnection);
var reportDir = Option("--report", @"C:\Yazılım\Disci\data\reports");

foreach (var name in new[] { "DentaLineSuit", "DentaLine2001", "DentaLineSuitTamir" })
    if (Process.GetProcessesByName(name).Length > 0)
    {
        Console.Error.WriteLine($"{name} açık. Önce programı kapatın.");
        return 2;
    }

var privateDir = Path.Combine(Path.GetTempPath(), $"disci-esitleme-{Environment.ProcessId}");
Directory.CreateDirectory(privateDir);
var report = new Report();
var watch = Stopwatch.StartNew();
try
{
    await new LegacySync(new Side("DentaLineSuit", dlsDir), new Side("DL2001", d01Dir), connection, report).RunAsync(privateDir);
}
catch (Exception e)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"HATA: {e.Message}");
    Console.Error.WriteLine(e.StackTrace);
    Console.Error.WriteLine("Yarım kalan bir eşitleme olabilir: iki klasörü de yedekten geri yükleyin.");
    return 1;
}
finally
{
    try { Directory.Delete(privateDir, true); } catch (IOException) { }
}

Directory.CreateDirectory(reportDir);
var path = Path.Combine(reportDir, $"esitleme-{DateTime.Now:yyyyMMdd-HHmmss}.md");
File.WriteAllText(path, $"# DentaLineSuit ↔ DentaLine 2001 eşitleme\n\n- Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}\n- DentaLineSuit: {dlsDir}\n" +
    $"- DentaLine 2001: {d01Dir}\n- Süre: {watch.Elapsed:mm\\:ss}\n{report}", new UTF8Encoding(true));
Console.WriteLine($"\nEşitleme bitti ({watch.Elapsed:mm\\:ss}). Rapor: {path}");
return 0;
