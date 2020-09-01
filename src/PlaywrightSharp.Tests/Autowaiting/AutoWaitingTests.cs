using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Autowaiting
{
    ///<playwright-file>autowaiting.spec.js</playwright-file>
    ///<playwright-describe>Auto waiting</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class AutoWaitingTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public AutoWaitingTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should await navigation when clicking anchor</playwright-it>
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
                Page.ClickAsync("a").ContinueWith(t => messages.Add("click")),
                Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated).ContinueWith(t => messages.Add("navigated")));

            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should await cross-process navigation when clicking anchor</playwright-it>
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
                Page.ClickAsync("a").ContinueWith(t => messages.Add("click")),
                Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated).ContinueWith(t => messages.Add("navigated")));

            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should await form-get on click</playwright-it>
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
                Page.ClickAsync("input[type=submit]").ContinueWith(t => messages.Add("click")),
                Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated).ContinueWith(t => messages.Add("navigated")));

            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should await form-post on click</playwright-it>
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
                Page.ClickAsync("input[type=submit]").ContinueWith(t => messages.Add("click")),
                Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated).ContinueWith(t => messages.Add("navigated")));

            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should await navigation when assigning location</playwright-it>
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
                Page.EvaluateAsync($"window.location.href = '{TestConstants.EmptyPage}'").ContinueWith(t => messages.Add("evaluate")),
                Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated).ContinueWith(t => messages.Add("navigated")));

            Assert.Equal("route|navigated|evaluate", string.Join("|", messages));
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should await navigation when assigning location twice</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should await navigation when evaluating reload</playwright-it>
        [Fact(Skip = "Flacky")]
        public async Task ShouldAwaitNavigationWhenEvaluatingReload()
        {
            var messages = new List<string>();
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await TaskUtils.WhenAll(
                Page.EvaluateAsync($"window.location.reload();").ContinueWith(t => messages.Add("evaluate")),
                Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated).ContinueWith(t => messages.Add("navigated")));

            Assert.Equal("route|navigated|evaluate", string.Join("|", messages));
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should await navigating specified target</playwright-it>
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

            var frame = Page.Frames.FirstOrDefault(f => f.Name == "target");

            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(t => messages.Add("click")),
                Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated).ContinueWith(t => messages.Add("navigated")));

            Assert.Equal(TestConstants.EmptyPage, frame.Url);
            Assert.Equal("route|navigated|click", string.Join("|", messages));
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should work with noWaitAfter: true</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithNoWaitAfterTrue()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context => Task.CompletedTask);

            await Page.SetContentAsync($@"<a href=""{ TestConstants.EmptyPage}"" target=target>empty.html</a>");
            await Page.ClickAsync("a", noWaitAfter: true);
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should work with waitForLoadState(load)</playwright-it>
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
                Page.ClickAsync("a").ContinueWith(t => Page.WaitForLoadStateAsync(LifecycleEvent.Load).ContinueWith(t =>
                {
                    messages.Add("clickload");
                    clickLoaded.TrySetResult(true);
                })),
                clickLoaded.Task,
                Page.WaitForNavigationAsync(LifecycleEvent.DOMContentLoaded).ContinueWith(t => messages.Add("domcontentloaded")));

            Assert.Equal("route|domcontentloaded|clickload", string.Join("|", messages));
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should work with goto following click</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            await Page.GoToAsync(TestConstants.EmptyPage);
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting</playwright-describe>
        ///<playwright-it>should report navigation in the log when clicking anchor</playwright-it>
        [Fact(Skip = "We ignore USES_HOOKS")]
        public void ShouldReportNavigationInTheLogWhenClickingAnchor() { }
    }
}
