using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Request.resourceType</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RequestResourceTypeTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RequestResourceTypeTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.resourceType</playwright-describe>
        ///<playwright-it>should return event source</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnEventSource()
        {
            const string sseMessage = "{\"foo\": \"bar\"}";

            Server.SetRoute("/sse", async ctx =>
            {
                ctx.Response.Headers["content-type"] = "text/event-stream";
                ctx.Response.Headers["connection"] = "keep-alive";
                ctx.Response.Headers["cache-control"] = "no-cache";

                await ctx.Response.Body.FlushAsync();
                await ctx.Response.WriteAsync($"data: {sseMessage}\r\r");
                await ctx.Response.Body.FlushAsync();
            });

            await Page.GoToAsync(TestConstants.EmptyPage);
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);

            Assert.Equal(sseMessage, await Page.EvaluateAsync<string>(@"() => {
                const eventSource = new EventSource('/sse');
                return new Promise(resolve => {
                    eventSource.onmessage = e => resolve(e.data);
                });
            }"));

            Assert.Equal(ResourceType.EventSource, requests[0].ResourceType);
        }
    }
}
