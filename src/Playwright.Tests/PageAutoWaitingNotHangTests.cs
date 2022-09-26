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

namespace Microsoft.Playwright.Tests;

public class PageAutoWaitingNotHangTests : PageTestEx
{
    [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "clicking on links which do not commit navigation")]
    public async Task ClickingOnLinksWhichDoNotCommitNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync($"<a href=\"{Server.EmptyPage}\">fooobar</a>");
        await Page.ClickAsync("a");
    }

    [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.stop async")]
    public Task CallingWindowStopAsync()
    {
        Server.SetRoute("/empty.html", _ => Task.CompletedTask);

        return Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                setTimeout(() => window.stop(), 100);
             }}", Server.EmptyPage);
    }

    [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.stop")]
    public Task CallingWindowStop()
    {
        Server.SetRoute("/empty.html", _ => Task.CompletedTask);

        return Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                window.stop();
             }}", Server.EmptyPage);
    }

    [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "assigning location to about:blank")]
    public async Task AssigningLocationToAboutBlank()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("window.location.href = 'about:blank';");
    }

    [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "assigning location to about:blank after non-about:blank")]
    public Task AssigningLocationToAboutBlankAfterNonAboutBlank()
    {
        Server.SetRoute("/empty.html", _ => Task.CompletedTask);

        return Page.EvaluateAsync($@"(url) => {{
                window.location.href = '{Server.EmptyPage}';
                window.location.href = 'about:blank';
             }}", Server.EmptyPage);
    }

    [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.open and window.close")]
    public async Task CallingWindowOpenAndWindowClose()
    {
        await Page.GotoAsync(Server.EmptyPage);

        await Page.EvaluateAsync($@"(url) => {{
                const popup = window.open(window.location.href);
                popup.close();
             }}", Server.EmptyPage);
    }

    [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "opening a popup")]
    public async Task OpeningAPopup()
    {
        await Page.GotoAsync(Server.EmptyPage);

        await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window._popup = window.open(window.location.href)"));
    }
}
