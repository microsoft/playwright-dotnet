using System.Dynamic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Evaluation
{
    ///<playwright-file>evaluation.spec.js</playwright-file>
    ///<playwright-describe>Frame.evaluate</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FrameEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("evaluation.spec.js", "Frame.evaluate", "should have different execution contexts")]
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

        [PlaywrightTest("evaluation.spec.js", "Frame.evaluate", "should have correct execution contexts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveCorrectExecutionContexts()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Assert.Equal(2, Page.Frames.Length);
            Assert.Empty(await Page.Frames[0].EvaluateAsync<string>("() => document.body.textContent.trim()"));
            Assert.Equal("Hi, I'm frame", await Page.Frames[1].EvaluateAsync<string>("() => document.body.textContent.trim()"));
        }

        [PlaywrightTest("evaluation.spec.js", "Frame.evaluate", "should dispose context on navigation")]
        [Fact(Skip = "Ignore USES_HOOKS")]
        public void ShouldDisposeContextOnNavigation()
        {
        }

        [PlaywrightTest("evaluation.spec.js", "Frame.evaluate", "should dispose context on cross-origin navigation")]
        [Fact(Skip = "Ignore USES_HOOKS")]
        public void ShouldDisposeContextOnCrossOriginNavigation()
        {
        }

        [PlaywrightTest("evaluation.spec.js", "Frame.evaluate", "should execute after cross-site navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExecuteAfterCrossSiteNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var mainFrame = Page.MainFrame;
            Assert.Contains("localhost", await mainFrame.EvaluateAsync<string>("() => window.location.href"));
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.Contains("127", await mainFrame.EvaluateAsync<string>("() => window.location.href"));
        }

        [PlaywrightTest("evaluation.spec.js", "Frame.evaluate", "should allow cross-frame js handles")]
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

        [PlaywrightTest("evaluation.spec.js", "Frame.evaluate", "should allow cross-frame element handles")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowCrossFrameElementHandles()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var bodyHandle = await Page.MainFrame.ChildFrames[0].QuerySelectorAsync("body");
            string result = await Page.EvaluateAsync<string>("body => body.innerHTML", bodyHandle);
            Assert.Equal("<div>Hi, I\'m frame</div>", result.Trim());
        }

        [PlaywrightTest("evaluation.spec.js", "Frame.evaluate", "should not allow cross-frame element handles when frames do not script each other")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAllowCrossFrameElementHandlesWhenFramesDoNotScriptEachOther()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.CrossProcessUrl + "/empty.html");
            var bodyHandle = await frame.QuerySelectorAsync("body");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("body => body.innerHTML", bodyHandle));
            Assert.Contains("Unable to adopt element handle from a different document", exception.Message);
        }
    }
}
