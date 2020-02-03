using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;
using Xunit;
using System.Net.Http;
using Microsoft.Extensions.Primitives;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace PlaywrightSharp.Tests.RequestInterception
{
    public class PageSetRequestInterceptionTests : PlaywrightSharpPageBaseTest
    {
        internal PageSetRequestInterceptionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should intercept</playwright-it>
        [Fact]
        public async Task ShouldIntercept()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                Assert.Contains("empty.html", e.Request.Url);
                Assert.True(e.Request.Headers.ContainsKey("user-agent"));
                Assert.Equal(HttpMethod.Get, e.Request.Method);
                Assert.Null(e.Request.PostData);
                Assert.True(e.Request.IsNavigationRequest);
                Assert.Equal(ResourceType.Document, e.Request.ResourceType);
                Assert.Same(e.Request.Frame, Page.MainFrame);
                Assert.Equal("about:blank", e.Request.Frame.Url);
                await e.Request.ContinueAsync();
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work when POST is redirected with 302</playwright-it>
        [Fact]
        public async Task ShouldWorkWhenPOSTIsRedirectedWith302()
        {

            Server.SetRedirect("/rredirect", "/empty.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            await page.setContent(@"
                  <form action='/rredirect' method='post'>
                    <input type=""hidden"" id=""foo"" name=""foo"" value=""FOOBAR"">
                  </form>");
            await Task.WhenAll(
              Page.QuerySelectorAsync("form").EvaluateAsync("form => form.submit()"),
              Page.WaitForNavigationAsync()
            );
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work when header manipulation headers with redirect</playwright-it>
        [Fact]
        public async Task ShouldWorkWhenHeaderManipulationHeadersWithRedirect()
        {

            Server.SetRedirect("/rrredirect", "/empty.html");
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) =>
            {
                var headers = new Dictionary<string, string>(e.Request.Headers) { ["foo"] = "bar" };
                e.Request.ContinueAsync(new Payload { Headers = headers });
            };
            await Page.GoToAsync(TestConstants.ServerUrl + "/rrredirect");
        }


        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should be able to remove headers</playwright-it>
        [Fact]
        public async Task ShouldBeAbleToRemoveHeaders()
        {

            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) =>
            {
                var headers = new Dictionary<string, string>(e.Request.Headers) { ["foo"] = "bar" };
                headers.Remove("origin");
                e.Request.ContinueAsync(new Payload { Headers = headers });
            };

            var originRequestHeader = Server.WaitForRequest("/empty.html", request => request.Headers["origin"]);
            await Task.WhenAll(
             originRequestHeader,
             Page.GoToAsync(TestConstants.EmptyPage)
         );
            Assert.Equal(StringValues.Empty, originRequestHeader.Result);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should contain referer header</playwright-it>
        [Fact]
        public async Task ShouldContainRefererHeader()
        {

            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>;
            Page.Request += (sender, e) =>
            {
                requests.Add(e.Request);
                e.Request.ContinueAsync();
            };
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.Contains("/one-style.css", requests[1].Url);
            Assert.Contains("/one-style.html", requests[1].Headers["referer"]);
        }


        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should properly return navigation response when URL has cookies</playwright-it>
        [Fact]
        public async Task ShouldProperlyReturnNavigationResponseWhenURLHasCookies()
        {
            // Setup cookie.
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "foo",
                Value = "bar"
            });

            // Setup request interception.
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) => e.Request.ContinueAsync();
            var response = await Page.ReloadAsync();
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should stop intercepting</playwright-it>
        [Fact]
        public async Task ShouldStopIntercepting()
        {
            await Page.SetRequestInterceptionAsync(true);
            async void ContinueRequestOnce(object sender, RequestEventArgs e)
            {
                Page.SetRequestInterceptionAsync -= ContinueRequestOnce;
                await e.Request.ContinueAsync();
            }
            Page.Request += ContinueRequestOnce;
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetRequestInterceptionAsync(false);
            await Page.GoToAsync(TestConstants.EmptyPage);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should show custom HTTP headers</playwright-it>
        [Fact]
        public async Task ShouldShowCustomHTTPHeaders()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["foo"] = "bar"
            });
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                Assert.Equal("bar", e.Request.Headers["foo"]);
                await e.Request.ContinueAsync();
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with redirect inside sync XHR</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRedirectInsideSyncXHR()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRedirect("/logo.png", "/pptr.png");
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            var status = await Page.EvaluateAsync<int>(@"async () => {
            var request = new XMLHttpRequest();
            request.open('GET', '/logo.png', false);  // `false` makes the request synchronous
            request.send(null);
            return request.status;
        }");
            Assert.Equal(200, status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with custom referer headers</playwright-it>
        [Fact]
        public async Task ShouldWorkWithCustomRefererHeaders()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = TestConstants.EmptyPage });
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                Assert.Equal(TestConstants.EmptyPage, e.Request.Headers["referer"]);
                await e.Request.ContinueAsync();
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should be abortable</playwright-it>
        [Fact]
        public async Task ShouldBeAbortable()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                if (e.Request.Url.EndsWith(".css"))
                    await e.Request.AbortAsync();
                else
                    await e.Request.ContinueAsync();
            };
            var failedRequests = 0;
            Page.RequestFailed += (sender, e) => ++failedRequests;
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.True(response.Ok);
            Assert.Null(response.Request.Failure);
            Assert.Equal(1, failedRequests);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should be abortable with custom error codes</playwright-it>
        [Fact]
        public async Task ShouldBeAbortableWithCustomErrorCodes()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) =>
            {
                e.Request.AbortAsync("internetdisconnected");
            };
            var failedRequest = null;
            Page.RequestFailed += (sender, e) => failedRequest = e.Request;
            await Page.GoToAsync(TestConstants.EmptyPage).ContinueWith(task => { });
            Assert.NotNull(failedRequest);
            if (TestConstants.IsWebKit)
                Assert.Equal("Request intercepted", failedRequest.Failure.ErrorText);
            else if (TestConstants.IsFirefox)
                Assert.Equal("NS_ERROR_OFFLINE", failedRequest.Failure.ErrorText);
            else
                Assert.Equal("net::ERR_INTERNET_DISCONNECTED", failedRequest.Failure.ErrorText);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should send referer</playwright-it>
        [Fact]
        public async Task ShouldSendReferer()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = "http://google.com/" });
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            var requestTask = Server.WaitForRequest("grid.html", request => request.Headers["referer"]);
            await Task.WhenAll(
              requestTask,
              Page.GoToAsync(TestConstants.ServerUrl + "/grid.html")
            );
            Assert.Equal("http://google.com/", requestTask.Result);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should fail navigation when aborting main resource</playwright-it>
        [Fact]
        public async Task ShouldFailNavigationWhenAbortingMainResource()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.AbortAsync();
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.GoToAsync(TestConstants.EmptyPage));
            Assert.NotNull(exception);
            if (TestConstants.IsWebKit)
                Assert.Contains("Request intercepted", exception.Message);
            else if (TestConstants.IsFirefox)
                Assert.Contains("NS_ERROR_FAILURE", exception.Message);
            else
                Assert.Contains("net::ERR_FAILED", exception.Message);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with redirects</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRedirects()
        {

            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += (sender, e) =>
            {
                await e.Request.ContinueAsync();
                requests.Add(e.Request);
            };
            Server.SetRedirect("/non-existing-page.html", "/non-existing-page-2.html");
            Server.SetRedirect("/non-existing-page-2.html", "/non-existing-page-3.html");
            Server.SetRedirect("/non-existing-page-3.html", "/non-existing-page-4.html");
            Server.SetRedirect("/non-existing-page-4.html", "/empty.html");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/non-existing-page.html");
            Assert.Equal(200, response.Status);
            Assert.Contains("empty.html", response.Url);
            Assert.Equal(5, requests.Count);
            Assert.Equal(ResourceType.Document, requests[2].ResourceType);
            // Check redirect chain
            var redirectChain = response.Request.RedirectChain;
            Assert.Equal(4, redirectChain.Count);
            Assert.Contains("/non-existing-page.html", redirectChain[0].url());
            Assert.Contains("/non-existing-page-3.html", redirectChain[2].url());
            for (var i = 0; i < redirectChain.length; ++i)
            {
                var request = redirectChain[i];
                Assert.True(request.IsNavigationRequest);
                Assert.Equal(request.redirectChain[i], request);
            }
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with redirects for subresources</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRedirectsForSubresources()
        {
            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += (sender, e) =>
            {
                await e.Request.ContinueAsync();
                requests.push(e.Request);
            };
            Server.SetRedirect("/one-style.css", "/two-style.css");
            Server.SetRedirect("/two-style.css", "/three-style.css");
            Server.SetRedirect("/three-style.css", "/four-style.css");
            Server.SetRoute("/four-style.css", context => context.Response.WriteAsync("body {box-sizing: border-box; }"));

            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Contains("one-style.html", response.Url);
            Assert.Equal(5, requests.Count);
            Assert.Equal(ResourceType.Document, requests[0].ResourceType);
            Assert.Equal(ResourceType.Stylesheet, requests[1].ResourceType);
            // Check redirect chain
            var redirectChain = requests[1].redirectChain();
            Assert.Equal(3, redirectChain.Count);
            Assert.Contains("/one-style.css", redirectChain[0].Url);
            Assert.Contains("/three-style.css", redirectChain[2].Url);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with equal requests</playwright-it>
        [Fact]
        public async Task ShouldWorkWithEqualRequests()
        {

            await Page.GoToAsync(TestConstants.EmptyPage);
            let responseCount = 1;
            server.setRoute('/zzz', (req, res) => res.end((responseCount++) * 11 + ''));
            await Page.SetRequestInterceptionAsync(true);

            let spinner = false;
            // Cancel 2nd request.
            Page.Request += (sender, e) =>
            {
                spinner? request.AbortAsync() : await e.Request.ContinueAsync();
                spinner = !spinner;
            });
            var results = await page.evaluate(() => Promise.all([
              fetch('/zzz').then(response => response.text()).catch (e => 'FAILED'),
        fetch('/zzz').then(response => response.text()).catch (e => 'FAILED'),
        fetch('/zzz').then(response => response.text()).catch (e => 'FAILED'),
      ]));
            expect(results).toEqual(['11', 'FAILED', '22']);
            }
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should navigate to dataURL and not fire dataURL requests</playwright-it>
        [Fact]
        public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests()
        {

            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += (sender, e) =>
            {
                requests.push(request);
                await e.Request.ContinueAsync();
            });
            var dataURL = 'data:text/html,<div>yo</div>';
            var response = await Page.GoToAsync(dataURL);
            expect(response).toBe(null);
            expect(requests.length).toBe(0);
        }
    }

    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Page.setRequestInterception</playwright-describe>
    ///<playwright-it>should be able to fetch dataURL and not fire dataURL requests</playwright-it>
    [Fact]
    public async Task ShouldBeAbleToFetchDataURLAndNotFireDataURLRequests()
    {

        await Page.GoToAsync(TestConstants.EmptyPage);
        await Page.SetRequestInterceptionAsync(true);
        var requests = new List<IRequest>();
        Page.Request += (sender, e) =>
        {
            requests.push(request);
            await e.Request.ContinueAsync();
        });
        var dataURL = 'data:text/html,<div>yo</div>';
        var text = await page.evaluate(url => fetch(url).then(r => r.text()), dataURL);
        expect(text).toBe('<div>yo</div>');
        expect(requests.length).toBe(0);
    }
}

