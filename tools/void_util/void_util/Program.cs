using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using void_lib;

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

        private static void VoidProgress(object sender, VoidProgress vp)
        {
            switch (vp)
            {
                case LogVoidProgress l:
                    {
                        Console.WriteLine(l.Log);
                        break;
                    }
                case LabelVoidProgress l:
                    {
                        Console.WriteLine(l.Label);
                        break;
                    }
                case PercentageVoidProgress p:
                    {
                        Console.Write($"\r{(100 * p.Percentage).ToString("000.00")}%");
                        break;
                    }
            }
        }

        private static async Task DownloadFileAsync(string url)
        {
            var dl = new Download(ua: UserAgent);
            dl.ProgressChanged += VoidProgress;

            var res = await dl.DownloadFileAsync(url);

            if(res != null)
            {
                var out_file = Path.Combine(Directory.GetCurrentDirectory(), res.Header.name);
                Console.WriteLine($"\nMoving file to {out_file}");
                File.Move(res.Filepath, out_file);
            }
        }

        private static async Task UploadFileAsync(string filename, byte[] key, byte[] iv)
        {
            var up = new Upload(host: BaseHostname, ua: UserAgent);
            up.ProgressChanged += VoidProgress;

            var rsp = await up.UploadFileAsync(filename, key, iv);
            if (rsp != null)
            {
                if (rsp.status == 200)
                {
                    Console.WriteLine($"\nUpload complete!\nUrl: https://{BaseHostname}/#{rsp.id}:{key.ToHex()}:{iv.ToHex()}");
                }
                else
                {
                    Console.WriteLine($"\nUpload error: {rsp.msg}");
                }
            }
        }        
    }

    internal class FileData
    {
        public FileHeader Header { get; set; }
        public string Hmac { get; set; }
        public byte Version { get; set; }
        public DateTime Uploaded { get; set; }
        public byte[] EncryptedPayload { get; set; }
    }
}
