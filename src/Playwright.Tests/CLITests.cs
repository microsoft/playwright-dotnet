/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    public class CLITests : PlaywrightTest
    {
        readonly TextWriter _originalStdOut = Console.Out;
        readonly TextWriter _originalStdError = Console.Error;
        StringWriter _cliStdOut;
        StringWriter _cliStdError;

        [SetUp]
        public void SetUpCLITests()
        {
            _cliStdOut = new();
            _cliStdError = new();
            Console.SetOut(_cliStdOut);
            Console.SetError(_cliStdError);
        }

        [TearDown]
        public void TearDownCLITests()
        {
            Console.SetOut(_originalStdOut);
            Console.SetError(_originalStdError);
        }

        [PlaywrightTest("cli.spec.ts", "")]
        public void ShouldBeAbleToRunCLICommands()
        {
            using var tempDir = new TempDirectory();
            string screenshotFile = Path.Combine(tempDir.Path, "screenshot.png");
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "screenshot", "-b", BrowserName, "data:text/html,Foobar", screenshotFile });

            Assert.AreEqual(0, exitCode);
            Assert.IsTrue(File.Exists(screenshotFile));
            StringAssert.Contains("Foobar", _cliStdOut.ToString());
            StringAssert.Contains(screenshotFile, _cliStdOut.ToString());
        }

        [PlaywrightTest("cli.spec.ts", "")]
        public void ShouldReturnExitCode1ForCommandNotFound()
        {
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "this-command-is-not-found" });

            Assert.AreEqual(1, exitCode);
            StringAssert.Contains("this-command-is-not-found", _cliStdError.ToString());
            StringAssert.Contains("unknown command", _cliStdError.ToString());
        }
    }
}
