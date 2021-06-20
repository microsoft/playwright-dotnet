using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageMouseTests : PageTestEx
    {
        [PlaywrightTest("page-mouse.spec.ts", "should click the document")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWindows: true)]
        public async Task ShouldClickTheDocument()
        {
            await Page.EvaluateAsync(@"() => {
                window.clickPromise = new Promise(resolve => {
                    document.addEventListener('click', event => {
                        resolve({
                            type: event.type,
                            detail: event.detail,
                            clientX: event.clientX,
                            clientY: event.clientY,
                            isTrusted: event.isTrusted,
                            button: event.button
                        });
                    });
                });
            }");
            await Page.Mouse.ClickAsync(50, 60);
            var clickEvent = await Page.EvaluateAsync<JsonElement>("() => window.clickPromise");
            Assert.AreEqual("click", clickEvent.GetProperty("type").GetString());
            Assert.AreEqual(1, clickEvent.GetProperty("detail").GetInt32());
            Assert.AreEqual(50, clickEvent.GetProperty("clientX").GetInt32());
            Assert.AreEqual(60, clickEvent.GetProperty("clientY").GetInt32());
            Assert.True(clickEvent.GetProperty("isTrusted").GetBoolean());
            Assert.AreEqual(0, clickEvent.GetProperty("button").GetInt32());
        }

        [PlaywrightTest("page-mouse.spec.ts", "should select the text with mouse")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectTheTextWithMouse()
        {
            await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
            await Page.FocusAsync("textarea");
            const string text = "This is the text that we are going to try to select. Let's see how it goes.";
            await Page.Keyboard.TypeAsync(text);
            // Firefox needs an extra frame here after typing or it will fail to set the scrollTop
            await Page.EvaluateAsync("() => new Promise(requestAnimationFrame)");
            await Page.EvaluateAsync("() => document.querySelector('textarea').scrollTop = 0");
            var dimensions = await Page.EvaluateAsync<JsonElement>(@"function dimensions() {
              const rect = document.querySelector('textarea').getBoundingClientRect();
              return {
                x: rect.left,
                y: rect.top,
                width: rect.width,
                height: rect.height
              };
            }");
            int x = dimensions.GetProperty("x").GetInt32();
            int y = dimensions.GetProperty("y").GetInt32();
            await Page.Mouse.MoveAsync(x + 2, y + 2);
            await Page.Mouse.DownAsync();
            await Page.Mouse.MoveAsync(200, 200);
            await Page.Mouse.UpAsync();
            Assert.AreEqual(text, await Page.EvaluateAsync<string>(@"() => {
                const textarea = document.querySelector('textarea');
                return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
            }"));
        }

        [PlaywrightTest("page-mouse.spec.ts", "should trigger hover state")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTriggerHoverState()
        {
            await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
            await Page.HoverAsync("#button-6");
            Assert.AreEqual("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
            await Page.HoverAsync("#button-2");
            Assert.AreEqual("button-2", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
            await Page.HoverAsync("#button-91");
            Assert.AreEqual("button-91", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
        }

        [PlaywrightTest("page-mouse.spec.ts", "should trigger hover state with removed window.Node")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTriggerHoverStateWithRemovedWindowNode()
        {
            await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
            await Page.EvaluateAsync("() => delete window.Node");
            await Page.HoverAsync("#button-6");
            Assert.AreEqual("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
        }

        [PlaywrightTest("page-mouse.spec.ts", "should set modifier keys on click")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetModifierKeysOnClick()
        {
            await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
            await Page.EvaluateAsync("() => document.querySelector('#button-3').addEventListener('mousedown', e => window.lastEvent = e, true)");
            var modifiers = new Dictionary<string, string> { ["Shift"] = "shiftKey", ["Control"] = "ctrlKey", ["Alt"] = "altKey", ["Meta"] = "metaKey" };
            // In Firefox, the Meta modifier only exists on Mac
            if (TestConstants.IsFirefox && !TestConstants.IsMacOSX)
            {
                modifiers.Remove("Meta");
            }

            foreach (string key in modifiers.Keys)
            {
                string value = modifiers[key];
                await Page.Keyboard.DownAsync(key);
                await Page.ClickAsync("#button-3");
                Assert.True(await Page.EvaluateAsync<bool>("mod => window.lastEvent[mod]", value));
                await Page.Keyboard.UpAsync(key);
            }
            await Page.ClickAsync("#button-3");
            foreach (string key in modifiers.Keys)
            {
                Assert.False(await Page.EvaluateAsync<bool>("mod => window.lastEvent[mod]", modifiers[key]));
            }
        }

        [PlaywrightTest("page-mouse.spec.ts", "should tween mouse movement")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTweenMouseMovement()
        {
            // The test becomes flaky on WebKit without next line.
            if (TestConstants.IsWebKit)
            {
                await Page.EvaluateAsync("() => new Promise(requestAnimationFrame)");
            }
            await Page.Mouse.MoveAsync(100, 100);
            await Page.EvaluateAsync(@"() => {
                window.result = [];
                document.addEventListener('mousemove', event => {
                    window.result.push([event.clientX, event.clientY]);
                });
            }");
            await Page.Mouse.MoveAsync(200, 300, new() { Steps = 5 });
            Assert.AreEqual(
                new[]
                {
                    new[] { 120, 140 },
                    new[] { 140, 180 },
                    new[] { 160, 220 },
                    new[] { 180, 260 },
                    new[] { 200, 300 }
                },
                await Page.EvaluateAsync<int[][]>("result"));
        }

        [PlaywrightTest("page-mouse.spec.ts", "should work with mobile viewports and cross process navigations")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkWithMobileViewportsAndCrossProcessNavigations()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 360, Height = 640 },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            await page.GotoAsync(Server.CrossProcessPrefix + "/mobile.html");
            await page.EvaluateAsync(@"() => {
                document.addEventListener('click', event => {
                    window.result = { x: event.clientX, y: event.clientY };
                });
            }");
            await page.Mouse.ClickAsync(30, 40);
            var result = await page.EvaluateAsync<JsonElement>("result");
            Assert.AreEqual(30, result.GetProperty("x").GetInt32());
            Assert.AreEqual(40, result.GetProperty("y").GetInt32());
        }
    }
}
