using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol.IO;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    internal static class ProtocolStreamReader
    {
        internal static async Task<string> ReadProtocolStreamStringAsync(ChromiumSession client, string handle, string path)
        {
            var result = new StringBuilder();
            var fs = !string.IsNullOrEmpty(path) ? AsyncFileHelper.CreateStream(path, FileMode.Create) : null;

            try
            {
                bool eof = false;

                while (!eof)
                {
                    var response = await client.SendAsync(new IOReadRequest
                    {
                        Handle = handle,
                    }).ConfigureAwait(false);

                    eof = response.Eof.Value;

                    result.Append(response.Data);

                    if (fs != null)
                    {
                        var data = Encoding.UTF8.GetBytes(response.Data);
                        await fs.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                    }
                }

                await client.SendAsync(new IOCloseRequest
                {
                    Handle = handle,
                }).ConfigureAwait(false);

                return result.ToString();
            }
            finally
            {
                fs?.Dispose();
            }
        }

        internal static async Task<byte[]> ReadProtocolStreamByteAsync(ChromiumSession client, string handle, string path)
        {
            IEnumerable<byte> result = null;
            bool eof = false;
            var fs = !string.IsNullOrEmpty(path) ? AsyncFileHelper.CreateStream(path, FileMode.Create) : null;

            try
            {
                while (!eof)
                {
                    var response = await client.SendAsync(new IOReadRequest
                    {
                        Handle = handle,
                    }).ConfigureAwait(false);

                    eof = response.Eof.Value;
                    var data = Convert.FromBase64String(response.Data);
                    result = result == null ? data : result.Concat(data);

                    if (fs != null)
                    {
                        await fs.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                    }
                }

                await client.SendAsync(new IOCloseRequest
                {
                    Handle = handle,
                }).ConfigureAwait(false);

                return result.ToArray();
            }
            finally
            {
                fs?.Dispose();
            }
        }
    }
}
