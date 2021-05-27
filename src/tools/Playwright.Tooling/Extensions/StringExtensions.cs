using System.Net;

namespace Playwright.Tooling.Extensions
{
    internal static class StringExtensions
    {
        public static string ToHtml(this string value) => WebUtility.HtmlEncode(value);
    }
}
