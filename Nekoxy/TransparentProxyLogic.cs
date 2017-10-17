using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TrotiNet;

namespace Nekoxy
{
    /// <summary>
    /// 通信データを透過し読み取るためのProxyLogic。
    /// Transfer-Encoding: chunked なHTTPリクエストの RequestBody の読み取りは未対応。
    /// </summary>
    internal class TransparentProxyLogic : ProxyLogic
    {
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
        public static ProxyConfig UpstreamProxyConfig { get; set; } = new ProxyConfig(ProxyConfigType.SystemProxy);

        /// <summary>
        /// TcpServerがインスタンスを生成する際に使用するメソッド。
        /// 接続(AcceptCallback)の都度呼び出され、インスタンスが生成される。
        /// </summary>
        /// <param name="clientSocket">Browser-Proxy間Socket。SocketBP。</param>
        /// <returns>ProxyLogicインスタンス。</returns>
        public new static TransparentProxyLogic CreateProxy(HttpSocket clientSocket)
            => new TransparentProxyLogic(clientSocket);

        /// <summary>
        /// SocketBPからインスタンスを初期化。
        /// 接続(AcceptCallback)の都度インスタンスが生成される。
        /// </summary>
        /// <param name="clientSocket">Browser-Proxy間Socket。SocketBP。</param>
        public TransparentProxyLogic(HttpSocket clientSocket) : base(clientSocket) { }

        /// <summary>
        /// クライアントからリクエストヘッダまで読み込み、サーバーアクセス前のタイミング。
        /// 上流プロキシの設定を行う。
        /// </summary>
        protected override void OnReceiveRequest()
        {
            this.SetUpstreamProxy();
        }

        private void SetUpstreamProxy()
        {
            this.RelayHttpProxyHost = null;
            this.RelayHttpProxyPort = 80;

            var config = UpstreamProxyConfig;

            if (config.Type == ProxyConfigType.DirectAccess) return;

            if (config.Type == ProxyConfigType.SpecificProxy)
            {
                this.RelayHttpProxyHost = string.IsNullOrWhiteSpace(config.SpecificProxyHost) ? null : config.SpecificProxyHost;
                this.RelayHttpProxyPort = config.SpecificProxyPort;
                return;
            }

            // システムプロキシ利用(既定)
            var requestUri = this.GetEffectiveRequestUri();
            if (requestUri != null)
            {
                var systemProxyConfig = WebRequest.GetSystemWebProxy();
                if (systemProxyConfig.IsBypassed(requestUri)) return; //ダイレクトアクセス
                var systemProxy = systemProxyConfig.GetProxy(requestUri);

                this.RelayHttpProxyHost = !systemProxy.IsOwnProxy() ? systemProxy.Host : null;
                this.RelayHttpProxyPort = systemProxy.Port;
            }
            else
            {
                //リクエストURIをうまく組み立てられなかった場合、自動構成は諦めて通常のプロキシ設定を適用
                var systemProxyHost = WinInetUtil.GetSystemHttpProxyHost();
                var systemProxyPort = WinInetUtil.GetSystemHttpProxyPort();
                if (systemProxyPort == HttpProxy.ListeningPort && systemProxyHost.IsLoopbackHost())
                    return; //自身が指定されていた場合上流には指定しない
                this.RelayHttpProxyHost = systemProxyHost;
                this.RelayHttpProxyPort = systemProxyPort;
            }
        }

        private Uri GetEffectiveRequestUri()
        {
            if (this.RequestLine.URI.Contains("://"))
                return new Uri(this.RequestLine.URI);

            int destinationPort;
            var originalUri = this.RequestLine.URI;
            // Parse とか言いながら RequestLine.URI の書き換えが発生する場合がある
            // authority-form で RelayHttpProxyHost が null の場合に発生
            var destinationHost = this.ParseDestinationHostAndPort(this.RequestLine, this.RequestHeaders, out destinationPort);
            this.RequestLine.URI = originalUri;
            var isDefaultPort = destinationPort == (this.RequestLine.Method == "CONNECT" ? 443 : 80);

            var scheme = this.RequestLine.Method == "CONNECT" ? "https" : "http";
            var authority = isDefaultPort ? destinationHost : $"{destinationHost}:{destinationPort}";
            var pathAndQuery = this.RequestLine.URI.Contains("/") ? this.RequestLine.URI : string.Empty;

            Uri uri;
            return Uri.TryCreate($"{scheme}://{authority}/{pathAndQuery}", UriKind.Absolute, out uri) ? uri : null;
        }

