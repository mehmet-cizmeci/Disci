using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Disci.LegacySync;

/// <summary>
/// Minimal Borland Database Engine (idapi32.dll, 32 bit) access. The old DentaLine programs read their Paradox tables
/// through BDE, so writing through BDE keeps the primary and secondary indexes (.PX, .XG0/.YG0) and the AutoInc
/// counters exactly as the programs expect. Strings are passed as raw bytes in the table's own code page (Delphi did the
/// ANSI→OEM conversion itself, BDE does not).
/// </summary>
internal static partial class Bde
{
    private const string Dll = "idapi32.dll";

    static Bde()
    {
        // idapi32.dll lives in the BDE folder named in the registry, not on the search path.
        NativeLibrary.SetDllImportResolver(typeof(Bde).Assembly, (name, _, _) =>
        {
            if (name != Dll) return IntPtr.Zero;
            var dir = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Borland\Database Engine", "DLLPATH", null) as string
                ?? Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Borland\Database Engine", "DLLPATH", null) as string
                ?? throw new DllNotFoundException("BDE (Borland Database Engine) kurulu değil: kayıt defterinde DLLPATH yok.");
            return NativeLibrary.Load(Path.Combine(dir.Split(';')[0], Dll));
        });
    }

    // Logical field types and subtypes (idapi.h).
    public const ushort fldZSTRING = 1, fldDATE = 2, fldBLOB = 3, fldBOOL = 4, fldINT16 = 5, fldINT32 = 6, fldFLOAT = 7,
        fldBCD = 8, fldBYTES = 9, fldTIME = 10, fldTIMESTAMP = 11;
    public const ushort fldstAUTOINC = 29;

    [DllImport(Dll)] public static extern ushort DbiInitFn(ushort version, IntPtr env);
    public static ushort DbiInit(IntPtr env)
    {
        ushort r = 0;
        foreach (ushort v in new ushort[] { 500, 0x0500, 400, 0x0400, 510, 0x0510, 300 })
            if ((r = DbiInitFn(v, env)) != 0x2F01) { Console.WriteLine($"  BDE arayüz sürümü {v}"); return r; }
        return r;
    }
    [DllImport(Dll)] public static extern ushort DbiExit();
    [DllImport(Dll)] public static extern ushort DbiGetErrorString(ushort result, byte[] text);
    [DllImport(Dll)] public static extern ushort DbiGetErrorContext(short context, byte[] text);
    [DllImport(Dll)] public static extern ushort DbiGetCurrSession(out IntPtr session);
    [DllImport(Dll)] public static extern ushort DbiSetProp(IntPtr obj, uint prop, IntPtr value);
    [DllImport(Dll)] public static extern ushort DbiGetProp(IntPtr obj, uint prop, byte[] value, ushort maxLen, out ushort len);
    [DllImport(Dll)] public static extern ushort DbiSetPrivateDir(byte[] dir);
    [DllImport(Dll)]
    public static extern ushort DbiOpenDatabase(byte[]? name, byte[]? type, int openMode, int shareMode, byte[]? password,
        ushort optFields, IntPtr optFieldDesc, IntPtr optParams, out IntPtr db);
    [DllImport(Dll)] public static extern ushort DbiCloseDatabase(ref IntPtr db);
    [DllImport(Dll)] public static extern ushort DbiSetDirectory(IntPtr db, byte[] dir);
    [DllImport(Dll)]
    public static extern ushort DbiOpenTable(IntPtr db, byte[] table, byte[] driver, byte[]? index, byte[]? tag, ushort indexId,
        int openMode, int shareMode, int xltMode, ushort uniDirectional, IntPtr optParams, out IntPtr cursor);
    [DllImport(Dll)] public static extern ushort DbiCloseCursor(ref IntPtr cursor);
    [DllImport(Dll)] public static extern ushort DbiGetCursorProps(IntPtr cursor, byte[] props);
    [DllImport(Dll)] public static extern ushort DbiGetFieldDescs(IntPtr cursor, byte[] descs);
    [DllImport(Dll)] public static extern ushort DbiInitRecord(IntPtr cursor, IntPtr rec);
    [DllImport(Dll)] public static extern ushort DbiPutField(IntPtr cursor, ushort field, IntPtr rec, byte[]? src);
    [DllImport(Dll)] public static extern ushort DbiGetField(IntPtr cursor, ushort field, IntPtr rec, byte[] dest, out ushort blank);
    [DllImport(Dll)] public static extern ushort DbiInsertRecord(IntPtr cursor, int lockType, IntPtr rec);
    [DllImport(Dll)] public static extern ushort DbiModifyRecord(IntPtr cursor, IntPtr rec, ushort freeLock);
    [DllImport(Dll)] public static extern ushort DbiGetRecord(IntPtr cursor, int lockType, IntPtr rec, IntPtr recProps);
    [DllImport(Dll)] public static extern ushort DbiGetNextRecord(IntPtr cursor, int lockType, IntPtr rec, IntPtr recProps);
    [DllImport(Dll)] public static extern ushort DbiSetToBegin(IntPtr cursor);
    [DllImport(Dll)] public static extern ushort DbiOpenBlob(IntPtr cursor, IntPtr rec, ushort field, int openMode);
    [DllImport(Dll)] public static extern ushort DbiPutBlob(IntPtr cursor, IntPtr rec, ushort field, uint offset, uint length, byte[] src);
    [DllImport(Dll)] public static extern ushort DbiFreeBlob(IntPtr cursor, IntPtr rec, ushort field);
    [DllImport(Dll)] public static extern ushort DbiDateEncode(ushort month, ushort day, short year, out int date);
    [DllImport(Dll)] public static extern ushort DbiTimeEncode(ushort hour, ushort minute, ushort milliSecond, out int time);
    [DllImport(Dll)] public static extern ushort DbiTimeStampEncode(int date, int time, out double timeStamp);

