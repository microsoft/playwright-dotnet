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
using System.Text.RegularExpressions;
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
        string[] expectedActionApiNames = new string[] { "BrowserContext.NewPageAsync", "Page.GotoAsync", "Page.SetContentAsync", "Page.ClickAsync", "Mouse.MoveAsync", "Mouse.DblClickAsync", "Keyboard.InsertTextAsync", "Page.WaitForTimeoutAsync", "Page.CloseAsync" };
        Assert.AreEqual(expectedActionApiNames, actualActionApiNames);

        Assert.GreaterOrEqual(events.Where(e => e?.ApiName == "Page.GotoAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e?.ApiName == "Page.SetContentAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e?.ApiName == "Page.ClickAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e?.ApiName == "Mouse.MoveAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e?.ApiName == "Mouse.DblClickAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e?.ApiName == "Keyboard.InsertTextAsync").Count(), 1);
        Assert.GreaterOrEqual(events.Where(e => e?.ApiName == "Page.CloseAsync").Count(), 1);

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
            Assert.GreaterOrEqual(events.Where(x => x?.ApiName == "Page.GotoAsync").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x?.ApiName == "Page.SetContentAsync").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x?.ApiName == "Page.ClickAsync").Count(), 1);
            Assert.AreEqual(0, events.Where(x => x?.ApiName == "Page.CloseAsync").Count());
            Assert.AreEqual(0, events.Where(x => x?.ApiName == "Page.DblClickAsync").Count());
        }

        {
            var (events, resources) = ParseTrace(trace2Path);
            Assert.AreEqual("context-options", events[0].Type);
            Assert.AreEqual(0, events.Where(x => x?.ApiName == "Page.GottoAsync").Count());
            Assert.AreEqual(0, events.Where(x => x?.ApiName == "Page.SetContentAsync").Count());
            Assert.AreEqual(0, events.Where(x => x?.ApiName == "Page.ClickAsync").Count());
            Assert.GreaterOrEqual(events.Where(x => x?.ApiName == "Page.CloseAsync").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x?.ApiName == "Page.DblClickAsync").Count(), 1);
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
                    "Page.ClickAsync"
                };
            Assert.AreEqual(expectedActionApiNames, actualActionApiNames);

            Assert.GreaterOrEqual(events.Where(x => x.Type == "frame-snapshot").Count(), 1);
            Assert.GreaterOrEqual(events.Where(x => x.Type == "resource-snapshot").Count(), 1);
        }
        {
            var (events, resources) = ParseTrace(traceFile2);
            Assert.AreEqual("context-options", events[0].Type);
            string[] actualActionApiNames = GetActions(events);
            string[] expectedActionApiNames = new string[] {
                    "Page.HoverAsync"
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
        Assert.AreEqual(1, sourceNames.Count());

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
                "Page.GotoAsync"
            };
        Assert.AreEqual(expectedActionApiNames, actualActionApiNames);
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

        var (events, resources) = ParseTrace(tracePath);
        CollectionAssert.IsNotEmpty(events);

        string[] actualActionApiNames = GetActions(events);
        string[] expectedActionApiNames = new string[] {
                "Page.GotoAsync",
                "Page.WaitForLoadStateAsync",
                "Page.WaitForLoadStateAsync"
            };
        Assert.AreEqual(expectedActionApiNames, actualActionApiNames);
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

        string[] ResourceNames(Dictionary<string, byte[]> resources)
        {
            return resources.Keys
                .Select(file => Regex.Replace(file, @"^resources/.*\.(html|css)$", "resources/XXX.$1"))
                .OrderBy(file => file)
                .ToArray();
        }

        {
            var (events, resources) = ParseTrace(Path.Combine(tracesDir.Path, "trace1.zip"));
            Assert.AreEqual(new[] { "Page.GotoAsync" }, GetActions(events));
            Assert.AreEqual(new[] { "resources/XXX.css", "resources/XXX.html", "trace.network", "trace.stacks", "trace.trace" }, ResourceNames(resources));
        }

        {
            var (events, resources) = ParseTrace(Path.Combine(tracesDir.Path, "trace2.zip"));
            Assert.AreEqual(new[] { "Page.GotoAsync" }, GetActions(events));
            Assert.AreEqual(new[] { "resources/XXX.css", "resources/XXX.html", "resources/XXX.html", "trace.network", "trace.stacks", "trace.trace" }, ResourceNames(resources));
        }
    }

    private static (IReadOnlyList<TraceEventEntry> Events, Dictionary<string, byte[]> Resources) ParseTrace(string path)
    {
        Dictionary<string, byte[]> resources = new();
        using var archive = ZipFile.OpenRead(path);
        foreach (var entry in archive.Entries)
        {
            var memoryStream = new MemoryStream();
            entry.Open().CopyTo(memoryStream);
            resources.Add(entry.FullName, memoryStream.ToArray());
        }
        Dictionary<string, TraceEventEntry> actionMap = new();
        List<TraceEventEntry> events = new();
        foreach (var fileName in new[] { "trace.trace", "trace.network" })
        {
            foreach (var line in Encoding.UTF8.GetString(resources[fileName]).Split("\n"))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                var @event = JsonSerializer.Deserialize<TraceEventEntry>(line, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                if (@event.Type == "before")
                {
                    @event.Type = "action";
                    events.Add(@event);
                    actionMap[@event.CallID] = @event;
                }
                else if (@event.Type == "input")
                {
                    // might be needed for future tests
                }
                else if (@event.Type == "after")
                {
                    var existing = actionMap[@event.CallID];
                    existing.Error = @event.Error;
                }
                else
                {
                    events.Add(@event);
                }
            }
        }
        return (events, resources);
    }

    private class TraceEventEntry
    {
        public string Type { get; set; }
        public string ApiName { get; set; }
        public TraceEventError Error { get; set; }
        public double StartTime { get; set; }
        public string CallID { get; set; }
    }

    private class TraceEventError
    {
        public string Name { get; set; }

        public string Message { get; set; }
    }

    string[] GetActions(IReadOnlyList<TraceEventEntry> events) => events.Where(action => action.Type == "action").OrderBy(action => action.StartTime).Select(action => action.ApiName).ToArray();
}
