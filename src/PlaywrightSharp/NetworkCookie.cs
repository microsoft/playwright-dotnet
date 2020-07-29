namespace PlaywrightSharp
{
    /// <summary>
    /// Cookie data.
    /// </summary>
    /// <seealso cref="IBrowserContext.GetCookiesAsync(string[])"/>
    public class NetworkCookie
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
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the expiration. Unix time in seconds.
        /// </summary>
        /// <value>Expiration.</value>
        public double Expires { get; set; }

        /// <summary>
        /// Gets or sets if it's HTTP only.
        /// </summary>
        /// <value>Whether it's http only or not.</value>
        public bool HttpOnly { get; set; }

        /// <summary>
        /// Gets or sets if it's secure.
        /// </summary>
        /// <value>Whether it's secure or not.</value>
        public bool Secure { get; set; }

        /// <summary>
        /// Gets or sets if it's session only.
        /// </summary>
        /// <value>Whether it's session only or not.</value>
        public bool Session { get; set; }

        /// <summary>
        /// Gets or sets the cookies SameSite value.
        /// </summary>
        public SameSite SameSite { get; set; }

        /// <summary>
        /// Converts a <see cref="NetworkCookie"/> to a <see cref="SetNetworkCookieParam"/>.
        /// </summary>
        /// <param name="cookie">Cookie to convert.</param>
        public static implicit operator SetNetworkCookieParam(NetworkCookie cookie)
            => cookie == null
            ? null
            : new SetNetworkCookieParam
            {
                Name = cookie.Name,
                Value = cookie.Value,
                Domain = cookie.Domain,
                Path = cookie.Path,
                Expires = cookie.Expires,
                HttpOnly = cookie.HttpOnly,
                Secure = cookie.Secure,
                SameSite = cookie.SameSite,
            };

        /// <summary>
        /// Converts a <see cref="NetworkCookie"/> to a <see cref="SetNetworkCookieParam"/>.
        /// </summary>
        /// <returns>A <see cref="SetNetworkCookieParam"/> with the matching properties set.</returns>
        public SetNetworkCookieParam ToSetNetworkCookieParam() => this;
    }
}
