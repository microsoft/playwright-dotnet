namespace Microsoft.Playwright.Tests;

public class PlaywrightDisposeTests : PlaywrightTestEx
{
    [PlaywrightTest]
    public async Task ShouldThrowTargetClosedExceptionAfterPlaywrightIsDisposed()
    {
        var localPlaywright = await Microsoft.Playwright.Playwright.CreateAsync();
        var disposed = false;

        try
        {
            var browser = await localPlaywright[BrowserName].LaunchAsync();
            var page = await browser.NewPageAsync();

            localPlaywright.Dispose();
            disposed = true;

            await PlaywrightAssert.ThrowsAsync<TargetClosedException>(() => page.TitleAsync());
        }
        finally
        {
            if (!disposed)
            {
                localPlaywright.Dispose();
            }
        }
    }
}
