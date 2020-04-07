using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Helper;
using PlaywrightSharp.Firefox.Protocol.Page;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Firefox.Input
{
    internal class FirefoxRawMouse : IRawMouse
    {
        private readonly FirefoxSession _session;

        public FirefoxRawMouse(FirefoxSession session) => _session = session;

        public Task DownAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers, int clickCount)
            => _session.SendAsync(new PageDispatchMouseEventRequest
            {
                Type = "mousedown",
                Button = lastButton.ToButtonNumber(),
                Buttons = buttons.ToButtonsMask(),
                X = x,
                Y = y,
                Modifiers = modifiers.ToModifiersMask(),
                ClickCount = clickCount,
            });

        public Task MoveAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers)
            => _session.SendAsync(new PageDispatchMouseEventRequest
            {
                Type = "mousemove",
                Button = 0,
                Buttons = buttons.ToButtonsMask(),
                X = x,
                Y = y,
                Modifiers = modifiers.ToModifiersMask(),
            });

        public Task UpAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers, int clickCount)
            => _session.SendAsync(new PageDispatchMouseEventRequest
            {
                Type = "mouseup",
                Button = lastButton.ToButtonNumber(),
                Buttons = buttons.ToButtonsMask(),
                X = x,
                Y = y,
                Modifiers = modifiers.ToModifiersMask(),
                ClickCount = clickCount,
            });
    }
}