///<playwright-file>interception.spec.js</playwright-file>
///<playwright-describe>Page.setRequestInterception</playwright-describe>
///<playwright-it>should navigate to URL with hash and and fire requests without hash</playwright-it>
[Fact(Skip = "Not implemented")]
public async Task ShouldNavigateToURLWithHashAndAndFireRequestsWithoutHash()
{

    await Page.SetRequestInterceptionAsync(true);
    var requests = new List<IRequest>();
    Page.Request += (sender, e) =>
    {
        requests.push(request);
        await e.Request.ContinueAsync();
    });
    var response = await Page.GoToAsync(server.EMPTY_PAGE + '#hash');
    expect(response.status()).toBe(200);
    expect(response.url()).toBe(server.EMPTY_PAGE);
    expect(requests.length).toBe(1);
    expect(requests[0].url()).toBe(server.EMPTY_PAGE);
}
}

///<playwright-file>interception.spec.js</playwright-file>
///<playwright-describe>Page.setRequestInterception</playwright-describe>
///<playwright-it>should work with encoded server</playwright-it>
[Fact]
public async Task ShouldWorkWithEncodedServer()
{

    // The requestWillBeSent will report encoded URL, whereas interception will
    // report URL as-is. @see crbug.com/759388
    await Page.SetRequestInterceptionAsync(true);
    Page.Request += async (sender, e) => await e.Request.ContinueAsync();
    var response = await Page.GoToAsync(server.PREFIX + '/some nonexisting page');
    expect(response.status()).toBe(404);
}
}

