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

using System.Text.Json;

namespace Microsoft.Playwright.Tests.Fixtures;

public sealed class PageWithHarOptions
{
    /// <summary>The file in the output directory of the test, 'test.har' when not set.</summary>
    public string OutputPath { get; set; }

    public HarContentPolicy? Content { get; set; }

    public bool? OmitContent { get; set; }
}

/// <summary><c>const { context, page, getLog, getZip } = await pageWithHar()</c>.</summary>
public sealed class PageWithHarResult
{
    private readonly string _harPath;

    public PageWithHarResult(IBrowserContext context, IPage page, string harPath)
    {
        Context = context;
        Page = page;
        _harPath = harPath;
    }

    public IBrowserContext Context { get; }

    public IPage Page { get; }

    /// <summary>Closes the context and gives the <c>log</c> of the HAR.</summary>
    public async Task<JsonElement> GetLog()
    {
        await Context.CloseAsync().ConfigureAwait(false);
        using var har = JsonDocument.Parse(await File.ReadAllBytesAsync(_harPath).ConfigureAwait(false));
        return har.RootElement.GetProperty("log").Clone();
    }

    /// <summary>Closes the context and gives the entries of the zipped HAR.</summary>
    public async Task<Dictionary<string, byte[]>> GetZip()
    {
        await Context.CloseAsync().ConfigureAwait(false);
        return Utils.ParseHar(_harPath);
    }
}

/// <summary><c>await pageWithHar(options)</c>: a page of a context that records a HAR.</summary>
public delegate Task<PageWithHarResult> PageWithHar(PageWithHarOptions options = null);

/// <summary>The <c>pageWithHar</c> fixture of <c>tests/config/browserTest.ts</c>.</summary>
[FixtureProvider(Order = 2)]
public static class PageWithHarFixtures
{
    [Fixture]
    public static PageWithHar PageWithHar(ContextFactory contextFactory, TestInfo testInfo) => async options =>
    {
        var harPath = testInfo.OutputPath(options?.OutputPath ?? "test.har");
        var context = await contextFactory(new() { RecordHarPath = harPath, RecordHarContent = options?.Content, RecordHarOmitContent = options?.OmitContent, IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        var page = await context.NewPageAsync().ConfigureAwait(false);
        return new PageWithHarResult(context, page, harPath);
    };
}
