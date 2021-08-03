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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.Playwright.Tests
{
    /// <summary>
    /// Enables decorating test facts with information about the corresponding test in the upstream repository.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PlaywrightTestAttribute : TestMethodAttribute
    {
        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        public PlaywrightTestAttribute()
        {

        }

        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        /// <param name="fileName"><see cref="FileName"/></param>
        /// <param name="nameOfTest"><see cref="TestName"/></param>
        public PlaywrightTestAttribute(string fileName, string nameOfTest)
        {
            FileName = fileName;
            TestName = nameOfTest;
        }

        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        /// <param name="fileName"><see cref="FileName"/></param>
        /// <param name="describe"><see cref="Describe"/></param>
        /// <param name="nameOfTest"><see cref="TestName"/></param>
        public PlaywrightTestAttribute(string fileName, string describe, string nameOfTest) : this(fileName, nameOfTest)
        {
            Describe = describe;
        }

        /// <summary>
        /// The file name origin of the test.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Returns the trimmed file name.
        /// </summary>
        public string TrimmedName => FileName.Substring(0, FileName.IndexOf('.'));

        /// <summary>
        /// The name of the test, the decorated code is based on.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// The describe of the test, the decorated code is based on, if one exists.
        /// </summary>
        public string Describe { get; set; }

        /// <summary>
        /// Determines the default amount of retries - to compensate for flaky tests.
        /// This can be overriden by setting a <see cref="FlakyAttribute"/>.
        /// </summary>
        public int DefaultRetries { get; set; } = 3;

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            var skipAttribute = testMethod.GetAttributes<SkipAttribute>(true).FirstOrDefault();
            if (skipAttribute?.ShouldSkipTest() ?? false)
            {
                return new[]
                {
                    new TestResult()
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestContextMessages = "Skipped due to browser/platform."
                    }
                };
            }

            var flakyAttribute = testMethod.GetAttributes<FlakyAttribute>(true).FirstOrDefault();

            int count = flakyAttribute?.MaximumRetries ?? DefaultRetries;
            int executionCount = 0;
            List<TestResult> testResults = new();
            Exception lastThrownException = null;

            while (count-- > 0)
            {
                executionCount++;
                var flakyTestResult = new TestResult()
                {
                    Outcome = UnitTestOutcome.Inconclusive
                };

                try
                {
                    var result = base.Execute(testMethod);

                    if (result.All(x =>
                         x.Outcome == UnitTestOutcome.Inconclusive
                         || x.Outcome == UnitTestOutcome.Passed))
                    {
                        testResults.AddRange(result);
                        break;
                    }

                    lastThrownException = result.Where(x => x.TestFailureException != null).LastOrDefault()?.TestFailureException;
                    flakyTestResult.LogError = string.Join(Environment.NewLine, result.Select(x => x.LogError));
                    flakyTestResult.TestFailureException = lastThrownException;
                    flakyTestResult.TestContextMessages = $"Test retries remaining: {count}";
                }
                catch (Exception ex)
                {
                    flakyTestResult.TestFailureException = ex;
                }

                testResults.Add(flakyTestResult);
            }

            if (testResults.All(x => x.Outcome == UnitTestOutcome.Inconclusive))
            {
                testResults.Add(new()
                {
                    Outcome = UnitTestOutcome.Failed,
                    TestFailureException = new AggregateException("All retries for this test have failed.", lastThrownException)
                });
            }

            return testResults.ToArray();
        }
    }
}
