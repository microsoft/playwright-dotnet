using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Chromium.Input
{
    internal class ChromiumRawMouse : IRawMouse
    {
        private readonly ChromiumSession _client;

        public ChromiumRawMouse(ChromiumSession client)
        {
            _client = client;
        }

        public Task DownAsync(double x, double y, MouseButton button, List<MouseButton> buttons, Modifier[] modifiers, int clickCount)
            => _client.SendAsync(new Protocol.Input.InputDispatchMouseEventRequest
            {
                Type = "mousePressed",
                Button = button.ToMouseButtonProtocol(),
                X = x,
                Y = y,
                Modifiers = modifiers.ToModifiersMask(),
                ClickCount = clickCount,
            });

        public Task MoveAsync(double x, double y, MouseButton button, List<MouseButton> buttons, Modifier[] modifiers)
            => _client.SendAsync(new Protocol.Input.InputDispatchMouseEventRequest
            {
                Type = "mouseMoved",
                Button = button.ToMouseButtonProtocol(),
                X = x,
                Y = y,
                Modifiers = modifiers.ToModifiersMask(),
            });

        public Task UpAsync(double x, double y, MouseButton button, List<MouseButton> buttons, Modifier[] modifiers, int clickCount)
            => _client.SendAsync(new Protocol.Input.InputDispatchMouseEventRequest
            {
                Type = "mouseReleased",
                Button = button.ToMouseButtonProtocol(),
                X = x,
                Y = y,
                Modifiers = modifiers.ToModifiersMask(),
                ClickCount = clickCount,
            });
    }
}
