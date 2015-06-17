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

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr InternetOpen(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszAgent,
            INTERNET_OPEN_TYPE dwAccessType,
            [MarshalAs(UnmanagedType.LPWStr)] string lpszProxyName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpszProxyBypass,
            int dwFlags);

        [DllImport("wininet.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InternetCloseHandle(IntPtr hInternet);

        [DllImport("WinInet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InternetSetOption(
            IntPtr hInternet,
            INTERNET_OPTION dwOption,
            IntPtr lpBuffer,
            int dwBufferLength);

        [DllImport("urlmon.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int UrlMkSetSessionOption(uint dwOption, INTERNET_PROXY_INFO pBuffer, uint dwBufferLength, uint dwReserved);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class INTERNET_PROXY_INFO
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint dwAccessType;

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

    [Flags]
    internal enum INTERNET_OPTION_PER_CONN_FLAGS
    {
        PROXY_TYPE_DIRECT = 0x00000001,
        PROXY_TYPE_PROXY = 0x00000002,
        PROXY_TYPE_AUTO_PROXY_URL = 0x00000004,
        PROXY_TYPE_AUTO_DETECT = 0x00000008,
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct INTERNET_PER_CONN_OPTION_LIST
    {
        public int dwSize;
        public IntPtr pszConnection;
        public int dwOptionCount;
        public int dwOptionError;
        public IntPtr pOptions;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INTERNET_PER_CONN_OPTION
    {
        public INTERNET_PER_CONN_OptionEnum dwOption;
        public INTERNET_PER_CONN_OPTION_OptionUnion Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct INTERNET_PER_CONN_OPTION_OptionUnion
    {
        [FieldOffset(0)]
        public int dwValue;

        [FieldOffset(0)]
        public IntPtr pszValue;

        [FieldOffset(0)]
        public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;
    }

    // ReSharper restore InconsistentNaming
}
