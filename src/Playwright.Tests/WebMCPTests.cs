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
using Microsoft.Playwright.TestAdapter;

namespace Microsoft.Playwright.Tests;

///<playwright-file>webmcp.spec.ts</playwright-file>
public class WebMCPTests : PlaywrightTestEx
{
    private const string AddTool = @"
        modelContext.registerTool({
            name: 'add',
            description: 'Adds two numbers',
            inputSchema: { type: 'object', properties: { a: { type: 'number' }, b: { type: 'number' } }, required: ['a', 'b'] },
            annotations: { readOnlyHint: true },
            async execute(input) { return { content: [{ type: 'text', text: String(input.a + input.b) }] }; },
        });
    ";

    private IBrowser _browser = null!;

    public IPage Page { get; private set; } = null!;

    [SetUp]
    public async Task LaunchWithWebMCP()
    {
        if (TestConstants.IsWebKit)
        {
            Assert.Ignore("WebKit does not implement WebMCP");
        }

        var options = new BrowserTypeLaunchOptions(PlaywrightSettingsProvider.LaunchOptions);
        if (TestConstants.IsFirefox)
        {
            var prefs = new Dictionary<string, object>(options.FirefoxUserPrefs ?? new Dictionary<string, object>())
            {
                ["dom.modelcontext.enabled"] = true,
                ["dom.modelcontext.testing.enabled"] = true,
            };
            options.FirefoxUserPrefs = prefs;
        }
        else
        {
            options.Args = (options.Args ?? Array.Empty<string>()).Append("--enable-features=WebMCP").ToList();
        }
        _browser = await BrowserType.LaunchAsync(options);
        Page = await _browser.NewPageAsync();
    }

    [TearDown]
    public async Task CloseBrowser()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
    }

    private static string RegisterScript(string tools)
        => "<script>const modelContext = document.modelContext || navigator.modelContext;" + tools + "</script>";

    private void Serve(string path, string body)
    {
        Server.SetRoute(path, async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<title>WebMCP</title>" + body);
        });
    }

    private static string TextContent(JsonElement result, int index = 0)
        => result.GetProperty("content")[index].GetProperty("text").GetString();

    [PlaywrightTest("webmcp.spec.ts", "should list tools registered by the page")]
    public async Task ShouldListToolsRegisteredByThePage()
    {
        Serve("/webmcp.html", RegisterScript(AddTool + @"
            modelContext.registerTool({
                name: 'noop',
                description: 'Does nothing',
                async execute() {},
            });
        "));
        await Page.GotoAsync(Server.Prefix + "/webmcp.html");

        // Browsers list tools in their own order.
        var tools = (await Page.Webmcp.ToolsAsync()).OrderBy(tool => tool.Name, StringComparer.Ordinal).ToList();
        Assert.AreEqual(2, tools.Count);

        Assert.AreEqual("add", tools[0].Name);
        Assert.AreEqual("Adds two numbers", tools[0].Description);
        var inputSchema = (JsonElement)tools[0].InputSchema;
        Assert.AreEqual("object", inputSchema.GetProperty("type").GetString());
        Assert.AreEqual("number", inputSchema.GetProperty("properties").GetProperty("a").GetProperty("type").GetString());
        Assert.AreEqual("number", inputSchema.GetProperty("properties").GetProperty("b").GetProperty("type").GetString());
        CollectionAssert.AreEqual(new[] { "a", "b" }, inputSchema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList());
        Assert.IsTrue(tools[0].Annotations.ReadOnly);
        Assert.IsNull(tools[0].Annotations.UntrustedContent);
        Assert.IsNull(tools[0].Annotations.Consequential);

        Assert.AreEqual("noop", tools[1].Name);
        Assert.AreEqual("Does nothing", tools[1].Description);
        Assert.IsNull(tools[1].InputSchema);
        Assert.IsNull(tools[1].Annotations);

        CollectionAssert.AreEqual(new[] { "add", "noop" }, (await Page.MainFrame.Webmcp.ToolsAsync()).Select(tool => tool.Name).OrderBy(name => name, StringComparer.Ordinal).ToList());
    }

    [PlaywrightTest("webmcp.spec.ts", "should call a tool")]
    public async Task ShouldCallATool()
    {
        Serve("/webmcp.html", RegisterScript(AddTool));
        await Page.GotoAsync(Server.Prefix + "/webmcp.html");

        var result = await Page.Webmcp.CallToolAsync<JsonElement>("add", new { a = 2, b = 40 });
        Assert.AreEqual("text", result.GetProperty("content")[0].GetProperty("type").GetString());
        Assert.AreEqual("42", TextContent(result));
        Assert.IsFalse(result.TryGetProperty("isError", out _));
    }

    [PlaywrightTest("webmcp.spec.ts", "should throw when the tool throws")]
    public async Task ShouldThrowWhenTheToolThrows()
    {
        Serve("/webmcp.html", RegisterScript(@"
            modelContext.registerTool({
                name: 'throwing',
                description: 'Throws',
                async execute() { throw new Error('boom'); },
            });
        "));
        await Page.GotoAsync(Server.Prefix + "/webmcp.html");

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Webmcp.CallToolAsync<JsonElement>("throwing"));
        // Chromium does not report the error that the tool threw.
        StringAssert.Contains(TestConstants.IsChromium ? "the invocation failed" : "boom", exception.Message);
    }

    [PlaywrightTest("webmcp.spec.ts", "should time out when the tool does not settle")]
    public async Task ShouldTimeOutWhenTheToolDoesNotSettle()
    {
        Serve("/webmcp.html", RegisterScript(@"
            modelContext.registerTool({
                name: 'stuck',
                description: 'Never resolves',
                execute() { return new Promise(() => {}); },
            });
        "));
        await Page.GotoAsync(Server.Prefix + "/webmcp.html");

        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.Webmcp.CallToolAsync<JsonElement>("stuck", new { }, new() { Timeout = 500 }));
        StringAssert.Contains("Timeout 500ms exceeded", exception.Message);
    }
}
