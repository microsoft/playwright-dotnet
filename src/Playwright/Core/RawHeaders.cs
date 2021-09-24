using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Playwright.Core
{
    internal class RawHeaders
    {
        private readonly Dictionary<string, List<string>> _headersMap = new();

        public RawHeaders(NameValueEntry[] headers)
        {
            HeadersArray = new(headers.Select(x => new ResponseHeadersArrayResult() { Name = x.Name, Value = x.Value }));
            foreach (var entry in headers)
            {
                var name = entry.Name.ToLower();
                if (!_headersMap.TryGetValue(name, out List<string> values))
                {
                    values = new List<string>();
                }

                values.Add(entry.Value);
                _headersMap[name] = values;
            }
        }

        public List<ResponseHeadersArrayResult> HeadersArray { get; }

        public Dictionary<string, string> Headers => _headersMap.Keys.ToDictionary(x => x, y => Get(y));

        public string Get(string name)
        {
            var values = GetAll(name);
            if (values == null)
                return null;
            return string.Join("set-cookie".Equals(name, StringComparison.OrdinalIgnoreCase) ? "\n" : ", ", values);
        }

        public string[] GetAll(string name)
        {
            if (_headersMap.TryGetValue(name.ToLower(), out List<string> values))
                return values.ToArray();
            return null;
        }
    }
}
