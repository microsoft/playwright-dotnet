using System;
using NUnitFrameworkBase = NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using Microsoft.Playwright.TestAdapter;

namespace Microsoft.Playwright.NUnit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PlaywrightTestAttribute : NUnitFrameworkBase.NUnitAttribute, IWrapTestMethod
    {
        public PlaywrightTestAttribute() : base() {
            
        }

        public TestCommand Wrap(TestCommand command)
            => new RetryTestCommand(command);

        public class RetryTestCommand : DelegatingTestCommand
        {
            public RetryTestCommand(TestCommand innerCommand)
                : base(innerCommand)
            {
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                Console.WriteLine("Execute" + PlaywrightSettingsProvider.Retries);
                string key = Test.Id;
                while (!TestHarnessStorage.IsLastRun(key))
                {
                    try
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    }
                    catch (System.Exception ex)
                    {
                        context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentResult.RecordException(ex);
                    }
                    TestHarnessStorage.IncrementRunCount(key);

                    if (context.CurrentResult.ResultState == ResultState.Success)
                        break;
                    if (context.CurrentResult.ResultState == ResultState.Failure && TestHarnessStorage.IsLastRun(key))
                        break;
                }
                TestHarnessStorage.ResetRunCount(key);
                return context.CurrentResult;
            }
        }
    }
}

