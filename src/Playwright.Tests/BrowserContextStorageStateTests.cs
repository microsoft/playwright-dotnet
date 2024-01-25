/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

public sealed class BrowserContextStorageStateTests : PageTestEx
{
    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should capture local storage")]
    public async Task ShouldCaptureLocalStorage()
    {
        var page1 = await Context.NewPageAsync();
        await page1.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });

        await page1.GotoAsync("https://www.example.com");
        await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
            }");
        await page1.GotoAsync("https://www.domain.com");
        await page1.EvaluateAsync(@"() =>
            {
                localStorage['name2'] = 'value2';
            }");

        string storage = await Context.StorageStateAsync();

        // TODO: think about IVT-in the StorageState and serializing
        string expected = @"{""cookies"":[],""origins"":[{""origin"":""https://www.example.com"",""localStorage"":[{""name"":""name1"",""value"":""value1""}]},{""origin"":""https://www.domain.com"",""localStorage"":[{""name"":""name2"",""value"":""value2""}]}]}";
        Assert.AreEqual(expected, storage);
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should set local storage")]
    public async Task ShouldSetLocalStorage()
    {
        var context = await Browser.NewContextAsync(new()
        {
            StorageState = "{\"cookies\":[],\"origins\":[{\"origin\":\"https://www.example.com\",\"localStorage\":[{\"name\":\"name1\",\"value\":\"value1\"}]}]}",
        });
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });
        await page.GotoAsync("https://www.example.com");
        var localStorage = await page.EvaluateAsync<string[]>("Object.keys(window.localStorage)");
        Assert.AreEqual(localStorage, new string[] { "name1" });
        var name1Value = await page.EvaluateAsync<string>("window.localStorage.getItem('name1')");
        Assert.AreEqual(name1Value, "value1");
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should round-trip through the file")]
    public async Task ShouldRoundTripThroughTheFile()
    {
        var page1 = await Context.NewPageAsync();
        await page1.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });

        await page1.GotoAsync("https://www.example.com");
        await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
                document.cookie = 'username=John Doe';
            }");
        using var tempDir = new TempDirectory();
        string path = Path.Combine(tempDir.Path, "storage-state.json");
        string storage = await Context.StorageStateAsync(new() { Path = path });
        Assert.AreEqual(storage, File.ReadAllText(path));

        await using var context = await Browser.NewContextAsync(new() { StorageStatePath = path });
        var page2 = await context.NewPageAsync();
        await page2.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });

        await page2.GotoAsync("https://www.example.com");
        Assert.AreEqual("value1", await page2.EvaluateAsync<string>("localStorage['name1']"));
        Assert.AreEqual("username=John Doe", await page2.EvaluateAsync<string>("document.cookie"));
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should capture cookies")]
    public async Task ShouldCaptureCookies()
    {
        Server.SetRoute("/setcookie.html", context =>
        {
            context.Response.Cookies.Append("a", "b");
            context.Response.Cookies.Append("empty", "");
            return Task.CompletedTask;
        });

        await Page.GotoAsync(Server.Prefix + "/setcookie.html");
        CollectionAssert.AreEqual(new[] { "a=b", "empty=" }, await Page.EvaluateAsync<string[]>(@"() =>
            {
                const cookies = document.cookie.split(';');
                return cookies.map(cookie => cookie.trim()).sort();
            }"));

        var storageState = await Context.StorageStateAsync();
        StringAssert.Contains(@"""name"":""a"",""value"":""b""", storageState);
        StringAssert.Contains(@"""name"":""empty"",""value"":""""", storageState);
        if (TestConstants.IsWebKit || TestConstants.IsFirefox)
        {
            StringAssert.Contains(@"""sameSite"":""None""", storageState);
        }
        else
        {
            StringAssert.Contains(@"""sameSite"":""Lax""", storageState);
        }
        StringAssert.DoesNotContain(@"""url"":null", storageState);

        await using var context2 = await Browser.NewContextAsync(new() { StorageState = storageState });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        CollectionAssert.AreEqual(new[] { "a=b", "empty=" }, await page2.EvaluateAsync<string[]>(@"() =>
            {
                const cookies = document.cookie.split(';');
                return cookies.map(cookie => cookie.trim()).sort();
            }"));
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should serialize storageState with lone surrogates")]
    public async Task ShouldSerializeStorageStateWithLoneSurrogates()
    {
        var searchMarker = "Hello world!";
        int[] chars = [
            14210,8342,610,1472,19632,13824,29376,52231,24579,88,36890,4099,29440,26416,368,7872,9985,62632,6848,21248,
            60513,2332,816,5504,9068,280,720,8260,54576,60417,14515,3472,4292,21022,23588,62856,15618,54344,16400,224,
            1729,31022,13314,55489,24597,51409,33318,22595,704,14765,778,56631,24578,56476,32964,39424,7828,8221,51744,
            3712,6344,53892,35214,12930,54335,17412,38458,35221,38530,12828,36826,52929,54075,14117,38543,51596,3520,
            9406,49282,46281,33302,38109,38419,5659,6227,1101,5,20566,6667,23670,6695,35098,16395,17190,49346,5565,
            46010,1051,47039,45173,1132,25204,31265,6934,352,33321,36748,40073,38546,1552,21249,6751,1046,12933,40065,
            22076,40682,6667,25192,32952,2312,49105,42577,9084,31760,49257,16515,37715,20904,2595,11524,35137,45905,
            25278,30832,13765,50053,714,1574,13587,5456,31714,51728,27160,204,18500,32854,57112,10241,11029,12673,
            16108,36873,40065,16816,16625,15436,13392,19254,37433,15982,8520,45550,11584,40368,52490,19,56704,1622,
            63553,51238,27755,34758,50245,12517,40704,7298,33479,35072,132,5252,1341,8513,37323,39640,6971,16403,17185,
            61873,32168,39565,32796,23697,24656,45365,52524,24701,20486,5280,10806,17,40,34384,21352,378,32109,27116,
            ..searchMarker.Select(c => (int)c),
            25868,39443,46994,36014,3254,24990,50578,57588,95,17205,2238,19477,12360,31960,34491,23471,54313,3566,
            22047,46654,16911,45251,54280,54371,11533,27568,7502,38757,24987,16635,9792,46500,864,35905,47223,41120,
            12047,40824,8224,1154,8560,37954,10000,18724,21097,18305,2338,17186,61967,8227,64361,63895,28094,22567,
            45901,35044,24343,17361,62467,12428,12940,58130,1794,2257,13824,33696,59144,3707,1121,9283,5060,35122,16882,
            16099,15720,55934,52917,44987,68,16649,720,31773,19171,36912,15372,33184,22574,64,142,13843,1477,44223,3872,
            1602,27321,3096,32826,33415,43034,62624,57963,48163,39146,7046,37300,27027,31927,15592,60218,24619,41025,
            22156,39659,27246,31265,36426,21236,15014,19376,26,43265,16592,6402,18144,63725,1389,368,26770,18656,10448,
            44291,37489,60845,49161,26831,198,32780,18498,2535,31051,11046,53820,22530,534,41057,29215,22784,0,
        ];
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"chars => window.localStorage.setItem('foo', chars.map(c => String.fromCharCode(c)).join(''))", chars);
        string storageState = await Context.StorageStateAsync();
        StringAssert.Contains(searchMarker, storageState);
    }
}
