using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekoxy
{
    /// <summary>
    /// プロキシ設定。
    /// </summary>
    public struct ProxyConfig
    {
        /// <summary>
        /// タイプ。
        /// </summary>
        public ProxyConfigType Type { get; }

        /// <summary>
        /// 指定プロキシのホスト。
        /// 
        /// TrotiNet は Dns.GetHostAddresses で取得されたアドレスを順番に接続試行するため、
        /// 接続先によっては動作が遅くなる可能性がある。
        /// 例えば 127.0.0.1 で待ち受けているローカルプロキシに対して接続したい場合、
        /// localhost を指定するとまず ::1 へ接続試行するため、動作が遅くなってしまう。
        /// </summary>
        public string SpecificProxyHost { get; }

        /// <summary>
        /// 指定プロキシのポート。
        /// </summary>
        public int SpecificProxyPort { get; }

        /// <summary>
        /// 設定を指定し、初期化します。
        /// </summary>
        /// <param name="type">プロキシタイプ</param>
        /// <param name="specificProxyHost">指定プロキシのホスト</param>
        /// <param name="specificProxyPort">指定プロキシのポート</param>
        public ProxyConfig(ProxyConfigType type, string specificProxyHost = null, int specificProxyPort = 80)
        {
            this.Type = type;
            this.SpecificProxyHost = specificProxyHost;
            this.SpecificProxyPort = specificProxyPort;
        }
    }
}
