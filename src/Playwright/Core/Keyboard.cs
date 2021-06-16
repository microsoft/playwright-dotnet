using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    internal partial class Keyboard : IKeyboard
    {
        private readonly PageChannel _channel;

        public Keyboard(PageChannel channel)
        {
            _channel = channel;
        }

        public Task DownAsync(string key) => _channel.KeyboardDownAsync(key);

        public Task UpAsync(string key) => _channel.KeyboardUpAsync(key);

        public Task PressAsync(string key, KeyboardPressOptions options = default)
           => _channel.PressAsync(key, options?.Delay);

        public Task TypeAsync(string text, KeyboardTypeOptions options = default)
           => _channel.TypeAsync(text, options?.Delay);

        public Task InsertTextAsync(string text) => _channel.InsertTextAsync(text);
    }
}
