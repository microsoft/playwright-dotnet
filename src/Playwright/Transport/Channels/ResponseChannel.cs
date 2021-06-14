using System.Threading.Tasks;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class ResponseChannel : Channel<Response>
    {
        public ResponseChannel(string guid, Connection connection, Response owner) : base(guid, connection, owner)
        {
        }

        internal async Task<string> GetBodyAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "body", null).ConfigureAwait(false))?.GetProperty("binary").ToString();

        internal async Task<string> FinishedAsync()
        {
            var element = await Connection.SendMessageToServerAsync(Guid, "finished", null).ConfigureAwait(false);
            if (element != null)
            {
                if (element.Value.TryGetProperty("error", out var errorValue))
                {
                    return errorValue.GetString();
                }
            }

            return null;
        }
    }
}
