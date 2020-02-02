using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>geolocation.spec.js</playwright-file>
    ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
    public class GeolocationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public GeolocationTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]

    }
}