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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/webmcp.spec.ts.</summary>
public class WebmcpTests
{
    // test.use({
    //   launchOptions: async ({ launchOptions, browserName }, use) => {
    //     if (browserName === 'firefox') {
    //       await use({
    //         ...launchOptions,
    //         firefoxUserPrefs: {
    //           ...launchOptions.firefoxUserPrefs,
    //           'dom.modelcontext.enabled': true,
    //           'dom.modelcontext.testing.enabled': true,
    //         },
    //       });
    //       return;
    //     }
    //     await use({ ...launchOptions, args: [...(launchOptions.args ?? []), '--enable-features=WebMCP'] });
    //   },
    // });
    [Fixture]
    public static BrowserTypeLaunchOptions LaunchOptions(BrowserTypeLaunchOptions launchOptions, string browserName)
    {
        if (browserName == "firefox")
        {
            return new(launchOptions)
            {
                FirefoxUserPrefs = new Dictionary<string, object>(launchOptions.FirefoxUserPrefs ?? [])
                {
                    ["dom.modelcontext.enabled"] = true,
                    ["dom.modelcontext.testing.enabled"] = true,
                },
            };
        }
        return new(launchOptions) { Args = [.. launchOptions.Args ?? [], "--enable-features=WebMCP"] };
    }

    [BeforeEach]
    public static void Skip(string browserName) => It.Skip(browserName == "webkit", "WebKit does not implement WebMCP");

    [BeforeEach]
    public static void Skip2(bool isBidi) => It.Skip(isBidi, "WebMCP is not implemented over BiDi");

    private static string RegisterScript(string tools)
    {
        return $@"<script>
    const modelContext = document.modelContext || navigator.modelContext;
    {tools}
  </script>";
    }

    private const string kAdd = @"
  modelContext.registerTool({
    name: 'add',
    description: 'Adds two numbers',
    inputSchema: { type: 'object', properties: { a: { type: 'number' }, b: { type: 'number' } }, required: ['a', 'b'] },
    annotations: { readOnlyHint: true },
    async execute(input) { return { content: [{ type: 'text', text: String(input.a + input.b) }] }; },
  });
";

    private static readonly object kAddSchema = new { type = "object", properties = new { a = new { type = "number" }, b = new { type = "number" } }, required = new[] { "a", "b" } };

    private static void Serve(Server server, string path, string body)
    {
        server.SetRoute(path, (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End($"<title>WebMCP</title>{body}");
        });
    }