///<playwright-file>interception.spec.js</playwright-file>
///<playwright-describe>Page.setRequestInterception</playwright-describe>
///<playwright-it>should work with badly encoded server</playwright-it>
[Fact]
public async Task ShouldWorkWithBadlyEncodedServer()
{

    await Page.SetRequestInterceptionAsync(true);
    server.setRoute('/malformed?rnd=%911', (req, res) => res.end());
    Page.Request += async (sender, e) => await e.Request.ContinueAsync();
    var response = await Page.GoToAsync(server.PREFIX + '/malformed?rnd=%911');
    expect(response.status()).toBe(200);
}
}

///<playwright-file>interception.spec.js</playwright-file>
///<playwright-describe>Page.setRequestInterception</playwright-describe>
///<playwright-it>should work with encoded server - 2</playwright-it>
[Fact]
public async Task ShouldWorkWithEncodedServer-2()
        {

      // The requestWillBeSent will report URL as-is, whereas interception will
      // report encoded URL for stylesheet. @see crbug.com/759388
      await Page.SetRequestInterceptionAsync(true);
var requests = new List<IRequest>();
Page.Request += (sender, e) => {
        await e.Request.ContinueAsync();
requests.push(request);
      });
      var response = await Page.GoToAsync(`data: text / html,< link rel = "stylesheet" href = "${server.PREFIX}/fonts?helvetica|arial" />`);
