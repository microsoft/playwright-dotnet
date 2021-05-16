using System.Collections.Generic;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Base class for <see cref="LaunchOptions"/> and <see cref="LaunchOptionsBase"/>.
    /// </summary>
    public abstract class LaunchOptionsBase
    {
        public bool? Headless { get; set; }

        public string[] Args { get; set; }

        public bool? Devtools { get; set; }

        public string ExecutablePath { get; set; }

        public string DownloadsPath { get; set; }

        public bool? IgnoreHTTPSErrors { get; set; }

        public int? Timeout { get; set; }

        public int? SlowMo { get; set; }

        public bool? IgnoreAllDefaultArgs { get; set; }

        public bool? HandleSIGINT { get; set; }

        public bool? HandleSIGTERM { get; set; }

        public bool? HandleSIGHUP { get; set; }

        public bool? ChromiumSandbox { get; set; }

        public string[] IgnoreDefaultArgs { get; set; }

        public Dictionary<string, string> Env { get; set; }

        public Proxy Proxy { get; set; }

        public string RecordHarPath { get; set; }

        public bool? RecordHarOmitContent { get; set; }

        public string RecordVideoDir { get; set; }

        public RecordVideoSize RecordVideoSize { get; set; }

        public BrowserChannel Channel { get; set; }
    }
}
