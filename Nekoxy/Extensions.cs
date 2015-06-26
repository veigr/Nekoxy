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

        /// <summary>
        /// ChunkedDataを送信。
        /// destとhandler両方指定できないと困るので作成。
        /// オリジナルのものはinternalでアクセス出来ない。
        /// </summary>
        /// <param name="src">送信元</param>
        /// <param name="dest">送信先</param>
        /// <param name="handler">パケットハンドラ</param>
        public static void TunnelChunkedDataTo(
            this HttpSocket src,
            HttpSocket dest,
            HttpSocket.MessagePacketHandler handler)
        {
            while (true)
            {
                var chunkHeader = src.ReadAsciiLine();
                if (chunkHeader.Length == 0)
                    throw new HttpProtocolBroken("Expected chunk header missing");

                var hSize = new string(chunkHeader.TakeWhile(x => !chunkSizeEnd.Contains(x)).ToArray());
                uint size;
                try
                {
                    size = Convert.ToUInt32(hSize, 16);
                }
                catch
                {
                    var s = chunkHeader.Length > 20
                        ? (chunkHeader.Substring(0, 17) + "...")
                        : chunkHeader;
                    throw new HttpProtocolBroken("Could not parse chunk size in: " + s);
                }

                if (dest != null) dest.WriteAsciiLine(chunkHeader);

                if (size == 0) break;

                src.TunnelDataTo(handler, size);

                var newLine = src.ReadAsciiLine();

                if (dest != null) dest.WriteAsciiLine(newLine);
            }

            string line;
            do
            {
                line = src.ReadAsciiLine();
                if (dest != null) dest.WriteAsciiLine(line);
            } while (line.Length != 0);
        }

        private static readonly Encoding defaultEncoding = Encoding.ASCII;
        private static readonly Regex charsetRegex = new Regex("charset=([\\w-]*)", RegexOptions.Compiled);
        private static readonly Regex mimeTypeRegex = new Regex("^([^;]+)", RegexOptions.Compiled);
        private static readonly char[] chunkSizeEnd = { ' ', ';' };

    }

    public class HttpProtocolBroken : Exception
    {
        internal HttpProtocolBroken(string msg) : base(msg) { }
    }
}
