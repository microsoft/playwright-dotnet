using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Input;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Chromium.Input
{
    internal class ChromiumRawKeyboard : IRawKeyboard
    {
        private const int KeypadLocation = 3;
        private readonly ChromiumSession _client;

        public ChromiumRawKeyboard(ChromiumSession client)
        {
            _client = client;
        }

        public Task SendTextAsync(string text) => _client.SendAsync(new InputInsertTextRequest { Text = text });

        public Task KeyDownAsync(Modifier[] modifiers, string code, int keyCode, int keyCodeWithoutLocation, string key, int location, bool autoRepeat, string text)
            => _client.SendAsync(new InputDispatchKeyEventRequest
            {
                Type = !string.IsNullOrEmpty(text) ? "keyDown" : "rawKeyDown",
                Modifiers = modifiers.ToModifiersMask(),
                WindowsVirtualKeyCode = keyCodeWithoutLocation,
                Code = code,
                Key = key,
                Text = text,
                UnmodifiedText = text,
                AutoRepeat = autoRepeat,
                Location = location,
                IsKeypad = location == KeypadLocation,
            });

        public Task KeyUpAsync(Modifier[] modifiers, string code, int keyCode, int keyCodeWithoutLocation, string key, int location)
            => _client.SendAsync(new InputDispatchKeyEventRequest
            {
                Type = "keyUp",
                Modifiers = modifiers.ToModifiersMask(),
                Key = key,
                WindowsVirtualKeyCode = keyCodeWithoutLocation,
                Code = code,
                Location = location,
            });
    }
}
