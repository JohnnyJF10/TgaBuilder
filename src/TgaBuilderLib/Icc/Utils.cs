using System.Buffers.Binary;
using System.Text;

namespace TgaBuilderLib.Icc;

// ---------- Binary helpers (big-endian) ----------

internal static class BEHelpers
{
    public static void WriteU8(Stream s, byte v) => s.WriteByte(v);
    public static void WriteU16(Stream s, ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b, v); s.Write(b); }
    public static void WriteU32(Stream s, uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); s.Write(b); }
    public static void WriteU64(Stream s, ulong v) { Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteUInt64BigEndian(b, v); s.Write(b); }
    public static void WriteI32(Stream s, int v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(b, v); s.Write(b); }

    public static void WriteS15Fixed16(Stream s, double v)
    {
        double scaled = Math.Round(v * 65536.0);
        if (scaled < int.MinValue) scaled = int.MinValue;
        if (scaled > int.MaxValue) scaled = int.MaxValue;
        WriteI32(s, (int)scaled);
    }

    public static void WriteU16Fixed16(Stream s, double v)
    {
        double scaled = Math.Round(v * 65536.0);
        if (scaled < 0) scaled = 0;
        if (scaled > uint.MaxValue) scaled = uint.MaxValue;
        WriteU32(s, (uint)scaled);
    }

    /// <summary>Write a 4-byte ASCII signature. Pads with spaces, or 4 NULs if input is empty.</summary>
    public static void WriteSig(Stream s, string sig)
    {
        Span<byte> buf = stackalloc byte[4];

        if (string.IsNullOrEmpty(sig))
        {
            buf.Clear();
        }
        else
        {
            byte[] raw = Encoding.ASCII.GetBytes(sig);
            int copy = Math.Min(4, raw.Length);

            for (int i = 0; i < copy; i++) buf[i] = raw[i];
            for (int i = copy; i < 4; i++) buf[i] = (byte)' ';
        }

        s.Write(buf);
    }

    public static void WriteXyz(Stream s, (double X, double Y, double Z) xyz)
    {
        WriteS15Fixed16(s, xyz.X);
        WriteS15Fixed16(s, xyz.Y);
        WriteS15Fixed16(s, xyz.Z);
    }

    public static void WriteDateTime(Stream s, DateTime dt)
    {
        WriteU16(s, (ushort)dt.Year);
        WriteU16(s, (ushort)dt.Month);
        WriteU16(s, (ushort)dt.Day);
        WriteU16(s, (ushort)dt.Hour);
        WriteU16(s, (ushort)dt.Minute);
        WriteU16(s, (ushort)dt.Second);
    }
}

// ---------- Tag-type encoders ----------

internal static class TagEncoders
{
    public static byte[] EncodeMluc(IReadOnlyList<MlucRecord> records)
    {
        const int recSize = 12;
        int headerLen = 8 + 4 + 4;                 // 'mluc' + reserved + count + recSize
        int recordsLen = records.Count * recSize;
        int stringsOffset = headerLen + recordsLen;

        // Pre-encode strings as UTF-16BE
        var encoded = records.Select(r => Encoding.BigEndianUnicode.GetBytes(r.Text)).ToArray();

        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("mluc"));
        BEHelpers.WriteU32(ms, 0);                        // reserved
        BEHelpers.WriteU32(ms, (uint)records.Count);
        BEHelpers.WriteU32(ms, recSize);

        int cursor = stringsOffset;

        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            byte[] lang = AsciiPair(r.Language);
            byte[] ctry = AsciiPair(r.Country);

            ms.Write(lang);
            ms.Write(ctry);
            BEHelpers.WriteU32(ms, (uint)encoded[i].Length);
            BEHelpers.WriteU32(ms, (uint)cursor);

            cursor += encoded[i].Length;
        }

        foreach (var s in encoded) ms.Write(s);
        return ms.ToArray();

        static byte[] AsciiPair(string s)
        {
            byte[] b = Encoding.ASCII.GetBytes(string.IsNullOrEmpty(s) ? "  " : s);
            var pair = new byte[] { (byte)' ', (byte)' ' };

            if (b.Length >= 1) pair[0] = b[0];
            if (b.Length >= 2) pair[1] = b[1];

            return pair;
        }
    }

    public static byte[] EncodeXyzType(IEnumerable<(double X, double Y, double Z)> xyzList)
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("XYZ "));
        BEHelpers.WriteU32(ms, 0);

        foreach (var xyz in xyzList) BEHelpers.WriteXyz(ms, xyz);

        return ms.ToArray();
    }

    public static byte[] EncodeSf32(double[] values)
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("sf32"));
        BEHelpers.WriteU32(ms, 0);

        foreach (var v in values) BEHelpers.WriteS15Fixed16(ms, v);

        return ms.ToArray();
    }

    public static byte[] EncodePara(int functionType, double[] parameters)
    {
        var expected = new Dictionary<int, int> { { 0, 1 }, { 1, 3 }, { 2, 4 }, { 3, 5 }, { 4, 7 } };

        if (!expected.TryGetValue(functionType, out int need))
            throw new ArgumentException($"Invalid parametric curve function type: {functionType}");

        if (parameters.Length != need)
            throw new ArgumentException(
                $"Function type {functionType} requires {need} parameters; got {parameters.Length}");

        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("para"));
        BEHelpers.WriteU32(ms, 0);
        BEHelpers.WriteU16(ms, (ushort)functionType);
        BEHelpers.WriteU16(ms, 0);                        // 2 reserved bytes

        foreach (var p in parameters) BEHelpers.WriteS15Fixed16(ms, p);

        return ms.ToArray();
    }

    public static byte[] EncodeCurv(ToneReproductionCurveSpec.CurvTrc spec)
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("curv"));
        BEHelpers.WriteU32(ms, 0);

        switch (spec.Mode)
        {
            case CurvMode.Identity:
                BEHelpers.WriteU32(ms, 0);
                break;

            case CurvMode.Gamma:
                BEHelpers.WriteU32(ms, 1);
                int raw = (int)Math.Round(spec.GammaVal * 256.0) & 0xFFFF;
                BEHelpers.WriteU16(ms, (ushort)raw);
                break;

            case CurvMode.Table:
                BEHelpers.WriteU32(ms, (uint)spec.Points.Length);
                foreach (var v in spec.Points) BEHelpers.WriteU16(ms, v);
                break;

            default:
                throw new ArgumentException($"Unknown curv mode: {spec.Mode}");
        }

        return ms.ToArray();
    }

    public static byte[] EncodeTrc(ToneReproductionCurveSpec spec) => spec switch
    {
        ToneReproductionCurveSpec.ParaTrc p => EncodePara(p.FunctionType, p.Parameters),
        ToneReproductionCurveSpec.CurvTrc c => EncodeCurv(c),
        _ => throw new ArgumentException($"Unknown TRC spec: {spec.GetType().Name}")
    };

    public static byte[] EncodeChrm(int phosphorType, IReadOnlyList<(double X, double Y)> xy)
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("chrm"));
        BEHelpers.WriteU32(ms, 0);
        BEHelpers.WriteU16(ms, (ushort)xy.Count);
        BEHelpers.WriteU16(ms, (ushort)phosphorType);

        foreach (var (x, y) in xy)
        {
            BEHelpers.WriteU16Fixed16(ms, x);
            BEHelpers.WriteU16Fixed16(ms, y);
        }

        return ms.ToArray();
    }
}
