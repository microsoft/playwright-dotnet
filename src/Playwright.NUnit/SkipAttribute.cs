/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
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
using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Microsoft.Playwright.NUnit
{
    public class SkipAttribute : NUnitAttribute, IWrapTestMethod
    {
        private readonly Targets[] _combinations;

        [Flags]
        public enum Targets : short
        {
            Windows = 1 << 0,
            Linux = 1 << 1,
            OSX = 1 << 2,
            Chromium = 1 << 3,
            Firefox = 1 << 4,
            Webkit = 1 << 5
        }

        /// <summary>
        /// Skips the combinations provided.
        /// </summary>
        /// <param name="pairs"></param>
        public SkipAttribute(params Targets[] combinations)
        {
            _combinations = combinations;
        }

        public TestCommand Wrap(TestCommand command)
        {
            return new IgnorableCommandWrapper(this, command);
        }

        private class IgnorableCommandWrapper : TestCommand
        {
            private readonly SkipAttribute _attParent;
            private readonly TestCommand _cmdParent;

            public IgnorableCommandWrapper(SkipAttribute parent, TestCommand cmdParent) : base(cmdParent.Test)
            {
                _attParent = parent;
                _cmdParent = cmdParent;
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                if (context.TestObject is not PlaywrightTest pwTest)
                {
                    Assert.Fail("Skip attribute cannot be used without a PlaywrightTest base class.");
                    return null;
                }

                var browserName = pwTest.BrowserName;
                if (_attParent._combinations.Any(combination =>
                {
                    var requirements = (Enum.GetValues(typeof(Targets)) as Targets[]).Where(x => combination.HasFlag(x));
                    return requirements.All(flag =>
                        flag switch
                        {
                            Targets.Windows => RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows),
                            Targets.Linux => RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux),
                            Targets.OSX => RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX),
                            Targets.Chromium => browserName == BrowserType.Chromium,
                            Targets.Firefox => browserName == BrowserType.Firefox,
                            Targets.Webkit => browserName == BrowserType.Webkit,
                            _ => false,
                        });
                }))
                {
                    Assert.Ignore("Skipped by browser/platform");
                    return null;
                }

                return _cmdParent.Execute(context);
            }
        }
    }
}
