using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium.Protocol.Network
{
    internal partial class CookieParam
    {
        public static implicit operator CookieParam(SetNetworkCookieParam cookie)
            => cookie == null
                ? null
                : new CookieParam
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain,
                    Path = cookie.Path,
                    Expires = cookie.Expires,
                    HttpOnly = cookie.HttpOnly,
                    Secure = cookie.Secure,
                    SameSite = (CookieSameSite)cookie.SameSite,
                };
    }
}
