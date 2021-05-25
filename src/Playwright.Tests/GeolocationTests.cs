using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class GeolocationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public GeolocationTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("geolocation.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 10,
                Latitude = 10
            });
            var geolocation = await Page.EvaluateAsync<Geolocation>(
                @"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                    resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
                }))");
            AssertEqual(10, 10, geolocation);
        }

        [PlaywrightTest("geolocation.spec.ts", "should throw when invalid longitude")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenInvalidLongitude()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() =>
                Context.SetGeolocationAsync(new Geolocation
                {
                    Longitude = 200,
                    Latitude = 100
                }));
            Assert.Contains("geolocation.longitude", exception.Message);
            Assert.Contains("failed", exception.Message);
        }

        [PlaywrightTest("geolocation.spec.ts", "should isolate contexts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateContexts()
        {
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 10,
                Latitude = 10
            });
            await Page.GotoAsync(TestConstants.EmptyPage);


            await using var context2 = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Permissions = new[] { "geolocation" },
                Geolocation = new Geolocation { Latitude = 20, Longitude = 20 },
            });

            var page2 = await context2.NewPageAsync();
            await page2.GotoAsync(TestConstants.EmptyPage);

            var geolocation = await Page.EvaluateAsync<Geolocation>(
                @"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                    resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
                }))");
            AssertEqual(10, 10, geolocation);

            var geolocation2 = await page2.EvaluateAsync<Geolocation>(
                @"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                    resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
                }))");
            AssertEqual(20, 20, geolocation2);
        }

        [PlaywrightTest("geolocation.spec.ts", "should throw with missing latitude")]
        [Fact(Skip = "We don't this test")]
        public void ShouldThrowWithMissingLatitude() { }

        [PlaywrightTest("geolocation.spec.ts", "should not modify passed default options object")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldNotModifyPassedDefaultOptionsObject()
        {
            var geolocation = new Geolocation { Latitude = 10, Longitude = 10 };
            BrowserNewContextOptions options = new BrowserNewContextOptions { Geolocation = geolocation };
            await using var context = await Browser.NewContextAsync(options);
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 20,
                Latitude = 20
            });
            Assert.Equal(options.Geolocation.Latitude, geolocation.Latitude);
            Assert.Equal(options.Geolocation.Longitude, geolocation.Longitude);
        }

        [PlaywrightTest("geolocation.spec.ts", "should throw with missing longitude in default options")]
        [Fact(Skip = "We don't this test")]
        public void ShouldThrowWithMissingLongitudeInDefaultOptions() { }

        [PlaywrightTest("geolocation.spec.ts", "should use context options")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseContextOptions()
        {
            var options = new BrowserNewContextOptions
            {
                Geolocation = new Geolocation
                {
                    Latitude = 10,
                    Longitude = 10
                },
                Permissions = new[] { "geolocation" },
            };

            await using var context = await Browser.NewContextAsync(options);
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);

            var geolocation = await page.EvaluateAsync<Geolocation>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
            }))");
            Assert.Equal(options.Geolocation.Latitude, geolocation.Latitude);
            Assert.Equal(options.Geolocation.Longitude, geolocation.Longitude);
        }

        [PlaywrightTest("geolocation.spec.ts", "watchPosition should be notified")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task WatchPositionShouldBeNotified()
        {
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Page.GotoAsync(TestConstants.EmptyPage);

            var messages = new List<string>();
            Page.Console += (_, e) => messages.Add(e.Text);

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

            await Page.RunAndWaitForEventAsync(PageEvent.Console, async () =>
            {
                await Context.SetGeolocationAsync(new Geolocation { Latitude = 0, Longitude = 10 });
            }, new WaitForEventOptions<IConsoleMessage>
            {
                Predicate = e => e.Text.Contains("lat=0 lng=10")
            });

            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console, new WaitForEventOptions<IConsoleMessage>
                {
                    Predicate = e => e.Text.Contains("lat=20 lng=30")
                }),
                Context.SetGeolocationAsync(new Geolocation { Latitude = 20, Longitude = 30 }));

            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console, new WaitForEventOptions<IConsoleMessage>
                {
                    Predicate = e => e.Text.Contains("lat=40 lng=50")
                }),
                Context.SetGeolocationAsync(new Geolocation { Latitude = 40, Longitude = 50 }));

            string allMessages = string.Join("|", messages);
            Assert.Contains("lat=0 lng=10", allMessages);
            Assert.Contains("lat=20 lng=30", allMessages);
            Assert.Contains("lat=40 lng=50", allMessages);
        }

        [PlaywrightTest("geolocation.spec.ts", "should use context options for popup")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseContextOptionsForPopup()
        {
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 10,
                Latitude = 10,
            });

            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/geolocation.html"));

            await popupTask.Result.WaitForLoadStateAsync();
            var geolocation = await popupTask.Result.EvaluateAsync<Geolocation>("() => window.geolocationPromise");
            Assert.Equal(10, geolocation.Longitude);
            Assert.Equal(10, geolocation.Longitude);
        }

        void AssertEqual(float lat, float lon, Geolocation geolocation)
        {
            Assert.Equal(lat, geolocation.Latitude);
            Assert.Equal(lon, geolocation.Longitude);
        }
    }
}
