using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class GeolocationTests : PageTestEx
    {
        [PlaywrightTest("geolocation.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Page.GotoAsync(Server.EmptyPage);
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenInvalidLongitude()
        {
            var exception = await AssertThrowsAsync<PlaywrightException>(() =>
                Context.SetGeolocationAsync(new Geolocation
                {
                    Longitude = 200,
                    Latitude = 100
                }));
            StringAssert.Contains("geolocation.longitude", exception.Message);
            StringAssert.Contains("failed", exception.Message);
        }

        [PlaywrightTest("geolocation.spec.ts", "should isolate contexts")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateContexts()
        {
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 10,
                Latitude = 10
            });
            await Page.GotoAsync(Server.EmptyPage);


            await using var context2 = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Permissions = new[] { "geolocation" },
                Geolocation = new Geolocation { Latitude = 20, Longitude = 20 },
            });

            var page2 = await context2.NewPageAsync();
            await page2.GotoAsync(Server.EmptyPage);

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
        [Test, Ignore("We don't this test")]
        public void ShouldThrowWithMissingLatitude() { }

        [PlaywrightTest("geolocation.spec.ts", "should not modify passed default options object")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldNotModifyPassedDefaultOptionsObject()
        {
            var geolocation = new Geolocation { Latitude = 10, Longitude = 10 };
            BrowserNewContextOptions options = new BrowserNewContextOptions { Geolocation = geolocation };
            await using var context = await Browser.NewContextAsync(options);
            await Page.GotoAsync(Server.EmptyPage);
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 20,
                Latitude = 20
            });
            Assert.AreEqual(options.Geolocation.Latitude, geolocation.Latitude);
            Assert.AreEqual(options.Geolocation.Longitude, geolocation.Longitude);
        }

        [PlaywrightTest("geolocation.spec.ts", "should throw with missing longitude in default options")]
        [Test, Ignore("We don't this test")]
        public void ShouldThrowWithMissingLongitudeInDefaultOptions() { }

        [PlaywrightTest("geolocation.spec.ts", "should use context options")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
            await page.GotoAsync(Server.EmptyPage);

            var geolocation = await page.EvaluateAsync<Geolocation>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
            }))");
            Assert.AreEqual(options.Geolocation.Latitude, geolocation.Latitude);
            Assert.AreEqual(options.Geolocation.Longitude, geolocation.Longitude);
        }

        [PlaywrightTest("geolocation.spec.ts", "watchPosition should be notified")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task WatchPositionShouldBeNotified()
        {
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Page.GotoAsync(Server.EmptyPage);

            var messages = new List<string>();
            Page.Console += (_, e) => messages.Add(e.Text);

            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 0,
                Latitude = 0
            });

            await Page.EvaluateAsync<Geolocation>(@"() => {
                navigator.geolocation.watchPosition(pos => {
                    const coords = pos.coords;
                    console.log(`lat=${coords.latitude} lng=${coords.longitude}`);
                }, err => {});
            }");

            await Page.RunAndWaitForConsoleMessageAsync(async () =>
            {
                await Context.SetGeolocationAsync(new Geolocation { Latitude = 0, Longitude = 10 });
            }, new PageRunAndWaitForConsoleMessageOptions
            {
                Predicate = e => e.Text.Contains("lat=0 lng=10")
            });

            await TaskUtils.WhenAll(
                Page.WaitForConsoleMessageAsync(new PageWaitForConsoleMessageOptions
                {
                    Predicate = e => e.Text.Contains("lat=20 lng=30")
                }),
                Context.SetGeolocationAsync(new Geolocation { Latitude = 20, Longitude = 30 }));

            await TaskUtils.WhenAll(
                Page.WaitForConsoleMessageAsync(new PageWaitForConsoleMessageOptions
                {
                    Predicate = e => e.Text.Contains("lat=40 lng=50")
                }),
                Context.SetGeolocationAsync(new Geolocation { Latitude = 40, Longitude = 50 }));

            string allMessages = string.Join("|", messages);
            StringAssert.Contains("lat=0 lng=10", allMessages);
            StringAssert.Contains("lat=20 lng=30", allMessages);
            StringAssert.Contains("lat=40 lng=50", allMessages);
        }

        [PlaywrightTest("geolocation.spec.ts", "should use context options for popup")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseContextOptionsForPopup()
        {
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Context.SetGeolocationAsync(new Geolocation
            {
                Longitude = 10,
                Latitude = 10,
            });

            var popupTask = Page.WaitForPopupAsync();

            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("url => window._popup = window.open(url)", Server.Prefix + "/geolocation.html"));

            await popupTask.Result.WaitForLoadStateAsync();
            var geolocation = await popupTask.Result.EvaluateAsync<Geolocation>("() => window.geolocationPromise");
            Assert.AreEqual(10, geolocation.Longitude);
            Assert.AreEqual(10, geolocation.Longitude);
        }

        void AssertEqual(float lat, float lon, Geolocation geolocation)
        {
            Assert.AreEqual(lat, geolocation.Latitude);
            Assert.AreEqual(lon, geolocation.Longitude);
        }
    }
}
