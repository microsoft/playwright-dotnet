using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests.Helpers
{
    internal static class HeaderDictionaryExtensions
    {
        public static Dictionary<string, string> ToDictionary(this IHeaderDictionary headers)
        {
            var dictionary = new Dictionary<string, string>();

            foreach (string key in headers.Keys)
            {
                dictionary[key] = headers[key];
            }

            return dictionary;
        }
    }
}