    public const ushort DBIERR_EOF = 0x2202;

    public static void Check(ushort result, string what)
    {
        if (result == 0) return;
        var buf = new byte[512];
        DbiGetErrorString(result, buf);
        var text = Encoding.Latin1.GetString(buf, 0, Math.Max(0, Array.IndexOf(buf, (byte)0)));
        var ctx = new StringBuilder();
        for (short c = 1; c <= 8; c++)
        {
            var b = new byte[512];
            if (DbiGetErrorContext(c, b) == 0 && b[0] != 0)
                ctx.Append(" | ").Append(Encoding.Latin1.GetString(b, 0, Array.IndexOf(b, (byte)0)));
        }
        throw new BdeException($"BDE hatası 0x{result:X4} ({what}): {text}{ctx}", result);
    }

    /// <summary>Null-terminated ANSI (Turkish Windows) text, for names and paths such as C:\Yazılım.</summary>
    public static byte[] Z(string s) => Encoding.GetEncoding(1254).GetBytes(s + "\0");
}

internal sealed class BdeException(string message, ushort code) : Exception(message)
{
    public ushort Code { get; } = code;
}

internal sealed record BdeField(ushort Number, string Name, ushort Type, ushort SubType, short Units1, ushort Length);

/// <summary>One BDE session on one Paradox directory.</summary>
internal sealed class BdeDatabase : IDisposable
{
    private IntPtr _db;

    public string Directory { get; }

