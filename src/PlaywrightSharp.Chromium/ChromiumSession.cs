using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumSession
    {
        internal Task<T> SendAsync<T>(string method, object args = null)
        {
            throw new NotImplementedException();
        }

        internal Task<JsonElement> SendAsync(string method, object args = null)
        {
            throw new NotImplementedException();
        }
    }
}
