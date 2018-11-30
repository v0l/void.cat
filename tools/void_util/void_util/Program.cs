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

namespace void_util
{
    class Program
    {
        public static string BaseHostname => "v3.void.cat";
        public static string UserAgent => "VoidUtil/1.0";

        static void PrintHelp()
        {
            Console.WriteLine($@"
Usage: void_util [MODE] [FILE|URL]

Modes:
    upload      Upload a file to {BaseHostname}
    download    Downloads a file and decrypts it
    pack        Packs a file into VBF format (can be useful for uploading with curl or another program)

");
        }

        static Task Main(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[0])
                {
                    case "upload":
                        {
                            using (var rng = new RNGCryptoServiceProvider())
                            {
                                var key = new byte[16];
                                var iv = new byte[16];

                                rng.GetBytes(key);
                                rng.GetBytes(iv);

                                return UploadFileAsync(args[1], key, iv);
                            }
                        }
                    case "download":
                        {
                            return DownloadFileAsync(args[1]);
                        }
                    case "pack":
                        {
                            Console.WriteLine("Mode not implemented yet, please check github for updates");
                            break;
                        }
                    default:
                        {
                            Console.WriteLine($"Unknown mode: {args[0]}");
                            PrintHelp();
                            break;
                        }
                }


                return Task.CompletedTask;
            }
            else
            {
                PrintHelp();
                return Task.CompletedTask;
            }
        }

        public static byte[] FromHex(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private static string ToHex(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty).ToLower();
        }

