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

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Tests.TestServer;

namespace Microsoft.Playwright.Tests;

///<playwright-file>tracing.spec.ts</playwright-file>
public class TracingTests : ContextTestEx
{
    [PlaywrightTest("tracing.spec.ts", "should collect trace with resources, but no js")]
    public async Task ShouldCollectTrace()
    {
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true
        });

        var page = await Context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/frames/frame.html");
        await page.SetContentAsync("<button>Click</button>");
        await page.ClickAsync("\"Click\"");
        await page.Mouse.MoveAsync(20, 20);
        await page.Mouse.DblClickAsync(20, 30);
        await page.Keyboard.InsertTextAsync("abc");
        await page.WaitForTimeoutAsync(2000); // Give it some time to produce screenshots.
        await page.CloseAsync();

        using var tmp = new TempDirectory();
        var tracePath = Path.Combine(tmp.Path, "trace.zip");
        await Context.Tracing.StopAsync(new() { Path = tracePath });

        await ShowTraceViewerAsync(tracePath, async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Create page"),
                new Regex(@"Navigate to ""/frames/frame.html"""),
                new Regex(@"Set content"),
                new Regex(@"Click"),
                new Regex(@"Mouse move"),
                new Regex(@"Double click"),
                new Regex(@"Insert ""abc"""),
                new Regex(@"Wait for timeout"),
                new Regex(@"Close")
            ]);
            await traceViewer.SelectActionAsync("Set content");
            await Expect(traceViewer.Page.Locator(".browser-frame-address-bar")).ToHaveTextAsync(Server.Prefix + "/frames/frame.html");
            var frame = await traceViewer.SnapshotFrame("Set content", 0, false);
            await Expect(frame.Locator("button")).ToHaveTextAsync("Click");

        });
    }

    [PlaywrightTest("tracing.spec.ts", "should collect two traces")]
    public async Task ShouldCollectTwoTraces()
    {
        var page = await Context.NewPageAsync();
        await Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<button>Click</button>");
        await page.ClickAsync("\"Click\"");

        using var tmp = new TempDirectory();
        var trace1Path = Path.Combine(tmp.Path, "trace1.zip");
        await Context.Tracing.StopAsync(new() { Path = trace1Path });

        await Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        await page.DblClickAsync("\"Click\"");
        await page.CloseAsync();
        var trace2Path = Path.Combine(tmp.Path, "trace2.zip");
        await Context.Tracing.StopAsync(new() { Path = trace2Path });

        await ShowTraceViewerAsync(trace1Path, async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Navigate to ""/empty.html"""),
                new Regex(@"Set content"),
                new Regex(@"Click")
            ]);
        });

        await ShowTraceViewerAsync(trace2Path, async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Double click"),
                new Regex(@"Close"),
            ]);
        });
    }

    [PlaywrightTest("tracing.spec.ts", "should work with multiple chunks")]
    public async Task ShouldWorkWithMultipleChunks()
    {
        using var tmp = new TempDirectory();

        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
        });

        var page = await Context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/frames/frame.html");

        await Context.Tracing.StartChunkAsync();
        await page.SetContentAsync("<button>Click</button>");
        await page.ClickAsync("'Click'");
        page.ClickAsync("'ClickNoButton'").IgnoreException();
        var traceFile1 = Path.Combine(tmp.Path, "trace1.zip");
        await Context.Tracing.StopChunkAsync(new TracingStopChunkOptions { Path = traceFile1 });

        await Context.Tracing.StartChunkAsync();
        await page.HoverAsync("'Click'");
        var traceFile2 = Path.Combine(tmp.Path, "trace2.zip");
        await Context.Tracing.StopChunkAsync(new TracingStopChunkOptions { Path = traceFile2 });

        await Context.Tracing.StartChunkAsync();
        await page.HoverAsync("'Click'");
        await Context.Tracing.StopChunkAsync(); // Should stop without a path.

        await ShowTraceViewerAsync(traceFile1, async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Set content"),
                new Regex(@"Click"),
                new Regex(@"Click")
            ]);
            await traceViewer.SelectSnapshotAsync("After");
            var frame = await traceViewer.SnapshotFrame("Set content", 0, false);
            await Expect(frame.Locator("button")).ToHaveTextAsync("Click");
        });
        await ShowTraceViewerAsync(traceFile2, async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToContainTextAsync([
                "Hover"
            ]);
            var frame = await traceViewer.SnapshotFrame("Hover", 0, false);
            await Expect(frame.Locator("button")).ToHaveTextAsync("Click");
        });
    }

    [PlaywrightTest("tracing.spec.ts", "should collect sources")]
    public async Task ShouldCollectSources()
    {
        await Context.Tracing.StartAsync(new()
        {
            Sources = true,
        });

        var page = await Context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/empty.html");
        await page.SetContentAsync("<button>Click</button>");
        async Task MyMethodOuter()
        {
            async Task MyMethodInner()
            {
                await page.GetByText("Click").ClickAsync();
            }
            await MyMethodInner();
        }
        await MyMethodOuter();
        await page.CloseAsync();

        using var tmp = new TempDirectory();
        var tracePath = Path.Combine(tmp.Path, "trace.zip");
        await Context.Tracing.StopAsync(new() { Path = tracePath });

        await ShowTraceViewerAsync(tracePath, async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Create page"),
                new Regex(@"Navigate to ""/empty.html"""),
                new Regex(@"Set content"),
                new Regex(@"Click"),
                new Regex(@"Close")
            ]);
            await traceViewer.ShowSourceTab();
            // TODO: these should not be anonymous.
            await Expect(traceViewer.StackFrames).ToHaveTextAsync([
                new Regex(@"\(anonymous\)TracingTests\.cs:\d+"),
                new Regex(@"\(anonymous\)TracingTests\.cs:\d+"),
                new Regex(@"\(anonymous\)TracingTests\.cs:\d+")
            ]);
            await traceViewer.SelectActionAsync("Set content");
            await Expect(traceViewer.Page.Locator(".source-tab-file-name")).ToHaveAttributeAsync("title", new StackTrace(true).GetFrame(0).GetFileName());
            await Expect(traceViewer.Page.Locator(".source-line-running")).ToContainTextAsync("await page.SetContentAsync(\"<button>Click</button>\");");
        });
    }

    [PlaywrightTest("tracing.spec.ts", "should not throw when stopping without start but not exporting")]
    public async Task ShouldNotThrowWhenStoppingWithoutStartButNotExporting()
    {
        await Context.Tracing.StopAsync();
    }

    [PlaywrightTest("tracing.spec.ts", "should not throw when stopping without passing a trace file")]
    public async Task ShouldNotThrowWhenStoppingWithoutPath()
    {
        await Context.Tracing.StartAsync(new()
        {
            Snapshots = true,
        });
        await Context.Tracing.StopAsync();
    }

    [PlaywrightTest()]
    public async Task ShouldSendDotNetTitles()
    {
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true
        });

        var page = await Context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var page1 = await Context.RunAndWaitForPageAsync(() => page.ClickAsync("a"));
        Assert.AreEqual(42, await page1.EvaluateAsync<int>("1 + 41"));
        // There should be a Route.ContinueAsync() entry for it in the trace.
        await page1.RouteAsync("**/empty.html", route => route.ContinueAsync());
        await page1.GotoAsync(Server.EmptyPage, new() { Timeout = 0 });
        // For internal routes, which are not handled there should be no Route.ContinueAsync() entry.
        await page1.GotoAsync(Server.Prefix + "/one-style.html", new() { Timeout = 0 });

        using var tmp = new TempDirectory();
        var tracePath = Path.Combine(tmp.Path, "trace.zip");
        await Context.Tracing.StopAsync(new() { Path = tracePath });

        await ShowTraceViewerAsync(tracePath, async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Create page"),
                new Regex(@"Navigate to ""/empty.html"""),
                new Regex(@"Set content"),
                // TODO: Should be: Wait for event "Page"
                new Regex(@"Wait for event ""context\.WaitForEventAsync\(""Page""\)"""),
                new Regex(@"Click"),
                new Regex(@"Evaluate"),
                new Regex(@"Navigate to ""/empty.html"""),
                new Regex(@"Navigate to ""/one-style.html"""),
            ]);
        });
    }

    [PlaywrightTest()]
    public async Task ShouldDisplayWaitForLoadStateEvenIfDidNotWaitForIt()
    {
        var page = await Context.NewPageAsync();
        await Context.Tracing.StartAsync();

        await page.GotoAsync(Server.EmptyPage);
        await page.WaitForLoadStateAsync(LoadState.Load);
        await page.WaitForLoadStateAsync(LoadState.Load);

        using var tmp = new TempDirectory();
        var tracePath = Path.Combine(tmp.Path, "trace.zip");
        await Context.Tracing.StopAsync(new() { Path = tracePath });

        await ShowTraceViewerAsync(tracePath, async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Navigate to ""/empty.html"""),
                new Regex(@"Wait for event ""frame.WaitForLoadStateAsync"""),
                new Regex(@"Wait for event ""frame.WaitForLoadStateAsync"""),
            ]);
        });
    }

    [PlaywrightTest("tracing.spec.ts", "should respect tracesDir and name")]
    public async Task ShouldRespectTracesDirAndName()
    {
        using var tracesDir = new TempDirectory();
        var browser = await BrowserType.LaunchAsync(new() { TracesDir = tracesDir.Path });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await context.Tracing.StartAsync(new() { Name = "name1", Snapshots = true });
        await page.GotoAsync(Server.Prefix + "/one-style.html");
        await context.Tracing.StopChunkAsync(new() { Path = Path.Combine(tracesDir.Path, "trace1.zip") });
        Assert.True(File.Exists(Path.Combine(tracesDir.Path, "name1.trace")));
        Assert.True(File.Exists(Path.Combine(tracesDir.Path, "name1.network")));

        await context.Tracing.StartChunkAsync(new() { Name = "name2" });
        await page.GotoAsync(Server.Prefix + "/har.html");
        await context.Tracing.StopAsync(new() { Path = Path.Combine(tracesDir.Path, "trace2.zip") });
        Assert.True(File.Exists(Path.Combine(tracesDir.Path, "name2.trace")));
        Assert.True(File.Exists(Path.Combine(tracesDir.Path, "name2.network")));

        await browser.CloseAsync();


        await ShowTraceViewerAsync(Path.Combine(tracesDir.Path, "trace1.zip"), async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Navigate to ""/one-style.html"""),
            ]);
            var frame = await traceViewer.SnapshotFrame("Navigate", 0, false);
            await Expect(frame.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
            await Expect(frame.Locator("body")).ToHaveTextAsync("hello, world!");
        });

        await ShowTraceViewerAsync(Path.Combine(tracesDir.Path, "trace2.zip"), async traceViewer =>
        {
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Navigate to ""/har.html"""),
            ]);
            var frame = await traceViewer.SnapshotFrame("Navigate", 0, false);
            await Expect(frame.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
            await Expect(frame.Locator("body")).ToHaveTextAsync("hello, world!");
        });
    }

    [PlaywrightTest("tracing.spec.ts", "should show tracing.group in the action list with location")]
    public async Task ShouldShowTracingGroupInActionList()
    {
        using var tracesDir = new TempDirectory();
        await Context.Tracing.StartAsync();
        var page = await Context.NewPageAsync();

        await Context.Tracing.GroupAsync("outer group");
        await page.GotoAsync("data:text/html,<!DOCTYPE html><body><div>Hello world</div></body>");
        await Context.Tracing.GroupAsync("inner group 1");
        await page.Locator("body").ClickAsync();
        await Context.Tracing.GroupEndAsync();
        await Context.Tracing.GroupAsync("inner group 2");
        await Expect(page.GetByText("Hello")).ToBeVisibleAsync();
        await Context.Tracing.GroupEndAsync();
        await Context.Tracing.GroupEndAsync();

        var tracePath = Path.Combine(tracesDir.Path, "trace.zip");
        await Context.Tracing.StopAsync(new() { Path = tracePath });

        await ShowTraceViewerAsync(tracePath, async traceViewer =>
        {
            await traceViewer.ExpandActionAsync("inner group 1");
            await Expect(traceViewer.ActionTitles).ToHaveTextAsync([
                new Regex(@"Create page"),
                new Regex("outer group"),
                new Regex(@"Navigate to ""data"),
                new Regex("inner group 1"),
                new Regex(@"Click"),
                new Regex("inner group 2"),
                new Regex(@"Expect ""ToBeVisibleAsync""")
            ]);
        });
    }

    private async Task ShowTraceViewerAsync(string path, Func<TraceViewerPage, Task> callback)
    {
        var (executablePath, _) = Driver.GetExecutablePath();
        var traceViewerPath = Path.GetFullPath(Path.Join(Path.GetDirectoryName(executablePath), "..", "..", "package", "lib", "vite", "traceViewer"));
        var server = SimpleServer.Create(8907 + WorkerIndex * 4 + 2, traceViewerPath);
        server.SetRoute("/trace.zip", context =>
        {
            context.Response.ContentType = "application/zip";
            return context.Response.SendFileAsync(path);
        });
        var page = await Browser.NewPageAsync();
        try
        {
            await server.StartAsync(TestContext.CurrentContext.CancellationToken);
            await page.GotoAsync(server.Prefix + $"/index.html?trace={server.Prefix}/trace.zip");
            await callback(new(page));
        }
        finally
        {
            await server.StopAsync();
            await page.CloseAsync();
        }
    }
}

