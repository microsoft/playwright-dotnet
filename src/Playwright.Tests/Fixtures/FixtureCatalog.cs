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

using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// A static class with the <see cref="FixtureAttribute"/> methods that every test can use, as the fixtures
/// of <c>tests/config/*.ts</c> upstream. A provider with a higher order overrides the ones before it.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class FixtureProviderAttribute : Attribute
{
    public int Order { get; set; }
}

internal sealed class FixtureDefinition
{
    public string Name { get; init; }

    public FixtureScope Scope { get; init; }

    public bool Auto { get; init; }

    public MethodInfo Method { get; init; }

    /// <summary>The fixture of the same name that this one overrides.</summary>
    public FixtureDefinition Super { get; init; }

    /// <summary>The fixture comes from a test class, not from a <see cref="FixtureProviderAttribute"/>.</summary>
    public bool FromTestClass { get; init; }

    public override string ToString() => $"{Name} ({Method.DeclaringType?.Name}.{Method.Name})";
}

/// <summary>The fixtures, hooks and marks that apply to the tests of one class.</summary>
internal sealed class TestClassInfo
{
    private static readonly Lazy<IReadOnlyDictionary<string, FixtureDefinition>> _builtins = new(ReadBuiltins);
    private static readonly ConcurrentDictionary<Type, TestClassInfo> _cache = new();

    private TestClassInfo()
    {
    }

    public Type Type { get; private init; }

    public IReadOnlyDictionary<string, FixtureDefinition> Fixtures { get; private init; }

    public IReadOnlyList<FixtureDefinition> AutoFixtures { get; private init; }

    /// <summary>Outermost class first.</summary>
    public IReadOnlyList<MethodInfo> BeforeEach { get; private init; }

    /// <summary>Innermost class first.</summary>
    public IReadOnlyList<MethodInfo> AfterEach { get; private init; }

    public IReadOnlyList<MethodInfo> BeforeAll { get; private init; }

    public IReadOnlyList<MethodInfo> AfterAll { get; private init; }

    /// <summary>The class and the classes it is nested in, outermost first.</summary>
    public IReadOnlyList<Type> Chain { get; private init; }

    /// <summary>
    /// Names the worker fixtures that the class overrides. Tests with the same signature share a worker,
    /// as upstream starts another worker process for other worker fixtures.
    /// </summary>
    public string WorkerSignature { get; private init; }

    public static TestClassInfo For(Type type) => _cache.GetOrAdd(type, Read);

    private static TestClassInfo Read(Type type)
    {
        var chain = new List<Type>();
        for (var current = type; current != null; current = current.DeclaringType)
        {
            chain.Insert(0, current);
        }

        var fixtures = new Dictionary<string, FixtureDefinition>(_builtins.Value);
        var beforeEach = new List<MethodInfo>();
        var afterEach = new List<MethodInfo>();
        var beforeAll = new List<MethodInfo>();
        var afterAll = new List<MethodInfo>();
        foreach (var level in chain)
        {
            var levelAfterEach = new List<MethodInfo>();
            foreach (var declaring in BaseTypesFirst(level))
            {
                foreach (var method in DeclaredMethods(declaring))
                {
                    if (method.GetCustomAttribute<FixtureAttribute>() is { } fixture)
                    {
                        AddFixture(fixtures, method, fixture, fromTestClass: true);
                    }
                    if (method.GetCustomAttribute<BeforeEachAttribute>() != null)
                    {
                        beforeEach.Add(method);
                    }
                    if (method.GetCustomAttribute<AfterEachAttribute>() != null)
                    {
                        levelAfterEach.Add(method);
                    }
                    if (method.GetCustomAttribute<BeforeAllAttribute>() != null)
                    {
                        beforeAll.Add(RequireStatic(method, "[BeforeAll]"));
                    }
                    if (method.GetCustomAttribute<AfterAllAttribute>() != null)
                    {
                        afterAll.Add(RequireStatic(method, "[AfterAll]"));
                    }
                }
            }
            afterEach.InsertRange(0, levelAfterEach);
        }

        var workerOverrides = fixtures.Values
            .SelectMany(Overrides)
            .Where(definition => definition.FromTestClass && definition.Scope == FixtureScope.Worker)
            .Select(definition => $"{definition.Method.DeclaringType.FullName}.{definition.Method.Name}")
            .OrderBy(name => name, StringComparer.Ordinal);

        return new TestClassInfo
        {
            Type = type,
            Chain = chain,
            Fixtures = fixtures,
            AutoFixtures = fixtures.Values.Where(definition => definition.Auto).OrderBy(definition => definition.Method.MetadataToken).ToList(),
            BeforeEach = beforeEach,
            AfterEach = afterEach,
            BeforeAll = beforeAll,
            AfterAll = afterAll,
            WorkerSignature = string.Join(";", workerOverrides),
        };
    }

