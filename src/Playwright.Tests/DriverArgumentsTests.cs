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

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Playwright.Tests;

public class DriverArgumentsTests
{
    // See https://github.com/microsoft/playwright-dotnet/issues/3430
    [TestCase("", "\"\"")]
    [TestCase("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [TestCase(@"C:\My Dir\", "\"C:\\My Dir\\\\\"")]
    [TestCase("plain", "\"plain\"")]
    [NonParallelizable]
    public void ShouldPreserveCLIArguments(string argument, string expected)
    {
        var previousNode = Environment.GetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH");
        var previousSearch = Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH");
        try
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", Environment.ProcessPath);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", Path.GetTempPath());
            var method = typeof(Program).GetMethod("CreateDriverProcess", BindingFlags.NonPublic | BindingFlags.Static);
            using var process = (Process)method.Invoke(null, new object[] { new[] { argument }, true });
            var entrypoint = Path.Combine(Path.GetTempPath(), ".playwright", "package", "cli.js");
            Assert.That(process.StartInfo.Arguments, Is.EqualTo($"\"{entrypoint}\" {expected}"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", previousNode);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", previousSearch);
        }
    }

    // See https://github.com/microsoft/playwright-dotnet/issues/3430
    [Test]
    public void ShouldQuoteEntrypointWithoutArguments()
    {
        var method = typeof(Microsoft.Playwright.Helpers.Driver).GetMethod("GetPath", BindingFlags.NonPublic | BindingFlags.Static);
        var path = Path.Combine(Path.GetTempPath(), "John Smith");
        var result = ((string ExecutablePath, Func<string, string> GetArgs))method.Invoke(null, new object[] { path });
        Assert.That(result.GetArgs(null), Is.EqualTo($"\"{Path.Combine(path, ".playwright", "package", "cli.js")}\""));
    }
}
