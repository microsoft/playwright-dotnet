/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class HarRouter
{
    private readonly LocalUtils _localUtils;

    private readonly string _harId;

    private readonly HarRouterOptions _options;

    private readonly HarNotFound _notFoundAction;

    private HarRouter(LocalUtils localUtils, string harId, HarNotFound notFoundAction, HarRouterOptions options)
    {
        _localUtils = localUtils;
        _harId = harId;
        _options = options;
        _notFoundAction = notFoundAction;
    }

    internal static async Task<HarRouter> CreateAsync(LocalUtils localUtils, string file, HarNotFound notFoundAction, HarRouterOptions options)
    {
        var (harId, error) = await localUtils.HarOpenAsync(file).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(error))
        {
            throw new PlaywrightException(error);
        }
        return new HarRouter(localUtils, harId, notFoundAction, options);
    }

    private async Task HandleAsync(Route route)
    {
        var request = route.Request;
        var response = await _localUtils.HarLookupAsync(
            harId: _harId,
            url: request.Url,
            method: request.Method,
            headers: (await request.HeadersArrayAsync().ConfigureAwait(false)).ToList(),
            postData: request.PostDataBuffer,
            isNavigationRequest: request.IsNavigationRequest).ConfigureAwait(false);

        if (response.Action == "redirect")
        {
            await route.RedirectNavigationRequestAsync(response.RedirectURL).ConfigureAwait(false);
            return;
        }
        if (response.Action == "fulfill")
        {
            await route.FulfillAsync(new()
            {
                Status = response.Status,
                Headers = new RawHeaders(response.Headers).Headers,
                BodyBytes = response.Body,
            }).ConfigureAwait(false);
            return;
        }
        if (response.Action == "error")
        {
            // Report the error, but fall through to the default handler.
        }

        if (_notFoundAction == HarNotFound.Abort)
        {
            await route.AbortAsync().ConfigureAwait(false);
            return;
        }

        await route.FallbackAsync().ConfigureAwait(false);
    }

    internal async Task AddContextRouteAsync(BrowserContext context)
    {
        if (!string.IsNullOrEmpty(_options.UrlString))
        {
            await context.RouteAsync(_options.UrlString, route => HandleAsync((Route)route)).ConfigureAwait(false);
        }
        else if (_options.UrlRegex != null)
        {
            await context.RouteAsync(_options.UrlRegex, route => HandleAsync((Route)route)).ConfigureAwait(false);
        }
        else
        {
            await context.RouteAsync("**/*", route => HandleAsync((Route)route)).ConfigureAwait(false);
        }

        context.Close += (_, _) => Dispose();
    }

    internal async Task AddPageRouteAsync(Page page)
    {
        if (!string.IsNullOrEmpty(_options.UrlString))
        {
            await page.RouteAsync(_options.UrlString, route => HandleAsync((Route)route)).ConfigureAwait(false);
        }
        else if (_options.UrlRegex != null)
        {
            await page.RouteAsync(_options.UrlRegex, route => HandleAsync((Route)route)).ConfigureAwait(false);
        }
        else
        {
            await page.RouteAsync("**/*", route => HandleAsync((Route)route)).ConfigureAwait(false);
        }

        page.Close += (_, _) => Dispose();
    }

    private void Dispose() => _localUtils.HarCloseAsync(_harId).IgnoreException();
}

internal class HarRouterOptions
{
    internal string UrlString { get; set; }

    internal Regex UrlRegex { get; set; }
}
