using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Name value entry.
    /// </summary>
    public class NameValueEntry : IEquatable<NameValueEntry>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueEntry"/> class.
        /// </summary>
        public NameValueEntry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueEntry"/> class.
        /// </summary>
        /// <param name="name">Entry name.</param>
        /// <param name="value">Entry value.</param>
        public NameValueEntry(string name, string value) => (Name, Value) = (name, value);

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc/>
        public bool Equals(NameValueEntry other)
            => other != null &&
                Name == other.Name &&
                Value == other.Value;

        /// <inheritdoc/>
        public override int GetHashCode()
            => 412870874 +
                EqualityComparer<string>.Default.GetHashCode(Name) +
                EqualityComparer<string>.Default.GetHashCode(Value);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as NameValueEntry);
    }
}
