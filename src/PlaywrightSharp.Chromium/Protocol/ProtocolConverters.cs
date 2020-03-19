using System;
using System.Linq;

namespace PlaywrightSharp.Chromium.Protocol
{
    internal static class ProtocolConverters
    {
        internal static Input.MouseButton ToMouseButtonProtocol(this PlaywrightSharp.Input.MouseButton button)
            => button switch
            {
                PlaywrightSharp.Input.MouseButton.Left => Input.MouseButton.Left,
                PlaywrightSharp.Input.MouseButton.Middle => Input.MouseButton.Middle,
                PlaywrightSharp.Input.MouseButton.Right => Input.MouseButton.Right,
                _ => Input.MouseButton.None,
            };

        internal static int ToModifiersMask(this PlaywrightSharp.Input.Modifier[] modifiers)
        {
            int mask = 0;
            if (modifiers.Contains(PlaywrightSharp.Input.Modifier.Alt))
            {
                mask |= 1;
            }

            if (modifiers.Contains(PlaywrightSharp.Input.Modifier.Control))
            {
                mask |= 2;
            }

            if (modifiers.Contains(PlaywrightSharp.Input.Modifier.Meta))
            {
                mask |= 4;
            }

            if (modifiers.Contains(PlaywrightSharp.Input.Modifier.Shift))
            {
                mask |= 8;
            }

            return mask;
        }
    }
}
