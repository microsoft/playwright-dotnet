using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightSharp
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
        /// <returns><see cref="IBrowser.NewContextAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)"/>.</returns>
        Task<IBrowserContext> NewContextAsync(BrowserContextOptions options);
    }
}
