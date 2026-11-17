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

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary><c>it.skip('title', ...)</c>, or an unconditional <c>test.skip()</c> at the top of a describe.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ItSkipAttribute(string reason = null) : Attribute
{
    public string Reason { get; } = reason;
}

/// <summary>
/// <c>it.fixme('title', ...)</c>, or an unconditional <c>test.fixme()</c> at the top of a describe.
/// Also <c>it.fail('title', ...)</c>, which the port does not run.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ItFixmeAttribute(string reason = null) : Attribute
{
    public string Reason { get; } = reason;
}

/// <summary><c>test.slow()</c> at the top of a file or a describe: three times the timeout.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ItSlowAttribute : Attribute
{
}

/// <summary>
/// The one mark that the port adds: a test that fails because of a bug or a gap that is not fixed yet.
/// The reason starts with <c>dotnet:</c> (the .NET client) or <c>harness:</c> (these fixtures).
/// The test is skipped, with the reason, as a test with <see cref="ItFixmeAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class FixmeAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;

    /// <summary>Limits the mark to some browsers: <c>"webkit"</c>, <c>"firefox,webkit"</c>.</summary>
    public string Browsers { get; set; }

    /// <summary>Limits the mark to some platforms: <c>"darwin"</c>, <c>"linux,win32"</c>.</summary>
    public string Platforms { get; set; }

    /// <summary>
    /// Limits the mark to the copies of a looped test or of a looped describe whose full name contains this
    /// text: <c>Reuse("launch")</c> of the class, <c>ShouldWork("a")</c> of the method.
    /// </summary>
    public string Case { get; set; }

    internal bool AppliesTo(string testFullName)
    {
        if (Browsers != null && !Split(Browsers).Contains(Env.BrowserName))
        {
            return false;
        }
        if (Platforms != null && !Split(Platforms).Contains(Env.Platform))
        {
            return false;
        }
        return Case == null || testFullName.Contains(Case, StringComparison.Ordinal);
    }

    private static string[] Split(string list) => list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
