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
            HeadersArray = new(headers.Select(x => new KeyValuePair<string, string>(x.Name, x.Value)));
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

        public List<KeyValuePair<string, string>> HeadersArray { get; }

        public IReadOnlyList<KeyValuePair<string, string>> Headers
        {
            get
            {
                var list = new List<KeyValuePair<string, string>>();
                foreach (var key in _headersMap.Keys)
                {
                    foreach (var value in _headersMap[key])
                        list.Add(new KeyValuePair<string, string>(key, value));
                }
                return list;
            }
        }

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
