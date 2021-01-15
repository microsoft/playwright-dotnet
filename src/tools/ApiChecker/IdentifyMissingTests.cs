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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace ApiChecker
{
#pragma warning disable SA1201 // Elements should appear in the correct order
    /// <summary>
    /// This will identify missing tests from upstream.
    /// </summary>
    internal static class IdentifyMissingTests
    {
        /// <summary>
        /// Describes the options for scaffolding the tests.
        /// </summary>
        [Verb("scaffold-test", HelpText = "Takes a spec.ts file and scaffolds the C# test.")]
        internal class IdentifyMissingTestsOptions
        {
            [Option(Required = true, HelpText = "Location of spec files.")]
            public string SpecFileLocations { get; set; }

            [Option(Required = true, HelpText = "The path to the DLL containing the PlaywrightSharp tests.", Default = "PlaywrightSharp.Tests.dll")]
            public string TestDLLPath { get; set; }

            [Option(Required = false, HelpText = "The search pattern to use for spec files.", Default = "*.spec.ts")]
            public string Pattern { get; set; }

            [Option(Required = false, Default = true, HelpText = "When True, looks inside subdirectories of specified location as well.")]
            public bool Recursive { get; set; }
        }

        /// <summary>
        /// Runs the scenario.
        /// </summary>
        /// <param name="options">The options argument.</param>
        public static void Run(IdentifyMissingTestsOptions options)
        {
            // get all files that match a pattern
            var directoryInfo = new DirectoryInfo(options.SpecFileLocations);
            if (!directoryInfo.Exists)
            {
                throw new ArgumentException($"The location ({directoryInfo.FullName}) specified does not exist.");
            }

            if (!File.Exists(options.TestDLLPath))
            {
                throw new ArgumentException($"The DLL ({options.TestDLLPath}) containing the Playwright Sharp tests, does not exist.");
            }

            // let's map the test cases from the spec files
            MapTestsCases(directoryInfo, options);


            // now, let's load the DLL and use some reflection-fu
            var assembly = Assembly.Load(options.TestDLLPath);
            foreach(var testType in assembly.GetTypes())
            {
                // check to see if it's actually a test type
            }
        }

        private static readonly List<(string FileName, string TestName)> _testPairs = new List<(string, string)>();

        private static void MapTestsCases(DirectoryInfo directoryInfo, IdentifyMissingTestsOptions options)
        {
            // get the sub-directories
            if (options.Recursive)
            {
                foreach (var subdirectory in directoryInfo.GetDirectories())
                {
                    MapTestsCases(subdirectory, options);
                }
            }

            foreach (var fileInfo in directoryInfo.GetFiles(options.Pattern))
            {
                ScaffoldTest.FindTestsInFile(fileInfo.FullName, (testName) =>
                {
                    _testPairs.Add(new(fileInfo.FullName, testName));
                });
            }
        }
    }
#pragma warning restore SA1201 // Elements should appear in the correct order
}
