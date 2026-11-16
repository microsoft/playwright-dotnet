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

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// Checks the ported tests against the upstream test registry, <c>.upstream/tests/registry.md</c>.
/// Every test of the registry is either ported, as an <see cref="UpstreamTestAttribute"/> method of the
/// expected class and name, or listed in <c>src/Playwright.Tests/upstream_not_ported.txt</c> with the reason.
///
/// <c>PWTEST_REGISTRY_FILES=page/page-check,library/har*</c> limits the check for missing tests to some
/// upstream files. <c>PWTEST_REGISTRY_REPORT=1</c> prints the coverage per subsystem.
/// </summary>
public class UpstreamRegistryTests
{
    private static readonly string _notPortedPath = Path.Combine(Env.RepoRoot, "src", "Playwright.Tests", "upstream_not_ported.txt");

    [Test]
    public void EveryUpstreamTestIsPortedOrListedAsNotPorted()
    {
        var problems = new List<string>();
        var registry = UpstreamRegistry.Tests;
        if (registry.Count == 0)
        {
            Assert.Fail($"{UpstreamRegistry.Path} is missing or empty. Run ./build.sh --build-driver");
        }

        var rules = ReadRules(problems);
        var notPorted = new Dictionary<string, Rule>();
        foreach (var test in registry.Values)
        {
            foreach (var rule in rules)
            {
                if (rule.Matches(test))
                {
                    notPorted.TryAdd(test.Id, rule);
                    rule.Used = true;
                }
            }
        }
        foreach (var rule in rules.Where(rule => !rule.Used))
        {
            problems.Add($"upstream_not_ported.txt:{rule.Line}: {rule.Selector} matches no registry test");
        }

        var ported = new Dictionary<string, List<MethodInfo>>();
        foreach (var method in typeof(UpstreamRegistryTests).Assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)))
        {
            var attribute = method.GetCustomAttribute<UpstreamTestAttribute>(false);
            if (attribute == null)
            {
                continue;
            }
            var where = $"{TypePath(method.DeclaringType)}.{method.Name}";
            if (attribute.Id == null)
            {
                problems.Add($"{where}: the title \"{attribute.Title}\" does not start with a registry identifier");
                continue;
            }
            if (!ported.TryGetValue(attribute.Id, out var methods))
            {
                ported[attribute.Id] = methods = [];
            }
            methods.Add(method);
            if (!registry.TryGetValue(attribute.Id, out var test))
            {
                problems.Add($"{where}: {attribute.Id} is not in the registry");
                continue;
            }
            CheckPort(method, attribute, test, where, problems);
            if (notPorted.TryGetValue(attribute.Id, out var rule))
            {
                problems.Add($"{where}: {attribute.Id} is ported, and upstream_not_ported.txt:{rule.Line} lists it as not ported");
            }
        }
        foreach (var (id, methods) in ported.Where(entry => entry.Value.Count > 1))
        {
            problems.Add($"{id} is ported {methods.Count} times: {string.Join(", ", methods.Select(method => $"{TypePath(method.DeclaringType)}.{method.Name}"))}");
        }

        var filters = (Environment.GetEnvironmentVariable("PWTEST_REGISTRY_FILES") ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var missing = registry.Values
            .Where(test => !ported.ContainsKey(test.Id) && !notPorted.ContainsKey(test.Id))
            .Where(test => filters.Length == 0 || filters.Any(filter => test.Code == filter || Glob(filter).IsMatch(test.FileWithoutExtension)))
            .OrderBy(test => test.File, StringComparer.Ordinal).ThenBy(test => test.Id, StringComparer.Ordinal)
            .ToList();
        foreach (var test in missing)
        {
            problems.Add($"{test.Id} is not ported and not in upstream_not_ported.txt: {test.File}  {UpstreamRegistry.SourcePath(test.File)}  {string.Join(".", test.Describes.Select(DescribeName).Append(UpstreamRegistry.PascalCase(test.Title)))}");
        }

        if (Environment.GetEnvironmentVariable("PWTEST_REGISTRY_REPORT") == "1")
        {
            TestContext.Out.WriteLine(Report(registry.Values, ported.Keys.ToHashSet(), notPorted.Keys.ToHashSet()));
        }
        if (problems.Count > 0)
        {
            Assert.Fail($"{problems.Count} problems:\n" + string.Join("\n", problems));
        }
    }

    private static void CheckPort(MethodInfo method, UpstreamTestAttribute attribute, UpstreamTest test, string where, List<string> problems)
    {
        // The class of the file, and the nested classes of the describes.
        var types = new List<Type>();
        for (var type = method.DeclaringType; type != null; type = type.DeclaringType)
        {
            types.Insert(0, type);
        }
        var expectedClass = UpstreamRegistry.ClassFullName(test.File);
        var expectedPath = expectedClass + string.Concat(test.Describes.Select(describe => "+" + DescribeName(describe)));
        // A nested class named Describe, Describe2, ... that stands for no title is a describe without a title.
        var classMatches = types[0].FullName == expectedClass;
        var describe = 0;
        for (var i = 1; classMatches && i < types.Count; i++)
        {
            var expected = describe < test.Describes.Count ? DescribeName(test.Describes[describe]) : null;
            if (expected != null && (types[i].Name == expected || types[i].Name == expected + "Describe"))
            {
                describe++;
            }
            else if (!Regex.IsMatch(types[i].Name, @"^Describe\d*$"))
            {
                classMatches = false;
            }
        }
        classMatches = classMatches && describe == test.Describes.Count;
        if (!classMatches)
        {
            problems.Add($"{where}: {attribute.Id} is a test of {test.File}{(test.Describes.Count > 0 ? " in describe " + string.Join(" › ", test.Describes) : string.Empty)}, its class is {expectedPath} in src/Playwright.Tests/{UpstreamRegistry.SourcePath(test.File)}");
        }

        var title = attribute.Title.Substring(attribute.Id.Length).TrimStart();
        if (test.Copies == 1 && title != test.Title)
        {
            problems.Add($"{where}: the title of {attribute.Id} is \"{attribute.Id} {test.Title}\"");
        }
        var expectedName = UpstreamRegistry.PascalCase(test.Copies == 1 ? test.Title : title);
        if (method.Name != expectedName && method.Name != $"{expectedName}_{attribute.Id.Replace("-", string.Empty)}")
        {
            problems.Add($"{where}: the method of {attribute.Id} is named {expectedName}, or {expectedName}_{attribute.Id.Replace("-", string.Empty)} when the class has two tests with that name");
        }
    }

    private static string DescribeName(string describe)
    {
        var name = UpstreamRegistry.PascalCase(describe);
        return name == "Test" && !Regex.IsMatch(describe, "^[Tt]est$") ? "Describe" : name;
    }

    private static string TypePath(Type type) => type.FullName.Replace("Microsoft.Playwright.Tests.", string.Empty);

    private static Regex Glob(string glob) => new("^" + Regex.Escape(glob).Replace("\\*", ".*").Replace("\\?", ".") + "$");

    private static List<Rule> ReadRules(List<string> problems)
    {
        var rules = new List<Rule>();
        if (!File.Exists(_notPortedPath))
        {
            return rules;
        }
        var number = 0;
        foreach (var raw in File.ReadLines(_notPortedPath))
        {
            number++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }
            var parts = line.Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                problems.Add($"upstream_not_ported.txt:{number}: the rule has no reason");
                continue;
            }
            rules.Add(new Rule { Line = number, Selector = parts[0], Reason = parts[1] });
        }
        return rules;
    }

    private static string Report(IEnumerable<UpstreamTest> tests, HashSet<string> ported, HashSet<string> notPorted)
    {
        var report = new StringBuilder();
        report.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{"code",-5}{"tests",6}{"n/a",6}{"done",6}{"todo",6}");
        var totals = new int[4];
        foreach (var group in tests.GroupBy(test => test.Code).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var count = group.Count();
            var excluded = group.Count(test => notPorted.Contains(test.Id));
            var done = group.Count(test => ported.Contains(test.Id));
            var row = new[] { count, excluded, done, count - excluded - done };
            for (var i = 0; i < row.Length; i++)
            {
                totals[i] += row[i];
            }
            report.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{group.Key,-5}{row[0],6}{row[1],6}{row[2],6}{row[3],6}");
        }
        report.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{"total",-5}{totals[0],6}{totals[1],6}{totals[2],6}{totals[3],6}");
        return report.ToString();
    }

    private sealed class Rule
    {
        public int Line { get; init; }

        public string Selector { get; init; }

        public string Reason { get; init; }

        public bool Used { get; set; }

        public bool Matches(UpstreamTest test)
        {
            if (Regex.IsMatch(Selector, @"^[A-Z]{2}-(?:[A-Z]{2,3}-)?\d{3}$"))
            {
                return test.Id == Selector;
            }
            if (Regex.IsMatch(Selector, "^[A-Z]{2}$"))
            {
                return test.Code == Selector;
            }
            return Glob(Selector).IsMatch(test.FileWithoutExtension);
        }
    }
}
