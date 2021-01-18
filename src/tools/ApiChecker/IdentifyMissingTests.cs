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
        [Verb("testtests", HelpText = "Checks if there are missing tests in the C# variant, compared to the specs.")]
        internal class IdentifyMissingTestsOptions
        {
            [Option(Required = true, HelpText = "Location of spec files.")]
            public string SpecFileLocations { get; set; }

            [Option(Required = false, HelpText = "The asembly containing the PlaywrightSharp tests.", Default = "PlaywrightSharp.Tests")]
            public string TestDLLName { get; set; }

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

            // let's map the test cases from the spec files
            MapTestsCases(directoryInfo, options);

            // now, let's load the DLL and use some reflection-fu
            var assembly = Assembly.Load(options.TestDLLName);
            string attributeTypeName = typeof(PlaywrightSharp.Tests.PlaywrightTestAttribute).Name;

            var attributes = assembly.DefinedTypes.SelectMany(
                type => type.GetMethods().SelectMany(
                    method => method.GetCustomAttributes<PlaywrightSharp.Tests.PlaywrightTestAttribute>()));

            var comparableList = attributes.Select(x => (x.FileName, x.TestName)).OrderBy(x => x.FileName).ToArray();

            int matchingTests = 0;
            int missingTests = 0;
            foreach (var atx in attributes)
            {
                if (!_testPairs.Contains((atx.TrimmedName, atx.TestName)))
                {
                    Console.WriteLine($"Missing: {atx.FileName}: {atx.TestName}");
                    missingTests++;
                }
                else
                {
                    matchingTests++;
                }
            }

            Console.WriteLine($"There are {matchingTests} matching tests, but we're missing {missingTests}. We have {_testPairs.Count} tests.");
        }

        private static readonly List<(string FileName, string TestName)> _testPairs = new();

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
                    _testPairs.Add(new(fileInfo.Name.Substring(0, fileInfo.Name.IndexOf('.')), testName));
                });
            }
        }
    }
#pragma warning restore SA1201 // Elements should appear in the correct order
}
