/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/tracing.spec.ts.</summary>
public class TracingTests
{
    [BeforeEach]
    public static void Skip(string trace) => Info.Skip(trace == "on");

    [UpstreamTest("TR-002 should use the correct title for event driven callbacks")]
    public async Task ShouldUseTheCorrectTitleForEventDrivenCallbacks(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync();
        // route.* calls should not be included in the trace
        await page.RouteAsync("**/empty.html", route => route.ContinueAsync());
        await page.GotoAsync(server.Prefix + "/empty.html");
        await page.GotoAsync(server.Prefix + "/grid.html");

        // The default internal dialog handler should not provide an action.
        await page.EvaluateAsync("() => alert('yo')");
        await page.ReloadAsync();
        // now we do it again with a dialog event listener attached which should produce an action.
        page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.AcceptAsync("answer!"));
        };
        await page.EvaluateAsync("() => alert('yo')");

        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });
        var (events, _, actions, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        Assert.That(events[0].GetProperty("type").GetString(), Is.EqualTo("context-options"));
        Assert.That(actions, Is.EqualTo(new[]
        {
            "Route requests",
            $"Navigate {server.Host}/empty.html",
            "Continue request",
            $"Navigate {server.Host}/grid.html",
            "Evaluate",
            "Reload",
            "Evaluate",
            "Accept dialog",
        }));
    }

    [UpstreamTest("TR-003 should not store secrets in context options")]
    public async Task ShouldNotStoreSecretsInContextOptions(IBrowser browser, Server server, TestInfo testInfo, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            BaseURL = server.Prefix,
            ViewportSize = new() { Width = 500, Height = 600 },
            DeviceScaleFactor = 2,
            IsMobile = false,
            UserAgent = "custom-ua",
            HttpCredentials = new() { Username = "user", Password = "secret-password" },
            ExtraHTTPHeaders = new Dictionary<string, string> { ["x-secret"] = "secret-header" },
            StorageState = JsonStringify(new { cookies = new[] { new { name = "session", value = "secret-cookie", domain = "localhost", path = "/", expires = -1, httpOnly = false, secure = false, sameSite = "Lax" } }, origins = Array.Empty<object>() }),
        });
        await context.Tracing.StartAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });
        await context.CloseAsync();

        var (events, _, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        Assert.That(events[0].GetProperty("type").GetString(), Is.EqualTo("context-options"));
        ExpectToEqual(events[0].GetProperty("options"), new
        {
            baseURL = server.Prefix,
            viewport = new { width = 500, height = 600 },
            deviceScaleFactor = 2,
            isMobile = false,
            userAgent = "custom-ua",
        });
        var traceText = Encoding.Latin1.GetString(File.ReadAllBytes(testInfo.OutputPath("trace.zip")));
        Assert.That(traceText, Does.Not.Contain("secret-password"));
        Assert.That(traceText, Does.Not.Contain("secret-header"));
        Assert.That(traceText, Does.Not.Contain("secret-cookie"));
    }

    [UpstreamTest("TR-004 should not collect snapshots by default")]
    public async Task ShouldNotCollectSnapshotsByDefault(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync();
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<button>Click</button>");
        await page.ClickAsync("\"Click\"");
        await page.CloseAsync();
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        var (events, _, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        Assert.That(events.Any(e => e.GetProperty("type").GetString() == "frame-snapshot"), Is.False);
        Assert.That(events.Any(e => e.GetProperty("type").GetString() == "resource-snapshot"), Is.False);
    }

    [UpstreamTest("TR-005 should not collect action screenshots and aria snapshots by default")]
    public async Task ShouldNotCollectActionScreenshotsAndAriaSnapshotsByDefault(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Snapshots = true });
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        var (events, _, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        Assert.That(events.Any(e => e.GetProperty("type").GetString() == "screenshot"), Is.False);
        Assert.That(events.Any(e => e.GetProperty("type").GetString() == "aria-snapshot"), Is.False);
    }

    [UpstreamTest("TR-006 should collect action screenshots")]
    public async Task ShouldCollectActionScreenshots(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { ScreenSnapshots = true });
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        var (events, resources, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        var clickCallId = events.First(e => e.GetProperty("type").GetString() == "before" && e.GetProperty("method").GetString() == "click").GetProperty("callId").GetString();
        var screenshots = events.Where(e => e.GetProperty("type").GetString() == "screenshot" && e.GetProperty("callId").GetString() == clickCallId).ToList();
        Assert.That(screenshots.Select(e => e.GetProperty("phase").GetString()), Is.EqualTo(new[] { "before", "action", "after" }));
        foreach (var screenshot in screenshots)
        {
            Assert.That(screenshot.GetProperty("file").GetString(), Is.EqualTo($"screenshots/{clickCallId}-{screenshot.GetProperty("phase").GetString()}.png"));
            var buffer = resources[screenshot.GetProperty("file").GetString()];
            // .NET: PNG.sync.read(buffer)
            Assert.That(ImageComparator.Decode("image/png", buffer).Width, Is.GreaterThan(0));
        }
    }

    [UpstreamTest("TR-007 should collect aria snapshots")]
    public async Task ShouldCollectAriaSnapshots(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { AriaSnapshots = true });
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        var (events, resources, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        var clickCallId = events.First(e => e.GetProperty("type").GetString() == "before" && e.GetProperty("method").GetString() == "click").GetProperty("callId").GetString();
        var ariaSnapshots = events.Where(e => e.GetProperty("type").GetString() == "aria-snapshot" && e.GetProperty("callId").GetString() == clickCallId).ToList();
        Assert.That(ariaSnapshots.Select(e => e.GetProperty("phase").GetString()), Is.EqualTo(new[] { "before", "action", "after" }));
        bool hasButton(JsonElement nodes) => nodes.EnumerateArray().Any(node => node.ValueKind == JsonValueKind.Object && ((node.TryGetProperty("role", out var role) && role.GetString() == "button" && node.TryGetProperty("name", out var name) && name.GetString() == "Click target") || (node.TryGetProperty("children", out var children) && hasButton(children))));
        foreach (var ariaSnapshot in ariaSnapshots)
        {
            Assert.That(ariaSnapshot.GetProperty("file").GetString(), Is.EqualTo($"aria/{clickCallId}-{ariaSnapshot.GetProperty("phase").GetString()}.json"));
            var snapshot = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(resources[ariaSnapshot.GetProperty("file").GetString()]));
            Assert.That(hasButton(snapshot), Is.EqualTo(true));
        }
    }

    [UpstreamTest("TR-008 can call tracing.group/groupEnd at any time and auto-close")]
    public async Task CanCallTracingGroupGroupEndAtAnyTimeAndAutoClose(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.GroupAsync("ignored");
        await context.Tracing.GroupEndAsync();
        await context.Tracing.GroupAsync("ignored2");

        await context.Tracing.StartAsync();
        await context.Tracing.GroupAsync("actual");
        await page.GotoAsync(server.EmptyPage);
        await context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        await context.Tracing.GroupAsync("ignored3");
        await context.Tracing.GroupEndAsync();
        await context.Tracing.GroupEndAsync();
        await context.Tracing.GroupEndAsync();

        var (events, _, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        var groups = events.Where(e => e.TryGetProperty("method", out var method) && method.GetString() == "tracingGroup").ToList();
        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.That(groups[0].GetProperty("title").GetString(), Is.EqualTo("actual"));
        Assert.That(events.Any(e => e.GetProperty("type").GetString() == "after" && e.GetProperty("callId").GetString() == groups[0].GetProperty("callId").GetString()), Is.EqualTo(true));
    }

    [UpstreamTest("TR-009 should not include buffers in the trace")]
    public async Task ShouldNotIncludeBuffersInTheTrace(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Snapshots = true });
        await page.GotoAsync(server.Prefix + "/empty.html");
        await page.ScreenshotAsync();
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });
        var (events, _, _, actionObjects, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        var screenshotEvent = actionObjects.Find(a => a["method"].GetString() == "screenshot");
        var phases = events.Where(e => e.GetProperty("type").GetString() == "frame-snapshot" && e.GetProperty("snapshot").GetProperty("callId").GetString() == screenshotEvent["callId"].GetString()).Select(e => e.GetProperty("snapshot").GetProperty("phase").GetString()).ToList();
        Assert.That(phases, Does.Contain("before"));
        Assert.That(phases, Does.Contain("after"));
        ExpectToEqual(screenshotEvent["result"], new
        {
            binary = "<Buffer>",
        });
    }

    [UpstreamTest("TR-010 should exclude internal pages")]
    public async Task ShouldExcludeInternalPages(string browserName, IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await page.GotoAsync(server.EmptyPage);

        await context.Tracing.StartAsync();
        await context.StorageStateAsync();
        await page.CloseAsync();
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        var trace = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        var pageIds = new HashSet<string>();
        trace.Events.ForEach(e =>
        {
            var pageId = (e.TryGetProperty("pageId", out var eventPageId) ? eventPageId.GetString() : null) ?? (e.TryGetProperty("params", out var @params) && @params.ValueKind == JsonValueKind.Object && @params.TryGetProperty("pageId", out var paramsPageId) ? paramsPageId.GetString() : null);
            if (!string.IsNullOrEmpty(pageId))
            {
                pageIds.Add(pageId);
            }
        });
        Assert.That(pageIds.Count, Is.EqualTo(1));
    }

    [UpstreamTest("TR-011 should record context API request trace independently")]
    public async Task ShouldRecordContextAPIRequestTraceIndependently(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        var browserTracePath = testInfo.OutputPath("browser-trace.zip");
        var apiTracePath = testInfo.OutputPath("api-trace.zip");
        var apiURL = server.Prefix + "/simple.json";
        Assert.That(context.APIRequest.Tracing, Is.Not.SameAs(context.Tracing));

        await context.Tracing.StartAsync(new() { Snapshots = true });
        await context.APIRequest.Tracing.StartAsync(new() { Snapshots = true });
        await page.GotoAsync(server.Prefix + "/one-style.html");
        await page.APIRequest.PostAsync(apiURL, new() { DataObject = new { foo = "bar" } });
        await context.Tracing.StopAsync(new() { Path = browserTracePath });
        await context.APIRequest.Tracing.StopAsync(new() { Path = apiTracePath });

        var browserTrace = await ParseTraceRaw(browserTracePath);
        Assert.That(browserTrace.Actions, Does.Contain($"Navigate {server.Host}/one-style.html"));
        Assert.That(browserTrace.Actions, Does.Not.Contain($"POST {server.Host}/simple.json"));
        Assert.That(browserTrace.Events.Any(@event => @event.GetProperty("type").GetString() == "resource-snapshot" && @event.GetProperty("snapshot").GetProperty("request").GetProperty("url").GetString().EndsWith("/simple.json", StringComparison.Ordinal)), Is.EqualTo(false));
        Assert.That(browserTrace.Events.Any(@event => @event.GetProperty("type").GetString() == "resource-snapshot" && @event.GetProperty("snapshot").GetProperty("request").GetProperty("url").GetString().EndsWith("/one-style.html", StringComparison.Ordinal)), Is.EqualTo(true));

        var apiTrace = await ParseTraceRaw(apiTracePath);
        Assert.That(apiTrace.Actions, Does.Contain($"POST {server.Host}/simple.json"));
        Assert.That(apiTrace.Actions, Does.Not.Contain($"Navigate {server.Host}/one-style.html"));
        var apiAction = apiTrace.ActionObjects.Find(action => action["class"].GetString() == "APIRequestContext" && action["method"].GetString() == "fetch");
        // .NET: ['tracing.spec.ts'], the test is in this C# file
        Assert.That(RelativeStack(apiAction, apiTrace.Stacks), Is.EqualTo(new[] { "TracingTests.cs" }));
        Assert.That(apiTrace.Events.Where(@event => @event.GetProperty("type").GetString() == "resource-snapshot").Select(@event => @event.GetProperty("snapshot").GetProperty("request").GetProperty("url").GetString()), Is.EqualTo(new[] { apiURL }));
        ExpectToEqual(apiTrace.Events.Where(@event => @event.GetProperty("type").GetString() == "resource-snapshot").Select(@event => @event.GetProperty("snapshot").GetProperty("_apiRequestRef").GetString()).ToArray(), new object[] { StringMatching(new Regex("^request-context@")) });
    }

    [UpstreamTest("TR-012 should collect two traces")]
    public async Task ShouldCollectTwoTraces(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<button>Click</button>");
        await page.ClickAsync("\"Click\"");
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace1.zip") });

        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        await page.DblClickAsync("\"Click\"");
        await page.CloseAsync();
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace2.zip") });

        {
            var (events, _, actions, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace1.zip"));
            Assert.That(events[0].GetProperty("type").GetString(), Is.EqualTo("context-options"));
            Assert.That(actions, Is.EqualTo(new[]
            {
                $"Navigate {server.Host}/empty.html",
                "Set content",
                "Click locator('text=\"Click\"')",
            }));
        }

        {
            var (events, _, actions, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace2.zip"));
            Assert.That(events[0].GetProperty("type").GetString(), Is.EqualTo("context-options"));
            Assert.That(actions, Is.EqualTo(new[]
            {
                "Double click locator('text=\"Click\"')",
                "Close page",
            }));
        }
    }

    [UpstreamTest("TR-013 should respect tracesDir and name")]
    public async Task ShouldRespectTracesDirAndName(IBrowserType browserType, Server server, string mode, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        var tracesDir = testInfo.OutputPath("traces");
        var browser = await browserType.LaunchAsync(new(launchOptions) { TracesDir = tracesDir });
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();

        await context.Tracing.StartAsync(new() { Name = "name1", Snapshots = true });
        await page.GotoAsync(server.Prefix + "/one-style.html");
        await context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace1.zip") });
        Assert.That(File.Exists(Path.Combine(tracesDir, "name1.trace")), Is.EqualTo(true));
        Assert.That(File.Exists(Path.Combine(tracesDir, "name1.network")), Is.EqualTo(true));

        await context.Tracing.StartChunkAsync(new() { Name = "name2" });
        await page.GotoAsync(server.Prefix + "/har.html");
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace2.zip") });
        Assert.That(File.Exists(Path.Combine(tracesDir, "name2.trace")), Is.EqualTo(true));
        Assert.That(File.Exists(Path.Combine(tracesDir, "name2.network")), Is.EqualTo(true));

        await browser.CloseAsync();

        string[] resourceNames(Dictionary<string, byte[]> resources)
        {
            return resources.Keys.Select(file =>
            {
                return Regex.Replace(file, @"^resources\/.*\.(html|css)$", "resources/XXX.$1");
            }).Order(StringComparer.Ordinal).ToArray();
        }

        {
            var (_, resources, actions, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace1.zip"));
            Assert.That(actions, Is.EqualTo(new[] { $"Navigate {server.Host}/one-style.html" }));
            Assert.That(resourceNames(resources), Is.EqualTo(new[]
            {
                "resources/XXX.css",
                "resources/XXX.html",
                "trace.network",
                "trace.stacks",
                "trace.trace",
            }));
        }

        {
            var (_, resources, actions, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace2.zip"));
            Assert.That(actions, Is.EqualTo(new[] { $"Navigate {server.Host}/har.html" }));
            Assert.That(resourceNames(resources), Is.EqualTo(new[]
            {
                "resources/XXX.css",
                "resources/XXX.html",
                "resources/XXX.html",
                "trace.network",
                "trace.stacks",
                "trace.trace",
            }));
        }
    }

    [UpstreamTest("TR-014 should not include trace resources from the previous chunks")]
    public async Task ShouldNotIncludeTraceResourcesFromThePreviousChunks(IBrowserContext context, IPage page, Server server, string browserName, string mode, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

        await context.Tracing.StartChunkAsync();
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <style>
      @keyframes move {
        from { marign-left: 0; }
        to   { margin-left: 1000px; }
      }
      button {
        animation: 20s linear move;
        animation-iteration-count: infinite;
      }
    </style>
    <button>Click</button>
  ");
        await page.ClickAsync("\"Click\"", new() { Force = true });
        // Give it enough time for both screenshots to get into the trace.
        await Task.Delay(3000);
        await context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace1.zip") });

        await context.Tracing.StartChunkAsync();
        await context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace2.zip") });

        string[] jpegs = [];
        {
            var (_, resources, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace1.zip"));
            var names = resources.Keys.ToArray();
            Assert.That(names.Count(n => n.EndsWith(".html", StringComparison.Ordinal)), Is.EqualTo(1));
            jpegs = names.Where(n => n.EndsWith(".jpeg", StringComparison.Ordinal)).ToArray();
            // 1 source file for the test.
            Assert.That(names.Count(n => n.StartsWith("src/", StringComparison.Ordinal)), Is.EqualTo(1));
        }

        {
            var (_, resources, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace2.zip"));
            var names = resources.Keys.ToArray();
            // 1 network resource should be preserved.
            Assert.That(names.Count(n => n.EndsWith(".html", StringComparison.Ordinal)), Is.EqualTo(1));
            // screenshots from the previous chunk should not be preserved.
            Assert.That(names.Count(n => jpegs.Contains(n)), Is.EqualTo(0));
            // 0 source files for the second test.
            Assert.That(names.Count(n => n.StartsWith("src/", StringComparison.Ordinal)), Is.EqualTo(0));
        }
    }

    [UpstreamTest("TR-015 should overwrite existing file")]
    public async Task ShouldOverwriteExistingFile(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<button>Click</button>");
        await page.ClickAsync("\"Click\"");
        var path = testInfo.OutputPath("trace1.zip");
        await context.Tracing.StopAsync(new() { Path = path });
        {
            var (_, resources, _, _, _) = await ParseTraceRaw(path);
            var names = resources.Keys.ToArray();
            Assert.That(names.Count(n => n.EndsWith(".html", StringComparison.Ordinal)), Is.EqualTo(1));
        }

        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        await context.Tracing.StopAsync(new() { Path = path });

        {
            var (_, resources, _, _, _) = await ParseTraceRaw(path);
            var names = resources.Keys.ToArray();
            Assert.That(names.Count(n => n.EndsWith(".html", StringComparison.Ordinal)), Is.EqualTo(0));
        }
    }

    [UpstreamTest("TR-016 should collect sources")]
    public async Task ShouldCollectSources(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Sources = true });
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<button>Click</button>");
        await page.ClickAsync("\"Click\"");
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace1.zip") });

        var (_, resources, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace1.zip"));
        var sourceNames = resources.Keys.Where(k => k.StartsWith("src/", StringComparison.Ordinal)).ToArray();
        Assert.That(sourceNames.Length, Is.EqualTo(1));
        // .NET: /^src\/[0-9a-f]{40}\.ts$/, the source of this test is a C# file
        Assert.That(sourceNames[0], Does.Match(@"^src\/[0-9a-f]{40}\.cs$"));
        var sourceFile = resources[sourceNames[0]];
        // .NET: __filename
        var thisFile = await File.ReadAllBytesAsync(new System.Diagnostics.StackFrame(true).GetFileName());
        Assert.That(sourceFile, Is.EqualTo(thisFile));
    }

    [UpstreamTest("TR-017 should record network failures")]
    public async Task ShouldRecordNetworkFailures(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Snapshots = true });
        await page.RouteAsync("**/*", route => route.AbortAsync("connectionaborted"));
        await Catch(() => page.GotoAsync(server.EmptyPage));
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace1.zip") });

        var (events, _, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace1.zip"));
        var requestEvent = events.FirstOrDefault(e => e.GetProperty("type").GetString() == "resource-snapshot" && e.GetProperty("snapshot").GetProperty("response").TryGetProperty("_failureText", out var failureText) && !string.IsNullOrEmpty(failureText.GetString()));
        Assert.That(requestEvent.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined));
        Assert.That(requestEvent.GetProperty("snapshot").GetProperty("_monotonicTime").GetDouble(), Is.GreaterThan(0));
        Assert.That(requestEvent.GetProperty("snapshot").GetProperty("time").GetDouble(), Is.GreaterThanOrEqualTo(0));
    }

    [UpstreamTest("TR-018 should recover tracing after a failed stop")]
    public async Task ShouldRecoverTracingAfterAFailedStop(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync();
        // Saving fails: a parent of the destination is a file, not a directory.
        var blocker = testInfo.OutputPath("blocker");
        await File.WriteAllTextAsync(blocker, string.Empty);
        var error = await ThrowsAsync<PlaywrightException>(() => context.Tracing.StopAsync(new() { Path = Path.Combine(blocker, "trace1.zip") }));
        Assert.That(error.Message, Does.Match("ENOTDIR|ENOENT|EEXIST"));

        // The failed stop must not wedge tracing for the rest of the context lifetime.
        await context.Tracing.StartAsync();
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace2.zip") });

        var (events, _, actions, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace2.zip"));
        Assert.That(events[0].GetProperty("type").GetString(), Is.EqualTo("context-options"));
        Assert.That(actions, Does.Contain("Click locator('button')"));
    }

    [UpstreamTest("TR-020 should not crash when browser closes mid-trace")]
    public async Task ShouldNotCrashWhenBrowserClosesMidTrace(IBrowserType browserType, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        var browser = await browserType.LaunchAsync(launchOptions);
        var page = await browser.NewPageAsync(newPageOptions);
        await page.Context.Tracing.StartAsync(new() { Snapshots = true, Screenshots = true });
        await page.GotoAsync(server.EmptyPage);
        await browser.CloseAsync();
        await Task.Delay(1000);  // Give it some time to throw errors
    }

    [UpstreamTest("TR-022 should not stall on dialogs")]
    public async Task ShouldNotStallOnDialogs(IPage page, IBrowserContext context, Server server)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        await page.GotoAsync(server.EmptyPage);

        page.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };

        await page.EvaluateAsync(@"() => {
    confirm('are you sure');
  }");
        await context.Tracing.StopAsync();
    }

    [UpstreamTest("TR-023 should produce screencast frames ${params.id}")]
    [Case("fit", 500, 500)]
    // Less than 450 to test firefox
    [Case("crop", 400, 800)]
    [Case("scale", 1024, 768)]
    public async Task ShouldProduceScreencastFrames(string id, int width, int height, string video, ContextFactory contextFactory, string browserName, string platform, bool headless, bool isHeadlessShell, TestInfo testInfo)
    {
        Info.Skip(video == "on", "Same screencast resolution conflicts");
        Info.Fixme(browserName == "chromium" && !isHeadlessShell, "Chromium (but not headless-shell) screencast has a min width issue");
        Info.Fixme(id == "fit" && browserName == "chromium" && platform == "darwin", "High DPI maxes image at 600x600");
        Info.Fixme(id == "fit" && browserName == "webkit" && platform == "linux", "Image size is flaky");
        Info.Fixme(browserName == "firefox" && !headless, "Image size is different");

        var scale = Math.Min(Math.Min(800d / width, 800d / height), 1);
        var previewWidth = width * scale;
        var previewHeight = height * scale;

        var context = await contextFactory(new() { ViewportSize = new() { Width = width, Height = height } });
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        var page = await context.NewPageAsync();
        // Make sure we have a chance to paint.
        for (var i = 0; i < 100; ++i)
        {
            var percentage = (i + 1) + "%";
            await page.SetContentAsync($"<body style=\"box-sizing: border-box; width: 100%; height: 100%; margin:0; background: linear-gradient(to bottom, red {percentage}, transparent {percentage}); border: 50px solid blue\"></body>");
            await Rafraf(page);
        }
        for (var i = 0; i < 10; ++i)
        {
            await Rafraf(page);
        }
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        var (events, resources, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        var frames = events.Where(e => e.GetProperty("type").GetString() == "screencast-frame").ToList();

        // Check all frame sizes.
        // .NET: frame2, buffer2 and image2 are frame, buffer and image, which the test declares again below.
        foreach (var frame2 in frames)
        {
            Assert.That(frame2.GetProperty("width").GetDouble(), Is.EqualTo(width));
            Assert.That(frame2.GetProperty("height").GetDouble(), Is.EqualTo(height));
            var buffer2 = resources[frame2.GetProperty("file").GetString()];
            // .NET: jpegjs.decode(buffer)
            var image2 = ImageComparator.Decode("image/jpeg", buffer2);
            Assert.That(image2.Width, Is.EqualTo(previewWidth));
            Assert.That(image2.Height, Is.EqualTo(previewHeight));
        }

        var frame = frames[frames.Count - 1]; // pick last frame.
        var buffer = resources[frame.GetProperty("file").GetString()];
        // .NET: jpegjs.decode(buffer)
        var image = ImageComparator.Decode("image/jpeg", buffer);
        Assert.That(image.Data.Length, Is.EqualTo(previewWidth * previewHeight * 4));
        ExpectRed(image.Data, (previewWidth * previewHeight * 4 / 2) + (previewWidth * 4 / 2)); // center is red
        ExpectBlue(image.Data, (previewWidth * 5 * 4) + (previewWidth * 4 / 2)); // top
        ExpectBlue(image.Data, (previewWidth * (previewHeight - 5) * 4) + (previewWidth * 4 / 2)); // bottom
        ExpectBlue(image.Data, (previewWidth * previewHeight * 4 / 2) + (5 * 4)); // left
        ExpectBlue(image.Data, (previewWidth * previewHeight * 4 / 2) + ((previewWidth - 5) * 4)); // right
    }

    [UpstreamTest("TR-024 should include interrupted actions")]
    public async Task ShouldIncludeInterruptedActions(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<button>Click</button>");
        IgnoreErrors(page.ClickAsync("\"ClickNoButton\""));
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") });
        await context.CloseAsync();

        var (_, _, actions, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        Assert.That(actions, Does.Contain("Click locator('text=\"ClickNoButton\"')"));
    }

    [UpstreamTest("TR-025 should throw when starting with different options")]
    public async Task ShouldThrowWhenStartingWithDifferentOptions(IBrowserContext context)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        var error = await Catch(() => context.Tracing.StartAsync(new() { Screenshots = false, Snapshots = false }));
        Assert.That(error.Message, Does.Contain("Tracing has been already started"));
    }

    [UpstreamTest("TR-026 should throw when stopping without start")]
    public async Task ShouldThrowWhenStoppingWithoutStart(IBrowserContext context, TestInfo testInfo)
    {
        var error = await Catch(() => context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") }));
        Assert.That(error.Message, Does.Contain("Must start tracing before stopping"));
    }

    [UpstreamTest("TR-027 should not throw when stopping without start but not exporting")]
    public async Task ShouldNotThrowWhenStoppingWithoutStartButNotExporting(IBrowserContext context, TestInfo testInfo)
    {
        await context.Tracing.StopAsync();
    }

    [UpstreamTest("TR-028 start should return a disposable that discards the trace")]
    public async Task StartShouldReturnADisposableThatDiscardsTheTrace(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        var disposable = await context.Tracing.StartAsync();
        await page.GotoAsync(server.EmptyPage);
        await disposable.DisposeAsync();
        var error = await Catch(() => context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") }));
        Assert.That(error.Message, Does.Contain("Must start tracing before stopping"));
    }

    [UpstreamTest("TR-029 startChunk should return a disposable that discards the chunk")]
    public async Task StartChunkShouldReturnADisposableThatDiscardsTheChunk(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync();
        var disposable = await context.Tracing.StartChunkAsync();
        await page.GotoAsync(server.EmptyPage);
        await disposable.DisposeAsync();
        var error = await Catch(() => context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace.zip") }));
        Assert.That(error.Message, Does.Contain("Must start tracing before stopping"));
    }

    [UpstreamTest("TR-030 should work with multiple chunks")]
    public async Task ShouldWorkWithMultipleChunks(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        await page.GotoAsync(server.Prefix + "/frames/frame.html");

        await context.Tracing.StartChunkAsync();
        await page.SetContentAsync("<button>Click</button>");
        await page.ClickAsync("\"Click\"");
        IgnoreErrors(page.ClickAsync("\"ClickNoButton\"", new() { Timeout = 0 }));
        await page.EvaluateAsync("() => {}");
        await context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        await context.Tracing.StartChunkAsync();
        await page.HoverAsync("\"Click\"");
        await context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace2.zip") });

        await context.Tracing.StartChunkAsync();
        await page.ClickAsync("\"Click\"");
        await context.Tracing.StopChunkAsync();  // Should stop without a path.

        var trace1 = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        Assert.That(trace1.Events[0].GetProperty("type").GetString(), Is.EqualTo("context-options"));
        Assert.That(trace1.Actions, Is.EqualTo(new[]
        {
            "Set content",
            "Click locator('text=\"Click\"')",
            "Click locator('text=\"ClickNoButton\"')",
            "Evaluate",
        }));
        Assert.That(trace1.Events.Any(e => e.GetProperty("type").GetString() == "frame-snapshot"), Is.True);
        Assert.That(trace1.Events.Any(e => e.GetProperty("type").GetString() == "resource-snapshot" && e.GetProperty("snapshot").GetProperty("request").GetProperty("url").GetString().EndsWith("style.css", StringComparison.Ordinal)), Is.True);

        var trace2 = await ParseTraceRaw(testInfo.OutputPath("trace2.zip"));
        Assert.That(trace2.Events[0].GetProperty("type").GetString(), Is.EqualTo("context-options"));
        Assert.That(trace2.Actions, Is.EqualTo(new[]
        {
            "Hover locator('text=\"Click\"')",
        }));
        Assert.That(trace2.Events.Any(e => e.GetProperty("type").GetString() == "frame-snapshot"), Is.True);
        Assert.That(trace2.Events.Any(e => e.GetProperty("type").GetString() == "resource-snapshot" && e.GetProperty("snapshot").GetProperty("request").GetProperty("url").GetString().EndsWith("style.css", StringComparison.Ordinal)), Is.True);
    }

    [UpstreamTest("TR-031 should export trace concurrently to second navigation")]
    public async Task ShouldExportTraceConcurrentlyToSecondNavigation(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        for (var timeout = 0; timeout < 200; timeout += 20)
        {
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
            await page.GotoAsync(server.Prefix + "/grid.html");

            // Navigate to the same page to produce the same trace resources
            // that might be concurrently exported.
            var promise = page.GotoAsync(server.Prefix + "/grid.html");
            await page.WaitForTimeoutAsync(timeout);
            await PromiseAll(
                promise,
                context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace.zip") }));
        }
    }

    [UpstreamTest("TR-032 should not hang for clicks that open dialogs")]
    public async Task ShouldNotHangForClicksThatOpenDialogs(IBrowserContext context, IPage page)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        var dialogPromise = Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h);
        await page.SetContentAsync("<div onclick='window.alert(123)'>Click me</div>");
        await Catch(() => page.ClickAsync("div", new() { Timeout = 3500 }));
        var dialog = await dialogPromise;
        await dialog.DismissAsync();
        await context.Tracing.StopAsync();
    }

    [UpstreamTest("TR-033 should ignore iframes in head")]
    public async Task ShouldIgnoreIframesInHead(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvaluateAsync(@"() => {
    document.head.appendChild(document.createElement('iframe'));
    // Add iframe in a shadow tree.
    const div = document.createElement('div');
    document.head.appendChild(div);
    const shadow = div.attachShadow({ mode: 'open' });
    shadow.appendChild(document.createElement('iframe'));
  }");

        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        await page.ClickAsync("button");
        await context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace.zip") });

        var trace = await ParseTraceRaw(testInfo.OutputPath("trace.zip"));
        Assert.That(trace.Actions, Is.EqualTo(new[] { "Click locator('button')" }));
        Assert.That(trace.Events.Any(e => e.GetProperty("type").GetString() == "frame-snapshot"), Is.True);
        Assert.That(trace.Events.Any(e => e.GetProperty("type").GetString() == "frame-snapshot" && JsonStringify(e.GetProperty("snapshot").GetProperty("html")).Contains("IFRAME", StringComparison.Ordinal)), Is.False);
    }

    [UpstreamTest("TR-034 should hide internal stack frames")]
    public async Task ShouldHideInternalStackFrames(IBrowserContext context, IPage page, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        Task evalPromise = null;
        page.Dialog += (_, dialog) =>
        {
            evalPromise = page.EvaluateAsync("2+2");
            IgnoreErrors(dialog.DismissAsync());
        };
        await page.SetContentAsync("<div onclick='window.alert(123)'>Click me</div>");
        await page.ClickAsync("div");
        await evalPromise;
        var tracePath = testInfo.OutputPath("trace.zip");
        await context.Tracing.StopAsync(new() { Path = tracePath });

        var trace = await ParseTraceRaw(tracePath);
        var actions = trace.ActionObjects.Where(a => a["class"].GetString() != "Tracing").ToList();
        Assert.That(actions, Has.Count.EqualTo(4));
        foreach (var action in actions)
        {
            // .NET: ['tracing.spec.ts'], the test is in this C# file
            Assert.That(RelativeStack(action, trace.Stacks), Is.EqualTo(new[] { "TracingTests.cs" }));
        }
    }

    [UpstreamTest("TR-035 should hide internal stack frames in expect")]
    public async Task ShouldHideInternalStackFramesInExpect(IBrowserContext context, IPage page, TestInfo testInfo)
    {
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        Task expectPromise = null;
        page.Dialog += (_, dialog) =>
        {
            expectPromise = Expect(page).ToHaveTitleAsync("Hello");
            IgnoreErrors(dialog.DismissAsync());
        };
        await page.SetContentAsync("<title>Hello</title><div onclick='window.alert(123)'>Click me</div>");
        await page.ClickAsync("div");
        await Expect(page.Locator("div")).ToBeVisibleAsync();
        await expectPromise;
        var tracePath = testInfo.OutputPath("trace.zip");
        await context.Tracing.StopAsync(new() { Path = tracePath });

        var trace = await ParseTraceRaw(tracePath);
        var actions = trace.ActionObjects.Where(a => a["class"].GetString() != "Tracing").ToList();
        Assert.That(actions, Has.Count.EqualTo(5));
        foreach (var action in actions)
        {
            // .NET: ['tracing.spec.ts'], the test is in this C# file
            Assert.That(RelativeStack(action, trace.Stacks), Is.EqualTo(new[] { "TracingTests.cs" }));
        }
    }

    [UpstreamTest("TR-036 should record global request trace")]
    public async Task ShouldRecordGlobalRequestTrace(IAPIRequestContext request, IBrowserContext context, Server server, TestInfo testInfo)
    {
        await request.Tracing.StartAsync(new() { Snapshots = true });
        var url = server.Prefix + "/simple.json";
        await request.GetAsync(url);
        var tracePath = testInfo.OutputPath("trace.zip");
        await request.Tracing.StopAsync(new() { Path = tracePath });

        var trace = await ParseTraceRaw(tracePath);
        var actions = trace.Events.Where(e => e.GetProperty("type").GetString() == "resource-snapshot").ToList();
        Assert.That(actions, Has.Count.EqualTo(1));
        ExpectToEqual(actions[0].GetProperty("snapshot").GetProperty("request"), ObjectContaining(new
        {
            method = "GET",
            url,
        }));
        ExpectToEqual(actions[0].GetProperty("snapshot").GetProperty("response"), ObjectContaining(new
        {
            status = 200,
            statusText = "OK",
            headers = ArrayContaining(new[]
            {
                ObjectContaining(new
                {
                    name = "Content-Length",
                    value = "15",
                }),
            }),
        }));
    }

    [UpstreamTest("TR-037 should store global request traces separately")]
    public async Task ShouldStoreGlobalRequestTracesSeparately(IAPIRequestContext request, Server server, IPlaywright playwright, string browserName, string mode, TestInfo testInfo)
    {
        var request2 = await playwright.APIRequest.NewContextAsync();
        await PromiseAll(
            request.Tracing.StartAsync(new() { Snapshots = true }),
            request2.Tracing.StartAsync(new() { Snapshots = true }));
        var url = server.Prefix + "/simple.json";
        await PromiseAll(
            request.GetAsync(url),
            request2.PostAsync(url));
        var tracePath = testInfo.OutputPath("trace.zip");
        var trace2Path = testInfo.OutputPath("trace2.zip");
        await PromiseAll(
            request.Tracing.StopAsync(new() { Path = tracePath }),
            request2.Tracing.StopAsync(new() { Path = trace2Path }));
        {
            var trace = await ParseTraceRaw(tracePath);
            var actions = trace.Events.Where(e => e.GetProperty("type").GetString() == "resource-snapshot").ToList();
            Assert.That(actions, Has.Count.EqualTo(1));
            ExpectToEqual(actions[0].GetProperty("snapshot").GetProperty("request"), ObjectContaining(new
            {
                method = "GET",
                url,
            }));
        }
        {
            var trace = await ParseTraceRaw(trace2Path);
            var actions = trace.Events.Where(e => e.GetProperty("type").GetString() == "resource-snapshot").ToList();
            Assert.That(actions, Has.Count.EqualTo(1));
            ExpectToEqual(actions[0].GetProperty("snapshot").GetProperty("request"), ObjectContaining(new
            {
                method = "POST",
                url,
            }));
        }
    }

    [UpstreamTest("TR-038 should store postData for global request")]
    public async Task ShouldStorePostDataForGlobalRequest(IAPIRequestContext request, Server server, TestInfo testInfo)
    {
        await request.Tracing.StartAsync(new() { Snapshots = true });
        var url = server.Prefix + "/simple.json";
        await request.PostAsync(url, new()
        {
            Data = "test",
        });
        var tracePath = testInfo.OutputPath("trace.zip");
        await request.Tracing.StopAsync(new() { Path = tracePath });

        var trace = await ParseTraceRaw(tracePath);
        var actions = trace.Events.Where(e => e.GetProperty("type").GetString() == "resource-snapshot").ToList();
        Assert.That(actions, Has.Count.EqualTo(1));
        var req = actions[0].GetProperty("snapshot").GetProperty("request");
        Assert.That(req.TryGetProperty("postData", out var postData) && postData.ValueKind == JsonValueKind.Object && postData.TryGetProperty("_file", out var postDataFile) ? postDataFile.GetString() : null, Is.Not.Empty);
        ExpectToEqual(req, ObjectContaining(new
        {
            method = "POST",
            url,
        }));
    }

    [UpstreamTest("TR-039 should not flush console events")]
    public async Task ShouldNotFlushConsoleEvents(IBrowserContext context, IPage page, string mode, TestInfo testInfo)
    {
        var testId = Info.TestId;
        await context.Tracing.StartAsync(new() { Name = testId });
        var promise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        {
            var counter = 0;
            page.Console += (_, _) =>
            {
                if (++counter == 100)
                {
                    promise.TrySetResult();
                }
            };
        }

        await page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => {
      for (let i = 0; i < 100; ++i)
        console.log('hello ' + i);
    }, 10);
    return 31415926;
  }");

        await promise.Task;

        var dir = Path.Combine(testInfo.Project.OutputDir, ArtifactsFolderName(testInfo.WorkerIndex), "traces");

        string content = null;
        await Expect(async () =>
        {
            var traceName = Directory.GetFileSystemEntries(dir).Select(Path.GetFileName).FirstOrDefault(name => name.EndsWith(testId + ".trace", StringComparison.Ordinal));
            content = await File.ReadAllTextAsync(Path.Combine(dir, traceName));
            Assert.That(content, Does.Contain("31415926"));
        }).ToPassAsync();
        Assert.That(content, Does.Not.Contain("hello 0"));

        await page.EvaluateAsync("() => 42");

        await Expect(async () =>
        {
            var traceName = Directory.GetFileSystemEntries(dir).Select(Path.GetFileName).FirstOrDefault(name => name.EndsWith(testId + ".trace", StringComparison.Ordinal));
            // .NET: content2 is content, which the test declares above.
            var content2 = await File.ReadAllTextAsync(Path.Combine(dir, traceName));
            Assert.That(content2, Does.Contain("hello 0"));
            Assert.That(content2, Does.Contain("hello 99"));
        }).ToPassAsync();
    }

    [UpstreamTest("TR-040 should flush console events on tracing stop")]
    public async Task ShouldFlushConsoleEventsOnTracingStop(IBrowserContext context, IPage page, TestInfo testInfo)
    {
        await context.Tracing.StartAsync();
        var promise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        {
            var counter = 0;
            page.Console += (_, _) =>
            {
                if (++counter == 100)
                {
                    promise.TrySetResult();
                }
            };
        }
        await page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => {
      for (let i = 0; i < 100; ++i)
        console.log('hello ' + i);
    });
  }");
        await promise.Task;
        var tracePath = testInfo.OutputPath("trace.zip");
        await context.Tracing.StopAsync(new() { Path = tracePath });
        var trace = await ParseTraceRaw(tracePath);
        var events = trace.Events.Where(e => e.GetProperty("type").GetString() == "console").ToList();
        Assert.That(events, Has.Count.EqualTo(100));
    }

    [UpstreamTest("TR-041 should not emit after w/o before")]
    [LocalFixme("dotnet: Page.WaitForConsoleMessageAsync names its wait 'page.WaitForEventAsync(\"Microsoft.Playwright.IConsoleMessage\")' (Page.InnerWaitForEventAsync), so the title in the trace is 'Wait for event \"page.WaitForEventAsync(\"Microsoft.Playwright.IConsoleMessage\")\"', upstream expects 'Wait for event \"console\"'")]
    public async Task ShouldNotEmitAfterWOBefore(IBrowserType browserType, string mode, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        var tracesDir = testInfo.OutputPath("traces");
        var browser = await browserType.LaunchAsync(new(launchOptions) { TracesDir = tracesDir });
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();

        await context.Tracing.StartAsync(new() { Name = "name1", Snapshots = true });
        // The wait spans the chunks: its "before" is in the first chunk, and its "after" must not get into the second one.
        var consolePromise = page.WaitForConsoleMessageAsync();
        await page.EvaluateAsync("() => 1");
        await context.Tracing.StopChunkAsync(new() { Path = testInfo.OutputPath("trace1.zip") });
        Assert.That(File.Exists(Path.Combine(tracesDir, "name1.trace")), Is.EqualTo(true));

        await context.Tracing.StartChunkAsync(new() { Name = "name2" });
        await page.EvaluateAsync("() => console.log('started')");
        await consolePromise;
        await context.Tracing.StopAsync(new() { Path = testInfo.OutputPath("trace2.zip") });
        Assert.That(File.Exists(Path.Combine(tracesDir, "name2.trace")), Is.EqualTo(true));

        await browser.CloseAsync();
        var minCallId = 100000;
        var sanitize = (JsonElement e) =>
        {
            if (e.GetProperty("type").GetString() == "after" || e.GetProperty("type").GetString() == "before")
            {
                minCallId = Math.Min(minCallId, int.Parse(e.GetProperty("callId").GetString().Split('@')[1], CultureInfo.InvariantCulture));
                return new
                {
                    type = e.GetProperty("type").GetString(),
                    callId = int.Parse(e.GetProperty("callId").GetString().Split('@')[1], CultureInfo.InvariantCulture) - minCallId,
                    title = e.TryGetProperty("title", out var title) ? title.GetString() : null,
                };
            }
            return null;
        };

        int call1;
        {
            var (events, _, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace1.zip"));
            var sanitized = events.Select(sanitize).Where(e => e != null).ToList();
            ExpectToEqual(sanitized, new object[]
            {
                new
                {
                    type = "before",
                    callId = AnyNumber,
                    title = "Wait for event \"console\"",
                },
                new
                {
                    type = "before",
                    callId = AnyNumber,
                },
                new
                {
                    type = "after",
                    callId = AnyNumber,
                },
            });
            Assert.That(sanitized[1].callId, Is.GreaterThan(sanitized[0].callId));
            Assert.That(sanitized[1].callId, Is.EqualTo(sanitized[2].callId));
            call1 = sanitized[1].callId;
        }

        int call2before;
        int call2after;
        {
            var (events, _, _, _, _) = await ParseTraceRaw(testInfo.OutputPath("trace2.zip"));
            var sanitized = events.Select(sanitize).Where(e => e != null).ToList();
            ExpectToEqual(sanitized, new object[]
            {
                new
                {
                    type = "before",
                    callId = AnyNumber,
                },
                new
                {
                    type = "after",
                    callId = AnyNumber,
                },
            });
            call2before = sanitized[0].callId;
            call2after = sanitized[1].callId;
        }
        Assert.That(call2before, Is.GreaterThan(call1));
        Assert.That(call2after, Is.EqualTo(call2before));
    }

    [UpstreamTest("TR-042 should save trace while a WebSocket keeps streaming frames")]
    public async Task ShouldSaveTraceWhileAWebSocketKeepsStreamingFrames(IBrowserContext context, IPage page, Server server, TestInfo testInfo)
    {
        var streaming = true;
        server.OnceWebSocketConnection(ws =>
        {
            var timer = SetInterval(
                () =>
                {
                    if (streaming && ws.ReadyState == 1)
                    {
                        IgnoreErrors(ws.SendAsync(new string('x', 16 * 1024)));
                    }
                },
                1);
            var stop = () => ClearInterval(timer);
            ws.OnClose((_, _) => stop());
            ws.OnError(_ => stop());
        });

        await context.Tracing.StartAsync(new() { Snapshots = true });

        await context.Tracing.StartChunkAsync();
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"url => {
    window.ws = new WebSocket(url);
    return new Promise(resolve => window.ws.addEventListener('open', () => resolve()));
  }", $"ws://{server.Host}/ws");
        await page.WaitForTimeoutAsync(100);
        var tracePath1 = testInfo.OutputPath("trace1.zip");
        await context.Tracing.StopChunkAsync(new() { Path = tracePath1 });

        streaming = false;
        await context.Tracing.StartChunkAsync();
        await page.WaitForTimeoutAsync(100);
        var tracePath2 = testInfo.OutputPath("trace2.zip");
        await context.Tracing.StopChunkAsync(new() { Path = tracePath2 });

        await page.EvaluateAsync(@"() => new Promise(resolve => {
    const ws = window.ws;
    if (ws.readyState === WebSocket.CLOSED) {
      resolve();
      return;
    }
    ws.addEventListener('close', () => resolve(), { once: true });
    ws.close();
  })");

        var webSocketLines = await PromiseAll(new[] { tracePath1, tracePath2 }.Select(async path =>
        {
            var (_, resources, _, _, _) = await ParseTraceRaw(path);
            var websocketResource = resources.First(entry => entry.Key.EndsWith(".jsonl", StringComparison.Ordinal));
            var lines = Encoding.UTF8.GetString(websocketResource.Value).Split('\n').Where(line => line.Length > 0).ToArray();
            Assert.That(lines.Length, Is.GreaterThan(0));
            foreach (var line in lines)
            {
                Assert.That(() => JsonSerializer.Deserialize<JsonElement>(line), Throws.Nothing);
            }
            return (path, lines);
        }));
        Assert.That(webSocketLines[0].path, Is.Not.EqualTo(webSocketLines[1].path));
        Assert.That(webSocketLines[1].lines, Is.EqualTo(webSocketLines[1].lines));
    }

    private static void ExpectRed(byte[] pixels, double offset)
    {
        var r = pixels[(int)offset];
        var g = pixels[(int)offset + 1];
        var b = pixels[(int)offset + 2];
        var a = pixels[(int)offset + 3];
        Assert.That(r, Is.GreaterThan(200));
        Assert.That(g, Is.LessThan(70));
        Assert.That(b, Is.LessThan(70));
        Assert.That(a, Is.EqualTo(255));
    }

    private static void ExpectBlue(byte[] pixels, double offset)
    {
        var r = pixels[(int)offset];
        var g = pixels[(int)offset + 1];
        var b = pixels[(int)offset + 2];
        var a = pixels[(int)offset + 3];
        Assert.That(r, Is.LessThan(70));
        Assert.That(g, Is.LessThan(70));
        Assert.That(b, Is.GreaterThan(200));
        Assert.That(a, Is.EqualTo(255));
    }

    private static string[] RelativeStack(Dictionary<string, JsonElement> action, Dictionary<string, List<TraceStackFrame>> stacks)
    {
        var stack = stacks.GetValueOrDefault(action["callId"].GetString()) ?? [];
        // .NET: __dirname
        var dirname = Path.GetDirectoryName(new System.Diagnostics.StackFrame(true).GetFileName());
        return stack.Select(f => ReplaceFirst(f.File, dirname + Path.DirectorySeparatorChar, string.Empty)).ToArray();
    }
}
