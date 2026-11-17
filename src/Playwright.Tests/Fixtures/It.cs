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

using NUnit.Framework.Internal;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// The calls that upstream makes on <c>it</c> or <c>test</c> inside a test or a hook:
/// <c>it.skip(condition, reason)</c> is <c>It.Skip(condition, reason)</c>.
/// </summary>
public static class It
{
    /// <summary><c>it.skip()</c>.</summary>
    public static void Skip() => Assert.Ignore("skip");

    /// <summary><c>it.skip(condition, description)</c>.</summary>
    public static void Skip(bool condition, string description = null)
    {
        if (condition)
        {
            Assert.Ignore(description ?? "skip");
        }
    }

    /// <summary><c>it.fixme()</c>: a skip that upstream has.</summary>
    public static void Fixme() => Assert.Ignore("fixme");

    /// <summary>
    /// <c>it.fixme(condition, description)</c>: a skip that upstream has. Also
    /// <c>it.fail(condition, description)</c>, which the port does not run.
    /// </summary>
    public static void Fixme(bool condition, string description = null)
    {
        if (condition)
        {
            Assert.Ignore(description == null ? "fixme" : "fixme: " + description);
        }
    }

    /// <summary><c>it.slow()</c>: three times the timeout.</summary>
    public static void Slow() => Info().Slow();

    /// <summary><c>it.slow(condition, description)</c>.</summary>
    public static void Slow(bool condition, string description = null)
    {
        if (condition)
        {
            Info().Slow();
        }
    }

    /// <summary><c>it.setTimeout(timeout)</c>.</summary>
    public static void SetTimeout(int timeout) => Info().SetTimeout(timeout);

    /// <summary><c>it.info()</c>.</summary>
    public static TestInfo Info() => (TestExecutionContext.CurrentContext.TestObject as UpstreamPlaywrightTest)?.TestInfo ?? throw new InvalidOperationException("It.Info() is called outside of a test");

    /// <summary><c>await it.step(title, async () => { ... })</c>.</summary>
    public static Task Step(string title, Func<Task> body) => body();

    /// <summary><c>const value = await it.step(title, async () => { ... })</c>.</summary>
    public static Task<T> Step<T>(string title, Func<Task<T>> body) => body();
}
