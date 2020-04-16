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
            SameSite = ConvertSameSite(cookie.SameSite),
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
            SameSite = ConvertSameSite(cookie.SameSite),
            Expires = cookie.Expires,
        };

        internal static CookieOptionsSameSite? ConvertSameSite(SameSite? sameSite)
            => sameSite switch
            {
                PlaywrightSharp.SameSite.None => CookieOptionsSameSite.None,
                PlaywrightSharp.SameSite.Lax => CookieOptionsSameSite.Lax,
                PlaywrightSharp.SameSite.Strict => CookieOptionsSameSite.Strict,
                _ => null,
            };

        internal static SameSite? ConvertSameSite(CookieOptionsSameSite? sameSite)
            => sameSite switch
            {
                CookieOptionsSameSite.None => PlaywrightSharp.SameSite.None,
                CookieOptionsSameSite.Lax => PlaywrightSharp.SameSite.Lax,
                CookieOptionsSameSite.Strict => PlaywrightSharp.SameSite.Strict,
                _ => null,
            };
    }
}
