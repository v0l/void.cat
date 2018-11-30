using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace void_util
{
    public class BasicStats
    {
        public int Files { get; set; }
        public long Size { get; set; }
        public long Transfer_24h { get; set; }
    }

    public class SiteInfo
    {
        public long max_upload_size { get; set; }
        public BasicStats basic_stats { get; set; }
        public string upload_host { get; set; }
        public string geoip_info { get; set; }
    }

    public class Cmd
    {
        public string cmd { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool ok { get; set; }
        public object msg { get; set; }
        public T data { get; set; }
        public Cmd cmd { get; set; }
    }

    public class VoidApi
    {
        public static async Task<string> CallApiAsync(string cmd)
        {
            var req = (HttpWebRequest)WebRequest.Create($"https://{Program.BaseHostname}/api");
            req.Method = "POST";
            req.ContentType = "application/json";
            req.UserAgent = Program.UserAgent;

            var cmd_data = Encoding.UTF8.GetBytes(cmd);
            await (await req.GetRequestStreamAsync()).WriteAsync(cmd_data, 0, cmd_data.Length);

            var rsp = await req.GetResponseAsync();
            using (var sr = new StreamReader(rsp.GetResponseStream()))
            {
                return await sr.ReadToEndAsync();
            }
        }

        public static async Task<SiteInfo> GetUploadHostAsync()
        {
            return JsonConvert.DeserializeObject<ApiResponse<SiteInfo>>(await CallApiAsync(JsonConvert.SerializeObject(new Cmd()
            {
                cmd = "site_info"
            }))).data;
        }
    }
}
