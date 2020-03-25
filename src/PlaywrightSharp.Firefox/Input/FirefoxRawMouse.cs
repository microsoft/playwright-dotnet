using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol.Page;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Firefox.Input
{
    internal class FirefoxRawMouse : IRawMouse
    {
        private readonly FirefoxSession _session;

        public FirefoxRawMouse(FirefoxSession session)
        {
            _session = session;
        }

        public Task DownAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers, int clickCount)
            => _session.SendAsync(new PageDispatchMouseEventRequest
            {
                Type = "mousedown",
                Button = ToButtonNumber(lastButton),
                Buttons = ToButtonsMask(buttons),
                X = x,
                Y = y,
                Modifiers = ToModifiersMask(modifiers),
                ClickCount = clickCount,
            });

        public Task MoveAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers)
            => _session.SendAsync(new PageDispatchMouseEventRequest
            {
                Type = "mousemove",
                Button = 0,
                Buttons = ToButtonsMask(buttons),
                X = x,
                Y = y,
                Modifiers = ToModifiersMask(modifiers),
            });

        public Task UpAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers, int clickCount)
            => _session.SendAsync(new PageDispatchMouseEventRequest
            {
                Type = "mouseup",
                Button = ToButtonNumber(lastButton),
                Buttons = ToButtonsMask(buttons),
                X = x,
                Y = y,
                Modifiers = ToModifiersMask(modifiers),
                ClickCount = clickCount,
            });

        private static int ToModifiersMask(Modifier[] modifiers)
        {
            int mask = 0;
            if (modifiers.Contains(Modifier.Alt))
            {
                mask |= 1;
            }

            if (modifiers.Contains(Modifier.Control))
            {
                mask |= 2;
            }

            if (modifiers.Contains(Modifier.Shift))
            {
                mask |= 4;
            }

            if (modifiers.Contains(Modifier.Meta))
            {
                mask |= 8;
            }

            return mask;
        }

        private static int ToButtonNumber(MouseButton button) => button switch
        {
            MouseButton.Left => 0,
            MouseButton.Middle => 1,
            MouseButton.Right => 2,
            _ => 0,
        };

        private static int ToButtonsMask(List<MouseButton> buttons)
        {
            int mask = 0;
            if (buttons.Contains(MouseButton.Left))
            {
                mask |= 1;
            }

            if (buttons.Contains(MouseButton.Right))
            {
                mask |= 2;
            }

            if (buttons.Contains(MouseButton.Middle))
            {
                mask |= 4;
            }

            return mask;
        }
    }
}
