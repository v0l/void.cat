using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace void_util
{
    public class ChunkStream : Stream, IDisposable
    {
        private static byte[] CRLF = new byte[] { 13, 10 };

        private bool LeaveOpen { get; set; } = false;
        private byte[] InternalBuffer { get; set; }
        private Stream BaseStream { get; set; }
        private int ReadingChunkSize { get; set; } = -1;
        private int ReadOffset { get; set; } = 0;
        private int LoadOffset { get; set; } 
        private int Loaded { get; set; }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length => Loaded - ReadOffset;

        public override long Position { get => 0; set { ; } }

        public override void Flush()
        {
            BaseStream.Flush();
        }
        
        /// <summary>
        /// Adds data to the start of the read buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void PreLoadBuffer(byte[] buffer, int offset, int count)
        {
            if(count > InternalBuffer.Length)
            {
                throw new Exception("Cant preload data larger than our buffer");
            }

            Array.Copy(buffer, offset, InternalBuffer, 0, count);

            Loaded += count;
            LoadOffset += count;
        }

        private async Task<bool> BufferSomeAsync(CancellationToken cancellationToken)
        {
            if(Loaded == InternalBuffer.Length)
            {
                return true;
            }

            var rlen = await BaseStream.ReadAsync(InternalBuffer, LoadOffset, InternalBuffer.Length - Loaded, cancellationToken);
            if(rlen != 0)
            {
                Loaded += rlen;
                LoadOffset += rlen;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool BufferSome()
        {
            if (Loaded == InternalBuffer.Length)
            {
                return true;
            }

            var rlen = BaseStream.Read(InternalBuffer, LoadOffset, InternalBuffer.Length - Loaded);
            if (rlen != 0)
            {
                Loaded += rlen;
                return true;
            }
            else
            {
                return false;
            }
        }

        private int ParseChunks(byte[] data, int offset, int count)
        {
            //prepare internal buffer for copying
            if (ReadingChunkSize == -1)
            {
                var clen_end = InternalBuffer.IndexOf(CRLF, ReadOffset, Loaded - ReadOffset);
                var hex_len = Encoding.ASCII.GetString(InternalBuffer, ReadOffset, clen_end - ReadOffset + 2);
                ReadingChunkSize = Convert.ToInt32(hex_len.Trim(), 16);
                ReadOffset += 2 + clen_end - ReadOffset;
            }

            var sending_data = Math.Min(count, ReadingChunkSize <= Loaded - ReadOffset ? ReadingChunkSize : Loaded);
            Array.Copy(InternalBuffer, ReadOffset, data, offset, sending_data);

            ReadOffset += sending_data;

            //did we send all of the chunk this time, if so expect CRLF and reset chunk read size
            if (sending_data == ReadingChunkSize)
            {
                ReadingChunkSize = -1;
                ReadOffset += 2;
            }

            //if we moved all our buffer then reset read to start of buffer
            if(ReadOffset == Loaded)
            {
                LoadOffset = 0;
                ReadOffset = 0;
                Loaded = 0;
            }

            //do we still have some data left on this chunk
            //adjust the chunk size so we will copy the rest next time
            if(sending_data < ReadingChunkSize)
            {
                ReadingChunkSize -= sending_data;
            }

            return sending_data;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if(await BufferSomeAsync(cancellationToken) || Length > 0)
            {
                return ParseChunks(buffer, offset, count);
            }
            else
            {
                return 0;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (BufferSome() || Length > 0)
            {
                return ParseChunks(buffer, offset, count);
            }
            else
            {
                return 0;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var chunk_len = Encoding.ASCII.GetBytes($"{count.ToString("X")}\r\n");
            await BaseStream.WriteAsync(chunk_len, 0, chunk_len.Length, cancellationToken);
            if (count > 0)
            {
                await BaseStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
            await BaseStream.WriteAsync(CRLF, 0, CRLF.Length, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var chunk_len = Encoding.ASCII.GetBytes($"{count.ToString("X")}\r\n");
            BaseStream.Write(chunk_len, 0, chunk_len.Length);
            if (count > 0)
            {
                BaseStream.Write(buffer, offset, count);
            }
            BaseStream.Write(CRLF, 0, CRLF.Length);
        }

        protected override void Dispose(bool disposing)
        {
            if (!LeaveOpen)
            {
                BaseStream.Dispose();
            }
            base.Dispose(disposing);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return BaseStream.FlushAsync(cancellationToken);
        }
        
        public ChunkStream(Stream stream, int bufferSize = 16 * 1024, bool leaveOpen = false)
        {
            BaseStream = stream;
            InternalBuffer = new byte[bufferSize];
            LeaveOpen = leaveOpen;
        }
    }
}
