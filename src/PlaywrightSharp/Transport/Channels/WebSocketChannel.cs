using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Converters;

namespace PlaywrightSharp.Transport.Channels
{
    internal class WebSocketChannel : Channel<WebSocket>
    {
        public WebSocketChannel(string guid, Connection connection, WebSocket owner) : base(guid, connection, owner)
        {
        }

        internal event EventHandler Close;

        internal event EventHandler<IWebSocketFrame> FrameSent;

        internal event EventHandler<IWebSocketFrame> FrameReceived;

        internal event EventHandler<string> SocketError;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "close":
                    Close?.Invoke(this, EventArgs.Empty);
                    break;
                case "frameSent":
                    FrameSent?.Invoke(
                        this,
                        new WebSocketFrame(
                            serverParams?.GetProperty("data").ToObject<string>(),
                            serverParams?.GetProperty("opcode").ToObject<int>() == 2));
                    break;
                case "frameReceived":
                    FrameReceived?.Invoke(
                        this,
                        new WebSocketFrame(
                            serverParams?.GetProperty("data").ToObject<string>(),
                            serverParams?.GetProperty("opcode").ToObject<int>() == 2));
                    break;
                case "socketError":
                    SocketError?.Invoke(this, serverParams?.GetProperty("error").ToObject<string>());
                    break;
            }
        }
    }
}
