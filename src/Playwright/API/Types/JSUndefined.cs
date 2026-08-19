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

namespace Microsoft.Playwright;

/// <summary>
/// Sentinel representing the JavaScript <c>undefined</c> value.
/// </summary>
/// <remarks>
/// <para>
/// Use this as the expected value of <see cref="ILocatorAssertions.ToHaveJSPropertyAsync"/>
/// to assert that a JavaScript property is <c>undefined</c> rather than <c>null</c>.
/// C# <see langword="null"/> continues to mean JavaScript <c>null</c>.
/// </para>
/// <para>
/// This sentinel is only supported by <see cref="ILocatorAssertions.ToHaveJSPropertyAsync"/>.
/// Passing it to evaluate APIs does not produce JavaScript <c>undefined</c>.
/// </para>
/// </remarks>
public sealed class JSUndefined
{
    private JSUndefined()
    {
    }

    /// <summary>
    /// The JavaScript <c>undefined</c> value.
    /// </summary>
    public static JSUndefined Value { get; } = new();

    /// <inheritdoc />
    public override string ToString() => "undefined";
}
