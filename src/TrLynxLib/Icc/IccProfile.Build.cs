using System.Security.Cryptography;
using System.Text;

namespace TrLynxLib.Icc;

public partial class IccProfile
{
    // ===================================================================
    //                          PUBLIC API
    // ===================================================================

    /// <summary>Build the ICC profile in memory and return the raw bytes.</summary>
    public byte[] Build()
    {
        // 1. Build all tag-data blobs
        var blobs = new List<TagBlob>
        {
            new("desc", TagEncoders.EncodeMluc(DescriptionRecords), null),
            new("cprt", TagEncoders.EncodeMluc(CopyrightRecords),   null),
            new("wtpt", TagEncoders.EncodeXyzType(new[] { MediaWhitePointXyz }), null),
            new("chad", TagEncoders.EncodeSf32(ChromaticAdaptationMatrix), null),
            new("rXYZ", TagEncoders.EncodeXyzType(new[] { RedMatrixColumnXyz   }), null),
            new("bXYZ", TagEncoders.EncodeXyzType(new[] { BlueMatrixColumnXyz  }), null),
            new("gXYZ", TagEncoders.EncodeXyzType(new[] { GreenMatrixColumnXyz }), null),
        };

        byte[] rTrc = TagEncoders.EncodeTrc(RedTrc);
        byte[] gTrc = TagEncoders.EncodeTrc(GreenTrc);
        byte[] bTrc = TagEncoders.EncodeTrc(BlueTrc);

        bool shareTrc = ShareIdenticalTrcBlocks
                        && rTrc.SequenceEqual(gTrc)
                        && gTrc.SequenceEqual(bTrc);

        if (shareTrc)
        {
            blobs.Add(new("rTRC", rTrc, "shared_trc"));
            blobs.Add(new("gTRC", rTrc, "shared_trc"));
            blobs.Add(new("bTRC", rTrc, "shared_trc"));
        }
        else
        {
            blobs.Add(new("rTRC", rTrc, null));
            blobs.Add(new("gTRC", gTrc, null));
            blobs.Add(new("bTRC", bTrc, null));
        }

        blobs.Add(new("chrm",
            TagEncoders.EncodeChrm(PhosphorType, ChromaticityXy),
            null));

        // 2. Layout
        int nTags = blobs.Count;
        int tagTableSize = 4 + nTags * 12;
        int dataStart = (128 + tagTableSize + 3) & ~3;

        int cursor = dataStart;
        var placed = new Dictionary<string, (int Off, int Length)>();
        var entries = new List<TagEntry>(nTags);

        foreach (var b in blobs)
        {
            if (b.ShareKey != null && placed.TryGetValue(b.ShareKey, out var prior))
            {
                entries.Add(new TagEntry(b.Signature, prior.Off, prior.Length, b.Bytes, false));
            }
            else
            {
                int off = cursor;
                int length = b.Bytes.Length;
                entries.Add(new TagEntry(b.Signature, off, length, b.Bytes, true));
                if (b.ShareKey != null) placed[b.ShareKey] = (off, length);
                cursor = (cursor + length + 3) & ~3;
            }
        }

        int totalSize = cursor;

        // 3. Tag table
        using var tt = new MemoryStream();
        BEHelpers.WriteU32(tt, (uint)nTags);

        foreach (var e in entries)
        {
            BEHelpers.WriteSig(tt, e.Signature);
            BEHelpers.WriteU32(tt, (uint)e.Offset);
            BEHelpers.WriteU32(tt, (uint)e.Length);
        }

        // Pad tag table out to dataStart
        int padToData = (dataStart - 128) - (int)tt.Length;
        if (padToData > 0) tt.Write(new byte[padToData]);

        // 4. Data section
        var dataSection = new byte[totalSize - dataStart];

        foreach (var e in entries)
        {
            if (!e.IsFirstOccurrence) continue;
            Buffer.BlockCopy(e.Blob, 0, dataSection, e.Offset - dataStart, e.Length);
        }

        // 5. Header
        byte[] header = BuildHeader(totalSize);

        var profile = new byte[totalSize];
        Buffer.BlockCopy(header, 0, profile, 0, 128);
        Buffer.BlockCopy(tt.ToArray(), 0, profile, 128, (int)tt.Length);
        Buffer.BlockCopy(dataSection, 0, profile, dataStart, dataSection.Length);

        // 6. Profile ID (MD5), spec-defined
        if (ComputeProfileId)
        {
            byte[] forHash = (byte[])profile.Clone();

            // Zero profile flags (44..48), rendering intent (64..68), profile ID (84..100)
            Array.Clear(forHash, 44, 4);
            Array.Clear(forHash, 64, 4);
            Array.Clear(forHash, 84, 16);

            byte[] md5 = MD5.HashData(forHash);
            Buffer.BlockCopy(md5, 0, profile, 84, 16);
        }

        return profile;
    }

    // ===================================================================
    //                       INTERNAL: HEADER
    // ===================================================================

    private byte[] BuildHeader(int profileSize)
    {
        var (major, minor, bugfix) = ProfileVersion;
        byte versionByte2 = (byte)(((minor & 0xF) << 4) | (bugfix & 0xF));

        using var ms = new MemoryStream(128);
        BEHelpers.WriteU32(ms, (uint)profileSize);
        BEHelpers.WriteSig(ms, PreferredCmm);
        BEHelpers.WriteU8(ms, (byte)major);
        BEHelpers.WriteU8(ms, versionByte2);
        BEHelpers.WriteU16(ms, 0);                              // reserved
        BEHelpers.WriteSig(ms, ProfileClass);
        BEHelpers.WriteSig(ms, DataColorSpace);
        BEHelpers.WriteSig(ms, Pcs);
        BEHelpers.WriteDateTime(ms, CreationDateTime ?? DateTime.UtcNow);
        ms.Write(Encoding.ASCII.GetBytes("acsp"));        // file signature
        ms.Write(EncodePlatform(PrimaryPlatformVal));
        BEHelpers.WriteU32(ms, ProfileFlags);
        BEHelpers.WriteSig(ms, DeviceManufacturer);
        BEHelpers.WriteSig(ms, DeviceModel);
        BEHelpers.WriteU64(ms, DeviceAttributes);
        BEHelpers.WriteU32(ms, RenderingIntent);
        BEHelpers.WriteXyz(ms, PcsIlluminantXyz);
        BEHelpers.WriteSig(ms, ProfileCreator);
        ms.Write(new byte[16]);                           // Profile ID (filled later if requested)
        ms.Write(new byte[28]);                           // Reserved

        if (ms.Length != 128)
            throw new InvalidOperationException($"Header must be 128 bytes, got {ms.Length}");

        return ms.ToArray();
    }

    private static byte[] EncodePlatform(PrimaryPlatform primaryPlatform)
        => Encoding.ASCII.GetBytes(primaryPlatform switch
        {
            PrimaryPlatform.None => "",
            PrimaryPlatform.Apple => "APPL",
            PrimaryPlatform.Microsoft => "MSFT",
            PrimaryPlatform.SiliconGraphics => "SGI ",
            PrimaryPlatform.Sun => "SUNW",
            PrimaryPlatform.Taligent => "TGNT",
            _ => throw new ArgumentOutOfRangeException(nameof(primaryPlatform))
        });
}
