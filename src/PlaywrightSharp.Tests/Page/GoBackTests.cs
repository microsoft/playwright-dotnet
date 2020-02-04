using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Helpers;
using Xunit;
using Xunit.Abstractions;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Page.goBack</playwright-describe>
    public class GoBackTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public GoBackTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goBack</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");

            var response = await Page.GoBackAsync();
            Assert.True(response.Ok);
            Assert.Equal(TestConstants.EmptyPage, response.Url);

            response = await Page.GoForwardAsync();
            Assert.True(response.Ok);
            Assert.Contains("grid", response.Url);

            response = await Page.GoForwardAsync();
            Assert.Null(response);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goBack</playwright-describe>
        ///<playwright-it>should work with HistoryAPI</playwright-it>
        [Fact]
        public async Task ShouldWorkWithHistoryAPI()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"
              history.pushState({ }, '', '/first.html');
              history.pushState({ }, '', '/second.html');
            ");
            Assert.Equal(TestConstants.ServerUrl + "/second.html", Page.Url);

            await Page.GoBackAsync();
            Assert.Equal(TestConstants.ServerUrl + "/first.html", Page.Url);
            await Page.GoBackAsync();
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
            await Page.GoForwardAsync();
            Assert.Equal(TestConstants.ServerUrl + "/first.html", Page.Url);
        }
    }
}