    [UpstreamTest("WM-001 should list tools registered by the page")]
    public async Task ShouldListToolsRegisteredByThePage(IPage page, Server server)
    {
        Serve(server, "/", RegisterScript(kAdd + @"
    modelContext.registerTool({
      name: 'noop',
      description: 'Does nothing',
      async execute() {},
    });
  "));
        await page.GotoAsync(server.Prefix + "/");

        // Browsers list tools in their own order.
        // .NET: sort((a, b) => a.name.localeCompare(b.name)) is a stable sort with the comparison of a culture.
        var tools = (await page.Webmcp.ToolsAsync()).OrderBy(a => a.Name, StringComparer.InvariantCulture);
        ExpectToEqual(tools, new object[]
        {
            new
            {
                name = "add",
                description = "Adds two numbers",
                inputSchema = kAddSchema,
                annotations = new { readOnly = true },
            },
            new
            {
                name = "noop",
                description = "Does nothing",
            },
        });
    }

    [UpstreamTest("WM-002 should report no tools when the page registers none")]
    public async Task ShouldReportNoToolsWhenThePageRegistersNone(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.Webmcp.ToolsAsync(), Is.EqualTo(Array.Empty<WebMCPTool>()));
    }

    [UpstreamTest("WM-003 should call a tool")]
    public async Task ShouldCallATool(IPage page, Server server)
    {
        Serve(server, "/", RegisterScript(kAdd));
        await page.GotoAsync(server.Prefix + "/");

        ExpectToEqual(await page.Webmcp.CallToolAsync<object>("add", new { a = 2, b = 40 }), new { content = new[] { new { type = "text", text = "42" } } });
    }

    [UpstreamTest("WM-004 should return isError results as is")]
    public async Task ShouldReturnIsErrorResultsAsIs(IPage page, Server server)
    {
        Serve(server, "/", RegisterScript(@"
    modelContext.registerTool({
      name: 'broken',
      description: 'Always fails',
      async execute() { return { content: [{ type: 'text', text: 'nope' }], isError: true }; },
    });
  "));
        await page.GotoAsync(server.Prefix + "/");

        ExpectToEqual(await page.Webmcp.CallToolAsync<object>("broken"), new { content = new[] { new { type = "text", text = "nope" } }, isError = true });
    }

    [UpstreamTest("WM-005 should throw when the tool throws")]
    public async Task ShouldThrowWhenTheToolThrows(IPage page, Server server, string browserName)
    {
        Serve(server, "/", RegisterScript(@"
    modelContext.registerTool({
      name: 'throwing',
      description: 'Throws',
      async execute() { throw new Error('boom'); },
    });
  "));
        await page.GotoAsync(server.Prefix + "/");

        var error = await Catch(() => page.Webmcp.CallToolAsync<object>("throwing"));
        // Chromium does not report the error that the tool threw.
        Assert.That(error.Message, Does.Match(browserName == "chromium" ? "the invocation failed|Tool execution failed" : "boom"));
    }

    [UpstreamTest("WM-006 should throw for an unknown tool")]
    public async Task ShouldThrowForAnUnknownTool(IPage page, Server server)
    {
        Serve(server, "/", RegisterScript(kAdd));
        await page.GotoAsync(server.Prefix + "/");

        var error = await Catch(() => page.Webmcp.CallToolAsync<object>("missing"));
        Assert.That(error.Message, Does.Contain("No WebMCP tool named \"missing\""));
        Assert.That(error.Message, Does.Contain("Available tools: add"));
    }

    [UpstreamTest("WM-007 should time out when the tool does not settle")]
    public async Task ShouldTimeOutWhenTheToolDoesNotSettle(IPage page, Server server)
    {
        Serve(server, "/", RegisterScript(@"
    modelContext.registerTool({
      name: 'stuck',
      description: 'Never resolves',
      execute() { return new Promise(() => {}); },
    });
  "));
        await page.GotoAsync(server.Prefix + "/");

        var error = await Catch(() => page.Webmcp.CallToolAsync<object>("stuck", new { }, new() { Timeout = 500 }));
        Assert.That(error.Message, Does.Contain("Timeout 500ms exceeded"));
    }

    [UpstreamTest("WM-008 should drop tools after navigation")]
    public async Task ShouldDropToolsAfterNavigation(IPage page, Server server)
    {
        Serve(server, "/", RegisterScript(kAdd));
        await page.GotoAsync(server.Prefix + "/");
        Assert.That(await page.Webmcp.ToolsAsync(), Has.Count.EqualTo(1));

        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.Webmcp.ToolsAsync(), Is.EqualTo(Array.Empty<WebMCPTool>()));
        var error = await Catch(() => page.Webmcp.CallToolAsync<object>("add", new { a = 1, b = 1 }));
        Assert.That(error.Message, Does.Contain("The frame does not register any WebMCP tools"));
    }

    [UpstreamTest("WM-009 should list a tool registered after load")]
    public async Task ShouldListAToolRegisteredAfterLoad(IPage page, Server server)
    {
        Serve(server, "/", RegisterScript(kAdd));
        await page.GotoAsync(server.Prefix + "/");
        Assert.That(await page.Webmcp.ToolsAsync(), Has.Count.EqualTo(1));

        await page.EvaluateAsync(@"() => {
    const modelContext = document.modelContext || navigator.modelContext;
    modelContext.registerTool({ name: 'late', description: 'Registered later', async execute() {} });
  }");
        // .NET: sort((a, b) => a.name.localeCompare(b.name)) is a stable sort with the comparison of a culture.
        var tools = (await page.Webmcp.ToolsAsync()).OrderBy(a => a.Name, StringComparer.InvariantCulture);
        Assert.That(tools.Select(tool => tool.Name), Is.EqualTo(new[] { "add", "late" }));
    }

    [UpstreamTest("WM-010 should list tools while a tool is running")]
    public async Task ShouldListToolsWhileAToolIsRunning(IPage page, Server server)
    {
        Serve(server, "/", RegisterScript(kAdd + @"
    modelContext.registerTool({
      name: 'gated',
      description: 'Resolves when released',
      execute() { return new Promise(resolve => window.release = () => resolve({ content: [{ type: 'text', text: 'released' }] })); },
    });
  "));
        await page.GotoAsync(server.Prefix + "/");

        var result = page.Webmcp.CallToolAsync<object>("gated");
        await page.WaitForFunctionAsync("() => !!window.release");
        Assert.That(await page.Webmcp.ToolsAsync(), Has.Count.EqualTo(2));
        await page.EvaluateAsync("() => window.release()");
        ExpectToEqual(await result, new { content = new[] { new { type = "text", text = "released" } } });
    }

    [UpstreamTest("WM-011 should list declarative tools")]
    public async Task ShouldListDeclarativeTools(IPage page, Server server, string browserName)
    {
        It.Skip(browserName != "chromium", "Only Chromium implements declarative tools");
        Serve(server, "/", @"
    <form toolname=""subscribe"" tooldescription=""Subscribes to the newsletter"">
      <input name=""email"" type=""email"">
      <button>Subscribe</button>
    </form>
  ");
        await page.GotoAsync(server.Prefix + "/");

        ExpectToEqual(await page.Webmcp.ToolsAsync(), new[]
        {
            ObjectContaining(new
            {
                name = "subscribe",
                description = "Subscribes to the newsletter",
                inputSchema = ObjectContaining(new { type = "object", properties = new { email = new { type = "string" } } }),
            }),
        });

        // A declarative tool waits for the user to submit the form.
        var error = await Catch(() => page.Webmcp.CallToolAsync<object>("subscribe", new { email = "me@example.com" }, new() { Timeout = 500 }));
        Assert.That(error.Message, Does.Contain("Timeout 500ms exceeded"));
    }

    [UpstreamTest("WM-012 should scope tools to their frame")]
    public async Task ShouldScopeToolsToTheirFrame(IPage page, Server server, string browserName)
    {
        It.Skip(browserName == "firefox", "Firefox does not support registering WebMCP tools in iframes yet, https://bugzilla.mozilla.org/show_bug.cgi?id=2019743");
        Serve(server, "/", RegisterScript(kAdd) + "<iframe src=\"/frame.html\"></iframe>");
        Serve(server, "/frame.html", RegisterScript(@"
    modelContext.registerTool({
      name: 'subscribe',
      description: 'Subscribes to the newsletter',
      async execute() { return { content: [{ type: 'text', text: 'subscribed' }] }; },
    });
  "));
        await page.GotoAsync(server.Prefix + "/");

        var childFrame = page.Frames[1];
        ExpectToEqual(await page.Webmcp.ToolsAsync(), new[] { ObjectContaining(new { name = "add" }) });
        ExpectToEqual(await childFrame.Webmcp.ToolsAsync(), new[] { ObjectContaining(new { name = "subscribe" }) });
        ExpectToEqual(await childFrame.Webmcp.CallToolAsync<object>("subscribe"), new { content = new[] { new { type = "text", text = "subscribed" } } });

        var error = await Catch(() => page.Webmcp.CallToolAsync<object>("subscribe"));
        Assert.That(error.Message, Does.Contain("No WebMCP tool named \"subscribe\""));
        Assert.That(error.Message, Does.Contain("Available tools: add"));
    }

    [UpstreamTest("WM-013 should call same-name tools in their own frame")]
    public async Task ShouldCallSameNameToolsInTheirOwnFrame(IPage page, Server server, string browserName)
    {
        It.Skip(browserName == "firefox", "Firefox does not support registering WebMCP tools in iframes yet, https://bugzilla.mozilla.org/show_bug.cgi?id=2019743");
        string registerEcho(string text) => RegisterScript($@"
    modelContext.registerTool({{
      name: 'echo',
      description: 'Echoes',
      async execute() {{ return {{ content: [{{ type: 'text', text: '{text}' }}] }}; }},
    }});
  ");
        Serve(server, "/", registerEcho("main") + "<iframe src=\"/frame.html\"></iframe>");
        Serve(server, "/frame.html", registerEcho("frame"));
        await page.GotoAsync(server.Prefix + "/");
        var childFrame = page.Frames[1];

        ExpectToEqual(await page.Webmcp.CallToolAsync<object>("echo"), new { content = new[] { new { type = "text", text = "main" } } });
        ExpectToEqual(await childFrame.Webmcp.CallToolAsync<object>("echo"), new { content = new[] { new { type = "text", text = "frame" } } });
    }

    [UpstreamTest("WM-014 should list and call tools in cross-origin and nested frames")]
    public async Task ShouldListAndCallToolsInCrossOriginAndNestedFrames(IPage page, Server server, string browserName)
    {
        It.Skip(browserName == "firefox", "Firefox does not support registering WebMCP tools in iframes yet, https://bugzilla.mozilla.org/show_bug.cgi?id=2019743");
        string registerEcho(string text) => RegisterScript($@"
    modelContext.registerTool({{
      name: 'echo-{text}',
      description: 'Echoes {text}',
      async execute() {{ return {{ content: [{{ type: 'text', text: '{text}' }}] }}; }},
    }});
  ");
        Serve(server, "/", registerEcho("main") + $"<iframe allow=\"tools\" src=\"{server.CrossProcessPrefix}/frame.html\"></iframe>");
        Serve(server, "/frame.html", registerEcho("frame") + "<iframe src=\"/nested.html\"></iframe>");
        Serve(server, "/nested.html", registerEcho("nested"));
        await page.GotoAsync(server.Prefix + "/");

        // .NET: const [mainFrame, frame, nested] = page.frames();
        var (mainFrame, frame, nested) = (page.Frames[0], page.Frames[1], page.Frames[2]);
        ExpectToEqual(await mainFrame.Webmcp.ToolsAsync(), new[] { new { name = "echo-main", description = "Echoes main" } });
        ExpectToEqual(await frame.Webmcp.ToolsAsync(), new[] { new { name = "echo-frame", description = "Echoes frame" } });
        ExpectToEqual(await nested.Webmcp.ToolsAsync(), new[] { new { name = "echo-nested", description = "Echoes nested" } });
        ExpectToEqual(await frame.Webmcp.CallToolAsync<object>("echo-frame"), new { content = new[] { new { type = "text", text = "frame" } } });
        ExpectToEqual(await nested.Webmcp.CallToolAsync<object>("echo-nested"), new { content = new[] { new { type = "text", text = "nested" } } });
    }
}
