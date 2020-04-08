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

        public Task KeyDownAsync(Modifier[] modifiers, string code, int keyCode, int keyCodeWithoutLocation, string key, double location, bool autoRepeat, string text)
        {
            throw new System.NotImplementedException();
        }

        public Task KeyUpAsync(Modifier[] modifiers, string code, int keyCode, int keyCodeWithoutLocation, string key, double location)
        {
            throw new System.NotImplementedException();
        }
    }
}
