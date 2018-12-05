using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace void_lib
{
    public static class Ext
    {
        public static byte[] FromHex(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string ToHex(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty).ToLower();
        }

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
