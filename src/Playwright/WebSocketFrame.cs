using System;
using System.Text;

namespace Microsoft.Playwright
{
    internal class WebSocketFrame : IWebSocketFrame
    {
        internal WebSocketFrame(string payload, bool isBase64)
        {
            if (isBase64)
            {
                Binary = Convert.FromBase64String(payload);
            }
            else
            {
                Binary = Encoding.UTF8.GetBytes(payload);
            }
        }

        public byte[] Binary { get; set; }

        public string Text => Encoding.UTF8.GetString(Binary);
    }
}
