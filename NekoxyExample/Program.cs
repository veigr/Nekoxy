using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nekoxy;

namespace NekoxyExample
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpProxy.Startup(12345);
            //HttpProxy.UpstreamProxyHost = "127.0.0.1";
            //HttpProxy.UpstreamProxyPort = 8888;
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Console.WriteLine(s.Request.RequestLine));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Console.WriteLine(s.Response.StatusLine));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Debug.WriteLine(s.Request.RequestLine));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Debug.WriteLine(s.Response.StatusLine));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Debug.WriteLine(s.Response.BodyAsString));
            while (true) System.Threading.Thread.Sleep(1000);
        }
    }
}
