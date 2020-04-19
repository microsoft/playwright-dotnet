namespace PlaywrightSharp.Firefox.Protocol.Browser
{
    internal partial class CookieOptions
    {
        public static implicit operator CookieOptions(SetNetworkCookieParam cookie) => new CookieOptions
        {
            Name = cookie.Name,
            Value = cookie.Value,
            Url = cookie.Url,
            Domain = cookie.Domain,
            Path = cookie.Path,
            Secure = cookie.Secure,
            HttpOnly = cookie.HttpOnly,
            SameSite = (CookieOptionsSameSite)cookie.SameSite,
            Expires = cookie.Expires,
        };

        public static implicit operator SetNetworkCookieParam(CookieOptions cookie) => new SetNetworkCookieParam
        {
            Name = cookie.Name,
            Value = cookie.Value,
            Url = cookie.Url,
            Domain = cookie.Domain,
            Path = cookie.Path,
            Secure = cookie.Secure,
            HttpOnly = cookie.HttpOnly,
            Expires = cookie.Expires,
        };
    }
}
