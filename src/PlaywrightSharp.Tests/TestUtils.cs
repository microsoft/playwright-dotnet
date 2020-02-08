using System;
using Xunit;

namespace PlaywrightSharp.Tests
{
    internal class TestUtils
    {
        internal static void AssertSSLError(string errorMessage)
        {
            if (TestConstants.IsChromium)
            {
                Assert.Contains("net::ERR_CERT_AUTHORITY_INVALID", errorMessage);
            }
            else if (TestConstants.IsWebKit)
            {
                if (TestConstants.IsMacOSX)
                    Assert.Contains("The certificate for this server is invalid", errorMessage);
                else if (TestConstants.IsWindows)
                    Assert.Contains("SSL peer certificate or SSH remote key was not OK", errorMessage);
                else
                    Assert.Contains("Unacceptable TLS certificate", errorMessage);
            }
            else
            {
                Assert.Contains("SSL_ERROR_UNKNOWN", errorMessage);
            }
        }
    }
}
