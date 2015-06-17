using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                dwAccessType = (uint)INTERNET_OPEN_TYPE.INTERNET_OPEN_TYPE_PROXY,
                lpszProxy = proxy,
                lpszProxyBypass = proxyBypass,
            };
            var dwBufferLength = (uint)Marshal.SizeOf(proxyInfo);
            NativeMethods.UrlMkSetSessionOption((uint)INTERNET_OPTION.INTERNET_OPTION_PROXY, proxyInfo, dwBufferLength, 0U);
        }

        /// <summary>
        /// urlmon.dllでプロセス内プロキシ設定を適用。
        /// </summary>
        /// <param name="listeningPort">ポート</param>
        internal static void SetProxyInProcessByUrlmon(int listeningPort)
        {
            SetProxyInProcess("http=localhost:" + listeningPort, "local");
        }

        /// <summary>
        /// WinInet.dllでプロセス内プロキシ設定を適用。
        /// 作ったけど面倒くさい感じなので未使用に。
        /// </summary>
        /// <param name="listeningPort">ポート</param>
        internal static void SetProxyInProcessByWinInet(int listeningPort)
        {
            var hInternet = NativeMethods.InternetOpen(null, INTERNET_OPEN_TYPE.INTERNET_OPEN_TYPE_PRECONFIG, null, null, 0);

            var options = new INTERNET_PER_CONN_OPTION[3];

            options[0].dwOption = INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_FLAGS_UI;
            options[0].Value.dwValue = (int)(INTERNET_OPTION_PER_CONN_FLAGS.PROXY_TYPE_DIRECT | INTERNET_OPTION_PER_CONN_FLAGS.PROXY_TYPE_PROXY);

            options[1].dwOption = INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_PROXY_SERVER;
            options[1].Value.pszValue = Marshal.StringToHGlobalAuto("http=localhost:" + listeningPort);

            options[2].dwOption = INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_PROXY_BYPASS;
            options[2].Value.pszValue = Marshal.StringToHGlobalAuto("local");


            var optionSize = Marshal.SizeOf(typeof(INTERNET_PER_CONN_OPTION));
            var optionPtr = Marshal.AllocCoTaskMem(optionSize * options.Length);
            for (var i = 0; i < options.Length; ++i)
            {
                var opt = new IntPtr(optionPtr.ToInt32() + (i * optionSize));
                Marshal.StructureToPtr(options[i], opt, false);
            }

            var list = new INTERNET_PER_CONN_OPTION_LIST();
            list.dwSize = Marshal.SizeOf(list);
            list.pszConnection = IntPtr.Zero;
            list.dwOptionCount = options.Length;
            list.dwOptionError = 0;
            list.pOptions = optionPtr;

            var ipcoListPtr = Marshal.AllocCoTaskMem(list.dwSize);
            Marshal.StructureToPtr(list, ipcoListPtr, false);

            try
            {
                var isSucceed = NativeMethods.InternetSetOption(hInternet, INTERNET_OPTION.INTERNET_OPTION_PER_CONNECTION_OPTION, ipcoListPtr, list.dwSize);
                if (!isSucceed) throw new Win32Exception(Marshal.GetLastWin32Error());

                NativeMethods.InternetSetOption(hInternet, INTERNET_OPTION.INTERNET_OPTION_SETTINGS_CHANGED, ipcoListPtr, list.dwSize);
                NativeMethods.InternetSetOption(hInternet, INTERNET_OPTION.INTERNET_OPTION_REFRESH, ipcoListPtr, list.dwSize);
            }
            finally
            {
                Marshal.FreeCoTaskMem(optionPtr);
                Marshal.FreeCoTaskMem(ipcoListPtr);
                NativeMethods.InternetCloseHandle(hInternet);
            }
        }
    }
}