    public BdeDatabase(string directory, string privateDir)
    {
        Directory = directory = Path.GetFullPath(directory);
        privateDir = Path.GetFullPath(privateDir);
        // BDE turns paths into the OEM code page and loses letters such as "ı" (C:\Yazılım → C:\YAZLM): use 8.3 names.
        directory = ShortPath(directory);
        privateDir = ShortPath(privateDir);
        Bde.Check(Bde.DbiInit(IntPtr.Zero), "DbiInit");
        Bde.Check(Bde.DbiSetPrivateDir(Bde.Z(privateDir)), "DbiSetPrivateDir");
        // The programs keep their own network control file; use one next to the data so nothing else is touched.
        Bde.Check(Bde.DbiGetCurrSession(out var session), "DbiGetCurrSession");
        var net = Bde.Z(privateDir);
        var handle = GCHandle.Alloc(net, GCHandleType.Pinned);
        try { Bde.Check(Bde.DbiSetProp(session, 0x00020002 /* sesNETFILE */, handle.AddrOfPinnedObject()), "sesNETFILE"); }
        finally { handle.Free(); }
        Bde.Check(Bde.DbiOpenDatabase(null, null, 0, 0, null, 0, IntPtr.Zero, IntPtr.Zero, out _db), "DbiOpenDatabase");
        Bde.Check(Bde.DbiSetDirectory(_db, Bde.Z(directory)), "DbiSetDirectory");
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathNameW(string longPath, StringBuilder shortPath, uint size);

    private static string ShortPath(string path)
    {
        if (path.All(c => c < 128)) return path;
        var sb = new StringBuilder(1024);
        uint n = GetShortPathNameW(path, sb, (uint)sb.Capacity);
        var s = n > 0 ? sb.ToString() : path;
        if (!s.All(c => c < 128))
            throw new InvalidOperationException($"Klasör yolunda Türkçe harf var ve kısa adı alınamadı: {path}");
        return s;
    }

    public BdeTable Open(string table, Encoding encoding) => new(_db, Path.GetFileName(table), encoding);

    public void Dispose()
    {
        if (_db != IntPtr.Zero) Bde.DbiCloseDatabase(ref _db);
        Bde.DbiExit();
    }
}

/// <summary>An exclusively opened Paradox table.</summary>
internal sealed class BdeTable : IDisposable
{
    private IntPtr _cursor;
    private readonly IntPtr _rec;
    private readonly int _recSize;

    public string Name { get; }
    public IReadOnlyList<BdeField> Fields { get; }
    public Encoding Encoding { get; }

    public BdeTable(IntPtr db, string table, Encoding encoding)
    {
        Name = table;
        Bde.Check(Bde.DbiOpenTable(db, Bde.Z(table), Bde.Z("PARADOX"), null, null, 0, 0 /* read-write */, 1 /* exclusive */,
            2 /* xltFIELD */, 0, IntPtr.Zero, out _cursor), $"{table} açılamadı");

        var props = new byte[4096];
        Bde.Check(Bde.DbiGetCursorProps(_cursor, props), "DbiGetCursorProps");
        int fieldCount = BinaryPrimitives.ReadUInt16LittleEndian(props.AsSpan(295));
        _recSize = BinaryPrimitives.ReadUInt16LittleEndian(props.AsSpan(299));
        if (fieldCount is <= 0 or > 1000 || _recSize is <= 0 or > 32000)
            throw new InvalidOperationException($"{table}: imleç bilgisi okunamadı (alan {fieldCount}, kayıt {_recSize}).");

        var descs = new byte[fieldCount * 128];
        Bde.Check(Bde.DbiGetFieldDescs(_cursor, descs), "DbiGetFieldDescs");
        // FLDDesc is 62 bytes (packed, 4-byte enums). Its iFldNum is not always the position (restructured tables), so
        // fields are addressed by position, and the layout is checked by every name being a plain identifier.
        const int stride = 62;
        if (!Enumerable.Range(0, fieldCount).All(k => descs[k * stride + 2] is >= (byte)'A' and <= (byte)'z'))
            throw new InvalidOperationException($"{table}: alan tanımları okunamadı ({fieldCount} alan): " +
                Convert.ToHexString(descs, 0, Math.Min(descs.Length, 240)));
        var fields = new List<BdeField>();
        for (int k = 0; k < fieldCount; k++)
        {
            var d = descs.AsSpan(k * stride);
            int nameEnd = d[2..34].IndexOf((byte)0);
            fields.Add(new BdeField((ushort)(k + 1), Encoding.Latin1.GetString(d.Slice(2, nameEnd)),
                BinaryPrimitives.ReadUInt16LittleEndian(d[34..]), BinaryPrimitives.ReadUInt16LittleEndian(d[36..]),
                BinaryPrimitives.ReadInt16LittleEndian(d[38..]), BinaryPrimitives.ReadUInt16LittleEndian(d[44..])));
        }
        Fields = fields;
        Encoding = encoding;
        _rec = Marshal.AllocHGlobal(_recSize);
    }

