using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Cookie set data.
    /// </summary>
    /// <seealso cref="IBrowserContext.AddCookiesAsync(SetNetworkCookieParam[])"/>
    /// <seealso cref="IBrowserContext.GetCookiesAsync(string[])"/>
    public class SetNetworkCookieParam : IEquatable<SetNetworkCookieParam>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        /// <value>The domain.</value>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the expiration. Unix time in seconds.
        /// </summary>
        /// <value>Expiration.</value>
        public decimal? Expires { get; set; }

        /// <summary>
        /// Gets or sets if it's HTTP only.
        /// </summary>
        /// <value>Whether it's http only or not.</value>
        public bool? HttpOnly { get; set; }

        /// <summary>
        /// Gets or sets if it's secure.
        /// </summary>
        /// <value>Whether it's secure or not.</value>
        public bool? Secure { get; set; }

        /// <summary>
        /// Gets or sets the cookies SameSite value.
        /// </summary>
        public SameSite SameSite { get; set; } = SameSite.None;

        /// <inheritdoc/>
        public bool Equals(SetNetworkCookieParam other)
            => other != null &&
                Name == other.Name &&
                Value == other.Value &&
                Domain == other.Domain &&
                Url == other.Url &&
                Path == other.Path &&
                Expires == other.Expires &&
                HttpOnly == other.HttpOnly &&
                Secure == other.Secure &&
                SameSite == other.SameSite;

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as SetNetworkCookieParam);

        /// <inheritdoc/>
        public override int GetHashCode()
            => 412870874 +
                EqualityComparer<string>.Default.GetHashCode(Name) +
                EqualityComparer<string>.Default.GetHashCode(Value) +
                EqualityComparer<string>.Default.GetHashCode(Domain) +
                EqualityComparer<string>.Default.GetHashCode(Url) +
                EqualityComparer<string>.Default.GetHashCode(Path) +
                EqualityComparer<decimal?>.Default.GetHashCode(Expires) +
                EqualityComparer<bool?>.Default.GetHashCode(HttpOnly) +
                EqualityComparer<bool?>.Default.GetHashCode(Secure) +
                EqualityComparer<SameSite>.Default.GetHashCode(SameSite);

        internal SetNetworkCookieParam Clone() => (SetNetworkCookieParam)MemberwiseClone();
    }
}
