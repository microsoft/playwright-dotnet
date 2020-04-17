using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium.Protocol.Network
{
    internal partial class Cookie
    {
        public static implicit operator NetworkCookie(Cookie cookie)
            => cookie == null
                ? null
                : new NetworkCookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain,
                    Path = cookie.Path,
                    Expires = cookie.Expires.Value,
                    HttpOnly = cookie.HttpOnly.Value,
                    Session = cookie.Session.Value,
                    Secure = cookie.Secure.Value,
                    SameSite = cookie.SameSite.HasValue ? (SameSite)cookie.SameSite : PlaywrightSharp.SameSite.None,
                };
    }
}
