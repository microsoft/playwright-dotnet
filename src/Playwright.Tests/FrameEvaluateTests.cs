using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class FrameEvaluateTests : PageTestEx
    {
        [PlaywrightTest("frame-evaluate.spec.ts", "should have different execution contexts")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveDifferentExecutionContexts()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            Assert.AreEqual(2, Page.Frames.Count);
            await Page.Frames.First().EvaluateAsync("() => window.FOO = 'foo'");
            await Page.Frames.ElementAt(1).EvaluateAsync("() => window.FOO = 'bar'");
            Assert.AreEqual("foo", await Page.Frames.First().EvaluateAsync<string>("() => window.FOO"));
            Assert.AreEqual("bar", await Page.Frames.ElementAt(1).EvaluateAsync<string>("() => window.FOO"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should have correct execution contexts")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveCorrectExecutionContexts()
        {
            await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            Assert.AreEqual(2, Page.Frames.Count);
            Assert.IsEmpty(await Page.Frames.First().EvaluateAsync<string>("() => document.body.textContent.trim()"));
            Assert.AreEqual("Hi, I'm frame", await Page.Frames.ElementAt(1).EvaluateAsync<string>("() => document.body.textContent.trim()"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should dispose context on navigation")]
        [Test, Ignore("Ignore USES_HOOKS")]
        public void ShouldDisposeContextOnNavigation()
        {
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should dispose context on cross-origin navigation")]
        [Test, Ignore("Ignore USES_HOOKS")]
        public void ShouldDisposeContextOnCrossOriginNavigation()
        {
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should execute after cross-site navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldExecuteAfterCrossSiteNavigation()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var mainFrame = Page.MainFrame;
            StringAssert.Contains("localhost", await mainFrame.EvaluateAsync<string>("() => window.location.href"));
            await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
            StringAssert.Contains("127", await mainFrame.EvaluateAsync<string>("() => window.location.href"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should allow cross-frame js handles")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowCrossFrameJsHandles()
        {
            await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            var handle = await Page.EvaluateHandleAsync(@"() => {
                const iframe = document.querySelector('iframe');
                const foo = { bar: 'baz' };
                iframe.contentWindow.__foo = foo;
                return foo;
            }");
            var childFrame = Page.MainFrame.ChildFrames.First();
            dynamic childResult = await childFrame.EvaluateAsync<ExpandoObject>("() => window.__foo");
            Assert.AreEqual("baz", childResult.bar);
            var exception = Assert.ThrowsAsync<PlaywrightException>(async () => await childFrame.EvaluateAsync<string>("foo => foo.bar", handle));
            Assert.AreEqual("JSHandles can be evaluated only in the context they were created!", exception.Message);
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should allow cross-frame element handles")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowCrossFrameElementHandles()
        {
            await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            var bodyHandle = await Page.MainFrame.ChildFrames.First().QuerySelectorAsync("body");
            string result = await Page.EvaluateAsync<string>("body => body.innerHTML", bodyHandle);
            Assert.AreEqual("<div>Hi, I\'m frame</div>", result.Trim());
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should not allow cross-frame element handles when frames do not script each other")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAllowCrossFrameElementHandlesWhenFramesDoNotScriptEachOther()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var frame = await FrameUtils.AttachFrameAsync(Page, "frame1", Server.CrossProcessPrefix + "/empty.html");
            var bodyHandle = await frame.QuerySelectorAsync("body");
            var exception = Assert.ThrowsAsync<PlaywrightException>(async () => await Page.EvaluateAsync("body => body.innerHTML", bodyHandle));
            StringAssert.Contains("Unable to adopt element handle from a different document", exception.Message);
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should throw for detached frames")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForDetachedFrames()
        {
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            var exception = Assert.ThrowsAsync<PlaywrightException>(async () => await frame1.EvaluateAsync("() => 7 * 8"));
            StringAssert.Contains("Execution Context is not available in detached frame", exception.Message);
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should be isolated between frames")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeIsolatedBetweenFrames()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            Assert.AreEqual(2, Page.Frames.Count);
            var frames = Page.Frames;
            Assert.That(frames.First(), Is.Not.EqualTo(frames.ElementAt(1)));

            await TaskUtils.WhenAll(
                frames.First().EvaluateAsync("() => window.a = 1"),
                frames.ElementAt(1).EvaluateAsync("() => window.a = 2")
            );

            var (result1, result2) = await TaskUtils.WhenAll(
                frames.First().EvaluateAsync<int>("() => window.a"),
                frames.ElementAt(1).EvaluateAsync<int>("() => window.a")
            );
            Assert.AreEqual(1, result1);
            Assert.AreEqual(2, result2);
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should work in iframes that failed initial navigation")]
        [Test, SkipBrowserAndPlatform(skipChromium: true, skipFirefox: true)]
        public async Task ShouldWorkIniframesThatFailedInitialNavigation()
        {
            await Page.SetContentAsync(
                @"<meta http-equiv=""Content-Security-Policy"" content=""script-src 'none';"">
                 <iframe src='javascript:""""'></iframe>",
                new PageSetContentOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            await Page.EvaluateAsync(@"() => {
                const iframe = document.querySelector('iframe');
                const div = iframe.contentDocument.createElement('div');
                iframe.contentDocument.body.appendChild(div);
            }");

            Assert.AreEqual("about:blank", Page.Frames.ElementAt(1).Url);
            Assert.AreEqual("about:blank", await Page.Frames.ElementAt(1).EvaluateAsync<string>("() => window.location.href"));
            Assert.NotNull(await Page.Frames.ElementAt(1).QuerySelectorAsync("DIV"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should work in iframes that failed initial navigation")]
        [Test, SkipBrowserAndPlatform(skipChromium: true, skipFirefox: true)]
        public async Task ShouldWorkInIframesThatInterruptedInitialJavascriptUrlNavigation()
        {
            await Page.GotoAsync(Server.EmptyPage);

            await Page.EvaluateAsync(@"() => {
                const iframe = document.createElement('iframe');
                iframe.src = 'javascript:""""';
                document.body.appendChild(iframe);
                iframe.contentDocument.open();
                iframe.contentDocument.write('<div>hello</div>');
                iframe.contentDocument.close();
            }");

            Assert.AreEqual(Server.EmptyPage, await Page.Frames.ElementAt(1).EvaluateAsync<string>("() => window.location.href"));
            Assert.NotNull(await Page.Frames.ElementAt(1).QuerySelectorAsync("DIV"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "evaluateHandle should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task EvaluateHandleShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var windowHandle = await Page.MainFrame.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }
    }
}
