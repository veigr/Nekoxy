using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Nekoxy.Win32
{
    // ReSharper disable InconsistentNaming
    internal static class NativeMethods
    {
        [DllImport("urlmon.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int UrlMkSetSessionOption(
            INTERNET_OPTION dwOption,
            INTERNET_PROXY_INFO pBuffer,
            uint dwBufferLength,
            uint dwReserved);

        [DllImport("winhttp.dll", SetLastError = true)]
        internal static extern bool WinHttpGetIEProxyConfigForCurrentUser(ref WinHttpCurrentUserIEProxyConfig pProxyConfig);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinHttpCurrentUserIEProxyConfig
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool AutoDetect;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string AutoConfigUrl;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Proxy;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string ProxyBypass;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal class INTERNET_PROXY_INFO
    {
        [MarshalAs(UnmanagedType.U4)]
        public INTERNET_OPEN_TYPE dwAccessType;

        [MarshalAs(UnmanagedType.LPStr)]
        public string lpszProxy;

        [MarshalAs(UnmanagedType.LPStr)]
        public string lpszProxyBypass;
    }

    internal enum INTERNET_OPEN_TYPE
    {
        INTERNET_OPEN_TYPE_PRECONFIG = 0,
        INTERNET_OPEN_TYPE_DIRECT = 1,
        INTERNET_OPEN_TYPE_PROXY = 3,
        INTERNET_OPEN_TYPE_PRECONFIG_WITH_NO_AUTOPROXY = 4,
    }

    internal enum INTERNET_OPTION
    {
        INTERNET_OPTION_REFRESH = 37,
        INTERNET_OPTION_PROXY = 38,
        INTERNET_OPTION_SETTINGS_CHANGED = 39,
        INTERNET_OPTION_PER_CONNECTION_OPTION = 75,

    }

    internal enum INTERNET_PER_CONN_OptionEnum
    {
        INTERNET_PER_CONN_FLAGS = 1,
        INTERNET_PER_CONN_PROXY_SERVER = 2,
        INTERNET_PER_CONN_PROXY_BYPASS = 3,
        INTERNET_PER_CONN_AUTOCONFIG_URL = 4,
        INTERNET_PER_CONN_AUTODISCOVERY_FLAGS = 5,
        INTERNET_PER_CONN_AUTOCONFIG_SECONDARY_URL = 6,
        INTERNET_PER_CONN_AUTOCONFIG_RELOAD_DELAY_MINS = 7,
        INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_TIME = 8,
        INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_URL = 9,
        INTERNET_PER_CONN_FLAGS_UI = 10,
    }
    // ReSharper restore InconsistentNaming
}
