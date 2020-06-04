using System.Linq;

namespace PlaywrightSharp.Helpers
{
    internal static class PlaywrightSharpExtensions
    {
        internal static string GetText(this ConsoleMessage message)
        {
            if (string.IsNullOrEmpty(message.Text))
            {
                return string.Join(" ", message.Args.Select(arg => ((JSHandle)arg).Context.Delegate.HandleToString(arg, false /* includeType */)).ToArray());
            }

            return message.Text;
        }
    }
}
