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
        static BatchBlock<string> _queue = new BatchBlock<string>(20);

        static void Main(string[] args)
        {
            var mt = startSvc();
            mt.Wait();
        }

        private static async Task startSvc()
        {
            var c = await ConnectionMultiplexer.ConnectAsync("localhost");
            await c.GetSubscriber().SubscribeAsync("ga-page-view", (a, b) =>
            {
                _queue.Post(b.ToString());
            });

            Console.WriteLine("Connected to redis");
            await sendStats();
        }

        private static async Task sendStats()
        {
            while (true)
            {
                var r = await _queue.ReceiveAsync();
                if (r != null)
                {
                    Console.WriteLine("Sending stats..");
                    await SendData(r);
                }
            }
        }


        private static async Task SendData(string[] payload)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("https://www.google-analytics.com/batch");
                req.Method = "POST";
                using (StreamWriter sw = new StreamWriter(await req.GetRequestStreamAsync()))
                {
                    await sw.WriteAsync(string.Join("\r\n", payload));
                }

                var rsp = (HttpWebResponse)await req.GetResponseAsync();
                if (rsp.StatusCode != HttpStatusCode.OK)
                {
                    using (StreamReader sr = new StreamReader(rsp.GetResponseStream()))
                    {
                        Console.WriteLine($"Got error reponse from analytics: {await sr.ReadToEndAsync()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending stats {ex.ToString()}");
            }
        }
    }
}