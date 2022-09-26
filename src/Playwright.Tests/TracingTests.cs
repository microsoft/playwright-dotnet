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
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright.Helpers;

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

        var (events, resources) = ParseTrace(tracePath);
        CollectionAssert.IsNotEmpty(events);

        Assert.AreEqual("context-options", events[0].Type);

        string[] actualActionApiNames = GetActions(events);
        string[] expectedActionApiNames = new string[] { "BrowserContext.NewPageAsync", "Page.GotoAsync", "Page.SetContentAsync", "Page.ClickAsync", "Mouse.MoveAsync", "Mouse.DblClickAsync", "Keyboard.InsertTextAsync", "Page.WaitForTimeoutAsync", "Page.CloseAsync", "Tracing.StopAsync" };
        Assert.AreEqual(expectedActionApiNames, actualActionApiNames);

        Assert.GreaterOrEqual(events.Where(e => e.Metadata?.ApiName == "Page.GotoAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e.Metadata?.ApiName == "Page.SetContentAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e.Metadata?.ApiName == "Page.ClickAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e.Metadata?.ApiName == "Mouse.MoveAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e.Metadata?.ApiName == "Mouse.DblClickAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e.Metadata?.ApiName == "Keyboard.InsertTextAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e.Metadata?.ApiName == "Page.CloseAsync").Count(), 1);

        Assert.GreaterOrEqual(events.Where(x => x.Type == "frame-snapshot").Count(), 1);
        Assert.GreaterOrEqual(events.Where(x => x.Type == "screencast-frame").Count(), 1);
        Assert.GreaterOrEqual(events.Where(x => x.Type == "resource-snapshot").Count(), 1);
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

        {
            var (events, resources) = ParseTrace(trace1Path);
            Assert.AreEqual("context-options", events[0].Type);
            Assert.GreaterOrEqual(events.Where(x => x.Metadata?.ApiName == "Page.GotoAsync").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x.Metadata?.ApiName == "Page.SetContentAsync").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x.Metadata?.ApiName == "Page.ClickAsync").Count(), 1);
            Assert.AreEqual(0, events.Where(x => x.Metadata?.ApiName == "Page.CloseAsync").Count());
            Assert.AreEqual(0, events.Where(x => x.Metadata?.ApiName == "Page.DblClickAsync").Count());
        }

        {
            var (events, resources) = ParseTrace(trace2Path);
            Assert.AreEqual("context-options", events[0].Type);
            Assert.AreEqual(0, events.Where(x => x.Metadata?.ApiName == "Page.GottoAsync").Count());
            Assert.AreEqual(0, events.Where(x => x.Metadata?.ApiName == "Page.SetContentAsync").Count());
            Assert.AreEqual(0, events.Where(x => x.Metadata?.ApiName == "Page.ClickAsync").Count());
            Assert.GreaterOrEqual(events.Where(x => x.Metadata?.ApiName == "Page.CloseAsync").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x.Metadata?.ApiName == "Page.DblClickAsync").Count(), 1);
        }

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

        {
            var (events, resources) = ParseTrace(traceFile1);
            Assert.AreEqual("context-options", events[0].Type);
            string[] actualActionApiNames = GetActions(events);
            string[] expectedActionApiNames = new string[] {
                    "Page.SetContentAsync",
                    "Page.ClickAsync",
                    "Page.ClickAsync",
                    "Tracing.StopChunkAsync",
                };
            Assert.AreEqual(expectedActionApiNames, actualActionApiNames);

            Assert.GreaterOrEqual(events.Where(x => x.Metadata?.ApiName == "Page.ClickAsync" && x.Metadata?.Error == null).Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x.Metadata?.ApiName == "Page.ClickAsync" && x.Metadata?.Error?.Error?.Message == "Action was interrupted").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x.Type == "frame-snapshot").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x.Type == "resource-snapshot").Count(), 1);
        }
        {
            var (events, resources) = ParseTrace(traceFile2);
            Assert.AreEqual("context-options", events[0].Type);
            string[] actualActionApiNames = GetActions(events);
            string[] expectedActionApiNames = new string[] {
                    "Page.HoverAsync",
                    "Tracing.StopChunkAsync",
                };
            Assert.AreEqual(expectedActionApiNames, actualActionApiNames);

            Assert.GreaterOrEqual(events.Where(x => x.Type == "frame-snapshot").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x.Type == "resource-snapshot").Count(), 1);
        }
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
        await page.ClickAsync("\"Click\"");
        await page.CloseAsync();

        using var tmp = new TempDirectory();
        var tracePath = Path.Combine(tmp.Path, "trace.zip");
        await Context.Tracing.StopAsync(new() { Path = tracePath });

        var (events, resources) = ParseTrace(tracePath);
        var sourceNames = resources.Keys.Where(key => key.EndsWith(".txt")).ToArray();
        Assert.AreEqual(sourceNames.Count(), 1);

        var sourceTraceFileContent = resources[sourceNames[0]];
        var currentFileContent = File.ReadAllText(new StackTrace(true).GetFrame(0).GetFileName());

        Assert.AreEqual(sourceTraceFileContent, currentFileContent);
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
    public async Task ShouldSendDotNetApiNames()
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

        var (events, resources) = ParseTrace(tracePath);
        CollectionAssert.IsNotEmpty(events);

        string[] actualActionApiNames = GetActions(events);
        string[] expectedActionApiNames = new string[] {
                "BrowserContext.NewPageAsync",
                "Page.GotoAsync",
                "Page.SetContentAsync",
                "BrowserContext.RunAndWaitForPageAsync",
                "Page.ClickAsync",
                "Page.EvaluateAsync",
                "Page.RouteAsync",
                "Page.GotoAsync",
                "Route.ContinueAsync",
                "Page.GotoAsync",
                "Tracing.StopAsync"
            };
        Assert.AreEqual(expectedActionApiNames, actualActionApiNames);
    }

    private static (IReadOnlyList<TraceEventEntry> Events, Dictionary<string, byte[]> Resources) ParseTrace(string path)
    {
        Dictionary<string, byte[]> resources = new();
        using var archive = ZipFile.OpenRead(path);
        foreach (var entry in archive.Entries)
        {
            var memoryStream = new MemoryStream();
            entry.Open().CopyTo(memoryStream);
            resources.Add(entry.Name, memoryStream.ToArray());
        }
        List<TraceEventEntry> events = new();
        foreach (var fileName in new[] { "trace.trace", "trace.network" })
        {
            foreach (var line in Encoding.UTF8.GetString(resources[fileName]).Split("\n"))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    events.Add(JsonSerializer.Deserialize<TraceEventEntry>(line,
                            new JsonSerializerOptions()
                            {
                                PropertyNameCaseInsensitive = true,
                            }));
                }
            }
        }
        return (events, resources);
    }

    private class TraceEventEntry
    {
        public string Type { get; set; }

        public TraceEventMetadata Metadata { get; set; }
    }

    private class TraceEventMetadata
    {
        public string Type { get; set; }

        public string Method { get; set; }

        public string PageId { get; set; }

        public string ApiName { get; set; }

        public bool Internal { get; set; }

        public double StartTime { get; set; }

        public TraceEventErrorWrapper Error { get; set; }
    }

    private class TraceEventErrorWrapper
    {
        public TraceEventError Error { get; set; }
    }

    private class TraceEventError
    {
        public string Name { get; set; }

        public string Message { get; set; }
    }

    string[] GetActions(IReadOnlyList<TraceEventEntry> events) => events.Where(action => action.Type == "action" && !action.Metadata.Internal).OrderBy(action => action.Metadata.StartTime).Select(action => action.Metadata.ApiName).ToArray();
}
