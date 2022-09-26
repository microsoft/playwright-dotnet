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

using System.Globalization;
using Microsoft.Playwright.Tests.TestServer;

namespace Microsoft.Playwright.Tests;

public class PageTestEx : PageTest
{
    public SimpleServer Server { get; internal set; }
    public SimpleServer HttpsServer { get; internal set; }
    public int BrowserMajorVersion { get; internal set; }

    [SetUp]
    public async Task HttpSetup()
    {
        var http = await HttpService.Register(this);
        Server = http.Server;
        HttpsServer = http.HttpsServer;
        BrowserMajorVersion = int.Parse(Browser.Version.Split(".")[0], CultureInfo.InvariantCulture);
        TestConstants.BrowserName = BrowserName;
    }
}
