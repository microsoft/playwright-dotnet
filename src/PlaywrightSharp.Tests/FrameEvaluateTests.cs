using System.Dynamic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FrameEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should have different execution contexts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveDifferentExecutionContexts()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, Page.Frames.Length);
            await Page.Frames[0].EvaluateAsync("() => window.FOO = 'foo'");
            await Page.Frames[1].EvaluateAsync("() => window.FOO = 'bar'");
            Assert.Equal("foo", await Page.Frames[0].EvaluateAsync<string>("() => window.FOO"));
            Assert.Equal("bar", await Page.Frames[1].EvaluateAsync<string>("() => window.FOO"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should have correct execution contexts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveCorrectExecutionContexts()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Assert.Equal(2, Page.Frames.Length);
            Assert.Empty(await Page.Frames[0].EvaluateAsync<string>("() => document.body.textContent.trim()"));
            Assert.Equal("Hi, I'm frame", await Page.Frames[1].EvaluateAsync<string>("() => document.body.textContent.trim()"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should dispose context on navigation")]
        [Fact(Skip = "Ignore USES_HOOKS")]
        public void ShouldDisposeContextOnNavigation()
        {
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should dispose context on cross-origin navigation")]
        [Fact(Skip = "Ignore USES_HOOKS")]
        public void ShouldDisposeContextOnCrossOriginNavigation()
        {
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should execute after cross-site navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExecuteAfterCrossSiteNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var mainFrame = Page.MainFrame;
            Assert.Contains("localhost", await mainFrame.EvaluateAsync<string>("() => window.location.href"));
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.Contains("127", await mainFrame.EvaluateAsync<string>("() => window.location.href"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should allow cross-frame js handles")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowCrossFrameJsHandles()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var handle = await Page.EvaluateHandleAsync(@"() => {
                const iframe = document.querySelector('iframe');
                const foo = { bar: 'baz' };
                iframe.contentWindow.__foo = foo;
                return foo;
            }");
            var childFrame = Page.MainFrame.ChildFrames[0];
            dynamic childResult = await childFrame.EvaluateAsync<ExpandoObject>("() => window.__foo");
            Assert.Equal("baz", childResult.bar);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => childFrame.EvaluateAsync<string>("foo => foo.bar", handle));
            Assert.Equal("JSHandles can be evaluated only in the context they were created!", exception.Message);
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should allow cross-frame element handles")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowCrossFrameElementHandles()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var bodyHandle = await Page.MainFrame.ChildFrames[0].QuerySelectorAsync("body");
            string result = await Page.EvaluateAsync<string>("body => body.innerHTML", bodyHandle);
            Assert.Equal("<div>Hi, I\'m frame</div>", result.Trim());
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should not allow cross-frame element handles when frames do not script each other")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAllowCrossFrameElementHandlesWhenFramesDoNotScriptEachOther()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.CrossProcessUrl + "/empty.html");
            var bodyHandle = await frame.QuerySelectorAsync("body");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("body => body.innerHTML", bodyHandle));
            Assert.Contains("Unable to adopt element handle from a different document", exception.Message);
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should throw for detached frames")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForDetachedFrames()
        {
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => frame1.EvaluateAsync("() => 7 * 8"));
            Assert.Contains("Execution Context is not available in detached frame", exception.Message);
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should be isolated between frames")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeIsolatedBetweenFrames()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, Page.Frames.Length);
            var frames = Page.Frames;
            Assert.NotSame(frames[0], frames[1]);

            await TaskUtils.WhenAll(
                frames[0].EvaluateAsync("() => window.a = 1"),
                frames[1].EvaluateAsync("() => window.a = 2")
            );

            var (result1, result2) = await TaskUtils.WhenAll(
                frames[0].EvaluateAsync<int>("() => window.a"),
                frames[1].EvaluateAsync<int>("() => window.a")
            );
            Assert.Equal(1, result1);
            Assert.Equal(2, result2);
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should work in iframes that failed initial navigation")]
        [SkipBrowserAndPlatformFact(skipChromium: true, skipFirefox: true)]
        public async Task ShouldWorkIniframesThatFailedInitialNavigation()
        {
            await Page.SetContentAsync(
                @"<meta http-equiv=""Content-Security-Policy"" content=""script-src 'none';"">
                 <iframe src='javascript:""""'></iframe>",
                LifecycleEvent.DOMContentLoaded);

            await Page.EvaluateAsync(@"() => {
                const iframe = document.querySelector('iframe');
                const div = iframe.contentDocument.createElement('div');
                iframe.contentDocument.body.appendChild(div);
            }");

            Assert.Equal("about:blank", Page.Frames[1].Url);
            Assert.Equal("about:blank", await Page.Frames[1].EvaluateAsync<string>("() => window.location.href"));
            Assert.NotNull(await Page.Frames[1].QuerySelectorAsync("DIV"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "should work in iframes that failed initial navigation")]
        [SkipBrowserAndPlatformFact(skipChromium: true, skipFirefox: true)]
        public async Task ShouldWorkInIframesThatInterruptedInitialJavascriptUrlNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Page.EvaluateAsync(@"() => {
                const iframe = document.createElement('iframe');
                iframe.src = 'javascript:""""';
                document.body.appendChild(iframe);
                iframe.contentDocument.open();
                iframe.contentDocument.write('<div>hello</div>');
                iframe.contentDocument.close();
            }");

            Assert.Equal(TestConstants.EmptyPage, await Page.Frames[1].EvaluateAsync<string>("() => window.location.href"));
            Assert.NotNull(await Page.Frames[1].QuerySelectorAsync("DIV"));
        }

        [PlaywrightTest("frame-evaluate.spec.ts", "evaluateHandle should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task EvaluateHandleShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var windowHandle = await Page.MainFrame.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }
    }
}
