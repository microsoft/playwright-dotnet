using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    internal class Mouse : IMouse
    {
        private readonly PageChannel _channel;

        public Mouse(PageChannel channel)
        {
            _channel = channel;
        }

        public Task ClickAsync(float x, float y, MouseButton button, int? clickCount, float? delay)
            => _channel.MouseClickAsync(x, y, delay ?? 0, EnsureDefaultValue(button), clickCount ?? 1);

        public Task DblclickAsync(float x, float y, MouseButton button, float? delay)
            => _channel.MouseClickAsync(x, y, delay ?? 0, EnsureDefaultValue(button), 2);

        public Task DownAsync(MouseButton button, int? clickCount)
            => _channel.MouseDownAsync(EnsureDefaultValue(button), clickCount ?? 1);

        public Task MoveAsync(float x, float y, int? steps)
            => _channel.MouseMoveAsync(x, y, steps ?? 1);

        public Task UpAsync(MouseButton button, int? clickCount)
            => _channel.MouseUpAsync(EnsureDefaultValue(button), clickCount ?? 1);

        private static MouseButton EnsureDefaultValue(MouseButton button) =>
            button switch
            {
                MouseButton.Undefined => MouseButton.Left,
                _ => button,
            };
    }
}