    public BdeField Field(string name) =>
        Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"{Name}: '{name}' alanı yok.");

    public bool Has(string name) => Fields.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Inserts a record; fields not given stay blank. AutoInc fields are filled by BDE. Returns the stored record.</summary>
    public Dictionary<string, object?> Insert(IReadOnlyDictionary<string, object?> values)
    {
        Bde.Check(Bde.DbiInitRecord(_cursor, _rec), "DbiInitRecord");
        var blobs = new List<ushort>();
        try
        {
            foreach (var (name, value) in values)
            {
                if (value is null) continue;
                var f = Field(name);
                if (f.SubType == Bde.fldstAUTOINC) continue;
                if (f.Type == Bde.fldBLOB)
                {
                    var bytes = value is string s ? Text(s) : (byte[])value;
                    if (bytes.Length == 0) continue;
                    Bde.Check(Bde.DbiOpenBlob(_cursor, _rec, f.Number, 0), $"{Name}.{f.Name} blob");
                    blobs.Add(f.Number);
                    Bde.Check(Bde.DbiPutBlob(_cursor, _rec, f.Number, 0, (uint)bytes.Length, bytes), $"{Name}.{f.Name} blob yaz");
                    continue;
                }
                Bde.Check(Bde.DbiPutField(_cursor, f.Number, _rec, Encode(f, value)), $"{Name}.{f.Name} = {value}");
            }
            Bde.Check(Bde.DbiInsertRecord(_cursor, 0, _rec), $"{Name} kayıt ekleme");
            // Blob handles live in the record buffer: free them before the buffer is overwritten with the stored record.
            foreach (var b in blobs) Bde.Check(Bde.DbiFreeBlob(_cursor, _rec, b), $"{Name} blob bırakma");
            blobs.Clear();
            Bde.Check(Bde.DbiGetRecord(_cursor, 0, _rec, IntPtr.Zero), $"{Name} eklenen kaydı okuma");
            return ReadCurrent();
        }
        finally
        {
            foreach (var b in blobs) Bde.DbiFreeBlob(_cursor, _rec, b);
        }
    }

    /// <summary>Walks every record; <paramref name="change"/> returns the fields to overwrite (or null to leave it).</summary>
    /// <summary>Number of records BDE reads through the table's index.</summary>
    public int Count()
    {
        int n = 0;
        Bde.Check(Bde.DbiSetToBegin(_cursor), "DbiSetToBegin");
        while (true)
        {
            var r = Bde.DbiGetNextRecord(_cursor, 0, _rec, IntPtr.Zero);
            if (r == Bde.DBIERR_EOF) return n;
            Bde.Check(r, $"{Name} okuma");
            n++;
        }
    }

    public int UpdateWhere(Func<Dictionary<string, object?>, IReadOnlyDictionary<string, object?>?> change)
    {
        int changed = 0;
        Bde.Check(Bde.DbiSetToBegin(_cursor), "DbiSetToBegin");
        while (true)
        {
            var r = Bde.DbiGetNextRecord(_cursor, 0, _rec, IntPtr.Zero);
            if (r == Bde.DBIERR_EOF) break;
            Bde.Check(r, $"{Name} okuma");
            var updates = change(ReadCurrent());
            if (updates is null || updates.Count == 0) continue;
            Bde.Check(Bde.DbiGetRecord(_cursor, 1 /* write lock */, _rec, IntPtr.Zero), $"{Name} kilit");
            foreach (var (name, value) in updates)
            {
                var f = Field(name);
                if (f.Type == Bde.fldBLOB) throw new NotSupportedException($"{Name}.{name}: blob güncelleme desteklenmiyor.");
                Bde.Check(Bde.DbiPutField(_cursor, f.Number, _rec, value is null ? null : Encode(f, value)), $"{Name}.{f.Name} = {value}");
            }
            Bde.Check(Bde.DbiModifyRecord(_cursor, _rec, 1), $"{Name} güncelleme");
            changed++;
        }
        return changed;
    }

