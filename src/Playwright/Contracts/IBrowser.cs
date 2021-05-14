using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IBrowser : IAsyncDisposable
    {
        /// <summary>See <see cref="IBrowser.NewContextAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)"/>.</summary>
        /// <param name="options">The parameters, but in an options format.</param>
        /// <returns><see cref="IBrowser.NewContextAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)"/>.</returns>
        Task<IBrowserContext> NewContextAsync(BrowserContextOptions options);

        /// <summary>See <see cref="IBrowser.NewPageAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)"/>.</summary>
        /// <param name="options">The parameters, but in an options format.</param>
        /// <returns><see cref="IBrowser.NewPageAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)"/>.</returns>
        Task<IPage> NewPageAsync(BrowserContextOptions options);
    }
}
