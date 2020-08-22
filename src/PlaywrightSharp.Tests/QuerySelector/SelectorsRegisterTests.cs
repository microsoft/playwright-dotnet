using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>selectors.register</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class SelectorsRegisterTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public SelectorsRegisterTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Skip = "We are not going to support this on v1.1.1")]
        public void ShouldWork()
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should work with path</playwright-it>
        [Fact(Skip = "We are not going to support this on v1.1.1")]
        public void ShouldWorkWithPath()
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should work in main and isolated world</playwright-it>
        [Fact(Skip = "We are not going to support this on v1.1.1")]
        public void ShouldWorkInMainAndIsolatedWorld()
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should handle errors</playwright-it>
        [Fact(Skip = "We are not going to support this on v1.1.1")]
        public void ShouldHandleErrors()
        {
        }
    }
}