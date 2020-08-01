using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.DispatchEvent
{
    ///<playwright-file>dispatchevent.spec.js</playwright-file>
    ///<playwright-describe>Page.dispatchEvent(drag)</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageDispatchEventDragTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageDispatchEventDragTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(drag)</playwright-describe>
        ///<playwright-it>should dispatch drag drop events</playwright-it>
        // [SkipBrowserAndPlatformFact(skipWebkit: true)]
        [Fact(Skip = "We need to improve serializaton")]
        public async Task ShouldDispatchDragDropEvents()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/drag-n-drop.html");
            var dataTransfer = await Page.EvaluateHandleAsync("() => new DataTransfer()");
            await Page.DispatchEventAsync("#source", "dragstart", new { dataTransfer });
            await Page.DispatchEventAsync("#source", "drop", new { dataTransfer });

            Assert.True(await Page.EvaluateAsync<bool>("() => return source.parentElement === target;"));
        }
    }
}
