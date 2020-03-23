using System.Reflection;
using System.Runtime.Serialization;
using PlaywrightSharp.Firefox.Protocol.Runtime;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Firefox.Helper
{
    internal static class FirefoxConverter
    {
        public static ConsoleType GetConsoleType(this RuntimeConsoleFirefoxEvent runtimeConsole) => runtimeConsole.Type.ToEnum<ConsoleType>();

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
    }
}
