using System.IO;
using System.Text;
using Xunit;

namespace PlaywrightSharp.Tests
{
    internal class TestUtils
    {
        internal static string FindParentDirectory(string directory)
        {
            string current = Directory.GetCurrentDirectory();
            while (!Directory.Exists(Path.Combine(current, directory)))
            {
                current = Directory.GetParent(current).FullName;
            }
            return Path.Combine(current, directory);
        }

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

        /// <summary>
        /// Removes as much whitespace as possible from a given string. Whitespace
        /// that separates letters and/or digits is collapsed to a space character.
        /// Other whitespace is fully removed.
        /// </summary>
        public static string CompressText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var sb = new StringBuilder();
            bool inWhitespace = false;
            foreach (char ch in text)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (ch != '\n' && ch != '\r')
                    {
                        inWhitespace = true;
                    }
                }
                else
                {
                    if (inWhitespace)
                    {
                        inWhitespace = false;
                        if (sb.Length > 0 && char.IsLetterOrDigit(sb[sb.Length - 1]) && char.IsLetterOrDigit(ch))
                        {
                            sb.Append(' ');
                        }
                    }
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        internal static string GetWebServerFile(string path) => Path.Combine(FindParentDirectory("PlaywrightSharp.TestServer"), "wwwroot", path);
    }
}
