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

namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies the syntax used in a string. This is a polyfill of the .NET 7+ attribute of the
/// same name, which is unavailable on netstandard2.0. Compilers and IDEs match the attribute by
/// its full name rather than by assembly identity, so an internal copy is enough for consumers
/// on any target framework.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class StringSyntaxAttribute : Attribute
{
    /// <summary>The syntax identifier for strings containing composite formats.</summary>
    public const string CompositeFormat = nameof(CompositeFormat);

    /// <summary>The syntax identifier for strings containing JSON.</summary>
    public const string Json = nameof(Json);

    /// <summary>The syntax identifier for strings containing regular expressions.</summary>
    public const string Regex = nameof(Regex);

    /// <summary>The syntax identifier for strings containing URIs.</summary>
    public const string Uri = nameof(Uri);

    /// <summary>The syntax identifier for strings containing XML.</summary>
    public const string Xml = nameof(Xml);

    /// <summary>Initializes a new instance of the <see cref="StringSyntaxAttribute"/> class.</summary>
    /// <param name="syntax">The syntax identifier.</param>
    public StringSyntaxAttribute(string syntax)
    {
        Syntax = syntax;
        Arguments = Array.Empty<object?>();
    }

    /// <summary>Initializes a new instance of the <see cref="StringSyntaxAttribute"/> class.</summary>
    /// <param name="syntax">The syntax identifier.</param>
    /// <param name="arguments">Optional arguments associated with the specific syntax employed.</param>
    public StringSyntaxAttribute(string syntax, params object?[] arguments)
    {
        Syntax = syntax;
        Arguments = arguments;
    }

    /// <summary>Gets the identifier of the syntax used.</summary>
    public string Syntax { get; }

    /// <summary>Gets the optional arguments associated with the specific syntax employed.</summary>
    public object?[] Arguments { get; }
}
