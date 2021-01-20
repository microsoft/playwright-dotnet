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
using System.Threading.Tasks;
using CommandLine;

namespace ApiChecker
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ScaffoldTest.ScaffoldTestOptions, CheckerOptions, IdentifyMissingTests.IdentifyMissingTestsOptions>(args)
                .WithParsed<ScaffoldTest.ScaffoldTestOptions>(o => ScaffoldTest.Run(o))
                .WithParsed<IdentifyMissingTests.IdentifyMissingTestsOptions>(o => IdentifyMissingTests.Run(o))
                .WithParsed<CheckerOptions>(o => RunApiChecker(o));
        }

        private static void RunApiChecker(CheckerOptions o)
        {
            var checker = new PlaywrightSharp.BuildTasks.ApiChecker
            {
                BasePath = o.BasePath,
                AssemblyPath = o.AssemblyPath,
                IsBuildTask = false,
            };
            checker.Execute();
        }
    }
}
