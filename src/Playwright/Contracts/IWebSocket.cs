using System;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IWebSocket
    {
        public Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> webSocketEvent, Func<Task> action = default, Func<T, bool> predicate = null, float? timeout = null);
    }
}
