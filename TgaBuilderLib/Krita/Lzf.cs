namespace TgaBuilderLib.Krita;

/// <summary>
/// liblzf-compatible LZF compressor.
/// Krita stores raster tiles with the "LZF" scheme, which is exactly this format.
/// Only compression is needed here; the produced stream is decodable by any liblzf
/// decompressor regardless of the hashing strategy used while compressing.
/// </summary>
internal static class Lzf
{
    private const int HLOG = 16;
    private const int HSIZE = 1 << HLOG;
    private const int MAX_LIT = 1 << 5;            // 32
    private const int MAX_OFF = 1 << 13;           // 8192
    private const int MAX_REF = (1 << 8) + (1 << 3); // 264

    /// <summary>
    /// Compresses <paramref name="inLen"/> bytes of <paramref name="input"/> into
    /// <paramref name="output"/>. Returns the number of bytes written, or 0 if the
    /// data did not fit into <paramref name="output"/> (i.e. it expanded).
    /// </summary>
    public static int Compress(byte[] input, int inLen, byte[] output)
    {
        if (inLen < 3)
        {
            // Not enough bytes to form a back-reference; emit as literals.
            return EmitAllLiterals(input, inLen, output);
        }

        int[] htab = new int[HSIZE];   // stores absolute positions; 0 == position 0 (excluded as ref)

        int outLen = output.Length;
        int ip = 0;
        int op = 0;
        int inEnd = inLen;

        int lit = 0;
        op++; // reserve control byte for the first literal run

        uint hval = (uint)((input[ip] << 8) | input[ip + 1]);

        while (ip < inEnd - 2)
        {
            hval = (hval << 8) | input[ip + 2];
            int hslot = (int)(((hval >> (3 * 8 - HLOG)) - hval * 5) & (HSIZE - 1));
            int reff = htab[hslot];
            htab[hslot] = ip;

            int off = ip - reff - 1;
            if (off < MAX_OFF
                && reff > 0                       // matches liblzf's "ref > in_data"
                && input[reff + 2] == input[ip + 2]
                && input[reff + 1] == input[ip + 1]
                && input[reff] == input[ip])
            {
                int len = 2;
                int maxlen = inEnd - ip - len;
                maxlen = maxlen > MAX_REF ? MAX_REF : maxlen;

                // Worst case we are about to write 3 control/data bytes + close a run.
                if (op + 3 + 1 >= outLen)
                {
                    if (op - (lit == 0 ? 1 : 0) + 3 + 1 >= outLen)
                        return 0;
                }

                output[op - lit - 1] = (byte)(lit - 1); // stop the current literal run
                if (lit == 0) op--;                      // undo run if it was empty

                do
                {
                    len++;
                }
                while (len < maxlen && input[reff + len] == input[ip + len]);

                len -= 2; // len is now (#matched octets - 1)... encoded value
                ip++;

                if (len < 7)
                {
                    output[op++] = (byte)((off >> 8) + (len << 5));
                }
                else
                {
                    output[op++] = (byte)((off >> 8) + (7 << 5));
                    output[op++] = (byte)(len - 7);
                }

                output[op++] = (byte)off;

                lit = 0;
                op++; // start a fresh literal run

                ip += len + 1;

                if (ip >= inEnd - 2)
                    break;

                // VERY_FAST: re-seed the hash table for the two positions before the
                // resume point (liblzf does two "--ip" here, not one), so that no input
                // byte is skipped when scanning resumes.
                ip -= 2;
                hval = (uint)((input[ip] << 8) | input[ip + 1]);

                hval = (hval << 8) | input[ip + 2];
                htab[(int)(((hval >> (3 * 8 - HLOG)) - hval * 5) & (HSIZE - 1))] = ip;
                ip++;

                hval = (hval << 8) | input[ip + 2];
                htab[(int)(((hval >> (3 * 8 - HLOG)) - hval * 5) & (HSIZE - 1))] = ip;
                ip++;
            }
            else
            {
                // literal byte
                if (op >= outLen)
                    return 0;

                lit++;
                output[op++] = input[ip++];

                if (lit == MAX_LIT)
                {
                    output[op - lit - 1] = (byte)(lit - 1); // stop run
                    lit = 0;
                    op++; // start run
                }
            }
        }

        if (op + 3 > outLen) // at most 3 bytes may still be needed
            return 0;

        while (ip < inEnd)
        {
            lit++;
            output[op++] = input[ip++];

            if (lit == MAX_LIT)
            {
                output[op - lit - 1] = (byte)(lit - 1);
                lit = 0;
                op++;
            }
        }

        output[op - lit - 1] = (byte)(lit - 1); // end run
        if (lit == 0) op--;                      // undo run if empty

        return op;
    }

    private static int EmitAllLiterals(byte[] input, int inLen, byte[] output)
    {
        int op = 0;
        int ip = 0;
        int lit = 0;
        op++; // control byte

        while (ip < inLen)
        {
            if (op >= output.Length) return 0;
            lit++;
            output[op++] = input[ip++];
            if (lit == MAX_LIT)
            {
                output[op - lit - 1] = (byte)(lit - 1);
                lit = 0;
                op++;
            }
        }

        output[op - lit - 1] = (byte)(lit - 1);
        if (lit == 0) op--;
        return op;
    }
}