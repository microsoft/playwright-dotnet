using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserContextChannel : Channel<BrowserContext>
    {
        public BrowserContextChannel(string guid, ConnectionScope scope, BrowserContext owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler Close;

        internal event EventHandler<BrowserContextOnPageEventArgs> Page;

        internal event EventHandler<BrowserContextBindingCallEventArgs> BindingCall;

        internal Task<PageChannel> NewPageAsync(string url)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "newPage",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                });

        internal Task CloseAsync() => Scope.SendMessageToServer(Guid, "close");

        internal Task SetDefaultTimeoutNoReplyAsync(int timeout)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task ExposeBindingAsync(string name)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "exposeBinding",
                new Dictionary<string, object>
                {
                    ["name"] = name,
                });

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            try
            {
                switch (method)
                {
                    case "close":
                        Close?.Invoke(this, EventArgs.Empty);
                        break;
                    case "bindingCall":
                        BindingCall?.Invoke(
                            this,
                            new BrowserContextBindingCallEventArgs
                            {
                                BidingCallChannel = serverParams?.ToObject<BindingCallChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()),
                            });
                        break;
                    case "page":
                        Page?.Invoke(
                            this,
                            new BrowserContextOnPageEventArgs
                            {
                                PageChannel = serverParams?.ToObject<PageChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()),
                            });
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}
