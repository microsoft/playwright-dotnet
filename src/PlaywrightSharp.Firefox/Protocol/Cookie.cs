namespace PlaywrightSharp.Firefox.Protocol.Browser
{
    internal partial class Cookie
    {
        public static implicit operator NetworkCookie(Cookie cookie) => new NetworkCookie
        {
            Name = cookie.Name,
            Value = cookie.Value,
            Domain = cookie.Domain,
            Path = cookie.Path,
            Secure = (bool)cookie.Secure,
            HttpOnly = (bool)cookie.HttpOnly,
            SameSite = (SameSite)cookie.SameSite,
            Expires = (double)cookie.Expires,
            Session = (bool)cookie.Session,
        };
    }
}
