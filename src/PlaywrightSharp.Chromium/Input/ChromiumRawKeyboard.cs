using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol.Input;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Chromium.Input
{
    internal class ChromiumRawKeyboard : IRawKeyboard
    {
        private readonly ChromiumSession _client;

        public ChromiumRawKeyboard(ChromiumSession client)
        {
            _client = client;
        }

        public Task SendTextAsync(string text) => _client.SendAsync(new InputInsertTextRequest { Text = text });
    }
}
