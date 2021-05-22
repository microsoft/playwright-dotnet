using System;
using System.Collections.Generic;

namespace Microsoft.Playwright
{
    internal class NameValueEntry : IEquatable<NameValueEntry>
    {
        public NameValueEntry()
        {
        }

        public NameValueEntry(string name, string value) => (Name, Value) = (name, value);

        public string Name { get; set; }

        public string Value { get; set; }

        public bool Equals(NameValueEntry other)
            => other != null &&
                Name == other.Name &&
                Value == other.Value;

        public override int GetHashCode()
            => 412870874 +
                EqualityComparer<string>.Default.GetHashCode(Name) +
                EqualityComparer<string>.Default.GetHashCode(Value);

        public override bool Equals(object obj) => Equals(obj as NameValueEntry);
    }
}
