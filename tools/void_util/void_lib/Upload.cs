using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace void_lib
{
    public class UploadResponse
    {
        public int status { get; set; }
        public string msg { get; set; }
        public string id { get; set; }
        public int[] sync { get; set; }
    }

    public class Upload : Progress<VoidProgress>
    {
        private Guid Id { get; set; } = Guid.NewGuid();
        private string UserAgent { get; set; }
        private string BaseHostname { get; set; }

        public Upload(string host = "v3.void.cat", string ua = "VoidLib/1.0")
        {
            BaseHostname = host;
            UserAgent = ua;
        }

        public Task<UploadResponse> UploadFileAsync(string file, byte[] key, byte[] iv)
        {
            var fi = new FileInfo(file);
            return UploadFileAsync(fi.OpenRead(), fi.Name, key, iv);
        }

        public async Task<UploadResponse> UploadFileAsync(Stream in_stream, string filename, byte[] key, byte[] iv)
        {
            base.OnReport(VoidProgress.Create(Id, label: "Starting.."));

            var site_info = await new VoidApi(BaseHostname).GetUploadHostAsync();
            base.OnReport(VoidProgress.Create(Id, log: $"Starting upload for: {filename} => {site_info.upload_host}\nUsing key: {key.ToHex()} and IV: {iv.ToHex()}"));

            var file_length = in_stream.Length;
            var header = JsonConvert.SerializeObject(new FileHeader()
            {
                name = filename,
                mime = "", // idk what to do with this haha, its not really important anyway since we dont preview in browser
                len = (ulong)file_length
            });

            base.OnReport(VoidProgress.Create(Id, log: $"Using header: {header}"));

            //unforutnatly we need to use a raw socket here because HttpWebRequest just bufferes forever
            //sad.. no good for large uploads
            var hosts = await Dns.GetHostAddressesAsync(site_info.upload_host);
            if (hosts.Length > 0)
            {
                var sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                var tcs = new TaskCompletionSource<bool>();
                var sae = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = new IPEndPoint(hosts[0], 443)
                };
                sae.Completed += (s, e) =>
                {
                    tcs.SetResult(true);
                };

                if (sock.ConnectAsync(sae))
                {
                    await tcs.Task;
                }

                using (var ssl_stream = new SslStream(new NetworkStream(sock)))
                {
                    await ssl_stream.AuthenticateAsClientAsync(site_info.upload_host);

                    var http_header = $"POST /upload HTTP/1.1\r\nHost: {site_info.upload_host}\r\nConnection: close\r\nContent-Type: application/octet-stream\r\nTransfer-Encoding: chunked\r\nUser-Agent: {UserAgent}\r\nTrailer: \r\nAccept-Encoding: 0\r\n\r\n";
                    var http_header_bytes = Encoding.UTF8.GetBytes(http_header);

                    await ssl_stream.WriteAsync(http_header_bytes, 0, http_header_bytes.Length);
                    await ssl_stream.FlushAsync();

                    using (var cs = new ChunkStream(ssl_stream, 16384, true))
                    {
                        //send the file data
                        byte[] hash;

                        //create hmac
                        base.OnReport(VoidProgress.Create(Id, label: "Hashing..."));
                        using (var hmac = HMAC.Create("HMACSHA256"))
                        {
                            hmac.Key = key;
                            hash = hmac.ComputeHash(in_stream);
                        }

                        base.OnReport(VoidProgress.Create(Id, log: $"HMAC is: {hash.ToHex()}"));
                        in_stream.Seek(0, SeekOrigin.Begin);

                        //write header to request stream
                        var vbf_buf = new byte[37];
                        vbf_buf[0] = 1;
                        Array.Copy(hash, 0, vbf_buf, 1, hash.Length);
                        var ts_buf = BitConverter.GetBytes((UInt32)DateTimeOffset.Now.ToUnixTimeSeconds());
                        Array.Copy(ts_buf, 0, vbf_buf, 33, ts_buf.Length);

                        await cs.WriteAsync(vbf_buf, 0, vbf_buf.Length);

                        base.OnReport(VoidProgress.Create(Id, label: "Uploading..."));
                        using (var aes = new AesManaged())
                        {
                            aes.Padding = PaddingMode.PKCS7;
                            aes.Mode = CipherMode.CBC;

                            using (var ds = aes.CreateEncryptor(key, iv))
                            {
                                var buf = new byte[ds.InputBlockSize * 64];
                                var out_buf = new byte[ds.OutputBlockSize * 64];

                                var header_bytes = Encoding.UTF8.GetBytes(header);
                                var hlb = BitConverter.GetBytes((UInt16)header_bytes.Length);
                                Array.Copy(hlb, buf, hlb.Length);
                                Array.Copy(header_bytes, 0, buf, 2, header_bytes.Length);

                                var init_offset = hlb.Length + header_bytes.Length;
                                long frlen = 0;
                                long tlen = 0;
                                while ((frlen = await in_stream.ReadAsync(buf, init_offset, buf.Length - init_offset)) > 0)
                                {
                                    var actual_rlen = (int)(init_offset + frlen);

                                    if (actual_rlen % ds.InputBlockSize != 0)
                                    {
                                        var last_block = ds.TransformFinalBlock(buf, 0, actual_rlen);
                                        await cs.WriteAsync(last_block, 0, last_block.Length);
                                    }
                                    else
                                    {
                                        var clen = ds.TransformBlock(buf, 0, actual_rlen, out_buf, 0);
                                        await cs.WriteAsync(out_buf, 0, clen);
                                    }

                                    //offset should always be 0 from after the first block
                                    if (init_offset != 0)
                                    {
                                        init_offset = 0;
                                    }
                                    tlen += frlen;
                                    base.OnReport(VoidProgress.Create(Id, percentage: tlen / (decimal)file_length, size: file_length));
                                }
                            }
                        }

                        //write end chunk
                        await cs.WriteAsync(new byte[0], 0, 0);
                        await cs.FlushAsync();
                    }

                    //fuck my life why am i doing this to mysefl..
                    var crlf = new byte[] { 13, 10, 13, 10 };
                    var sb_headers = new StringBuilder();
                    var rlen = 0;
                    var header_buff = new byte[256];
                    var header_end = 0;
                    while ((rlen = await ssl_stream.ReadAsync(header_buff, 0, header_buff.Length)) != 0)
                    {
                        if ((header_end = header_buff.IndexOf(crlf)) != -1)
                        {
                            sb_headers.Append(Encoding.UTF8.GetString(header_buff, 0, header_end + 4));
                            break;
                        }
                        else
                        {
                            sb_headers.Append(Encoding.UTF8.GetString(header_buff, 0, rlen));
                        }
                    }

                    var header_dict = sb_headers.ToString().Split('\n').Select(a =>
                    {
                        var i = a.IndexOf(":");
                        return i == -1 ? null : new string[] { a.Substring(0, i), a.Substring(i + 2) };
                    }).Where(a => a != null).ToDictionary(a => a[0].Trim(), b => b.Length > 1 ? b[1].Trim() : null);

                    if (header_dict.ContainsKey("Content-Length"))
                    {
                        using (var msb = new MemoryStream())
                        {
                            msb.Write(header_buff, header_end + 4, rlen - header_end - 4);
                            await ssl_stream.CopyToAsync(msb);

                            return JsonConvert.DeserializeObject<UploadResponse>(Encoding.UTF8.GetString(msb.ToArray()));
                        }
                    }
                    else
                    {
                        if (header_dict.ContainsKey("Transfer-Encoding") && header_dict["Transfer-Encoding"] == "chunked")
                        {
                            using (var msb = new MemoryStream())
                            {
                                using (var cr = new ChunkStream(ssl_stream))
                                {
                                    cr.PreLoadBuffer(header_buff, header_end + 4, rlen - header_end - 4);

                                    await cr.CopyToAsync(msb);
                                }

                                return JsonConvert.DeserializeObject<UploadResponse>(Encoding.UTF8.GetString(msb.ToArray()));
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
