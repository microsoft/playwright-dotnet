using System;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IWebSocket
    {
        /// <summary>
        /// Waits for event to fire and passes its value into the predicate function. Resolves when the predicate returns truthy value.
        /// Will throw an Exception if the <see cref="IWebSocket"/> is closed before the event is fired.
        /// </summary>
        /// <param name="webSocketEvent">Event to wait for.</param>
        /// <param name="predicate">Receives the event data and resolves when the waiting should resolve.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <typeparam name="T">Resulting event args.</typeparam>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// // wait for console event:
        /// var console = await page.WaitForEvent(PageEvent.Console);
        ///
        /// // wait for popup event:
        /// var popup = await page.WaitForEvent(PageEvent.Popup);
        ///
        /// // wait for dialog event:
        /// var dialog = await page.WaitForEvent(PageEvent.Dialog);
        ///
        /// // wait for request event:
        /// var request = await page.WaitForEvent(PageEvent.Request);
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the predicate returns truthy value. Yielding the information of the event.</returns>
        public Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> webSocketEvent, Func<T, bool> predicate = null, float? timeout = null);
    }
}
