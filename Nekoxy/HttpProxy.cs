using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrotiNet;

namespace Nekoxy
{
    /// <summary>
    /// HTTPプロキシサーバー。
    /// HTTPプロトコルにのみ対応し、HTTPS等はサポートしない。
    /// </summary>
    public static class HttpProxy
    {
        private static TcpServer server;

        /// <summary>
        /// HTTPレスポンスをプロキシ クライアントに送信完了した際に発生。
        /// </summary>
        public static event Action<Session> AfterSessionComplete;

        /// <summary>
        /// リクエストヘッダを読み込み完了した際に発生。
        /// ボディは受信前。
        /// </summary>
        public static event Action<HttpRequest> AfterReadRequestHeaders;

        /// <summary>
        /// レスポンスヘッダを読み込み完了した際に発生。
        /// ボディは受信前。
        /// </summary>
        public static event Action<HttpResponse> AfterReadResponseHeaders;

        /// <summary>
        /// 上流プロキシ設定。
        /// </summary>
        public static ProxyConfig UpstreamProxyConfig
        {
            get { return TransparentProxyLogic.UpstreamProxyConfig; }
            set { TransparentProxyLogic.UpstreamProxyConfig = value; }
        }

        /// <summary>
        /// プロキシサーバーが Listening 中かどうかを取得。
        /// </summary>
        public static bool IsInListening => server != null && server.IsListening;

        /// <summary>
        /// 指定ポートで Listening を開始する。
        /// Shutdown() を呼び出さずに2回目の Startup() を呼び出した場合、InvalidOperationException が発生する。
        /// </summary>
        /// <param name="listeningPort">Listeningするポート。</param>
        /// <param name="useIpV6">falseの場合、127.0.0.1で待ち受ける。trueの場合、::1で待ち受ける。既定false。</param>
        /// <param name="isSetProxyInProcess">trueの場合、プロセス内IEプロキシの設定を実施し、HTTP通信をNekoxyに向ける。既定true。</param>
        public static void Startup(int listeningPort, bool useIpV6 = false, bool isSetProxyInProcess = true)
        {
            if (server != null) throw new InvalidOperationException("Calling Startup() twice without calling Shutdown() is not permitted.");

            TransparentProxyLogic.AfterSessionComplete += InvokeAfterSessionComplete;
            TransparentProxyLogic.AfterReadRequestHeaders += InvokeAfterReadRequestHeaders;
            TransparentProxyLogic.AfterReadResponseHeaders += InvokeAfterReadResponseHeaders;
            ListeningPort = listeningPort;
            try
            {
                if (isSetProxyInProcess)
                    WinInetUtil.SetProxyInProcessForNekoxy(listeningPort);

                server = new TcpServer(listeningPort, useIpV6);
                server.Start(TransparentProxyLogic.CreateProxy);
                server.InitListenFinished.WaitOne();
                if (server.InitListenException != null) throw server.InitListenException;
            }
            catch (Exception)
            {
                Shutdown();
                throw;
            }
        }

        /// <summary>
        /// Listening しているスレッドを終了し、ソケットを閉じる。
        /// </summary>
        public static void Shutdown()
        {
            TransparentProxyLogic.AfterSessionComplete -= InvokeAfterSessionComplete;
            TransparentProxyLogic.AfterReadRequestHeaders -= InvokeAfterReadRequestHeaders;
            TransparentProxyLogic.AfterReadResponseHeaders -= InvokeAfterReadResponseHeaders;
            server?.Stop();
            server = null;
        }

        internal static int ListeningPort { get; set; }

        private static void InvokeAfterSessionComplete(Session session)
            => AfterSessionComplete?.Invoke(session);

        private static void InvokeAfterReadRequestHeaders(HttpRequest request)
            => AfterReadRequestHeaders?.Invoke(request);

        private static void InvokeAfterReadResponseHeaders(HttpResponse response)
            => AfterReadResponseHeaders?.Invoke(response);
    }
}
