using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    /// <summary>
    /// <para>
    /// A Browser is created via <see cref="IBrowserType.LaunchAsync"/>. An example of using
    /// a <see cref="IBrowser"/> to create a <see cref="IPage"/>.
    /// </para>
    /// </summary>
    public partial interface IBrowser : IAsyncDisposable
    {
        /// <summary><para>Creates a new browser context. It won't share cookies/cache with other browser contexts.</para></summary>
        /// <param name="options">The parameters, but in an options format.</param>
        /// <returns><see cref="IBrowser.NewContextAsync(bool?, bool?, ColorScheme, float?, IEnumerable{KeyValuePair{string, string}}, Geolocation, bool?, HttpCredentials, bool?, bool?, bool?, string, bool?, IEnumerable{string}, Proxy, bool?, string, string, RecordVideoSize, ScreenSize, string, string, string, string, ViewportSize)"/>.</returns>
        Task<IBrowserContext> NewContextAsync(BrowserContextOptions options);
    }
}
