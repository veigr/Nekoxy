using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nekoxy.Win32;
using TrotiNet;
using System.ComponentModel;

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
        /// HTTPレスポンスをプロキシクライアントに送信完了した際に発生。
        /// </summary>
        public static event Action<Session> AfterSessionComplete;

        /// <summary>
        /// アップストリームプロキシのホスト名。
        /// アップストリームプロキシは UpstreamProxyHost が null の場合無効となる。
        /// TrotiNet は Dns.GetHostAddresses で取得されたアドレスを順番に接続試行するため、
        /// 接続先によっては動作が遅くなる可能性がある。
        /// 例えば 127.0.0.1 で待ち受けているローカルプロキシに対して接続したい場合、
        /// localhost を指定するとまず ::1 へ接続試行するため、動作が遅くなってしまう。
        /// </summary>
        public static string UpstreamProxyHost
        {
            get { return TransparentProxyLogic.UpstreamProxyHost; }
            set { TransparentProxyLogic.UpstreamProxyHost = value; }
        }

        /// <summary>
        /// アップストリームプロキシのポート番号。
        /// アップストリームプロキシは UpstreamProxyHost が null の場合無効となる。
        /// </summary>
        public static int UpstreamProxyPort
        {
            get { return TransparentProxyLogic.UpstreamProxyPort; }
            set { TransparentProxyLogic.UpstreamProxyPort = value; }
        }

        /// <summary>
        /// 指定ポートで Listening を開始する。
        /// Shutdown() を呼び出さずに2回目の Startup() を呼び出した場合、InvalidOperationException が発生する。
        /// </summary>
        /// <param name="listeningPort">Listeningするポート。</param>
        /// <param name="useIpV6">falseの場合、127.0.0.1で待ち受ける。trueの場合、::1で待ち受ける。既定false。</param>
        /// <param name="isSetIEProxySettings">IEプロキシの設定を実施する。既定true。</param>
        public static void Startup(int listeningPort, bool useIpV6 = false, bool isSetIEProxySettings = true)
        {
            if (server != null) throw new InvalidOperationException("Calling Startup() twice without calling Shutdown() is not permitted.");

            TransparentProxyLogic.AfterSessionComplete += InvokeAfterSessionComplete;
            server = new TcpServer(listeningPort, useIpV6);
            server.Start(TransparentProxyLogic.CreateProxy);
            if (isSetIEProxySettings) SetProxyInProcessByUrlmon(listeningPort);
        }

        /// <summary>
        /// Listening しているスレッドを終了し、ソケットを閉じる。
        /// </summary>
        public static void Shutdown()
        {
            server.Stop();
            TransparentProxyLogic.AfterSessionComplete -= InvokeAfterSessionComplete;
            server = null;
        }

        private static void InvokeAfterSessionComplete(Session session)
        {
            if (AfterSessionComplete != null)
                AfterSessionComplete.Invoke(session);
        }

        /// <summary>
        /// urlmon.dllでプロセス内プロキシ設定を適用。
        /// </summary>
        /// <param name="listeningPort">ポート</param>
        private static void SetProxyInProcessByUrlmon(int listeningPort)
        {
            var proxyInfo = new INTERNET_PROXY_INFO
            {
                dwAccessType = (uint)INTERNET_OPEN_TYPE.INTERNET_OPEN_TYPE_PROXY,
                lpszProxy = "http=localhost:" + listeningPort,
                lpszProxyBypass = "local",
            };
            var dwBufferLength = (uint)Marshal.SizeOf(proxyInfo);
            NativeMethods.UrlMkSetSessionOption((uint)INTERNET_OPTION.INTERNET_OPTION_PROXY, proxyInfo, dwBufferLength, 0U);
        }

        /// <summary>
        /// WinInet.dllでプロセス内プロキシ設定を適用。
        /// 作ったけど面倒くさい感じなので未使用に。
        /// </summary>
        /// <param name="listeningPort">ポート</param>
        private static void SetProxyInProcessByWinInet(int listeningPort)
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
