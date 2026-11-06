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

    // See https://github.com/microsoft/playwright-dotnet/issues/3419
    [PlaywrightTest]
    public async Task ShouldDisposeWhileDriverIsSendingEvents()
    {
        var localPlaywright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await localPlaywright[BrowserName].LaunchAsync();
        var page = await browser.NewPageAsync();
        var sendingEvents = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        page.Console += (_, _) => sendingEvents.TrySetResult(true);
        await page.EvaluateAsync(ConsoleFlood);
        await sendingEvents.Task;

        // Used to hang on Windows, where the driver blocks on writing events that nobody reads.
        await Task.Run(() => localPlaywright.Dispose());
    }

    // See https://github.com/microsoft/playwright-dotnet/issues/3419
    [PlaywrightTest]
    public async Task ShouldDisposeFromEventHandlerWhileDriverIsSendingEvents()
    {
        var localPlaywright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await localPlaywright[BrowserName].LaunchAsync();
        var page = await browser.NewPageAsync();
        var disposed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = 0;
        page.Console += (_, _) =>
        {
            if (Interlocked.Increment(ref events) != 10)
            {
                return;
            }
            try
            {
                // The handler runs on the thread that reads the driver output.
                localPlaywright.Dispose();
                disposed.TrySetResult(true);
            }
            catch (Exception e)
            {
                disposed.TrySetException(e);
            }
        };
        await page.EvaluateAsync(ConsoleFlood);
        await disposed.Task;
    }

    // Many times more than a pipe holds, and it never stops.
    private const string ConsoleFlood = "() => { setInterval(() => { for (let i = 0; i < 200; i++) console.log('x'.repeat(20000)); }, 0); }";
}
