using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="BrowserContextOptions.StorageState"/>.
    /// </summary>
    public class StorageState : IEquatable<StorageState>
    {
        /// <summary>
        /// Cookie list.
        /// </summary>
        public ICollection<SetNetworkCookieParam> Cookies { get; set; } = new List<SetNetworkCookieParam>();

        /// <summary>
        /// List of local storage per origin.
        /// </summary>
        public ICollection<StorageStateOrigin> Origins { get; set; } = new List<StorageStateOrigin>();

        /// <inheritdoc/>
        public bool Equals(StorageState other)
            => other != null &&
                Cookies.SequenceEqual(other.Cookies) &&
                Origins.SequenceEqual(other.Origins);

        /// <inheritdoc/>
        public override int GetHashCode()
            => 412870874 +
                EqualityComparer<ICollection<SetNetworkCookieParam>>.Default.GetHashCode(Cookies) +
                EqualityComparer<ICollection<StorageStateOrigin>>.Default.GetHashCode(Origins);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as StorageState);
    }
}