class TraceViewerPage(IPage page)
{
    public IPage Page { get; } = page;

    public ILocator ActionsTree => Page.GetByTestId("actions-tree");

    public ILocator ActionTitles => Page.Locator(".action-title");

    public ILocator StackFrames => Page.GetByTestId("stack-trace-list").Locator(".list-view-entry");

    public async Task SelectActionAsync(string title, int ordinal = 0)
    {
        await Page.Locator($".action-title:has-text(\"{title}\")").Nth(ordinal).ClickAsync();
    }

    public async Task SelectSnapshotAsync(string name)
    {
        await Page.ClickAsync($".snapshot-tab .tabbed-pane-tab-label:has-text(\"{name}\")");
    }

    public async Task<IFrameLocator> SnapshotFrame(string actionName, int ordinal = 0, bool hasSubframe = false)
    {
        await SelectActionAsync(actionName, ordinal);
        while (Page.Frames.Count < (hasSubframe ? 4 : 3))
        {
            var tcs = new TaskCompletionSource();
            Page.FrameAttached += (_, _) => tcs.TrySetResult();
            await tcs.Task;
        }
        return Page.FrameLocator("iframe.snapshot-visible[name=snapshot]");
    }

    internal Task ShowSourceTab() => Page.ClickAsync("text='Source'");

    internal Task ExpandActionAsync(string title, int ordinal = 0) =>
        ActionsTree.Locator(".tree-view-entry", new() { HasText = title }).Nth(ordinal).Locator(".codicon-chevron-right").ClickAsync();
}
