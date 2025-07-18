using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Level
{
    public class SubStream : Stream
    {
        private readonly Stream _base;
        private readonly long _start;
        private readonly long _length;
        private long _position;

        public SubStream(Stream baseStream, uint length)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _start = baseStream.Position;
            _length = length;
            _position = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length) return 0;

            long remaining = _length - _position;
            if (count > remaining)
                count = (int)remaining;

            int bytesRead = _base.Read(buffer, offset, count);
            _position += bytesRead;
            return bytesRead;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

}
