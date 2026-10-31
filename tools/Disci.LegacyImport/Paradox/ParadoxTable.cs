using System.Buffers.Binary;
using System.Text;

namespace Disci.LegacyImport.Paradox;

public enum ParadoxFieldType : byte
{
    Alpha = 0x01, Date = 0x02, Short = 0x03, Long = 0x04, Currency = 0x05, Number = 0x06,
    Logical = 0x09, Memo = 0x0C, Blob = 0x0D, FmtMemo = 0x0E, Ole = 0x0F, Graphic = 0x10,
    Time = 0x14, Timestamp = 0x15, AutoInc = 0x16, Bcd = 0x17, Bytes = 0x18,
}

public sealed record ParadoxField(string Name, ParadoxFieldType Type, int Size);

/// <summary>
/// Read-only reader for Paradox 4-7 tables (.DB with optional .MB memo file). No BDE needed.
/// Values: string, DateOnly, TimeOnly, DateTime, int, double, decimal, bool or byte[]; null when empty.
/// </summary>
public sealed class ParadoxTable
{
    // Delphi's TDateTime zero; DentaLineSuit writes it for "no date".
    private static readonly DateOnly EmptyDate = new(1899, 12, 30);

    private readonly byte[] _data;
    private readonly byte[]? _memo;
    private readonly int _recordSize, _headerSize, _blockSize, _firstBlock, _fileBlocks;

    public string Name { get; }
    public IReadOnlyList<ParadoxField> Fields { get; }
    public int HeaderRecordCount { get; }
    public Encoding Encoding { get; }

    static ParadoxTable() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public ParadoxTable(string path)
    {
        Name = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
        _data = ReadShared(path);
        var memoPath = Path.ChangeExtension(path, ".MB");
        if (File.Exists(memoPath))
            _memo = ReadShared(memoPath);

        var d = _data.AsSpan();
        _recordSize = BinaryPrimitives.ReadUInt16LittleEndian(d);
        _headerSize = BinaryPrimitives.ReadUInt16LittleEndian(d[0x02..]);
        _blockSize = d[0x05] * 1024;
        HeaderRecordCount = BinaryPrimitives.ReadInt32LittleEndian(d[0x06..]);
        _fileBlocks = BinaryPrimitives.ReadUInt16LittleEndian(d[0x0C..]);
        _firstBlock = BinaryPrimitives.ReadUInt16LittleEndian(d[0x0E..]);
        int fieldCount = BinaryPrimitives.ReadUInt16LittleEndian(d[0x21..]);
        int version = d[0x39];

        // Tables tagged 1252 actually hold Turkish text; 1254 is 1252 plus the Turkish letters.
        int codePage = version >= 4 ? BinaryPrimitives.ReadUInt16LittleEndian(d[0x6A..]) : 437;
        Encoding = Encoding.GetEncoding(codePage == 1252 ? 1254 : codePage);

        int pos = version >= 4 ? 0x78 : 0x58;
        var types = new (ParadoxFieldType Type, int Size)[fieldCount];
        for (int i = 0; i < fieldCount; i++)
            types[i] = ((ParadoxFieldType)d[pos + 2 * i], d[pos + 2 * i + 1]);
        pos += 2 * fieldCount + 4 + 4 * fieldCount;
        pos += version >= 0x0C ? 261 : 79;

        var fields = new List<ParadoxField>(fieldCount);
        foreach (var (type, size) in types)
        {
            int end = Array.IndexOf(_data, (byte)0, pos);
            fields.Add(new ParadoxField(Encoding.GetString(_data, pos, end - pos), type, size));
            pos = end + 1;
        }
        Fields = fields;
    }

