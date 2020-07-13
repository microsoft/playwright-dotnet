using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>browserType.launchServer</playwright-describe>
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class BrowserTypeLaunchServerTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserTypeLaunchServerTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchServer</playwright-describe>
        ///<playwright-it>should return child_process instance</playwright-it>
        [Retry]
        public async Task ShouldReturnChildProcessInstance()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            Assert.True(browserServer.ProcessId > 0);
        }


        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchServer</playwright-describe>
        ///<playwright-it>should fire close event</playwright-it>
        [Retry]
        public async Task ShouldFireCloseEvent()
        {
            var tcs = new TaskCompletionSource<bool>();
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            browserServer.Closed += (sender, e) => tcs.TrySetResult(true);
            await Task.WhenAll(tcs.Task, browserServer.CloseAsync());
        }
    }
}
