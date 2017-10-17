using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nekoxy;

namespace NekoxyExample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HttpProxy.Shutdown();
            //HttpProxy.UpstreamProxyConfig = new ProxyConfig(ProxyConfigType.SpecificProxy, "127.0.0.1", 8888);
            HttpProxy.Startup(12345, false, false);
            HttpProxy.AfterReadRequestHeaders += r => Task.Run(() => Console.WriteLine(r));
            HttpProxy.AfterReadResponseHeaders += r => Task.Run(() => Console.WriteLine(r));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Debug.WriteLine(s));
            //HttpProxy.AfterSessionComplete += s => Task.Run(() => Debug.WriteLine(s.Response.BodyAsString));
            while (true) System.Threading.Thread.Sleep(1000);
        }
    }
}
