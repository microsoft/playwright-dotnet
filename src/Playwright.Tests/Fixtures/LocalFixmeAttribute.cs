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

using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// The one mark that the port adds: a test that fails because of a bug or a gap that is not fixed yet.
/// The reason starts with <c>dotnet:</c> (the .NET client) or <c>harness:</c> (these fixtures).
/// The test does not run, in any browser: it is skipped, with the reason.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class LocalFixmeAttribute(string reason) : NUnitAttribute, IApplyToTest
{
    public string Reason { get; } = reason;

    public void ApplyToTest(Test test)
    {
        if (test.RunState != RunState.NotRunnable)
        {
            test.RunState = RunState.Ignored;
            test.Properties.Set(PropertyNames.SkipReason, Reason);
        }
    }
}
