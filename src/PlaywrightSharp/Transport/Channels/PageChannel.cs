using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PageChannel : Channel<Page>
    {
        public PageChannel(string guid, ConnectionScope scope, Page owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler Closed;

        internal override void OnMessage(string method, PlaywrightSharpServerParams serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
