using Microsoft.Playwright.Core;

namespace Microsoft.Playwright;

public static class Assertions
{
    public static ILocatorAssertions Expect(ILocator locator) => new LocatorAssertions(locator, false);

    public static IPageAssertions Expect(IPage page) => new PageAssertions(page, false);

    public static IAPIResponseAssertions Expect(IAPIResponse response) => new APIResponseAssertions(response, false);
}
