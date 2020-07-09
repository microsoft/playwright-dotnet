using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Async stacks</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class AsyncStacksTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public AsyncStacksTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Async stacks</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Skip = "We don't need to test this in .NET")]
        public async Task ShouldWork()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Abort(); // is this right?
                return Task.CompletedTask;
            });
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.GoToAsync(TestConstants.EmptyPage));
            // TODO: is this right?
            Assert.Contains(nameof(AsyncStacksTests), exception.StackTrace);
        }
    }
}
