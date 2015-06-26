using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// レスポンス送信後に発生するイベント。
        /// </summary>
        public static event Action<Session> AfterSessionComplete;

        /// <summary>
        /// アップストリームプロキシの指定を有効にする。既定値false。
        /// trueの場合、デフォルトプロキシを無視し、UpstreamProxyHost プロパティと UpstreamProxyPort プロパティをアップストリームプロキシに設定する。
        /// </summary>
        public static bool IsEnableUpstreamProxy { get; set; }

        /// <summary>
        /// インスタンス初期化時にRelayHttpProxyHostに設定される値。
        /// </summary>
        public static string UpstreamProxyHost { get; set; }

        /// <summary>
        /// インスタンス初期化時にRelayHttpProxyPortに設定される値。
        /// </summary>
        public static int UpstreamProxyPort { get; set; }

        /// <summary>
        /// UpstreamProxyHostがnullの場合に用いられるデフォルトホスト。
        /// </summary>
        public static string DefaultUpstreamProxyHost { get; set; }

        /// <summary>
        /// UpstreamProxyHostがnullの場合に用いられるデフォルトポート番号。
        /// </summary>
        public static int DefaultUpstreamProxyPort { get; set; }

        /// <summary>
        /// TcpServerがインスタンスを生成する際に使用するメソッド。
        /// 接続(AcceptCallback)の都度呼び出され、インスタンスが生成される。
        /// </summary>
        /// <param name="clientSocket">Browser-Proxy間Socket。SocketBP。</param>
        /// <returns>ProxyLogicインスタンス。</returns>
        public new static TransparentProxyLogic CreateProxy(HttpSocket clientSocket)
        {
            return new TransparentProxyLogic(clientSocket);
        }

        /// <summary>
        /// SocketBPからインスタンスを初期化。
        /// 接続(AcceptCallback)の都度インスタンスが生成される。
        /// </summary>
        /// <param name="clientSocket">Browser-Proxy間Socket。SocketBP。</param>
        public TransparentProxyLogic(HttpSocket clientSocket) : base(clientSocket)
        {
            this.RelayHttpProxyHost = IsEnableUpstreamProxy ? UpstreamProxyHost : DefaultUpstreamProxyHost;
            this.RelayHttpProxyPort = IsEnableUpstreamProxy ? UpstreamProxyPort : DefaultUpstreamProxyPort;
        }

        /// <summary>
        /// SendResponseをoverrideし、リクエストデータを読み取る。
        /// </summary>
        protected override void SendRequest()
        {
            this.currentSession = new Session();

            //HTTPメソッド送信
            this.SocketPS.WriteAsciiLine(this.RequestLine.RequestLine);
            //HTTPリクエストヘッダ送信
            this.SocketPS.WriteAsciiLine(this.RequestHeaders.HeadersInOrder);

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
        /// サーバーからブラウザーへレスポンスデータを送信する。
        /// State.OnResponseMessagePacketが役に立たないのでSendResponse総取っ換え。
        /// </summary>
        override protected void SendResponse()
        {
            var responseStream = new MemoryStream();  //List<byte>より圧倒的に早い
            try
            {
                var tooLarge = false;
                HttpSocket.MessagePacketHandler sendResponseHandler = (packet, offset, size) =>
                {
                    if (size == 0) return;
                    //2GB超えは対応できない。そもそもデカいデータを想定するならメモリに入れてはいけないのではないか…？
                    if (int.MaxValue < offset + size)
                    {
                        tooLarge = true;
                        return;
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    responseStream.Write(packet, (int)offset, (int)size); //データ読み取り

                    if (this.SocketBP.WriteBinary(packet, offset, size) != size) //データ送信
                        throw new IoBroken();
                };

                this.SendResponseToBrowser(sendResponseHandler);
                responseStream.Close();

                if (tooLarge) return;   //2GB以上のデータの場合は AfterSessionComplete を発生させない
                if (AfterSessionComplete == null) return;

                var responseData = responseStream.ToArray();
                //Content-Encoding対応っぽい
                using (var ms = new MemoryStream())
                {
                    var stream = this.GetResponseMessageStream(responseData);
                    stream.CopyTo(ms);
                    responseData = ms.ToArray();
                }

                this.currentSession.Response = new HttpResponse(this.ResponseStatusLine, this.ResponseHeaders, responseData);
                AfterSessionComplete.Invoke(this.currentSession);
            }
            finally
            {
                responseStream.Dispose();
            }

            this.State.NextStep = null;
        }

        /// <summary>
        /// サーバーからブラウザーへレスポンスデータを送信
        /// </summary>
        /// <param name="sendResponseHandler">レスポンスデータ読み取り＆送信用ハンドラ</param>
        private void SendResponseToBrowser(HttpSocket.MessagePacketHandler sendResponseHandler)
        {
            if (!(this.ResponseHeaders.TransferEncoding == null && this.ResponseHeaders.ContentLength == null))
                this.SendResponseStatusAndHeaders();


            var status = this.ResponseStatusLine.StatusCode;
            if (this.RequestLine.Method.Equals("HEAD")
                || status == 204 || status == 304 || (100 <= status && status <= 199))
            {
                this.SendResponseStatusAndHeaders();
                this.CloseProxyServerConnection();
                return;
            }

            var isChunked = false;
            var messageLength = 0u;
            if (this.ResponseHeaders.TransferEncoding != null)
            {
                isChunked = this.ResponseHeaders.TransferEncoding.Contains("chunked");
            }
            else if (this.ResponseHeaders.ContentLength != null)
            {
                messageLength = (uint)this.ResponseHeaders.ContentLength;
                if (messageLength == 0)
                {
                    this.CloseProxyServerConnection();
                    return;
                }
            }
            else
            {
                var buffer = new byte[512];
                this.SocketPS.TunnelDataTo(ref buffer);

                this.ResponseHeaders.ContentLength = (uint)buffer.Length;
                this.SocketBP.WriteAsciiLine(this.ResponseStatusLine.StatusLine);
                this.SocketBP.WriteAsciiLine(this.ResponseHeaders.HeadersInOrder);

                this.SocketBP.TunnelDataTo(this.TunnelBP, buffer);
                return;
            }

            if (!this.State.bPersistConnectionPS)
                this.SocketPS.TunnelDataTo(sendResponseHandler);
            else if (isChunked)
                this.SocketPS.TunnelChunkedDataTo(this.SocketBP, sendResponseHandler);
            else
                this.SocketPS.TunnelDataTo(sendResponseHandler, messageLength);

            sendResponseHandler(null, 0, 0);

            this.CloseProxyServerConnection();
        }

        /// <summary>
        /// プロキシサーバー間コネクションを、KeepAliveとかじゃなかったら閉じる
        /// </summary>
        private void CloseProxyServerConnection()
        {
            if (this.State.bPersistConnectionPS || this.SocketPS == null) return;

            this.SocketPS.CloseSocket();
            this.SocketPS = null;
        }

        private Session currentSession; //SendRequestで初期化してOnReceiveResponseの最後でイベントに投げる
    }
}
