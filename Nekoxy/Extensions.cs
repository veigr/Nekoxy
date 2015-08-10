using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TrotiNet;

namespace Nekoxy
{
    internal static class Extensions
    {
        public static Encoding GetEncoding(this HttpHeaders headers)
        {
            if (!headers.Headers.ContainsKey("content-type")) return defaultEncoding;
            var match = charsetRegex.Match(headers.Headers["content-type"]);
            if (!match.Success) return defaultEncoding;
            try
            {
                return Encoding.GetEncoding(match.Groups[1].Value);
            }
            catch
            {
                return defaultEncoding;
            }
        }

        public static string GetMimeType(this string contentType)
        {
            var match = mimeTypeRegex.Match(contentType);
            return match.Success
                ? match.Groups[1].Value
                : string.Empty;
        }

        public static bool IsLoopbackHost(this string hostName)
        {
            var addresses = Dns.GetHostEntry(hostName).AddressList;
            return addresses.Any(IPAddress.IsLoopback);
        }

        public static bool IsUnknownLength(this HttpHeaders responseHeaders)
        {
            var isChunked = responseHeaders.TransferEncoding?.Contains("chunked") ?? false;
            return !isChunked && responseHeaders.ContentLength == null;
        }

        public static string ToString(this byte[] bytes, Encoding charset)
        {
            return charset.GetString(bytes);
        }

        private static readonly Encoding defaultEncoding = Encoding.ASCII;
        private static readonly Regex charsetRegex = new Regex("charset=([\\w-]*)", RegexOptions.Compiled);
        private static readonly Regex mimeTypeRegex = new Regex("^([^;]+)", RegexOptions.Compiled);
    }
}
