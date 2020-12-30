using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>geolocation.spec.js</playwright-file>
    ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 10,
                Latitude = 10
            });
            var geolocation = await Page.EvaluateAsync<Geolocation>(
                @"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                    resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
                }))");
            Assert.Equal(new Geolocation
            {
                Latitude = 10,
                Longitude = 10
            }, geolocation);
        }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should throw when invalid longitude</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenInvalidLongitude()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Context.SetGeolocationAsync(new Geolocation
                {
                    Longitude = 200,
                    Latitude = 100
                }));
            Assert.Contains("geolocation.longitude", exception.Message);
            Assert.Contains("failed", exception.Message);
        }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should isolate contexts</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateContexts()
        {
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 10,
                Latitude = 10
            });
            await Page.GoToAsync(TestConstants.EmptyPage);


            await using var context2 = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Permissions = new[] { ContextPermission.Geolocation },
                Geolocation = new Geolocation { Latitude = 20, Longitude = 20 },
            });

            var page2 = await context2.NewPageAsync();
            await page2.GoToAsync(TestConstants.EmptyPage);

            var geolocation = await Page.EvaluateAsync<Geolocation>(
                @"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                    resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
                }))");
            Assert.Equal(new Geolocation
            {
                Latitude = 10,
                Longitude = 10
            }, geolocation);

            var geolocation2 = await page2.EvaluateAsync<Geolocation>(
                @"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                    resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
                }))");
            Assert.Equal(new Geolocation
            {
                Latitude = 20,
                Longitude = 20
            }, geolocation2);
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
            var geolocation = new Geolocation { Latitude = 10, Longitude = 10 };
            BrowserContextOptions options = new BrowserContextOptions { Geolocation = geolocation };
            await using var context = await Browser.NewContextAsync(options);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetGeolocationAsync(new Geolocation
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseContextOptions()
        {
            var options = new BrowserContextOptions
            {
                Geolocation = new Geolocation
                {
                    Latitude = 10,
                    Longitude = 10
                },
                Permissions = new[] { ContextPermission.Geolocation },
            };

            await using var context = await Browser.NewContextAsync(options);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            var geolocation = await page.EvaluateAsync<Geolocation>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
            }))");
            Assert.Equal(options.Geolocation, geolocation);
        }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>watchPosition should be notified</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task WatchPositionShouldBeNotified()
        {
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            await Page.GoToAsync(TestConstants.EmptyPage);

            var messages = new List<string>();
            Page.Console += (sender, e) => messages.Add(e.Message.Text);

            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 0,
                Latitude = 0
            });

            var geolocation = await Page.EvaluateAsync<Geolocation>(@"() => {
                navigator.geolocation.watchPosition(pos => {
                    const coords = pos.coords;
                    console.log(`lat=${coords.latitude} lng=${coords.longitude}`);
                }, err => {});
            }");

            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console, e => e.Message.Text.Contains("lat=0 lng=10")),
                Context.SetGeolocationAsync(new Geolocation { Latitude = 0, Longitude = 10 }));

            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console, e => e.Message.Text.Contains("lat=20 lng=30")),
                Context.SetGeolocationAsync(new Geolocation { Latitude = 20, Longitude = 30 }));

            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console, e => e.Message.Text.Contains("lat=40 lng=50")),
                Context.SetGeolocationAsync(new Geolocation { Latitude = 40, Longitude = 50 }));

            string allMessages = string.Join("|", messages);
            Assert.Contains("lat=0 lng=10", allMessages);
            Assert.Contains("lat=20 lng=30", allMessages);
            Assert.Contains("lat=40 lng=50", allMessages);
        }

        ///<playwright-file>geolocation.spec.js</playwright-file>
        ///<playwright-describe>Overrides.setGeolocation</playwright-describe>
        ///<playwright-it>should use context options for popup</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseContextOptionsForPopup()
        {
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 10,
                Latitude = 10,
            });

            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/geolocation.html"));

            await popupTask.Result.Page.WaitForLoadStateAsync();
            var geolocation = await popupTask.Result.Page.EvaluateAsync<Geolocation>("() => window.geolocationPromise");
            Assert.Equal(10, geolocation.Longitude);
            Assert.Equal(10, geolocation.Longitude);
        }
    }
}
