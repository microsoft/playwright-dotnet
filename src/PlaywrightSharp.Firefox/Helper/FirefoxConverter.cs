using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using PlaywrightSharp.Firefox.Protocol.Runtime;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Firefox.Helper
{
    internal static class FirefoxConverter
    {
        public static ConsoleType GetConsoleType(this RuntimeConsoleFirefoxEvent runtimeConsole)
            => runtimeConsole.Type == "warn" ? ConsoleType.Warning : runtimeConsole.Type.ToEnum<ConsoleType>();

        public static string ToStringValue(this RemoteObjectUnserializableValue value)
        {
            return typeof(RemoteObjectUnserializableValue).GetField(value.ToString()).GetCustomAttribute<EnumMemberAttribute>().Value;
        }

        public static ConsoleMessageLocation ToConsoleMessageLocation(this RuntimeConsoleFirefoxEvent runtimeConsole) => new ConsoleMessageLocation
        {
            ColumnNumber = (int?)runtimeConsole.Location.ColumnNumber,
            LineNumber = (int?)runtimeConsole.Location.LineNumber,
            URL = runtimeConsole.Location.Url,
        };

        public static int ToModifiersMask(this Modifier[] modifiers)
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

        public static int ToButtonNumber(this MouseButton button) => button switch
        {
            MouseButton.Left => 0,
            MouseButton.Middle => 1,
            MouseButton.Right => 2,
            _ => 0,
        };

        public static int ToButtonsMask(this List<MouseButton> buttons)
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
