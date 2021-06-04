using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageAutoWaitingBasicTests : PageTestEx
    {
        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when clicking anchor")]
        [Test, Ignore("Flacky")]
        public async Task ShouldAwaitNavigationWhenClickingAnchor()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($"<a href=\"{Server.EmptyPage}\">empty.html</a>");
            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
                Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

            Assert.AreEqual("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await cross-process navigation when clicking anchor")]
        [Test, Ignore("Flacky")]
        public async Task ShouldAwaitCrossProcessNavigationWhenClickingAnchor()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($"<a href=\"{Server.CrossProcessPrefix}/empty.html\">empty.html</a>");
            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
                Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

            Assert.AreEqual("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await form-get on click")]
        [Test, Ignore("Flacky")]
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
                <form action=""{Server.EmptyPage}"" method=""get"">
                    <input name=""foo"" value=""bar"">
                    <input type=""submit"" value=""Submit"">
                </form>");

            await TaskUtils.WhenAll(
                Page.ClickAsync("input[type=submit]").ContinueWith(_ => messages.Add("click")),
                Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

            Assert.AreEqual("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await form-post on click")]
        [Test, Ignore("Flacky")]
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
                <form action=""{ Server.EmptyPage}"" method=""post"">
                    <input name=""foo"" value=""bar"">
                    <input type=""submit"" value=""Submit"">
                </form>");

            await TaskUtils.WhenAll(
                Page.ClickAsync("input[type=submit]").ContinueWith(_ => messages.Add("click")),
                Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

            Assert.AreEqual("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when assigning location")]
        [Test, Ignore("Flacky")]
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
                Page.EvaluateAsync($"window.location.href = '{Server.EmptyPage}'").ContinueWith(_ => messages.Add("evaluate")),
                Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

            Assert.AreEqual("route|navigated|evaluate", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when assigning location twice")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
                window.location.href = '{Server.EmptyPage}?cancel';
                window.location.href = '{Server.EmptyPage}?override';");
            messages.Add("evaluate");

            Assert.AreEqual("routeoverride|evaluate", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when evaluating reload")]
        [Test, Ignore("Flacky")]
        public async Task ShouldAwaitNavigationWhenEvaluatingReload()
        {
            var messages = new List<string>();
            await Page.GotoAsync(Server.EmptyPage);
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await TaskUtils.WhenAll(
                Page.EvaluateAsync($"window.location.reload();").ContinueWith(_ => messages.Add("evaluate")),
                Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

            Assert.AreEqual("route|navigated|evaluate", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigating specified target")]
        [Test, Ignore("Flacky")]
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
                <a href=""{ Server.EmptyPage}"" target=target>empty.html</a>
                <iframe name=target></iframe>");

            var frame = Page.Frame("target");

            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
                Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

            Assert.AreEqual(Server.EmptyPage, frame.Url);
            Assert.AreEqual("route|navigated|click", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with noWaitAfter: true")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoWaitAfterTrue()
        {
            Server.SetRoute("/empty.html", _ => Task.CompletedTask);
            await Page.SetContentAsync($@"<a href=""{ Server.EmptyPage}"" target=target>empty.html</a>");
            await Page.ClickAsync("a", new PageClickOptions { NoWaitAfter = true });
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with waitForLoadState(load)")]
        [Test, Ignore("Flacky")]
        public async Task ShouldWorkWithWaitForLoadStateLoad()
        {
            var messages = new List<string>();
            Server.SetRoute("/empty.html", context =>
            {
                messages.Add("route");
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
            });

            await Page.SetContentAsync($"<a href=\"{Server.EmptyPage}\">empty.html</a>");
            var clickLoaded = new TaskCompletionSource<bool>();

            await TaskUtils.WhenAll(
                Page.ClickAsync("a").ContinueWith(_ => Page.WaitForLoadStateAsync(LoadState.Load).ContinueWith(_ =>
                {
                    messages.Add("clickload");
                    clickLoaded.TrySetResult(true);
                })),
                clickLoaded.Task,
                Page.WaitForNavigationAsync(new PageWaitForNavigationOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ContinueWith(_ => messages.Add("domcontentloaded")));

            Assert.AreEqual("route|domcontentloaded|clickload", string.Join("|", messages));
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with goto following click")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
                <form action=""{ Server.EmptyPage}/login.html"" method=""get"">
                    <input type=""text"">
                    <input type=""submit"" value=""Submit"">
                </form>");

            await Page.FillAsync("input[type=text]", "admin");
            await Page.ClickAsync("input[type=submit]");
            await Page.GotoAsync(Server.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-basic.spec.ts", "should report navigation in the log when clicking anchor")]
        [Test, Ignore("We ignore USES_HOOKS")]
        public void ShouldReportNavigationInTheLogWhenClickingAnchor() { }
    }
}
