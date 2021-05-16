using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Playwright
{
    /// <summary>
    /// See <see cref="StorageState.Origins"/>.
    /// </summary>
    internal class StorageStateOrigin : IEquatable<StorageStateOrigin>
    {
        /// <summary>
        /// Origin.
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// Local storage.
        /// </summary>
        public ICollection<NameValueEntry> LocalStorage { get; set; } = new List<NameValueEntry>();

        public bool Equals(StorageStateOrigin other)
            => other != null &&
                Origin == other.Origin &&
                LocalStorage.SequenceEqual(other.LocalStorage);

        public override int GetHashCode()
            => 412870874 +
                EqualityComparer<string>.Default.GetHashCode(Origin) +
                EqualityComparer<ICollection<NameValueEntry>>.Default.GetHashCode(LocalStorage);

        public override bool Equals(object obj) => Equals(obj as StorageStateOrigin);
    }
}
