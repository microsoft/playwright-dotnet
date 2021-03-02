namespace Microsoft.Playwright.Helpers
{
    /// <summary>
    /// Environment variables used by the Playwright.
    /// </summary>
    public static class EnvironmentVariables
    {
        /// <summary>
        /// Optional drivers path.
        /// </summary>
        public const string DriverPathEnvironmentVariable = "PLAYWRIGHT_DRIVER_PATH";

        /// <summary>
        /// Environment variable use to set the browsers path.
        /// </summary>
        public const string BrowsersPathEnvironmentVariable = "PLAYWRIGHT_BROWSERS_PATH";
    }
}
