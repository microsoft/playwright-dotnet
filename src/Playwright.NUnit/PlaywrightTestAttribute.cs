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
using Microsoft.Playwright.TestAdapter;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnitFrameworkBase = NUnit.Framework;

namespace Microsoft.Playwright.NUnit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PlaywrightTestAttribute : NUnitFrameworkBase.NUnitAttribute, IWrapTestMethod
    {
        public PlaywrightTestAttribute() : base()
        {
        }

        public TestCommand Wrap(TestCommand command) => new RetryTestCommand(command);

        public class RetryTestCommand : DelegatingTestCommand
        {
            public RetryTestCommand(TestCommand innerCommand) : base(innerCommand)
            {
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                context.CurrentTest.Properties.Set("RetryCount", 0);
                while (true)
                {
                    try
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    }
                    catch (Exception ex)
                    {
                        context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentResult.RecordException(ex);
                    }
                    if (context.CurrentResult.ResultState == ResultState.Success)
                    {
                        break;
                    }
                    if (Test2RetryCount(context.CurrentTest) == PlaywrightSettingsProvider.Retries)
                    {
                        break;
                    }
                    context.CurrentTest.Properties.Set("RetryCount", Test2RetryCount(context.CurrentTest) + 1);
                }
                if (Test2RetryCount(context.CurrentTest) > 0)
                {
                    if (!string.IsNullOrEmpty(context.CurrentResult.Output))
                    {
                        context.CurrentResult.OutWriter.Write("\n");
                    }
                    var retryCount = Test2RetryCount(context.CurrentTest);
                    context.CurrentResult.OutWriter.Write($"Test was retried {retryCount} time{(retryCount > 1 ? "s" : "")}.");
                }
                return context.CurrentResult;
            }

            private static int Test2RetryCount(Test test)
            {
                return (int)test.Properties.Get("RetryCount")!;
            }
        }
    }
}

