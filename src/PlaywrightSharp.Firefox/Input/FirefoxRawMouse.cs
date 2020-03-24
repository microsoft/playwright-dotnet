using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Firefox.Input
{
    internal class FirefoxRawMouse : IRawMouse
    {
        public FirefoxRawMouse(FirefoxSession session)
        {
        }

        public Task DownAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers, int clickCount)
        {
            throw new System.NotImplementedException();
        }

        public Task MoveAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers)
        {
            throw new System.NotImplementedException();
        }

        public Task UpAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers, int clickCount)
        {
            throw new System.NotImplementedException();
        }
    }
}
