using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class SocksSocketInitializer
    {
        public string DstAddr { get; set; }

        public int DstPort { get; set; }
    }
}
