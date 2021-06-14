using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Playwright.Core
{
    internal class StorageState : IEquatable<StorageState>
    {
        /// <summary>
        /// Cookie list.
        /// </summary>
        public ICollection<Cookie> Cookies { get; set; } = new List<Cookie>();

        /// <summary>
        /// List of local storage per origin.
        /// </summary>
        public ICollection<StorageStateOrigin> Origins { get; set; } = new List<StorageStateOrigin>();

        public bool Equals(StorageState other)
            => other != null &&
                Cookies.SequenceEqual(other.Cookies) &&
                Origins.SequenceEqual(other.Origins);

        public override int GetHashCode()
            => 412870874 +
                EqualityComparer<ICollection<Cookie>>.Default.GetHashCode(Cookies) +
                EqualityComparer<ICollection<StorageStateOrigin>>.Default.GetHashCode(Origins);

        public override bool Equals(object obj) => Equals(obj as StorageState);
    }
}