expect(response).toBe(null);
expect(requests.length).toBe(1);
expect(requests[0].response().status()).toBe(404);
    }
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should not throw "Invalid Interception Id" if the request was cancelled</playwright-it>
        [Fact]
public async Task ShouldNotThrow"InvalidInterceptionId"IfTheRequestWasCancelled()
{

    await page.setContent('<iframe></iframe>');
    await Page.SetRequestInterceptionAsync(true);
    let request = null;
    page.on('request', async r => request = r);
    page.$eval('iframe', (frame, url) => frame.src = url, server.EMPTY_PAGE),
      // Wait for request interception.
      await utils.waitEvent(page, 'request');
    // Delete frame to cause request to be canceled.
    await page.$eval('iframe', frame => frame.remove());
    let error = null;
    await request.continue().catch (e => error = e);
    expect(error).toBe(null);
    }
}

///<playwright-file>interception.spec.js</playwright-file>
///<playwright-describe>Page.setRequestInterception</playwright-describe>
///<playwright-it>should throw if interception is not enabled</playwright-it>
[Fact]
public async Task ShouldThrowIfInterceptionIsNotEnabled()
{
    async({ newPage, server}) => {
        let error = null;
        var page = await newPage();
        page.on('request', async request =>
        {
            try
            {
                await await e.Request.ContinueAsync();
            }
            catch (e)
            {
                error = e;
            }
        });
        await Page.GoToAsync(TestConstants.EmptyPage);
        expect(error.message).toContain('Request Interception is not enabled');
    }
}

