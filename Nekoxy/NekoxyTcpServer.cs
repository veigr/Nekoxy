using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekoxy
{
    /// <summary>
    /// 不具合修正のためにしぶしぶ継承
    /// </summary>
    internal class NekoxyTcpServer : TrotiNet.TcpServer
    {
        public NekoxyTcpServer(int listeningPort, bool isUseIPv6) : base(listeningPort, isUseIPv6) { }

        /// <summary>
        /// TrotiNet.TcpServer.Stop() では Keep-Alive な Socket の後始末が行われないのでとりあえずこれで
        /// </summary>
        public void Shutdown()
        {
            base.Stop();

            foreach (var socket in ConnectedSockets.Values.ToArray())
            {
                this.CloseSocket(socket);
            }
        }

        [Obsolete("Shutdow()メソッドを使用してください。", true)]
        public new void Stop() { }
    }
}
