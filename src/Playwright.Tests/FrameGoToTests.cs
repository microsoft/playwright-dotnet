using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class FrameGoToTests : PageTestEx
    {
        [PlaywrightTest("frame-goto.spec.ts", "should navigate subframes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateSubFrames()
        {
            await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            Assert.AreEqual(1, Page.Frames.Where(f => f.Url.Contains("/frames/one-frame.html")).Count());
            Assert.AreEqual(1, Page.Frames.Where(f => f.Url.Contains("/frames/frame.html")).Count());
            var childFrame = Page.FirstChildFrame();
            var response = await childFrame.GotoAsync(Server.EmptyPage);
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            Assert.AreEqual(response.Frame, childFrame);
        }

        [PlaywrightTest("frame-goto.spec.ts", "should reject when frame detaches")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectWhenFrameDetaches()
        {
            await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            Server.SetRoute("/empty.html", _ => Task.Delay(10000));
            var waitForRequestTask = Server.WaitForRequest("/empty.html");
            var navigationTask = Page.FirstChildFrame().GotoAsync(Server.EmptyPage);
            await waitForRequestTask;
            await Page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => navigationTask);
            StringAssert.Contains("frame was detached", exception.Message);
        }

        [PlaywrightTest("frame-goto.spec.ts", "should continue after client redirect")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldContinueAfterClientRedirect()
        {
            Server.SetRoute("/frames/script.js", _ => Task.Delay(10000));
            string url = Server.Prefix + "/frames/child-redirect.html";
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 5000 }));

            StringAssert.Contains("Timeout 5000ms", exception.Message);
            StringAssert.Contains($"navigating to \"{url}\", waiting until \"networkidle\"", exception.Message);
        }

        [PlaywrightTest("frame-goto.spec.ts", "should return matching responses")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnMatchingResponses()
        {
            await Page.GotoAsync(Server.EmptyPage);
            // Attach three frames.
            var matchingData = new MatchingResponseData[]
            {
                new() { FrameTask =  FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage)},
                new() { FrameTask =  FrameUtils.AttachFrameAsync(Page, "frame2", Server.EmptyPage)},
                new() { FrameTask =  FrameUtils.AttachFrameAsync(Page, "frame3", Server.EmptyPage)}
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
                matchingData[i].NavigationTask = matchingData[i].FrameTask.Result.GotoAsync($"{Server.Prefix}/one-style.html?index={i}");
                await waitRequestTask;
            }
            // Respond from server out-of-order.
            string[] serverResponseTexts = new string[] { "AAA", "BBB", "CCC" };
            for (int i = 0; i < 3; ++i)
            {
                matchingData[i].ServerResponseTcs.TrySetResult(serverResponseTexts[i]);
                var response = await matchingData[i].NavigationTask;
                Assert.AreEqual(matchingData[i].FrameTask.Result, response.Frame);
                Assert.AreEqual(serverResponseTexts[i], await response.TextAsync());
            }
        }

        class MatchingResponseData
        {
            public Task<IFrame> FrameTask { get; internal set; }
            public TaskCompletionSource<string> ServerResponseTcs { get; internal set; } = new();
            public Task<IResponse> NavigationTask { get; internal set; }
        }
    }
}
