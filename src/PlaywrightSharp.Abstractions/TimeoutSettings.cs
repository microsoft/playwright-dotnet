namespace PlaywrightSharp
{
    internal class TimeoutSettings
    {
        private int? _defaultTimeout;
        private int? _defaultNavigationTimeout;

        public int Timeout => _defaultTimeout ?? Playwright.DefaultTimeout;

        public int NavigationTimeout => _defaultNavigationTimeout ?? _defaultTimeout ?? Playwright.DefaultTimeout;

        public void SetDefaultTimeout(int timeout) => _defaultTimeout = timeout;

        public void SetDefaultNavigationTimeout(int timeout) => _defaultNavigationTimeout = timeout;
    }
}
