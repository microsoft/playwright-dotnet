using System;

namespace Microsoft.Playwright.TestAdapter;

public class PlaywrightSettings
{
    public PlaywrightBrowser Browser { get; set; } = PlaywrightBrowser.Chromium;
    public BrowserTypeLaunchOptions? LaunchOptions { get; set; }
    public TimeSpan? ExpectTimeout { get; set; }
}
