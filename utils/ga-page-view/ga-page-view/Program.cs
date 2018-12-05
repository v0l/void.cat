using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ga_page_view
{
    class Program
    {
        static ConnectionMultiplexer c { get; set; }
        static BatchBlock<string> _queue = new BatchBlock<string>(20);
        static string Token { get; set; }
        static string Channel { get; set; }

        static Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Required args: channel token_auth");
                return Task.CompletedTask;
            }

            Channel = args[0];
            Token = args[1];

            Console.WriteLine($"Token is: {Token}\nChannel is: {Channel}");
            return startSvc();
        }

        private static async Task startSvc()
        {
            c = await ConnectionMultiplexer.ConnectAsync("localhost");
            await c.GetSubscriber().SubscribeAsync(Channel, queueMsg);

            Console.WriteLine("Connected to redis");

            _queue.LinkTo(new ActionBlock<string[]>(async (r) =>
            {
                Console.WriteLine("Sending stats");
                await SendData(r);
            }));

            await _queue.Completion;
        }

        private static void queueMsg(RedisChannel a, RedisValue b)
        {
            try
            {
                Console.Write("."); //tick
                _queue.Post(b.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Queue msg failed.. {ex.ToString()}");
            }
        }

        private static async Task SendData(string[] payload)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("https://matomo.trash.lol/piwik.php");
                req.Method = "POST";
                using (StreamWriter sw = new StreamWriter(await req.GetRequestStreamAsync()))
                {
                    await sw.WriteAsync(JsonConvert.SerializeObject(new BulkStats
                    {
                        requests = payload,
                        token_auth = Token
                    }));
                }

                using (var rsp = (HttpWebResponse)await req.GetResponseAsync())
                {
                    using (StreamReader sr = new StreamReader(rsp.GetResponseStream()))
                    {
                        var rsp_json = await sr.ReadToEndAsync();
                        Console.WriteLine($"Got reponse from analytics: {rsp_json}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending stats {ex.ToString()}");
            }
        }
    }

    internal class BulkStats
    {
        public string[] requests { get; set; }
        public string token_auth { get; set; }
    }
}