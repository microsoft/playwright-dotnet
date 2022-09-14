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
using System.Runtime.CompilerServices;
using Microsoft.Playwright.TestAdapter;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnitFrameworkBase = NUnit.Framework;

[assembly: InternalsVisibleToAttribute("Microsoft.Playwright.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleToAttribute("Playwright.TestingHarnessTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]

namespace Microsoft.Playwright.NUnit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal class PlaywrightTestAttribute : NUnitFrameworkBase.TestAttribute, IWrapTestMethod
{
    public PlaywrightTestAttribute() : base()
    {
    }

    public TestCommand Wrap(TestCommand command) => new RetryTestCommand(command);

    internal class RetryTestCommand : DelegatingTestCommand
    {
        public RetryTestCommand(TestCommand innerCommand) : base(innerCommand)
        {
        }

        public override TestResult Execute(TestExecutionContext context)
        {
            List<SlimTestResult> failedResults = new List<SlimTestResult>();
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
                failedResults.Add(new SlimTestResult(context.CurrentResult));
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
                context.CurrentResult.OutWriter.Write(GenerateOutputLog(failedResults, retryCount));
            }
            return context.CurrentResult;
        }

        private static int Test2RetryCount(Test test)
            => (int)test.Properties.Get("RetryCount")!;

        private static string GenerateOutputLog(List<SlimTestResult> failedResults, int retryCount)
        {
            var logSeparator = new String('=', 80);
            string output = $"{logSeparator}\n";
            output += $"Test was retried {retryCount} time{(retryCount > 1 ? "s" : "")}.\n";

            if (failedResults.Count > 0)
            {
                output += $"\nFailing test runs:\n";
                foreach (var result in failedResults)
                {
                    if (result == null)
                    {
                        continue;
                    }
                    output += new String('-', 40) + "\n";
                    output += $"  Test: {result.FullName}\n";
                    output += $"  Outcome: {result.ResultState}\n";
                    if (!string.IsNullOrEmpty(result.Output))
                    {
                        output += $"  Standard Output Messages: \n{Indent(result.Output.TrimEnd(), 4)}\n";
                    }
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        output += $"  Message \n{Indent(result.Message!.TrimEnd(), 4)}\n";
                    }
                }
            }

            output += logSeparator;
            return output;
        }

        private static string Indent(string text, int indent)
        {
            return text.Split(new[] { "\n" }, StringSplitOptions.None).Select(line => new String(' ', indent) + line).Aggregate((a, b) => a + "\n" + b);
        }
    }

    private class SlimTestResult
    {
        public string FullName { get; }
        public string Output { get; }
        public ResultState ResultState { get; }
        public string? Message { get; }

        internal SlimTestResult(TestResult result)
        {
            FullName = result.FullName;
            Output = result.Output;
            ResultState = result.ResultState;
            Message = result.Message;
        }
    }
}