        /// <summary>
        /// SendResponseをoverrideし、リクエストデータを読み取る。
        /// </summary>
        protected override void SendRequest()
        {
            AfterReadRequestHeaders?.Invoke(new HttpRequest(this.RequestLine, this.RequestHeaders, null));

            this.currentSession = new Session();

            //HTTPリクエストヘッダ送信
            this.SocketPS.WriteBinary(Encoding.ASCII.GetBytes(
                $"{this.RequestLine.RequestLine}\r\n{this.RequestHeaders.HeadersInOrder}\r\n"));

            byte[] request = null;
            if (this.State.bRequestHasMessage)
            {
                if (this.State.bRequestMessageChunked)
                {
                    //FIXME: chunked request のデータ読み取りは未対応
                    this.SocketBP.TunnelChunkedDataTo(this.SocketPS);
                }
                else
                {
                    //Requestデータを読み取って流す
                    request = new byte[this.State.RequestMessageLength];
                    this.SocketBP.TunnelDataTo(request, this.State.RequestMessageLength);
                    this.SocketPS.TunnelDataTo(this.TunnelPS, request);
                }
            }
            this.currentSession.Request = new HttpRequest(this.RequestLine, this.RequestHeaders, request);

            //ReadResponseへ移行
            this.State.NextStep = this.ReadResponse;
        }

        /// <summary>
        /// OnReceiveResponseをoverrideし、レスポンスデータを読み取る。
        /// </summary>
        protected override void OnReceiveResponse()
        {
            AfterReadResponseHeaders?.Invoke(new HttpResponse(this.ResponseStatusLine, this.ResponseHeaders, null));

            //200だけ
            if (this.ResponseStatusLine.StatusCode != 200) return;

            // GetContentだけやるとサーバーからデータ全部読み込むけどクライアントに送らないってことになる。
            // のでTransferEncodingとContentLengthを書き換えてchunkedじゃないレスポンスとしてクライアントに送信してやる必要がある。
            // 
            // RFC 7230 の 3.3.1 を見る限りだと、Transfer-Encoding はリクエスト/レスポンスチェーンにいるどの recipient も
            // デコードしておっけーみたいに書いてあるから、HTTP的には問題なさそう。
            // https://tools.ietf.org/html/rfc7230#section-3.3.1
            // 
            // ただ4.1.3のロジックとTrotiNetのとを見比べると trailer フィールドへの対応が足りてるのかどうか疑問が残る。
            // https://tools.ietf.org/html/rfc7230#section-4.1.3
            var response = this.ResponseHeaders.IsUnknownLength()
                ? this.GetContentWhenUnknownLength()
                : this.GetContent();
            this.State.NextStep = null; //既定の後続動作(SendResponse)をキャンセル(自前で送信処理を行う)

            //Content-Encoding対応っぽい
            using (var ms = new MemoryStream())
            {
                var stream = this.GetResponseMessageStream(response);
                stream.CopyTo(ms);
                var content = ms.ToArray();
                this.currentSession.Response = new HttpResponse(this.ResponseStatusLine, this.ResponseHeaders, content);
            }

            // Transfer-Encoding: Chunked をやめて Content-Length を使うようヘッダ書き換え
            this.ResponseHeaders.TransferEncoding = null;
            this.ResponseHeaders.ContentLength = (uint)response.Length;

            this.SendResponseStatusAndHeaders(); //クライアントにHTTPステータスとヘッダ送信
            this.SocketBP.TunnelDataTo(this.TunnelBP, response); //クライアントにレスポンスボディ送信

            //keep-aliveとかじゃなかったら閉じる
            if (!this.State.bPersistConnectionPS)
            {
                this.SocketPS?.CloseSocket();
                this.SocketPS = null;
            }

            //AfterSessionCompleteイベント
            AfterSessionComplete?.Invoke(this.currentSession);
        }

        /// <summary>
        /// Transfer-Encoding も Content-Length も不明の場合、TrotiNet の SendResponse() にならい、Socket.Receive() が 0 になるまで受ける。
        /// </summary>
        /// <returns></returns>
        private byte[] GetContentWhenUnknownLength()
        {
            var buffer = new byte[512];
            this.SocketPS.TunnelDataTo(ref buffer); // buffer の長さは内部で調整される
            return buffer;
        }

        private Session currentSession; //SendRequestで初期化してOnReceiveResponseの最後でイベントに投げる
    }
}
