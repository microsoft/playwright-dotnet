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

public class PermissionsTests : PageTestEx
{
    [PlaywrightTest("permissions.spec.ts", "should be prompt by default")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldBePromptByDefault()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("prompt", await GetPermissionAsync(Page, "geolocation"));
    }

    [PlaywrightTest("permissions.spec.ts", "should deny permission when not listed")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldDenyPermissionWhenNotListed()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Context.GrantPermissionsAsync(Array.Empty<string>(), new() { Origin = Server.EmptyPage });
        Assert.AreEqual("denied", await GetPermissionAsync(Page, "geolocation"));
    }

    [PlaywrightTest("permissions.spec.ts", "should fail when bad permission is given")]
    public async Task ShouldFailWhenBadPermissionIsGiven()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
            Context.GrantPermissionsAsync(new[] { "foo" }, new() { Origin = Server.EmptyPage }));
    }

    [PlaywrightTest("permissions.spec.ts", "should grant geolocation permission when listed")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldGrantGeolocationPermissionWhenListed()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
    }

    [PlaywrightTest("permissions.spec.ts", "should grant notifications permission when listed")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldGrantNotificationsPermissionWhenListed()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Context.GrantPermissionsAsync(new[] { "notifications" });
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "notifications"));
    }

    [PlaywrightTest("permissions.spec.ts", "should accumulate when adding")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldAccumulateWhenAdding()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        await Context.GrantPermissionsAsync(new[] { "notifications" });
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "notifications"));
    }

    [PlaywrightTest("permissions.spec.ts", "should clear permissions")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldClearPermissions()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
        await Context.ClearPermissionsAsync();
        await Context.GrantPermissionsAsync(new[] { "notifications" });
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "notifications"));
        Assert.That("granted", Is.Not.EqualTo(await GetPermissionAsync(Page, "geolocation")));
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "notifications"));
    }

    [PlaywrightTest("permissions.spec.ts", "should grant permission when listed for all domains")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldGrantPermissionWhenListedForAllDomains()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Context.GrantPermissionsAsync(new[] { "geolocation" });
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
    }

    [PlaywrightTest("permissions.spec.ts", "should grant permission when creating context")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldGrantPermissionWhenCreatingContext()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            Permissions = new[] { "geolocation" },
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("granted", await GetPermissionAsync(page, "geolocation"));
    }

    [PlaywrightTest("permissions.spec.ts", "should reset permissions")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldResetPermissions()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Context.GrantPermissionsAsync(new[] { "geolocation" }, new() { Origin = Server.EmptyPage });
        Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
        await Context.ClearPermissionsAsync();
        Assert.AreEqual("prompt", await GetPermissionAsync(Page, "geolocation"));
    }

    [PlaywrightTest("permissions.spec.ts", "should trigger permission onchange")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldTriggerPermissionOnchange()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"() => {
                window.events = [];
                return navigator.permissions.query({ name: 'geolocation'}).then(function(result) {
                    window.events.push(result.state);
                    result.onchange = function() {
                        window.events.push(result.state);
                    };
                });
            }");
        Assert.AreEqual(new[] { "prompt" }, await Page.EvaluateAsync<string[]>("window.events"));
        await Context.GrantPermissionsAsync(Array.Empty<string>(), new() { Origin = Server.EmptyPage });
        Assert.AreEqual(new[] { "prompt", "denied" }, await Page.EvaluateAsync<string[]>("window.events"));
        await Context.GrantPermissionsAsync(new[] { "geolocation" }, new() { Origin = Server.EmptyPage });
        Assert.AreEqual(
            new[] { "prompt", "denied", "granted" },
            await Page.EvaluateAsync<string[]>("window.events"));
        await Context.ClearPermissionsAsync();
        Assert.AreEqual(
            new[] { "prompt", "denied", "granted", "prompt" },
            await Page.EvaluateAsync<string[]>("window.events"));
    }

    [PlaywrightTest("permissions.spec.ts", "should trigger permission onchange")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldIsolatePermissionsBetweenBrowserContexts()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await using var otherContext = await Browser.NewContextAsync();
        var otherPage = await otherContext.NewPageAsync();
        await otherPage.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("prompt", await GetPermissionAsync(Page, "geolocation"));
        Assert.AreEqual("prompt", await GetPermissionAsync(otherPage, "geolocation"));

        await Context.GrantPermissionsAsync(Array.Empty<string>(), new() { Origin = Server.EmptyPage });
        await otherContext.GrantPermissionsAsync(new[] { "geolocation" }, new() { Origin = Server.EmptyPage });
        Assert.AreEqual("denied", await GetPermissionAsync(Page, "geolocation"));
        Assert.AreEqual("granted", await GetPermissionAsync(otherPage, "geolocation"));

        await Context.ClearPermissionsAsync();
        Assert.AreEqual("prompt", await GetPermissionAsync(Page, "geolocation"));
        Assert.AreEqual("granted", await GetPermissionAsync(otherPage, "geolocation"));

        await otherContext.CloseAsync();
    }

    private static Task<string> GetPermissionAsync(IPage page, string name)
        => page.EvaluateAsync<string>(
            "name => navigator.permissions.query({ name }).then(result => result.state)",
            name);
}
