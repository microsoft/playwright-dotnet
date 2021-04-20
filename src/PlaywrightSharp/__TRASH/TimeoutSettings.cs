namespace PlaywrightSharp
{
    internal class TimeoutSettings
    {
        private int? _defaultTimeout;
        private int? _defaultNavigationTimeout;

        public TimeoutSettings(TimeoutSettings parent = null)
        {
            if (parent != null)
            {
                _defaultTimeout = parent.Timeout;
                _defaultTimeout = parent.NavigationTimeout;
            }
        }

        public int Timeout => _defaultTimeout ?? Playwright.DefaultTimeout;

        public int NavigationTimeout => _defaultNavigationTimeout ?? _defaultTimeout ?? Playwright.DefaultTimeout;

        public void SetDefaultTimeout(int timeout) => _defaultTimeout = timeout;

        public void SetDefaultNavigationTimeout(int timeout) => _defaultNavigationTimeout = timeout;
    }
}
