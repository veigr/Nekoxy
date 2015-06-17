using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrotiNet;

namespace Nekoxy
{
    /// <summary>
    /// HTTPレスポンスデータ。
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// HTTPステータス、ヘッダ、ボディを元に初期化。
        /// </summary>
        /// <param name="statusLine">HTTPステータスライン。</param>
        /// <param name="headers">HTTPレスポンスヘッダ。</param>
        /// <param name="body">HTTPレスポンスボディ。</param>
        public HttpResponse(HttpStatusLine statusLine, HttpHeaders headers, byte[] body)
        {
            this.StatusLine = statusLine;
            this.Headers = headers;
            this.Body = body;
        }

        /// <summary>
        /// HTTPステータスライン。
        /// </summary>
        public HttpStatusLine StatusLine { get; private set; }

        /// <summary>
        /// HTTPヘッダヘッダ。
        /// </summary>
        public HttpHeaders Headers { get; private set; }

        /// <summary>
        /// HTTPレスポンスボディ。
        /// </summary>
        public byte[] Body { get; private set; }

        /// <summary>
        /// content-type ヘッダ。
        /// </summary>
        public string ContentType
        {
            get
            {
                return this.Headers.Headers.ContainsKey("content-type")
                    ? this.Headers.Headers["content-type"]
                    : string.Empty;
            }
        }

        /// <summary>
        /// content-type ヘッダから MIME Type のみ取得。
        /// </summary>
        public string MimeType
        {
            get { return this.ContentType.GetMimeType(); }
        }

        /// <summary>
        /// レスポンスの文字エンコーディング。
        /// content-typeヘッダに指定されたcharsetを元に生成される。
        /// 指定がない場合はUS-ASCII。
        /// </summary>
        public Encoding Charset
        {
            get { return this.Headers.GetEncoding(); }
        }

        /// <summary>
        /// HTTPレスポンスボディを文字列で取得する。
        /// </summary>
        public string BodyAsString
        {
            get { return this.Charset.GetString(this.Body); }
        }
    }
}
