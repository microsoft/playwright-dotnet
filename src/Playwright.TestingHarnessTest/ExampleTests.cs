using System;
using System.Threading.Tasks;
using Microsoft.Playwright.TestAdapter;
using Microsoft.Playwright.Xunit.v3;
using Playwright.TestingHarnessTest.Xunit;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Playwright.TestingHarnessTest.Xunit;

public class xunitv3basicspectsshouldbeabletosetthebrowserviatherunsettingsfile : PageTest
{
    private readonly ITestOutputHelper output;

    public xunitv3basicspectsshouldbeabletosetthebrowserviatherunsettingsfile(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task Test()
    {
        await Page.GotoAsync("about:blank");
        output.WriteLine("BrowserName: " + BrowserName);
        output.WriteLine("BrowserType: " + BrowserType.Name);
        output.WriteLine("User-Agent: " + await Page.EvaluateAsync<string>("() => navigator.userAgent"));
    }
}
