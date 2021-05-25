using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
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

        public Task PressAsync(string key, float? delay) => _channel.PressAsync(key, delay);

        public Task TypeAsync(string text, float? delay) => _channel.TypeAsync(text, delay);

        public Task InsertTextAsync(string text) => _channel.InsertTextAsync(text);
    }
}
