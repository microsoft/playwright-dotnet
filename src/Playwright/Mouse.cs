using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
{
    internal class Mouse : IMouse
    {
        private readonly PageChannel _channel;

        public Mouse(PageChannel channel)
        {
            _channel = channel;
        }

        public Task ClickAsync(float x, float y, MouseButton button, int? clickCount, float? delay)
            => _channel.MouseClickAsync(x, y, delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), clickCount ?? 1);

        public Task DblClickAsync(float x, float y, MouseButton button, float? delay)
            => _channel.MouseClickAsync(x, y, delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), 2);

        public Task DownAsync(MouseButton button, int? clickCount)
            => _channel.MouseDownAsync(button.EnsureDefaultValue(MouseButton.Left), clickCount ?? 1);

        public Task MoveAsync(float x, float y, int? steps)
            => _channel.MouseMoveAsync(x, y, steps ?? 1);

        public Task UpAsync(MouseButton button, int? clickCount)
            => _channel.MouseUpAsync(button.EnsureDefaultValue(MouseButton.Left), clickCount ?? 1);
    }
}
