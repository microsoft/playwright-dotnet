using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>geolocation.spec.js</playwright-file>
    ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
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
        public async Task ShouldWork()
        {
            await Context.SetPermissionsAsync(TestConstants.ServerUrl, ContextPermission.Geolocation);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetGeolocationAsync(new GeolocationOption
            {
                Longitude = 10,
                Latitude = 10
            });
            var geolocation = await Page.EvaluateAsync<GeolocationOption>(
                @"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                    resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
                }))");
            Assert.Equal(new GeolocationOption
            {
                Latitude = 10,
                Longitude = 10
            }, geolocation);
        }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should throw when invalid longitude</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldThrowWhenInvalidLongitude()
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                Context.SetGeolocationAsync(new GeolocationOption
                {
                    Longitude = 200,
                    Latitude = 100
                }));
            Assert.Contains("Invalid longitude '200'", exception.Message);
        }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should throw with missing latitude</playwright-it>
        [Fact(Skip = "We don't this test")]
        public void ShouldThrowWithMissingLatitude() { }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should not modify passed default options object</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldNotModifyPassedDefaultOptionsObject()
        {
            var geolocation = new GeolocationOption { Latitude = 10, Longitude = 10 };
            BrowserContextOptions options = new BrowserContextOptions { Geolocation = geolocation };
            var context = await NewContextAsync(options);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetGeolocationAsync(new GeolocationOption
            {
                Longitude = 20,
                Latitude = 20
            });
            Assert.Equal(options.Geolocation, geolocation);
        }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should throw with missing longitude in default options</playwright-it>
        [Fact(Skip = "We don't this test")]
        public void ShouldThrowWithMissingLongitudeInDefaultOptions() { }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should use context options</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldUseContextOptions()
        {
            var options = new BrowserContextOptions
            {
                Geolocation = new GeolocationOption
                {
                    Latitude = 10,
                    Longitude = 10
                },
                Permissions = new Dictionary<string, ContextPermission[]>
                {
                    [TestConstants.ServerUrl] = new[] { ContextPermission.Geolocation }
                }
            };

            var context = await NewContextAsync(options);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            var geolocation = await page.EvaluateAsync<GeolocationOption>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
            }))");
            Assert.Equal(options.Geolocation, geolocation);
        }
    }
}