        private static async Task UploadFileAsync(string filename, byte[] key, byte[] iv)
        {
            if (File.Exists(filename))
            {
                var file_info = new FileInfo(filename);

                var site_info = await VoidApi.GetUploadHostAsync();
                Console.WriteLine($"Starting upload for: {file_info.Name} => {site_info.upload_host}\nUsing key: {ToHex(key)} and IV: {ToHex(iv)}");

                var file_length = file_info.Length;
                var header = JsonConvert.SerializeObject(new FileHeader()
                {
                    name = file_info.Name,
                    mime = "", // idk what to do with this haha, its not really important anyway since we dont preview in browser
                    len = (ulong)file_length
                });

                Console.WriteLine($"Using header: {header}");

                // unforutnatly we need to use a raw socket here because HttpWebRequest just bufferes forever
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
                            using (var fs = file_info.OpenRead())
                            {
                                byte[] hash;

                                //create hmac
                                Console.WriteLine("Hashing...");
                                using (var hmac = HMAC.Create("HMACSHA256"))
                                {
                                    hmac.Key = key;
                                    hash = hmac.ComputeHash(fs);
                                }

                                Console.WriteLine($"Hash is {ToHex(hash)}");
                                fs.Seek(0, SeekOrigin.Begin);

                                //write header to request stream
                                var vbf_buf = new byte[37];
                                vbf_buf[0] = 1;
                                Array.Copy(hash, 0, vbf_buf, 1, hash.Length);
                                var ts_buf = BitConverter.GetBytes((UInt32)DateTimeOffset.Now.ToUnixTimeSeconds());
                                Array.Copy(ts_buf, 0, vbf_buf, 33, ts_buf.Length);

                                await cs.WriteAsync(vbf_buf, 0, vbf_buf.Length);

                                Console.WriteLine("Encrypting and Uploading...");
                                using (var aes = new AesManaged())
                                {
                                    aes.Padding = PaddingMode.PKCS7;
                                    aes.Mode = CipherMode.CBC;

                                    using (var ds = aes.CreateEncryptor(key, iv))
                                    {
                                        var buf = new byte[ds.InputBlockSize * 1000];
                                        var out_buf = new byte[ds.OutputBlockSize * 1000];

                                        var header_bytes = Encoding.UTF8.GetBytes(header);
                                        var hlb = BitConverter.GetBytes((UInt16)header_bytes.Length);
                                        Array.Copy(hlb, buf, hlb.Length);
                                        Array.Copy(header_bytes, 0, buf, 2, header_bytes.Length);

                                        var init_offset = hlb.Length + header_bytes.Length;
                                        long frlen = 0;
                                        long tlen = 0;
                                        while ((frlen = await fs.ReadAsync(buf, init_offset, buf.Length - init_offset)) > 0)
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
                                            Console.Write($"\r{(100 * (tlen / (decimal)file_length)).ToString("000.0")}%");
                                        }
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

                                PrintUploadResult(msb.ToArray(), key, iv);
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

                                    PrintUploadResult(msb.ToArray(), key, iv);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("\nError: file not found!");
            }
        }


        private static void PrintUploadResult(byte[] data, byte[] key, byte[] iv)
        {
            var json_data = Encoding.UTF8.GetString(data);
            try
            {
                var rsp = JsonConvert.DeserializeObject<UploadResponse>(json_data);
                if (rsp != null)
                {
                    if (rsp.status == 200)
                    {
                        Console.WriteLine($"\nUpload complete!\nUrl: https://{BaseHostname}/#{rsp.id}:{ToHex(key)}:{ToHex(iv)}");
                    }
                    else
                    {
                        Console.WriteLine($"\nUpload error: {rsp.msg}");
                    }
                }
                else
                {
                    Console.WriteLine($"\nGot invalid response: {json_data}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got unknown response: \n{json_data}");
            }
        }

        private static async Task DownloadFileAsync(string url)
        {
            var url_base = new Uri(url);
            var hash_frag = url_base.Fragment.Substring(1).Split(':');

            Console.WriteLine($"Starting download for: {hash_frag[0]}");

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

                Console.WriteLine($"Blob version is {version}, HMAC is {ToHex(hmac_data)}");

                var tmp_name = Path.GetTempFileName();
                string real_name = null;
                using (var tmp_file = new FileStream(tmp_name, FileMode.Open, FileAccess.ReadWrite))
                {
                    using (var aes = new AesManaged())
                    {
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Mode = CipherMode.CBC;

                        using (var ds = aes.CreateDecryptor(FromHex(hash_frag[1]), FromHex(hash_frag[2])))
                        {
                            var buf = new byte[ds.InputBlockSize * 1024];
                            var out_buf = new byte[ds.OutputBlockSize * 1024];

                            bool first_block = true;
                            int read_offset = 0;
                            int last_rlen = 0;
                            long t_len = 0;
                            while (true)
                            {
                                var rlen = await rsp_stream.ReadAsync(buf, read_offset, buf.Length - read_offset);

                                //end do final block
                                if (rlen == 0)
                                {
                                    var last_buf = ds.TransformFinalBlock(buf, 0, last_rlen);
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
                                        last_rlen = rlen;
                                        read_offset = 0;
                                    }
                                }

                                var clen = ds.TransformBlock(buf, 0, rlen, out_buf, 0);
                                if (first_block)
                                {
                                    first_block = false;
                                    var hlen = BitConverter.ToUInt16(out_buf, 0);
                                    var header = Encoding.UTF8.GetString(out_buf, 2, hlen);
                                    Console.WriteLine($"Header is: {header}");

                                    var header_obj = JsonConvert.DeserializeObject<FileHeader>(header);
                                    real_name = header_obj.name;

                                    var file_start = 2 + hlen;
                                    await tmp_file.WriteAsync(out_buf, file_start, clen - file_start);
                                }
                                else
                                {
                                    await tmp_file.WriteAsync(out_buf, 0, clen);
                                }

                                t_len += rlen;

                                Console.Write($"\r{(100 * (t_len / (decimal)file_length)).ToString("000.0")}%");
                            }
                        }
                    }

                    tmp_file.Seek(0, SeekOrigin.Begin);

                    using (var hmac = HMAC.Create("HMACSHA256"))
                    {
                        var hmac_test = hmac.ComputeHash(tmp_file);

                        if (ToHex(hmac_test) == ToHex(hmac_data))
                        {
                            Console.WriteLine("HMAC verified!");
                        }
                        else
                        {
                            throw new Exception($"HMAC verify failed.. {ToHex(hmac_test)} != {ToHex(hmac_data)}");
                        }
                    }
                }

                //file is downloaded to temp path, move it now
                var out_file = Path.Combine(Directory.GetCurrentDirectory(), real_name);
                Console.WriteLine($"\nMoving file to {out_file}");
                File.Move(tmp_name, out_file);
            }

            Console.WriteLine("\nDone!");
        }
    }

    internal class FileHeader
    {
        public string name { get; set; }
        public string mime { get; set; }
        public ulong len { get; set; }
    }

    internal class FileData
    {
        public FileHeader Header { get; set; }
        public string Hmac { get; set; }
        public byte Version { get; set; }
        public DateTime Uploaded { get; set; }
        public byte[] EncryptedPayload { get; set; }
    }

    internal class UploadResponse
    {
        public int status { get; set; }
        public string msg { get; set; }
        public string id { get; set; }
        public int[] sync { get; set; }
    }
}
