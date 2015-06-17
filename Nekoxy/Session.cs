using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nekoxy
{
    /// <summary>
    /// HTTPセッションデータ。
    /// </summary>
    public class Session
    {
        /// <summary>
        /// HTTPリクエストデータ。
        /// </summary>
        public HttpRequest Request { get; internal set; }

        /// <summary>
        /// HTTPレスポンスデータ。
        /// </summary>
        public HttpResponse Response { get; internal set; }
    }
}
