#region Licence
/*
Copyright (c) 2013, Darren Horrocks
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace TrLynxLib.Psd;

internal static class RleHelper
{
    internal static int EncodedRow(
        Stream stream,
        byte[] imgData,
        int startIdx,
        int columns)
    {
        int remaining = columns;
        int src = startIdx;

        long startPos = stream.Position;

        while (remaining > 0)
        {
            int i = 0;

            while (i < 128
                   && (remaining > i)
                   && imgData[src] == imgData[src + i])
            {
                i++;
            }

            if (i > 1)
            {
                // Run found
                stream.WriteByte((byte)(-(i - 1)));
                stream.WriteByte(imgData[src]);

                src += i;
                remaining -= i;
            }
            else
            {
                // Search literal block
                i = 0;

                while (i < 128 && (remaining - (i + 1) > 0)
                      && (imgData[src + i] != imgData[src + i + 1]
                      || remaining <= (i + 2)
                      || imgData[src + i] != imgData[src + i + 2]))
                {
                    i++;
                }

                if (remaining == 1)
                    i = 1;

                if (i > 0)
                {
                    stream.WriteByte((byte)(i - 1));

                    for (int j = 0; j < i; j++)
                        stream.WriteByte(imgData[src + j]);

                    src += i;
                    remaining -= i;
                }
            }
        }

        return (int)(stream.Position - startPos);
    }

    internal static void DecodedRow(Stream stream, byte[] imgData, int startIdx, int columns, int compressedLength = -1)
    {
        Stream targetStream = stream;
        MemoryStream? memStream = null;

        // If compressedLength >= 0, we have a hint that this row is compressed and how many bytes belong to it.
        if (compressedLength > 0)
        {
            byte[] compressedBuffer = new byte[compressedLength];

            // Read exactly compressedLength bytes from the stream into the buffer,
            // handling cases where Read might return less than requested
            int bytesRead = 0;
            while (bytesRead < compressedLength)
            {
                int read = stream.Read(compressedBuffer, bytesRead, compressedLength - bytesRead);

                if (read == 0)
                    break; // End of stream reached before we got all the data we expectedsS

                bytesRead += read;
            }

            // We feed the compressed data into a MemoryStream, so that we can read it as if it were a normal stream.
            memStream = new MemoryStream(compressedBuffer);
            targetStream = memStream;
        }

        try
        {
            int count = 0;
            while (count < columns)
            {
                byte byteValue = (byte)targetStream.ReadByte();

                int len = byteValue;
                if (len < 128)
                {
                    len++;
                    while (len != 0 && startIdx + count < imgData.Length)
                    {
                        byteValue = (byte)targetStream.ReadByte();

                        imgData[startIdx + count] = byteValue;
                        count++;
                        len--;
                    }
                }
                else if (len > 128)
                {
                    // Next -len+1 bytes in the dest are replicated from next source byte.
                    // (Interpret len as a negative 8-bit int.)
                    len ^= 0x0FF;
                    len += 2;
                    byteValue = (byte)targetStream.ReadByte();

                    while (len != 0 && startIdx + count < imgData.Length)
                    {
                        imgData[startIdx + count] = byteValue;
                        count++;
                        len--;
                    }
                }
                else if (len == 128)
                {
                    // Do nothing
                }
            }
        }
        finally
        {
            // Important: If we created a MemoryStream, we need to dispose it to free resources.
            // If we didn't create one, this will just be a no-op.
            memStream?.Dispose();
        }
    }
}