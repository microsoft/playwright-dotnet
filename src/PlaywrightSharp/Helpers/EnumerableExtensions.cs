using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaywrightSharp.Helpers
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<NameValueEntry> Remap(this IEnumerable<KeyValuePair<string, string>> input)
        {
            if (input == null)
            {
                return null;
            }

            return input.Select(x => new NameValueEntry(x.Key, x.Value)).ToArray();
        }
    }
}
