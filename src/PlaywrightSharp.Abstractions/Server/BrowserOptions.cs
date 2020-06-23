namespace PlaywrightSharp.Server
{
    internal class BrowserOptions
    {
        public int SlowMo { get; set; }

        public PersistentContextOptions Persistent { get; set; }

        public bool Headful { get; set; }

        public string DownloadsPath { get; set; }

        public BrowserServer OwnedServer { get; set; }

        public ProxySettings Proxy { get; set; }

        internal TestHookBeforeCreateBrowserDelegate TestHookBeforeCreateBrowser { get; set; }
    }
}