    private static IEnumerable<FixtureDefinition> Overrides(FixtureDefinition definition)
    {
        for (var current = definition; current != null; current = current.Super)
        {
            yield return current;
        }
    }

    private static MethodInfo RequireStatic(MethodInfo method, string what)
    {
        if (!method.IsStatic)
        {
            throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name}: a {what} method is static");
        }
        return method;
    }

    private static IEnumerable<Type> BaseTypesFirst(Type type)
    {
        var types = new List<Type>();
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            types.Insert(0, current);
        }
        return types;
    }

    private static IEnumerable<MethodInfo> DeclaredMethods(Type type) => type
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .OrderBy(method => method.MetadataToken);

    private static void AddFixture(Dictionary<string, FixtureDefinition> fixtures, MethodInfo method, FixtureAttribute attribute, bool fromTestClass)
    {
        var name = char.ToLowerInvariant(method.Name[0]) + method.Name.Substring(1);
        fixtures.TryGetValue(name, out var super);
        var scope = attribute.Scope;
        if (super != null && super.Scope != scope)
        {
            if (fromTestClass && scope == FixtureScope.Test && super.Scope == FixtureScope.Worker)
            {
                // An override keeps the scope of the fixture it overrides, as test.use() does.
                scope = FixtureScope.Worker;
            }
            else
            {
                throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name}: fixture \"{name}\" is a {super.Scope.ToString().ToLowerInvariant()} fixture, it cannot become a {scope.ToString().ToLowerInvariant()} fixture");
            }
        }
        if (scope == FixtureScope.Worker && !method.IsStatic)
        {
            throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name}: a worker fixture is a static method");
        }
        fixtures[name] = new FixtureDefinition
        {
            Name = name,
            Scope = scope,
            Auto = attribute.Auto || (super?.Auto ?? false),
            Method = method,
            Super = super,
            FromTestClass = fromTestClass,
        };
    }

    private static IReadOnlyDictionary<string, FixtureDefinition> ReadBuiltins()
    {
        var fixtures = new Dictionary<string, FixtureDefinition>();
        var providers = typeof(TestClassInfo).Assembly.GetTypes()
            .Select(type => (type, attribute: type.GetCustomAttribute<FixtureProviderAttribute>()))
            .Where(provider => provider.attribute != null)
            .OrderBy(provider => provider.attribute.Order)
            .ThenBy(provider => provider.type.FullName, StringComparer.Ordinal);
        foreach (var (type, _) in providers)
        {
            foreach (var method in DeclaredMethods(type))
            {
                if (method.GetCustomAttribute<FixtureAttribute>() is { } fixture)
                {
                    if (!method.IsStatic)
                    {
                        throw new InvalidOperationException($"{type.Name}.{method.Name}: a fixture of a [FixtureProvider] is a static method");
                    }
                    AddFixture(fixtures, method, fixture, fromTestClass: false);
                }
            }
        }
        return fixtures;
    }
}
