/**
 * Copyright 2017 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/geolocation.spec.ts.</summary>
public class GeolocationTests
{
    [UpstreamTest("GE-001 should work @smoke")]
    public async Task ShouldWork(Server server, ContextFactory contextFactory)
    {
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        await context.GrantPermissionsAsync(new[] { "geolocation" });
        await page.GotoAsync(server.EmptyPage);
        await context.SetGeolocationAsync(new() { Longitude = 10, Latitude = 10 });
        var geolocation = await page.EvaluateAsync<object>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
    resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude });
  }))");
        ExpectToEqual(geolocation, new
        {
            latitude = 10,
            longitude = 10,
        });
    }

    [UpstreamTest("GE-002 should throw when invalid longitude")]
    public async Task ShouldThrowWhenInvalidLongitude(ContextFactory contextFactory)
    {
        var context = await contextFactory();
        Exception error = null;
        try
        {
            await context.SetGeolocationAsync(new() { Longitude = 200, Latitude = 10 });
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error.Message, Does.Contain("geolocation.longitude: precondition -180 <= LONGITUDE <= 180 failed."));
    }

    [UpstreamTest("GE-003 should isolate contexts")]
    public async Task ShouldIsolateContexts(Server server, ContextFactory contextFactory, IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        await context.GrantPermissionsAsync(new[] { "geolocation" });
        await context.SetGeolocationAsync(new() { Longitude = 10, Latitude = 10 });
        await page.GotoAsync(server.EmptyPage);

        var context2 = await browser.NewContextAsync(new(contextOptions)
        {
            Permissions = new[] { "geolocation" },
            Geolocation = new() { Longitude = 20, Latitude = 20 },
        });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(server.EmptyPage);

        var geolocation = await page.EvaluateAsync<object>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
    resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude });
  }))");
        ExpectToEqual(geolocation, new
        {
            latitude = 10,
            longitude = 10,
        });

        var geolocation2 = await page2.EvaluateAsync<object>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
    resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude });
  }))");
        ExpectToEqual(geolocation2, new
        {
            latitude = 20,
            longitude = 20,
        });

        await context2.CloseAsync();
    }

    [UpstreamTest("GE-005 should not modify passed default options object")]
    public async Task ShouldNotModifyPassedDefaultOptionsObject(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var geolocation = new Geolocation { Longitude = 10, Latitude = 10 };
        var options = new BrowserNewContextOptions(contextOptions) { Geolocation = geolocation };
        var context = await browser.NewContextAsync(options);
        await context.SetGeolocationAsync(new() { Longitude = 20, Latitude = 20 });
        Assert.That(options.Geolocation, Is.SameAs(geolocation));
        await context.CloseAsync();
    }

    [UpstreamTest("GE-007 should use context options")]
    public async Task ShouldUseContextOptions(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var options = new BrowserNewContextOptions(contextOptions) { Geolocation = new() { Longitude = 10, Latitude = 10 }, Permissions = new[] { "geolocation" } };
        var context = await browser.NewContextAsync(options);
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

        var geolocation = await page.EvaluateAsync<object>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
    resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude });
  }))");
        ExpectToEqual(geolocation, new
        {
            latitude = 10,
            longitude = 10,
        });
        await context.CloseAsync();
    }

    [UpstreamTest("GE-008 watchPosition should be notified")]
    public async Task WatchPositionShouldBeNotified(Server server, ContextFactory contextFactory)
    {
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        await context.GrantPermissionsAsync(new[] { "geolocation" });
        await page.GotoAsync(server.EmptyPage);
        var messages = new List<string>();
        page.Console += (_, message) => messages.Add(message.Text);

        await context.SetGeolocationAsync(new() { Latitude = 0, Longitude = 0 });
        await page.EvaluateAsync(@"() => {
    navigator.geolocation.watchPosition(pos => {
      const coords = pos.coords;
      console.log(`lat=${coords.latitude} lng=${coords.longitude}`);
    }, err => {});
  }");
        await PromiseAll(
            page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text.Contains("lat=0 lng=10") }),
            context.SetGeolocationAsync(new() { Latitude = 0, Longitude = 10 }));
        await PromiseAll(
            page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text.Contains("lat=20 lng=30") }),
            context.SetGeolocationAsync(new() { Latitude = 20, Longitude = 30 }));
        await PromiseAll(
            page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text.Contains("lat=40 lng=50") }),
            context.SetGeolocationAsync(new() { Latitude = 40, Longitude = 50 }));

        var allMessages = string.Join("|", messages);
        Assert.That(allMessages, Does.Contain("lat=0 lng=10"));
        Assert.That(allMessages, Does.Contain("lat=20 lng=30"));
        Assert.That(allMessages, Does.Contain("lat=40 lng=50"));
    }

    [UpstreamTest("GE-009 should use context options for popup")]
    public async Task ShouldUseContextOptionsForPopup(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        await context.GrantPermissionsAsync(new[] { "geolocation" });
        await context.SetGeolocationAsync(new() { Longitude = 10, Latitude = 10 });
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window['_popup'] = window.open(url)", server.Prefix + "/geolocation.html"));
        await popup.WaitForLoadStateAsync();
        var geolocation = await popup.EvaluateAsync<object>("() => window['geolocationPromise']");
        ExpectToEqual(geolocation, new { longitude = 10, latitude = 10 });
    }
}
