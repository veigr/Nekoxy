using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekoxy
{
    /// <summary>
    /// プロキシ設定の種類。
    /// </summary>
    public enum ProxyConfigType
    {
        /// <summary>
        /// システムのプロキシ設定を使用。
        /// </summary>
        SystemProxy,
        /// <summary>
        /// 指定のプロキシ設定を使用。
        /// </summary>
        SpecificProxy,
        /// <summary>
        /// プロキシを使用せず、直接サーバーに接続。
        /// </summary>
        DirectAccess,
    }
}
