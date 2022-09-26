/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

public class GeolocationTests : PageTestEx
{
    [PlaywrightTest("geolocation.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        await Page.GotoAsync(Server.EmptyPage);
        await Context.SetGeolocationAsync(new()
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
    public async Task ShouldThrowWhenInvalidLongitude()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
            Context.SetGeolocationAsync(new()
            {
                Longitude = 200,
                Latitude = 100
            }));
        StringAssert.Contains("geolocation.longitude", exception.Message);
        StringAssert.Contains("failed", exception.Message);
    }

    [PlaywrightTest("geolocation.spec.ts", "should isolate contexts")]
    public async Task ShouldIsolateContexts()
    {
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        await Context.SetGeolocationAsync(new()
        {
            Longitude = 10,
            Latitude = 10
        });
        await Page.GotoAsync(Server.EmptyPage);


        await using var context2 = await Browser.NewContextAsync(new()
        {
            Permissions = new[] { "geolocation" },
            Geolocation = new() { Latitude = 20, Longitude = 20 },
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

    [PlaywrightTest("geolocation.spec.ts", "should not modify passed default options object")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldNotModifyPassedDefaultOptionsObject()
    {
        var geolocation = new Geolocation { Latitude = 10, Longitude = 10 };
        BrowserNewContextOptions options = new() { Geolocation = geolocation };
        await using var context = await Browser.NewContextAsync(options);
        await Page.GotoAsync(Server.EmptyPage);
        await Context.SetGeolocationAsync(new()
        {
            Longitude = 20,
            Latitude = 20
        });
        Assert.AreEqual(options.Geolocation.Latitude, geolocation.Latitude);
        Assert.AreEqual(options.Geolocation.Longitude, geolocation.Longitude);
    }

    [PlaywrightTest("geolocation.spec.ts", "should use context options")]
    public async Task ShouldUseContextOptions()
    {
        var options = new BrowserNewContextOptions()
        {
            Geolocation = new()
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
    public async Task WatchPositionShouldBeNotified()
    {
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        await Page.GotoAsync(Server.EmptyPage);

        var messages = new List<string>();
        Page.Console += (_, e) => messages.Add(e.Text);

        await Context.SetGeolocationAsync(new()
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
            await Context.SetGeolocationAsync(new() { Latitude = 0, Longitude = 10 });
        }, new()
        {
            Predicate = e => e.Text.Contains("lat=0 lng=10")
        });

        await TaskUtils.WhenAll(
            Page.WaitForConsoleMessageAsync(new()
            {
                Predicate = e => e.Text.Contains("lat=20 lng=30")
            }),
            Context.SetGeolocationAsync(new() { Latitude = 20, Longitude = 30 }));

        await TaskUtils.WhenAll(
            Page.WaitForConsoleMessageAsync(new()
            {
                Predicate = e => e.Text.Contains("lat=40 lng=50")
            }),
            Context.SetGeolocationAsync(new() { Latitude = 40, Longitude = 50 }));

        string allMessages = string.Join("|", messages);
        StringAssert.Contains("lat=0 lng=10", allMessages);
        StringAssert.Contains("lat=20 lng=30", allMessages);
        StringAssert.Contains("lat=40 lng=50", allMessages);
    }

    [PlaywrightTest("geolocation.spec.ts", "should use context options for popup")]
    public async Task ShouldUseContextOptionsForPopup()
    {
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        await Context.SetGeolocationAsync(new()
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
