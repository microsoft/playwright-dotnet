using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PageChannel : Channel<Page>
    {
        public PageChannel(string guid, ConnectionScope scope, Page owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler Closed;

        internal event EventHandler Crashed;

        internal event EventHandler<PageChannelRequestEventArgs> Request;

        internal event EventHandler<PageChannelPopupEventArgs> Popup;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
                case "crash":
                    Crashed?.Invoke(this, EventArgs.Empty);
                    break;
                case "popup":
                    Popup?.Invoke(this, new PageChannelPopupEventArgs
                    {
                        Page = serverParams?.ToObject<PageChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object,
                    });
                    break;
                case "request":
                    Request?.Invoke(this, new PageChannelRequestEventArgs { RequestChannel = null });
                    break;
            }
        }

        internal Task CloseAsync(PageCloseOptions options)
            => Scope.SendMessageToServer(
                Guid,
                "close",
                options);
    }
}
