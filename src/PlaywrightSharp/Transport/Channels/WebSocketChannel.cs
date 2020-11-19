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

        internal event EventHandler<WebSocketFrameEventArgs> FrameSent;

        internal event EventHandler<WebSocketFrameEventArgs> FrameReceived;

        internal event EventHandler<WebSocketErrorEventArgs> SocketError;

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
                        new WebSocketFrameEventArgs
                        {
                            Payload = serverParams?.GetProperty("data").ToObject<string>(),
                        });
                    break;
                case "frameReceived":
                    FrameReceived?.Invoke(
                        this,
                        new WebSocketFrameEventArgs
                        {
                            Payload = serverParams?.GetProperty("data").ToObject<string>(),
                        });
                    break;
                case "error":
                    SocketError?.Invoke(
                        this,
                        new WebSocketErrorEventArgs
                        {
                            ErrorMessage = serverParams?.GetProperty("error").ToObject<string>(),
                        });
                    break;
            }
        }
    }
}
