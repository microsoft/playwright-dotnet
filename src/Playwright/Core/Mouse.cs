using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    internal partial class Mouse : IMouse
    {
        private readonly PageChannel _channel;

        public Mouse(PageChannel channel)
        {
            _channel = channel;
        }

        public Task ClickAsync(float x, float y, MouseClickOptions options = default)
            => _channel.MouseClickAsync(x, y, delay: options?.Delay, button: options?.Button, clickCount: options?.ClickCount);

        public Task DblClickAsync(float x, float y, MouseDblClickOptions options = default)
            => _channel.MouseClickAsync(x, y, delay: options?.Delay, button: options?.Button, 2);

        public Task DownAsync(MouseDownOptions options = default)
            => _channel.MouseDownAsync(button: options?.Button, clickCount: options?.ClickCount);

        public Task MoveAsync(float x, float y, MouseMoveOptions options = default)
            => _channel.MouseMoveAsync(x, y, steps: options?.Steps);

        public Task UpAsync(MouseUpOptions options = default)
            => _channel.MouseUpAsync(button: options?.Button, clickCount: options?.ClickCount);
    }
}
