using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Frame.goto</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class GoToTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public GoToTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Frame.goto</playwright-describe>
        ///<playwright-it>should navigate subframes</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateSubFrames()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Assert.Single(Page.Frames.Where(f => f.Url.Contains("/frames/one-frame.html")));
            Assert.Single(Page.Frames.Where(f => f.Url.Contains("/frames/frame.html")));
            var childFrame = Page.FirstChildFrame();
            var response = await childFrame.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Same(response.Frame, childFrame);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Frame.goto</playwright-describe>
        ///<playwright-it>should reject when frame detaches</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectWhenFrameDetaches()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Server.SetRoute("/empty.html", context => Task.Delay(10000));
            var waitForRequestTask = Server.WaitForRequest("/empty.html");
            var navigationTask = Page.FirstChildFrame().GoToAsync(TestConstants.EmptyPage);
            await waitForRequestTask;
            await Page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(async () => await navigationTask);
            Assert.Contains("frame was detached", exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Frame.goto</playwright-describe>
        ///<playwright-it>should continue after client redirect</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldContinueAfterClientRedirect()
        {
            Server.SetRoute("/frames/script.js", context => Task.Delay(10000));
            string url = TestConstants.ServerUrl + "/frames/child-redirect.html";
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.GoToAsync(url, LifecycleEvent.Networkidle, null, 5000));

            Assert.Contains("Timeout 5000ms", exception.Message);
            Assert.Contains($"navigating to \"{url}\", waiting until \"networkidle\"", exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Frame.goto</playwright-describe>
        ///<playwright-it>should return matching responses</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnMatchingResponses()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            // Attach three frames.
            var matchingData = new MatchingResponseData[]
            {
                new MatchingResponseData{ FrameTask =  FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage)},
                new MatchingResponseData{ FrameTask =  FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage)},
                new MatchingResponseData{ FrameTask =  FrameUtils.AttachFrameAsync(Page, "frame3", TestConstants.EmptyPage)}
            };

            await TaskUtils.WhenAll(matchingData.Select(m => m.FrameTask));

            // Navigate all frames to the same URL.
            var requestHandler = new RequestDelegate(async (context) =>
            {
                if (int.TryParse(context.Request.Query["index"], out int index))
                {
                    await context.Response.WriteAsync(await matchingData[index].ServerResponseTcs.Task);
                }
            });

            Server.SetRoute("/one-style.html?index=0", requestHandler);
            Server.SetRoute("/one-style.html?index=1", requestHandler);
            Server.SetRoute("/one-style.html?index=2", requestHandler);

            for (int i = 0; i < 3; ++i)
            {
                var waitRequestTask = Server.WaitForRequest("/one-style.html");
                matchingData[i].NavigationTask = matchingData[i].FrameTask.Result.GoToAsync($"{TestConstants.ServerUrl}/one-style.html?index={i}");
                await waitRequestTask;
            }
            // Respond from server out-of-order.
            string[] serverResponseTexts = new string[] { "AAA", "BBB", "CCC" };
            for (int i = 0; i < 3; ++i)
            {
                matchingData[i].ServerResponseTcs.TrySetResult(serverResponseTexts[i]);
                var response = await matchingData[i].NavigationTask;
                Assert.Same(matchingData[i].FrameTask.Result, response.Frame);
                Assert.Equal(serverResponseTexts[i], await response.GetTextAsync());
            }
        }

        class MatchingResponseData
        {
            public Task<IFrame> FrameTask { get; internal set; }
            public TaskCompletionSource<string> ServerResponseTcs { get; internal set; } = new TaskCompletionSource<string>();
            public Task<IResponse> NavigationTask { get; internal set; }
        }
    }
}
