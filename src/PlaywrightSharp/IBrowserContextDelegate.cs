using System.Threading.Tasks;
using PlaywrightSharp.Chromium;

namespace PlaywrightSharp
{
    /// <summary>
    /// Browser context delegate.
    /// </summary>
    internal interface IBrowserContextDelegate
    {
        /// <summary>
        /// <see cref="IBrowserContext"/> using the delegate.
        /// </summary>
        BrowserContext BrowserContext { get; set; }

        /// <summary>
        /// Creates a new page in the context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the new page is created, yielding the <see cref="global::PlaywrightSharp.IPage"/>.</returns>
        Task<IPage> NewPage();

        /// <summary>
        /// An array of all pages inside the browser context.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that completes when get browser context got all the pages, yielding the pages inside that browser context.
        /// </returns>
        Task<IPage[]> GetPagesAsync();
    }
}
