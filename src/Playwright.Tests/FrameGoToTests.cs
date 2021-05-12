using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FrameGoToTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameGoToTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("frame-goto.spec.ts", "should navigate subframes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateSubFrames()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Assert.Single(Page.Frames.Where(f => f.Url.Contains("/frames/one-frame.html")));
            Assert.Single(Page.Frames.Where(f => f.Url.Contains("/frames/frame.html")));
            var childFrame = Page.FirstChildFrame();
            var response = await childFrame.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Same(response.Frame, childFrame);
        }

        [PlaywrightTest("frame-goto.spec.ts", "should reject when frame detaches")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectWhenFrameDetaches()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Server.SetRoute("/empty.html", _ => Task.Delay(10000));
            var waitForRequestTask = Server.WaitForRequest("/empty.html");
            var navigationTask = Page.FirstChildFrame().GotoAsync(TestConstants.EmptyPage);
            await waitForRequestTask;
            await Page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(async () => await navigationTask);
            Assert.Contains("frame was detached", exception.Message);
        }

        [PlaywrightTest("frame-goto.spec.ts", "should continue after client redirect")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldContinueAfterClientRedirect()
        {
            Server.SetRoute("/frames/script.js", _ => Task.Delay(10000));
            string url = TestConstants.ServerUrl + "/frames/child-redirect.html";
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.GotoAsync(url, WaitUntilState.NetworkIdle, 5000));

            Assert.Contains("Timeout 5000ms", exception.Message);
            Assert.Contains($"navigating to \"{url}\", waiting until \"networkidle\"", exception.Message);
        }

        [PlaywrightTest("frame-goto.spec.ts", "should return matching responses")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnMatchingResponses()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
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
                matchingData[i].NavigationTask = matchingData[i].FrameTask.Result.GotoAsync($"{TestConstants.ServerUrl}/one-style.html?index={i}");
                await waitRequestTask;
            }
            // Respond from server out-of-order.
            string[] serverResponseTexts = new string[] { "AAA", "BBB", "CCC" };
            for (int i = 0; i < 3; ++i)
            {
                matchingData[i].ServerResponseTcs.TrySetResult(serverResponseTexts[i]);
                var response = await matchingData[i].NavigationTask;
                Assert.Same(matchingData[i].FrameTask.Result, response.Frame);
                Assert.Equal(serverResponseTexts[i], await response.TextAsync());
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
