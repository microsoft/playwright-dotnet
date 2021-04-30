using System;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IWebSocket
    {
        /// <inheritdoc cref="WaitForEventAsync(string, float?)"/>
        public Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> webSocketEvent, Func<T, bool> predicate = null, float? timeout = null);
    }
}