    /// <summary>Non-blob fields of the current record buffer, decoded (strings, int, double, bool, DateOnly...).</summary>
    private Dictionary<string, object?> ReadCurrent()
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in Fields)
        {
            if (f.Type == Bde.fldBLOB) continue;
            var buf = new byte[Math.Max(16, (int)f.Length + 2)];
            Bde.Check(Bde.DbiGetField(_cursor, f.Number, _rec, buf, out var blank), $"{Name}.{f.Name} okuma");
            if (blank != 0) { result[f.Name] = null; continue; }
            result[f.Name] = f.Type switch
            {
                Bde.fldZSTRING => Encoding.GetString(buf, 0, Math.Max(0, Array.IndexOf(buf, (byte)0))),
                Bde.fldINT16 => (int)BitConverter.ToInt16(buf),
                Bde.fldINT32 => BitConverter.ToInt32(buf),
                Bde.fldFLOAT => BitConverter.ToDouble(buf),
                Bde.fldBOOL => BitConverter.ToInt16(buf) != 0,
                Bde.fldDATE => DateOnly.FromDayNumber(BitConverter.ToInt32(buf) - 1),
                _ => buf[..f.Length],
            };
        }
        return result;
    }

    /// <summary>
    /// Text in the table's code page. DentaLine 2001 has a few tables tagged 866 that only ever hold plain ASCII
    /// (Turkish letters written without dots/cedillas), so text for them is folded the same way.
    /// </summary>
    private byte[] Text(string s)
    {
        if (Encoding.CodePage == 866)
            s = new string(s.Select(c => c switch
            {
                'ç' => 'c', 'Ç' => 'C', 'ğ' => 'g', 'Ğ' => 'G', 'ı' => 'i', 'İ' => 'I',
                'ö' => 'o', 'Ö' => 'O', 'ş' => 's', 'Ş' => 'S', 'ü' => 'u', 'Ü' => 'U', _ => c,
            }).ToArray());
        return Encoding.GetBytes(s);
    }

    private byte[] Encode(BdeField f, object value)
    {
        switch (f.Type)
        {
            case Bde.fldZSTRING:
            {
                var s = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
                var bytes = Text(s);
                int max = f.Units1 > 0 ? f.Units1 : f.Length - 1;
                if (bytes.Length > max) bytes = bytes[..max];
                return [.. bytes, 0];
            }
            case Bde.fldINT16: return BitConverter.GetBytes(Convert.ToInt16(value));
            case Bde.fldINT32: return BitConverter.GetBytes(Convert.ToInt32(value));
            case Bde.fldFLOAT: return BitConverter.GetBytes(Convert.ToDouble(value));
            case Bde.fldBOOL: return BitConverter.GetBytes((short)((bool)value ? 1 : 0));
            case Bde.fldDATE:
            {
                var d = (DateOnly)value;
                Bde.Check(Bde.DbiDateEncode((ushort)d.Month, (ushort)d.Day, (short)d.Year, out var date), "tarih");
                return BitConverter.GetBytes(date);
            }
            case Bde.fldTIME:
            {
                var t = (TimeOnly)value;
                Bde.Check(Bde.DbiTimeEncode((ushort)t.Hour, (ushort)t.Minute, (ushort)(t.Second * 1000 + t.Millisecond), out var time), "saat");
                return BitConverter.GetBytes(time);
            }
            case Bde.fldTIMESTAMP:
            {
                var dt = (DateTime)value;
                Bde.Check(Bde.DbiDateEncode((ushort)dt.Month, (ushort)dt.Day, (short)dt.Year, out var date), "tarih");
                Bde.Check(Bde.DbiTimeEncode((ushort)dt.Hour, (ushort)dt.Minute, (ushort)(dt.Second * 1000 + dt.Millisecond), out var time), "saat");
                Bde.Check(Bde.DbiTimeStampEncode(date, time, out var ts), "zaman damgası");
                return BitConverter.GetBytes(ts);
            }
            default:
                throw new NotSupportedException($"{Name}.{f.Name}: alan türü {f.Type} yazılamıyor.");
        }
    }

    public void Dispose()
    {
        if (_cursor != IntPtr.Zero) Bde.DbiCloseCursor(ref _cursor);
        Marshal.FreeHGlobal(_rec);
    }
}
