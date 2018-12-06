using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace void_lib
{
    public class DownloadResult
    {
        public string Filepath { get; set; }
        public FileHeader Header { get; set; }
    }

    public class Download : Progress<VoidProgress>
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public FileHeader Header { get; private set; }
        private string UserAgent { get; set; }
        private Stopwatch Timer { get; set; }

        public decimal CalculatedSpeed { get; private set; }

        public Download(string ua = "VoidUtil/1.0")
        {
            UserAgent = ua;
        }

        /// <summary>
        /// Downloads a file and returns the temp file name and the FileHeader associated with the file
        /// </summary>
        /// <param name="url">Full url including key and iv</param>
        /// <returns></returns>
        public async Task<DownloadResult> DownloadFileAsync(string url)
        {
            Timer = Stopwatch.StartNew();

            var url_base = new Uri(url);
            var hash_frag = url_base.Fragment.Substring(1).Split(':');
            var key = hash_frag[1].FromHex();
            var iv = hash_frag[2].FromHex();

            base.OnReport(VoidProgress.Create(Id, label: $"Starting..."));
            base.OnReport(VoidProgress.Create(Id, log: $"Starting download for: {hash_frag[0]}"));

            var req = (HttpWebRequest)WebRequest.Create($"{url_base.Scheme}://{url_base.Host}/{hash_frag[0]}");
            req.UserAgent = UserAgent;

            var rsp = await req.GetResponseAsync();
            var file_length = rsp.ContentLength;
            using (var rsp_stream = rsp.GetResponseStream())
            {
                var version = rsp_stream.ReadByte();
                var hmac_data = new byte[32];
                var ts = new byte[4];
                await rsp_stream.ReadAsync(hmac_data, 0, hmac_data.Length);
                await rsp_stream.ReadAsync(ts, 0, ts.Length);

                base.OnReport(VoidProgress.Create(Id, log: $"Blob version is {version}, HMAC is {hmac_data.ToHex()}"));

                var tmp_name = Path.GetTempFileName();
                using (var tmp_file = new FileStream(tmp_name, FileMode.Open, FileAccess.ReadWrite))
                {
                    using (var aes = new AesManaged())
                    {
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Mode = CipherMode.CBC;

                        using (var ds = aes.CreateDecryptor(key, iv))
                        {
                            var buf = new byte[ds.InputBlockSize * 1024];
                            var out_buf = new byte[ds.OutputBlockSize * 1024];

                            bool first_block = true;
                            int read_offset = 0;
                            long t_len = 0;
                            while (true)
                            {
                                var rlen = await rsp_stream.ReadAsync(buf, read_offset, buf.Length - read_offset);

                                //end do final block
                                if (rlen == 0)
                                {
                                    var last_buf = ds.TransformFinalBlock(buf, 0, read_offset);
                                    await tmp_file.WriteAsync(last_buf, 0, last_buf.Length);
                                    break;
                                }
                                else
                                {
                                    if ((read_offset + rlen) % ds.InputBlockSize != 0)
                                    {
                                        read_offset += rlen;
                                        continue;
                                    }
                                    else
                                    {
                                        rlen += read_offset;
                                        read_offset = 0;
                                    }
                                }

                                var clen = ds.TransformBlock(buf, 0, rlen, out_buf, 0);
                                if (first_block)
                                {
                                    first_block = false;
                                    var hlen = BitConverter.ToUInt16(out_buf, 0);
                                    var header = Encoding.UTF8.GetString(out_buf, 2, hlen);
                                    base.OnReport(VoidProgress.Create(Id, log: $"Header is: {header}"));

                                    Header = JsonConvert.DeserializeObject<FileHeader>(header);

                                    var file_start = 2 + hlen;
                                    await tmp_file.WriteAsync(out_buf, file_start, clen - file_start);
                                }
                                else
                                {
                                    await tmp_file.WriteAsync(out_buf, 0, clen);
                                }

                                t_len += rlen;

                                base.OnReport(VoidProgress.Create(Id, percentage: t_len / (decimal)file_length, size: file_length));
                                CalculatedSpeed = (decimal)(t_len / Timer.Elapsed.TotalSeconds);
                            }
                        }
                    }
                    base.OnReport(VoidProgress.Create(Id, percentage: 1));

                    tmp_file.Seek(0, SeekOrigin.Begin);

                    using (var hmac = HMAC.Create("HMACSHA256"))
                    {
                        hmac.Key = key;

                        var hmac_test = hmac.ComputeHash(tmp_file);

                        if (hmac_test.ToHex() == hmac_data.ToHex())
                        {
                            base.OnReport(VoidProgress.Create(Id, log: "HMAC verified!"));
                        }
                        else
                        {
                            throw new Exception($"HMAC verify failed.. {hmac_test.ToHex()} != {hmac_data.ToHex()}");
                        }
                    }
                }

                //file is downloaded to temp path, move it now
                return new DownloadResult()
                {
                    Filepath = tmp_name,
                    Header = Header
                };
            }
        }
    }
}
