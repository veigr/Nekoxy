using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nekoxy.Win32;

namespace Nekoxy
{
    /// <summary>
    /// WinINet関連ユーティリティ。
    /// </summary>
    public static class WinInetUtil
    {
        /// <summary>
        /// urlmon.dllでプロセス内プロキシ設定を適用。
        /// </summary>
        /// <param name="proxy">プロキシサーバー</param>
        /// <param name="proxyBypass">バイパスリスト</param>
        public static void SetProxyInProcess(string proxy, string proxyBypass)
        {
            var proxyInfo = new INTERNET_PROXY_INFO
            {
                dwAccessType = INTERNET_OPEN_TYPE.INTERNET_OPEN_TYPE_PROXY,
                lpszProxy = proxy,
                lpszProxyBypass = proxyBypass,
            };
            var dwBufferLength = (uint)Marshal.SizeOf(proxyInfo);
            NativeMethods.UrlMkSetSessionOption(INTERNET_OPTION.INTERNET_OPTION_PROXY, proxyInfo, dwBufferLength, 0U);
        }

        /// <summary>
        /// urlmon.dllでプロセス内プロキシ設定を適用。
        /// </summary>
        /// <param name="listeningPort">ポート</param>
        internal static void SetProxyInProcessByUrlmon(int listeningPort)
        {
            SetProxyInProcess(
                GetProxyConfig(listeningPort),
                WinHttpGetIEProxyConfigForCurrentUser().ProxyBypass ?? "local");
        }

        /// <summary>
        /// システムプロキシに設定されたHTTPプロキシのホスト名を取得。
        /// </summary>
        /// <returns>システムHTTPプロキシのホスト名。設定されていない場合はnullを返す。</returns>
        internal static string GetSystemHttpProxyHost()
        {
            var proxy = GetSystemHttpProxy();
            return proxy != null
                ? GetSystemHttpProxy().Split(':')[0]
                : null;
        }

        /// <summary>
        /// システムプロキシに設定されたHTTPプロキシのポート番号を取得。
        /// </summary>
        /// <returns>システムHTTPプロキシのポート番号。設定されていない場合は0を返す。</returns>
        internal static int GetSystemHttpProxyPort()
        {
            var proxy = GetSystemHttpProxy();
            return proxy != null
                ? int.Parse(GetSystemHttpProxy().Split(':')[1])
                : 0;
        }

        /// <summary>
        /// システムプロキシに設定されたHTTPプロキシ設定を取得。
        /// </summary>
        /// <returns>システムHTTPプロキシ設定。設定されていない場合はnullを返す。</returns>
        private static string GetSystemHttpProxy()
        {
            var proxyConfig = WinHttpGetIEProxyConfigForCurrentUser();
            if (proxyConfig.Proxy == null) return null;

            var configs = proxyConfig.Proxy.Split(';');
            if (!proxyConfig.Proxy.Contains("=")) return configs[0];

            return configs.Any(x => x.StartsWith("http="))
                ? new string(configs.First(x => x.StartsWith("http=")).Replace("http=", "").ToArray())
                : null;
        }

        /// <summary>
        /// システムプロキシのhttpプロキシ設定をNekoxyに置換したプロキシ設定を取得。
        /// </summary>
        /// <param name="listeningPort">Listeningポート</param>
        /// <returns>編集後プロキシ設定</returns>
        private static string GetProxyConfig(int listeningPort)
        {
            var localProxy = "http=localhost:" + listeningPort;
            var proxyConfig = WinHttpGetIEProxyConfigForCurrentUser();
            if (string.IsNullOrWhiteSpace(proxyConfig.Proxy)) return localProxy;

            var configs = proxyConfig.Proxy.Split(';');
            if (!proxyConfig.Proxy.Contains("=")) return localProxy + ";https=" + configs[0] + ";ftp=" + configs[0];

            return configs.Any(x => x.StartsWith("http="))
                ? string.Join(";", configs.Select(x => x.StartsWith("http=") ? localProxy : x))
                : localProxy + ";" + string.Join(";", configs);
        }

        /// <summary>
        /// WinHTTPでIEプロキシ設定を取得。
        /// </summary>
        /// <returns></returns>
        private static WinHttpCurrentUserIEProxyConfig WinHttpGetIEProxyConfigForCurrentUser()
        {
            //ここだけWinINetではない……
            var ieProxyConfig = new WinHttpCurrentUserIEProxyConfig();
            NativeMethods.WinHttpGetIEProxyConfigForCurrentUser(ref ieProxyConfig);
            return ieProxyConfig;
        }
    }

}