    public IEnumerable<ParadoxRecord> ReadRecords()
    {
        var seen = new HashSet<int>();
        int block = _firstBlock;
        while (block != 0 && block <= _fileBlocks && seen.Add(block))
        {
            int offset = _headerSize + (block - 1) * _blockSize;
            int next = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(offset));
            int addDataSize = BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(offset + 4));
            if (addDataSize >= 0)
            {
                int count = addDataSize / _recordSize + 1;
                for (int i = 0; i < count; i++)
                    yield return Decode(offset + 6 + i * _recordSize);
            }
            block = next;
        }
    }

    private ParadoxRecord Decode(int start)
    {
        var values = new object?[Fields.Count];
        int pos = start;
        for (int i = 0; i < Fields.Count; i++)
        {
            var f = Fields[i];
            var raw = _data.AsSpan(pos, f.Size);
            pos += f.Size;
            values[i] = raw.IndexOfAnyExcept((byte)0) < 0 ? null : DecodeValue(f, raw);
        }
        return new ParadoxRecord(this, values);
    }

    private object? DecodeValue(ParadoxField f, ReadOnlySpan<byte> raw)
    {
        switch (f.Type)
        {
            case ParadoxFieldType.Alpha:
            {
                int len = raw.IndexOf((byte)0);
                var s = Encoding.GetString(len < 0 ? raw : raw[..len]).TrimEnd();
                return s.Length == 0 ? null : s;
            }
            case ParadoxFieldType.Date:
            {
                int days = BinaryPrimitives.ReadInt32BigEndian(Flip(raw));
                if (days <= 0 || days > DateOnly.MaxValue.DayNumber + 1) return null;
                var date = DateOnly.FromDayNumber(days - 1);
                return date == EmptyDate ? null : date;
            }
            case ParadoxFieldType.Short:
                return (int)BinaryPrimitives.ReadInt16BigEndian(Flip(raw));
            case ParadoxFieldType.Long:
            case ParadoxFieldType.AutoInc:
                return BinaryPrimitives.ReadInt32BigEndian(Flip(raw));
            case ParadoxFieldType.Number:
            case ParadoxFieldType.Currency:
                return BinaryPrimitives.ReadDoubleBigEndian(Flip(raw));
            case ParadoxFieldType.Logical:
                return raw[0] switch { 0x81 => true, 0x80 => false, _ => null };
            case ParadoxFieldType.Time:
                return TimeOnly.FromTimeSpan(TimeSpan.FromMilliseconds(BinaryPrimitives.ReadInt32BigEndian(Flip(raw))));
            case ParadoxFieldType.Timestamp:
            {
                double ms = BinaryPrimitives.ReadDoubleBigEndian(Flip(raw));
                try { return DateTime.MinValue.AddMilliseconds(ms).AddDays(-1); }
                catch (ArgumentOutOfRangeException) { return null; }
            }
            case ParadoxFieldType.Bcd:
                return DecodeBcd(raw);
            case ParadoxFieldType.Memo:
            case ParadoxFieldType.FmtMemo:
            {
                var bytes = ReadBlob(raw);
                return bytes is null ? null : Encoding.GetString(bytes).TrimEnd('\0');
            }
            case ParadoxFieldType.Blob:
            case ParadoxFieldType.Ole:
            case ParadoxFieldType.Graphic:
                return ReadBlob(raw);
            default:
                return raw.ToArray();
        }
    }

    /// <summary>Paradox stores numbers big-endian with the sign bit inverted.</summary>
    private static byte[] Flip(ReadOnlySpan<byte> raw)
    {
        var b = raw.ToArray();
        if ((b[0] & 0x80) != 0)
            b[0] &= 0x7F;
        else
            for (int i = 0; i < b.Length; i++) b[i] ^= 0xFF;
        return b;
    }

    private static decimal DecodeBcd(ReadOnlySpan<byte> raw)
    {
        var b = raw.ToArray();
        bool negative = (b[0] & 0x80) == 0;
        if (negative)
            for (int i = 0; i < b.Length; i++) b[i] ^= 0xFF;
        int decimals = b[0] & 0x3F;
        decimal value = 0;
        for (int i = 1; i < b.Length; i++)
            value = value * 100 + (b[i] >> 4) * 10 + (b[i] & 0x0F);
        for (int i = 0; i < decimals; i++) value /= 10;
        return negative ? -value : value;
    }

    /// <summary>The last 10 bytes of a memo/blob field point into the .MB file; the rest holds its start inline.</summary>
    private byte[]? ReadBlob(ReadOnlySpan<byte> raw)
    {
        var inline = raw[..^10];
        uint offset = BinaryPrimitives.ReadUInt32LittleEndian(raw[^10..]);
        int length = BinaryPrimitives.ReadInt32LittleEndian(raw[^6..]);
        if (length <= 0) return null;
        if (offset == 0 || _memo is null)
            return inline[..Math.Min(length, inline.Length)].ToArray();

        int blockOffset = (int)(offset & 0xFFFFFF00);
        int index = (int)(offset & 0xFF);
        int dataStart = index == 0xFF
            ? blockOffset + 9
            : blockOffset + 16 * _memo[blockOffset + 12 + index * 5];
        return _memo.AsSpan(dataStart, Math.Min(length, _memo.Length - dataStart)).ToArray();
    }

    private static byte[] ReadShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[fs.Length];
        fs.ReadExactly(buffer);
        return buffer;
    }
}

public sealed class ParadoxRecord(ParadoxTable table, object?[] values)
{
    public object? this[string field] => values[IndexOf(field)];

    public string? Str(string field) => this[field] as string;
    public int? Int(string field) => this[field] is int i ? i : null;
    public double? Num(string field) => this[field] is double d ? d : null;
    public decimal? Dec(string field, int digits = 2) =>
        this[field] is double d && double.IsFinite(d) ? Math.Round((decimal)d, digits) : null;
    public DateOnly? Date(string field) => this[field] as DateOnly?;
    public TimeOnly? Time(string field) => this[field] as TimeOnly?;
    public bool? Bool(string field) => this[field] as bool?;
    public byte[]? Bytes(string field) => this[field] as byte[];

    public IEnumerable<(string Field, object? Value)> All() =>
        table.Fields.Select((f, i) => (f.Name, values[i]));

    private int IndexOf(string field)
    {
        for (int i = 0; i < table.Fields.Count; i++)
            if (string.Equals(table.Fields[i].Name, field, StringComparison.OrdinalIgnoreCase))
                return i;
        throw new KeyNotFoundException($"{table.Name}: '{field}' alanı yok.");
    }
}
