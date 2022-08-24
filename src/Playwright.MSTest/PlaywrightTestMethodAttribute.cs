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
using System.Linq;
using Microsoft.Playwright.TestAdapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestUnitTesting = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.MSTest
{
    public class PlaywrightTestMethodAttribute : MSTestUnitTesting.TestMethodAttribute
    {
        internal static TestContext TestContext { get; set; } = null!;

        private static int RetryCount
        {
            get => (int)TestContext.Properties["RetryCount"];
            set => TestContext.Properties["RetryCount"] = value;
        }

        public override MSTestUnitTesting.TestResult[] Execute(MSTestUnitTesting.ITestMethod testMethod)
        {
            MSTestUnitTesting.TestResult[] currentResults = new MSTestUnitTesting.TestResult[] { };
            MSTestUnitTesting.TestResult[] failedResults = new MSTestUnitTesting.TestResult[] { };
            RetryCount = 0;
            while (true)
            {
                currentResults = base.Execute(testMethod);
                var allPassed = (currentResults ?? new MSTestUnitTesting.TestResult[] { }).All(r => r.Outcome == MSTestUnitTesting.UnitTestOutcome.Passed);

                if (allPassed)
                {
                    break;
                }
                if (currentResults != null)
                {
                    foreach (var result in currentResults)
                    {
                        result.DisplayName = $"{testMethod.TestClassName} (retry #{RetryCount})";
                    }
                }
                failedResults = failedResults.Concat(currentResults).ToArray();
                if (RetryCount == PlaywrightSettingsProvider.Retries)
                {
                    break;
                }
                RetryCount++;
            }
            if (RetryCount > 0)
            {
                if (!string.IsNullOrEmpty(currentResults.Last().LogOutput))
                {
                    currentResults.Last().LogOutput += "\n";
                }
                currentResults.Last().LogOutput += GenerateOutputLog(failedResults);
            }
            return currentResults.ToArray();
        }

        private static string GenerateOutputLog(MSTestUnitTesting.TestResult[] failedResults)
        {
            var logSeparator = new String('=', 80);
            string output = $"{logSeparator}\n";
            output += $"Test was retried {RetryCount} time{(RetryCount > 1 ? "s" : "")}.";

            if (failedResults.Length > 0)
            {
                output += $"\nFailing test runs:\n";
                foreach (var result in failedResults)
                {
                    output += new String('-', 40) + "\n";
                    output += $"  Test: {result.DisplayName}\n";
                    output += $"  Outcome: {result.Outcome}\n";
                    if (result.TestFailureException != null)
                    {
                        output += $"  Exception: \n{Indent(result.TestFailureException.ToString(), 4)}\n";
                    }
                    if (!string.IsNullOrEmpty(result.LogOutput))
                    {
                        output += $"  Standard Output Messages: \n{Indent(result.LogOutput.TrimEnd(), 4)}\n";
                    }
                    if (!string.IsNullOrEmpty(result.LogError))
                    {
                        output += $"  Standard Error Messages: \n{Indent(result.LogError.TrimEnd(), 4)}\n";
                    }
                }
            }

            output += $"{logSeparator}";
            return output;
        }

        private static string Indent(string text, int indent)
        {
            return text.Split(new[] { "\n" }, StringSplitOptions.None).Select(line => new String(' ', indent) + line).Aggregate((a, b) => a + "\n" + b);
        }
    }
}
