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

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// Marks the port of one upstream test, <c>it('CH-001 should check the box @smoke', async ({ page }) => ...)</c>.
/// The title is the upstream title, with the identifier and the tags. The parameters of the method are
/// fixtures, resolved by name as upstream destructures them: <c>(IPage page, Server server)</c>.
/// See <c>src/Playwright.Tests/AGENTS.md</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class UpstreamTestAttribute : NUnitAttribute, ISimpleTestBuilder, IImplyFixture, IApplyToTest
{
    private static readonly Regex _idRegex = new(@"^([A-Z]{2}-(?:[A-Z]{2,3}-)?\d{3})(?:\s|$)", RegexOptions.Compiled);

    private static int _caseOrder;

    public UpstreamTestAttribute(string title)
    {
        Title = title;
    }

    /// <summary>The upstream title, as written in the <c>it()</c> call.</summary>
    public string Title { get; }

    /// <summary>The registry identifier, the first token of the title.</summary>
    public string Id => _idRegex.Match(Title) is { Success: true } match ? match.Groups[1].Value : null;

    public TestMethod BuildFrom(IMethodInfo method, Test suite) => Build(method, suite, this, null, null);

    /// <summary>The tests of a class run in the order of the source, which is the upstream order.</summary>
    public void ApplyToTest(Test test)
    {
        if (test.Method != null && !test.Properties.ContainsKey(PropertyNames.Order))
        {
            test.Properties.Set(PropertyNames.Order, test.Method.MethodInfo.MetadataToken & 0xFFFFFF);
        }
    }

    internal static UpstreamTestAttribute Of(MethodInfo method) => method.GetCustomAttribute<UpstreamTestAttribute>(false);

    internal static TestMethod Build(IMethodInfo method, Test suite, UpstreamTestAttribute attribute, object[] caseArguments, string caseName)
    {
        var testMethod = new NUnitTestCaseBuilder().BuildTestMethod(new FixtureMethodInfo(method, caseArguments), suite, null);
        if (caseName != null)
        {
            // The copies of a looped test run in the order of the loop.
            testMethod.Properties.Set(PropertyNames.Order, Interlocked.Increment(ref _caseOrder));
            var prefix = testMethod.FullName.Substring(0, testMethod.FullName.Length - testMethod.Name.Length);
            testMethod.Name = $"{testMethod.Name}({caseName})";
            testMethod.FullName = prefix + testMethod.Name;
        }
        if (attribute == null)
        {
            testMethod.MakeInvalid($"[Case] and [CaseSource] need [UpstreamTest] on the same method");
            return testMethod;
        }
        if (attribute.Id == null)
        {
            testMethod.MakeInvalid($"The title \"{attribute.Title}\" does not start with a registry identifier, for example \"CH-001 \"");
            return testMethod;
        }
        if (method.MethodInfo.ReturnType != typeof(Task))
        {
            testMethod.MakeInvalid("A ported test is 'public async Task'");
            return testMethod;
        }
        var fixtureParameters = method.MethodInfo.GetParameters().Length - (caseArguments?.Length ?? 0);
        if (fixtureParameters < 0)
        {
            testMethod.MakeInvalid($"The case has {caseArguments.Length} values, the method has {method.MethodInfo.GetParameters().Length} parameters");
            return testMethod;
        }
        testMethod.Properties.Set(PropertyNames.Description, attribute.Title);
        testMethod.Properties.Add(PropertyNames.Category, attribute.Id);
        foreach (Match tag in Regex.Matches(attribute.Title, @"(?<=\s)@[\w-]+"))
        {
            testMethod.Properties.Add(PropertyNames.Category, tag.Value);
        }
        return testMethod;
    }
}

/// <summary>
/// One copy of a test that upstream declares in a loop, <c>for (const kind of kinds) it(`... ${kind}`, ...)</c>.
/// The values go to the first parameters of the method, in order; the fixtures come after them.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class CaseAttribute : NUnitAttribute, ITestBuilder, IImplyFixture
{
    public CaseAttribute(params object[] arguments)
    {
        Arguments = arguments ?? [null];
    }

    public object[] Arguments { get; }

    public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test suite)
    {
        yield return UpstreamTestAttribute.Build(method, suite, UpstreamTestAttribute.Of(method.MethodInfo), Arguments, CaseSourceAttribute.Display(Arguments));
    }
}

