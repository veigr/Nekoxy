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
            HttpProxy.IsEnableUpstreamProxy = true;
            HttpProxy.UpstreamProxyHost = null;
            HttpProxy.UpstreamProxyPort = 8888;
            HttpProxy.Startup(12345, false, false);
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Console.WriteLine(s.Request.RequestLine));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Console.WriteLine(s.Response.StatusLine));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Debug.WriteLine(s.Request.RequestLine));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Debug.WriteLine(s.Response.StatusLine));
            HttpProxy.AfterSessionComplete += s => Task.Run(() => Debug.WriteLine(s.Response.BodyAsString));
            while (true) System.Threading.Thread.Sleep(1000);
        }
    }
}
