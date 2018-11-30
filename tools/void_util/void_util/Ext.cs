using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace void_util
{
    public static class Ext
    {
        public static async Task CopyToAsync(this Stream in_stream, Stream out_stream, Action<long> progress)
        {
            long total = 0;
            var buff = new byte[1024];
            int rlen = 0;
            while((rlen = await in_stream.ReadAsync(buff, 0, buff.Length)) != 0)
            {
                await out_stream.WriteAsync(buff, 0, rlen);
                total += rlen;
                progress(total);
            }
        }

        public static int IndexOf(this byte[] data, byte[] seq, int offset = 0, int length = -1)
        {
            if(length == -1)
            {
                length = data.Length;
            }

            if(offset + length > data.Length)
            {
                throw new IndexOutOfRangeException();
            }

            for(var x = offset; x < offset + length; x++)
            {
                bool checkpos = true;
                for (var y = 0;y < seq.Length; y++)
                {
                    if(data[x+y] != seq[y])
                    {
                        checkpos = false;
                        break;
                    }
                }

                if (checkpos)
                {
                    return x;
                }
            }

            return -1;
        }
    }
}
