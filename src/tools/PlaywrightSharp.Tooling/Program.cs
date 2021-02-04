/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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
using System.Threading.Tasks;
using CommandLine;
using PlaywrightSharp.Tooling.Options;

namespace PlaywrightSharp.Tooling
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ScaffoldTestOptions, IdentifyMissingTestsOptions, DownloadDriversOptions, ApiCheckerOptions>(args)
                .WithParsed<ScaffoldTestOptions>(o => ScaffoldTest.Run(o))
                .WithParsed<IdentifyMissingTestsOptions>(o => IdentifyMissingTests.Run(o))
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                .WithParsed<DownloadDriversOptions>(o => DriverDownloader.RunAsync(o).GetAwaiter().GetResult())
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                .WithParsed<ApiCheckerOptions>(o => ApiChecker.Run(o));
        }
    }
}
