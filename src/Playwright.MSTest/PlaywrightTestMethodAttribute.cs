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
using System.Linq;
using Microsoft.Playwright.TestAdapter;
using MSTestUnitTesting = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.MSTest
{
    public class PlaywrightTestMethodAttribute : MSTestUnitTesting.TestMethodAttribute
    {
        // First run is always 0, for each subsequent run, the run count is incremented by 1.
        internal static readonly IDictionary<string, int> Test2RetryCount = new Dictionary<string, int>();

        public override MSTestUnitTesting.TestResult[] Execute(MSTestUnitTesting.ITestMethod testMethod)
        {
            var key = testMethod.TestClassName + "." + testMethod.TestMethodName;
            MSTestUnitTesting.TestResult[] results = new MSTestUnitTesting.TestResult[] { };
            Test2RetryCount[key] = -1;
            while (Test2RetryCount[key] < PlaywrightSettingsProvider.Retries)
            {
                Test2RetryCount[key]++;
                results = base.Execute(testMethod);
                var allPassed = (results ?? new MSTestUnitTesting.TestResult[] { }).All(r => r.Outcome == MSTestUnitTesting.UnitTestOutcome.Passed);

                if (allPassed)
                {
                    break;
                }
            }
            if (Test2RetryCount[key] > 0)
            {
                if (!string.IsNullOrEmpty(results.Last().LogOutput))
                {
                    results.Last().LogError += "\n";
                }
                var retryCount = Test2RetryCount[key];
                results.Last().LogError += $"Test was retried {retryCount} time{(retryCount > 1 ? "s" : "")}."; ;
            }
            Test2RetryCount.Remove(key);
            return results.ToArray();
        }
    }
}
