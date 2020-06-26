namespace PlaywrightSharp.Server
{
    internal class BrowserOptions
    {
        public int SlowMo { get; set; }

        public BrowserContextOptions Persistent { get; set; }

        public bool Headful { get; set; }

        public string DownloadsPath { get; set; }

        public IBrowserServer OwnedServer { get; set; }

        public ProxySettings Proxy { get; set; }

        internal TestHookDelegate TestHook { get; set; }
    }
}
