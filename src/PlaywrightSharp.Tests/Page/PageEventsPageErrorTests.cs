using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.PageError</playwright-describe>
    public class PageEventsPageErrorTests : PlaywrightSharpPageBaseTest
    {
        internal PageEventsPageErrorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.PageError</playwright-describe>
        ///<playwright-it>should fire</playwright-it>
        [Fact]
        public async Task ShouldFire()
        {
            let error = null;
            Page.once('pageerror', e => error = e);
            await Promise.all([
              Page.GoToAsync(TestConstants.ServerUrl + '/error.html'),
              waitEvent(page, 'pageerror')
            ]);
            expect(error.message).toContain('Fancy');

        }

    }

}
