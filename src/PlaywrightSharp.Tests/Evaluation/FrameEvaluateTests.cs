using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Evaluation
{
    ///<playwright-file>evaluation.spec.js</playwright-file>
    ///<playwright-describe>Frame.evaluate</playwright-describe>
    public class FrameEvaluateTests : PlaywrightSharpPageBaseTest
    {
        internal FrameEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should have different execution contexts</playwright-it>
        [Fact]
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

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should have correct execution contexts</playwright-it>
        [Fact]
        public async Task ShouldHaveCorrectExecutionContexts()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Assert.Equal(2, Page.Frames.Length);
            Assert.Empty(await Page.Frames[0].EvaluateAsync<string>("() => document.body.textContent.trim()"));
            Assert.Equal("Hi, I'm frame", await Page.Frames[1].EvaluateAsync<string>("() => document.body.textContent.trim()"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should dispose context on navigation</playwright-it>
        [Fact]
        public async Task ShouldDisposeContextOnNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Assert.Equal(2, Page.Frames.Length);
            // TODO: expect(page._delegate._contextIdToContext.size).toBe(4);
            await Page.GoToAsync(TestConstants.EmptyPage);
            // TODO: expect(page._delegate._contextIdToContext.size).toBe(2);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should dispose context on cross-origin navigation</playwright-it>
        [Fact]
        public async Task ShouldDisposeContextOnCrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Assert.Equal(2, Page.Frames.Length);
            // TODO: expect(page._delegate._contextIdToContext.size).toBe(4);
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            // TODO: expect(page._delegate._contextIdToContext.size).toBe(2);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should execute after cross-site navigation</playwright-it>
        [Fact]
        public async Task ShouldExecuteAfterCrossSiteNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var mainFrame = Page.MainFrame;
            Assert.Contains("localhost", await mainFrame.EvaluateAsync<string>("() => window.location.href"));
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.Contains("127", await mainFrame.EvaluateAsync<string>("() => window.location.href"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should allow cross-frame js handles</playwright-it>
        [Fact(Skip = "Not implemented")]
        public async Task ShouldAllowCrossFrameJsHandles()
        {
            // TODO: this should be possible because frames script each other, but
            // protocol implementations do not support this.
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var handle = await Page.EvaluateHandleAsync(@"() => {
                const iframe = document.querySelector('iframe');
                const foo = { bar: 'baz' };
                iframe.contentWindow.__foo = foo;
                return foo;
            }");
            var childFrame = Page.MainFrame.ChildFrames[0];
            var childResult = await childFrame.EvaluateAsync<object>("() => window.__foo");
            Assert.Equal(new { bar = "baz" }, childResult);
            var result = await childFrame.EvaluateAsync<string>("foo => foo.bar", handle);
            Assert.Equal("baz", result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should allow cross-frame element handles</playwright-it>
        [Fact]
        public async Task ShouldAllowCrossFrameElementHandles()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var bodyHandle = await Page.MainFrame.ChildFrames[0].QuerySelectorAsync("body");
            var result = await Page.EvaluateAsync<string>("body => body.innerHTML", bodyHandle);
            Assert.Equal("<div>Hi, I\'m frame</div>", result.Trim());
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should not allow cross-frame element handles when frames do not script each other</playwright-it>
        [Fact(Skip = "Not implemented")]
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
