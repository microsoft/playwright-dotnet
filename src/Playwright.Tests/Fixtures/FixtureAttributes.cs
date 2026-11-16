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

public enum FixtureScope
{
    Test,
    Worker,
}

/// <summary>
/// Defines a fixture, as <c>test.extend({ name: async ({ dep }, use) => ... })</c> and <c>test.use({ name: value })</c>
/// do. The name of the fixture is the name of the method with a lower-case first letter. The parameters are
/// fixtures, by name; a parameter with the name of the fixture itself is the fixture that this one overrides.
/// The method returns the value (or a task of it), or takes a last parameter <c>Func&lt;T, Task&gt; use</c>
/// and awaits it where upstream awaits <c>use(value)</c>: what follows is the teardown.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class FixtureAttribute : Attribute
{
    public FixtureScope Scope { get; set; } = FixtureScope.Test;

    /// <summary>Set up for every test, also when no test parameter asks for it.</summary>
    public bool Auto { get; set; }
}

/// <summary><c>test.beforeEach(async ({ page }) => ...)</c>. The parameters are fixtures.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class BeforeEachAttribute : Attribute
{
}

/// <summary><c>test.afterEach(async ({ page }) => ...)</c>. The parameters are fixtures.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AfterEachAttribute : Attribute
{
}

/// <summary>
/// <c>test.beforeAll(async ({ browser }) => ...)</c>: runs once per worker, before the first test of the
/// class in that worker. The method is static and its parameters are worker fixtures.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class BeforeAllAttribute : Attribute
{
}

/// <summary><c>test.afterAll(...)</c>: runs when the worker that ran the <see cref="BeforeAllAttribute"/> hooks of the class closes.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AfterAllAttribute : Attribute
{
}

/// <summary><c>it.skip('title', ...)</c>, or an unconditional <c>test.skip()</c> at the top of a describe.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ItSkipAttribute : Attribute
{
    public ItSkipAttribute(string reason = null)
    {
        Reason = reason;
    }

    public string Reason { get; }
}

/// <summary><c>it.fixme('title', ...)</c>, or an unconditional <c>test.fixme()</c> at the top of a describe.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ItFixmeAttribute : Attribute
{
    public ItFixmeAttribute(string reason = null)
    {
        Reason = reason;
    }

    public string Reason { get; }
}

/// <summary><c>it.fail('title', ...)</c>: the test runs and must fail.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ItFailAttribute : Attribute
{
    public ItFailAttribute(string reason = null)
    {
        Reason = reason;
    }

    public string Reason { get; }
}

/// <summary><c>test.slow()</c> at the top of a file or a describe: three times the timeout.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ItSlowAttribute : Attribute
{
}

/// <summary>
/// The one mark that the port adds: a test that fails because of a bug or a gap that is not fixed yet.
/// The reason starts with <c>dotnet:</c> (the .NET client) or <c>harness:</c> (these fixtures).
/// The test still runs. Its failure is reported as skipped, with the reason, and it fails
/// when it starts to pass, so that the mark gets removed.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class FixmeAttribute : Attribute
{
    public FixmeAttribute(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }

    /// <summary>Limits the mark to some browsers: <c>"webkit"</c>, <c>"firefox,webkit"</c>.</summary>
    public string Browsers { get; set; }

    /// <summary>Limits the mark to some platforms: <c>"darwin"</c>, <c>"linux,win32"</c>.</summary>
    public string Platforms { get; set; }

    /// <summary>
    /// Limits the mark to the copies of a looped test or of a looped describe whose full name contains this
    /// text: <c>Reuse("launch")</c> of the class, <c>ShouldWork("a")</c> of the method.
    /// </summary>
    public string Case { get; set; }

    /// <summary>
    /// False for a test that fails nearly always and can pass by chance, for example on a value that the
    /// client rounds: the test does not fail when it passes. The reason says why it can pass.
    /// </summary>
    public bool Strict { get; set; } = true;
}
