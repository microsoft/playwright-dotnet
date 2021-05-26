using System.Collections.Generic;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class SocksSocketInitializer
    {
        public string DstAddr { get; set; }

        public int DstPort { get; set; }
    }
}
