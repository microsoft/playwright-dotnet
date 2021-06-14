using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class AndroidDeviceInitializer
    {
        public string Model { get; set; }

        public string Serial { get; set; }
    }
}
