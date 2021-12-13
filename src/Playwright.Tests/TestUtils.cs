/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    internal class TestUtils
    {
        internal static string FindParentDirectory(string directory)
        {
            // when using Directory.GetCurrentDirectory, for .NET 4.8 under test explorer on Windows,
            // the location was Local App Data
            string current = AppContext.BaseDirectory;
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
                StringAssert.Contains("net::ERR_CERT_AUTHORITY_INVALID", errorMessage);
            }
            else if (TestConstants.IsWebKit)
            {
                if (TestConstants.IsMacOSX)
                    StringAssert.Contains("The certificate for this server is invalid", errorMessage);
                else if (TestConstants.IsWindows)
                    StringAssert.Contains("SSL peer certificate or SSH remote key was not OK", errorMessage);
                else
                    StringAssert.Contains("Unacceptable TLS certificate", errorMessage);
            }
            else
            {
                StringAssert.Contains("SSL_ERROR_UNKNOWN", errorMessage);
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

        internal static async Task RegisterEngineWithPathAsync(IPlaywright playwright, string name, string path)
        {
            try
            {
                await playwright.Selectors.RegisterAsync(name, new() { Path = path });
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("has been already registered"))
            {
            }
        }

        internal static string GetWebServerFile(string path) => Path.Combine(FindParentDirectory("Playwright.Tests.TestServer"), "assets", path);

        internal static async Task VerifyViewportAsync(IPage page, int width, int height)
        {
            Assert.AreEqual(width, (int)page.ViewportSize.Width);
            Assert.AreEqual(height, (int)page.ViewportSize.Height);
            Assert.AreEqual(width, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.AreEqual(height, await page.EvaluateAsync<int>("window.innerHeight"));
        }

        internal static async Task RegisterEngineAsync(IPlaywright playwright, string name, string script, bool? contentScript = null)
        {
            try
            {
                await playwright.Selectors.RegisterAsync(name, new() { Script = script, ContentScript = contentScript });
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("has been already registered"))
            {
            }
        }
    }
}