///<playwright-file>interception.spec.js</playwright-file>
///<playwright-describe>Page.setRequestInterception</playwright-describe>
///<playwright-it>should intercept main resource during cross-process navigation</playwright-it>
[Fact]
public async Task ShouldInterceptMainResourceDuringCross-processNavigation()
{

    await Page.GoToAsync(TestConstants.EmptyPage);
    await Page.SetRequestInterceptionAsync(true);
    let intercepted = false;
    Page.Request += (sender, e) =>
    {
        if (request.url().includes(server.CROSS_PROCESS_PREFIX + '/empty.html'))
            intercepted = true;
        await e.Request.ContinueAsync();
    });
    var response = await Page.GoToAsync(server.CROSS_PROCESS_PREFIX + '/empty.html');
    expect(response.ok()).toBe(true);
    expect(intercepted).toBe(true);
}
}

///<playwright-file>interception.spec.js</playwright-file>
///<playwright-describe>Page.setRequestInterception</playwright-describe>
///<playwright-it>should not throw when continued after navigation</playwright-it>
[Fact]
public async Task ShouldNotThrowWhenContinuedAfterNavigation()
{

    await Page.SetRequestInterceptionAsync(true);
    Page.Request += (sender, e) =>
    {
        if (request.url() !== server.PREFIX + '/one-style.css')
            await e.Request.ContinueAsync();
    });
    // For some reason, Firefox issues load event with one outstanding request.
    var failed = Page.GoToAsync(server.PREFIX + '/one-style.html', { waitUntil: FFOX ? 'networkidle0' : 'load' }).catch (e => e);
    var request = await page.waitForRequest(server.PREFIX + '/one-style.css');
    await Page.GoToAsync(server.PREFIX + '/empty.html');
    var error = await failed;
    expect(error.message).toBe('Navigation to ' + server.PREFIX + '/one-style.html was canceled by another one');
    var notAnError = await request.continue().then(() => null).catch (e => e);
    expect(notAnError).toBe(null);
    }
}

///<playwright-file>interception.spec.js</playwright-file>
///<playwright-describe>Page.setRequestInterception</playwright-describe>
///<playwright-it>should not throw when continued after cross-process navigation</playwright-it>
[Fact]
public async Task ShouldNotThrowWhenContinuedAfterCross-processNavigation()
{

    await Page.SetRequestInterceptionAsync(true);
    Page.Request += (sender, e) =>
    {
        if (request.url() !== server.PREFIX + '/one-style.css')
            await e.Request.ContinueAsync();
    });
    // For some reason, Firefox issues load event with one outstanding request.
    var failed = Page.GoToAsync(server.PREFIX + '/one-style.html', { waitUntil: FFOX ? 'networkidle0' : 'load' }).catch (e => e);
    var request = await page.waitForRequest(server.PREFIX + '/one-style.css');
    await Page.GoToAsync(server.CROSS_PROCESS_PREFIX + '/empty.html');
    var error = await failed;
    expect(error.message).toBe('Navigation to ' + server.PREFIX + '/one-style.html was canceled by another one');
    var notAnError = await request.continue().then(() => null).catch (e => e);
    expect(notAnError).toBe(null);
    }
}

}
    }
}