/// <summary>
/// The copies of a test that upstream declares in a loop, from a static field, property or method of the
/// class that holds the loop values. An item is the value of the first parameter, or an <c>object[]</c>
/// with the values of the first parameters.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class CaseSourceAttribute : NUnitAttribute, ITestBuilder, IImplyFixture
{
    public CaseSourceAttribute(string sourceName)
    {
        SourceName = sourceName;
    }

    public string SourceName { get; }

    public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test suite)
    {
        var attribute = UpstreamTestAttribute.Of(method.MethodInfo);
        IEnumerable items;
        try
        {
            items = ReadSource(method.MethodInfo.DeclaringType);
        }
        catch (Exception e)
        {
            var invalid = UpstreamTestAttribute.Build(method, suite, attribute, null, null);
            invalid.MakeInvalid($"[CaseSource(\"{SourceName}\")]: {(e is TargetInvocationException ? e.InnerException : e).Message}");
            return [invalid];
        }
        var tests = new List<TestMethod>();
        foreach (var item in items)
        {
            var arguments = item as object[] ?? [item];
            tests.Add(UpstreamTestAttribute.Build(method, suite, attribute, arguments, Display(arguments)));
        }
        return tests;
    }

    private IEnumerable ReadSource(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        for (var current = type; current != null; current = current.DeclaringType)
        {
            if (current.GetField(SourceName, flags) is { } field)
            {
                return (IEnumerable)field.GetValue(null);
            }
            if (current.GetProperty(SourceName, flags) is { } property)
            {
                return (IEnumerable)property.GetValue(null);
            }
            if (current.GetMethod(SourceName, flags, null, Type.EmptyTypes, null) is { } method)
            {
                return (IEnumerable)method.Invoke(null, null);
            }
        }
        throw new InvalidOperationException($"{type.Name} has no static member \"{SourceName}\"");
    }

    internal static string Display(object[] arguments) => string.Join(", ", arguments.Select(DisplayOne));

    private static string DisplayOne(object value) => value switch
    {
        null => "null",
        string s => "\"" + s + "\"",
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        Delegate => "function",
        IEnumerable e => "[" + string.Join(", ", e.Cast<object>().Select(DisplayOne)) + "]",
        _ => value.ToString(),
    };
}

/// <summary>
/// What NUnit sees of a ported test: a method without parameters that returns a task. The task resolves
/// the fixtures, runs the hooks and the test, and tears the fixtures down, see <see cref="TestRunner"/>.
/// </summary>
internal sealed class FixtureMethodInfo : IMethodInfo
{
    private readonly IMethodInfo _method;
    private readonly object[] _caseArguments;

    public FixtureMethodInfo(IMethodInfo method, object[] caseArguments)
    {
        _method = method;
        _caseArguments = caseArguments ?? [];
    }

    public ITypeInfo TypeInfo => _method.TypeInfo;

    public MethodInfo MethodInfo => _method.MethodInfo;

    public string Name => _method.Name;

    public bool IsAbstract => _method.IsAbstract;

    public bool IsPublic => _method.IsPublic;

    public bool IsStatic => _method.IsStatic;

    public bool ContainsGenericParameters => false;

    public bool IsGenericMethod => false;

    public bool IsGenericMethodDefinition => false;

    public ITypeInfo ReturnType => _method.ReturnType;

    public IParameterInfo[] GetParameters() => [];

    public Type[] GetGenericArguments() => [];

    public IMethodInfo MakeGenericMethod(params Type[] typeArguments) => throw new NotSupportedException();

    public T[] GetCustomAttributes<T>(bool inherit)
        where T : class => _method.GetCustomAttributes<T>(inherit);

    public bool IsDefined<T>(bool inherit)
        where T : class => _method.IsDefined<T>(inherit);

    public object Invoke(object fixture, params object[] args) => TestRunner.RunAsync(fixture, _method.MethodInfo, _caseArguments);
}
