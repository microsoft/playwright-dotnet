using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAutoWaitingBasicTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAutoWaitingBasicTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when clicking anchor")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldAwaitNavigationWhenClickingAnchor()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($"<a href=\"{TestConstants.EmptyPage}\">empty.html</a>");
            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
                Page.WaitForEventAsync(PageEvent.FrameNavigated).ContinueWith(_ => messages.Add("navigated")));

            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await cross-process navigation when clicking anchor")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldAwaitCrossProcessNavigationWhenClickingAnchor()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($"<a href=\"{TestConstants.CrossProcessHttpPrefix}/empty.html\">empty.html</a>");
            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
                Page.WaitForEventAsync(PageEvent.FrameNavigated).ContinueWith(_ => messages.Add("navigated")));

            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await form-get on click")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldAwaitFormGetOnClick()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html?foo=bar", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($@"
                <form action=""{TestConstants.EmptyPage}"" method=""get"">
                    <input name=""foo"" value=""bar"">
                    <input type=""submit"" value=""Submit"">
                </form>");

            await TaskUtils.WhenAll(
                Page.ClickAsync("input[type=submit]").ContinueWith(_ => messages.Add("click")),
                Page.WaitForEventAsync(PageEvent.FrameNavigated).ContinueWith(_ => messages.Add("navigated")));

            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await form-post on click")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldAwaitFormPostOnClick()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($@"
                <form action=""{ TestConstants.EmptyPage}"" method=""post"">
                    <input name=""foo"" value=""bar"">
                    <input type=""submit"" value=""Submit"">
                </form>");

            await TaskUtils.WhenAll(
                Page.ClickAsync("input[type=submit]").ContinueWith(_ => messages.Add("click")),
                Page.WaitForEventAsync(PageEvent.FrameNavigated).ContinueWith(_ => messages.Add("navigated")));

            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when assigning location")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldAwaitNavigationWhenAssigningLocation()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await TaskUtils.WhenAll(
                Page.EvaluateAsync($"window.location.href = '{TestConstants.EmptyPage}'").ContinueWith(_ => messages.Add("evaluate")),
                Page.WaitForEventAsync(PageEvent.FrameNavigated).ContinueWith(_ => messages.Add("navigated")));

            Assert.Equal("route|navigated|evaluate", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when assigning location twice")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAwaitNavigationWhenAssigningLocationTwice()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html?cancel", context =>
            {
                return context.Response.WriteAsync("done");
            });

            Server.SetRoute("/empty.html?override", context =>
            {
                messages.Add("routeoverride");
                return context.Response.WriteAsync("done");
            });

            await Page.EvaluateAsync($@"
                window.location.href = '{TestConstants.EmptyPage}?cancel';
                window.location.href = '{TestConstants.EmptyPage}?override';");
            messages.Add("evaluate");

            Assert.Equal("routeoverride|evaluate", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when evaluating reload")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldAwaitNavigationWhenEvaluatingReload()
        {
            var messages = new List<string>();
            await Page.GotoAsync(TestConstants.EmptyPage);
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await TaskUtils.WhenAll(
                Page.EvaluateAsync($"window.location.reload();").ContinueWith(_ => messages.Add("evaluate")),
                Page.WaitForEventAsync(PageEvent.FrameNavigated).ContinueWith(_ => messages.Add("navigated")));

            Assert.Equal("route|navigated|evaluate", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigating specified target")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldAwaitNavigatingSpecifiedTarget()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($@"
                <a href=""{ TestConstants.EmptyPage}"" target=target>empty.html</a>
                <iframe name=target></iframe>");

            var frame = Page.Frame("target");

            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
                Page.WaitForEventAsync(PageEvent.FrameNavigated).ContinueWith(_ => messages.Add("navigated")));

            Assert.Equal(TestConstants.EmptyPage, frame.Url);
            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with noWaitAfter: true")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoWaitAfterTrue()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", _ => Task.CompletedTask);

            await Page.SetContentAsync($@"<a href=""{ TestConstants.EmptyPage}"" target=target>empty.html</a>");
            await Page.ClickAsync("a", noWaitAfter: true);
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with waitForLoadState(load)")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldWorkWithWaitForLoadStateLoad()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($"<a href=\"{TestConstants.EmptyPage}\">empty.html</a>");
            var clickLoaded = new TaskCompletionSource<bool>();

            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(_ => Page.WaitForLoadStateAsync(LoadState.Load).ContinueWith(_ =>
                {
                    messages.Add("clickload");
                    clickLoaded.TrySetResult(true);
                })),
                clickLoaded.Task,
                Page.WaitForNavigationAsync(WaitUntilState.DOMContentLoaded).ContinueWith(_ => messages.Add("domcontentloaded")));

            Assert.Equal("route|domcontentloaded|clickload", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with goto following click")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithGotoFollowingClick()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("You are logged in");
            });

            await Page.SetContentAsync($@"
                <form action=""{ TestConstants.EmptyPage}/login.html"" method=""get"">
                    <input type=""text"">
                    <input type=""submit"" value=""Submit"">
                </form>");

            await Page.FillAsync("input[type=text]", "admin");
            await Page.ClickAsync("input[type=submit]");
            await Page.GotoAsync(TestConstants.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should report navigation in the log when clicking anchor")]
        [Fact(Skip = "We ignore USES_HOOKS")]
        public void ShouldReportNavigationInTheLogWhenClickingAnchor() { }
    }
}
