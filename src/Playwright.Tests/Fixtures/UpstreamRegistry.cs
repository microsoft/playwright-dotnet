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

using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>A row of <c>.upstream/tests/registry.md</c>: one upstream test.</summary>
internal sealed class UpstreamTest
{
    /// <summary><c>CH-001</c>.</summary>
    public string Id { get; init; }

    /// <summary>Relative to <c>tests/</c>: <c>page/page-check.spec.ts</c>.</summary>
    public string File { get; init; }

    /// <summary>The titles of the describes that hold the test, outermost first.</summary>
    public IReadOnlyList<string> Describes { get; init; }

    /// <summary>The title without the identifier. Of a test that a loop declares, the title with the variants in braces.</summary>
    public string Title { get; init; }

    /// <summary>How many times a loop declares the test.</summary>
    public int Copies { get; init; }

    /// <summary><c>page/page-check</c>.</summary>
    public string FileWithoutExtension => File.EndsWith(".spec.ts", StringComparison.Ordinal) ? File.Substring(0, File.Length - ".spec.ts".Length) : File;

    public string Code => Id.Substring(0, 2);
}

/// <summary>
/// The registry of the upstream tests, <c>tests/registry.md</c> at <c>DriverVersion</c>, and the names
/// that the port of a test has. The rules are in <c>src/Playwright.Tests/AGENTS.md</c>.
/// </summary>
internal static class UpstreamRegistry
{
    private static readonly Regex _row = new(
        @"^\| `(?<id>[A-Z]{2}-(?:[A-Z]{2,3}-)?\d{3})` \| `(?<file>[^`]+)` \| (?<title>.*?)(?: \*\((?<copies>\d+) copies\)\*)?(?: \| \d{4}-\d{2}-\d{2} \| `[0-9a-f]+`)? \|$",
        RegexOptions.Compiled);

    private static readonly Lazy<IReadOnlyDictionary<string, UpstreamTest>> _tests = new(Read);

    public static string Path { get; } = System.IO.Path.Combine(Env.UpstreamTestsDir, "registry.md");

    public static IReadOnlyDictionary<string, UpstreamTest> Tests => _tests.Value;

    public static UpstreamTest Find(string id) => id != null && Tests.TryGetValue(id, out var test) ? test : null;

    /// <summary><c>page/page-check.spec.ts</c> is <c>Microsoft.Playwright.Tests.Page.PageCheckTests</c>.</summary>
    public static string ClassFullName(string file)
    {
        var segments = (file.EndsWith(".spec.ts", StringComparison.Ordinal) ? file.Substring(0, file.Length - ".spec.ts".Length) : file).Split('/');
        var result = new StringBuilder("Microsoft.Playwright.Tests");
        for (var i = 0; i < segments.Length - 1; i++)
        {
            result.Append('.').Append(PascalCase(segments[i]));
        }
        return result.Append('.').Append(PascalCase(segments[^1])).Append("Tests").ToString();
    }

    /// <summary><c>Page/PageCheckTests.cs</c>, relative to <c>src/Playwright.Tests</c>.</summary>
    public static string SourcePath(string file) => ClassFullName(file).Substring("Microsoft.Playwright.Tests.".Length).Replace('.', '/') + ".cs";

    /// <summary>
    /// The name of the method of a test, or of the nested class of a describe: the words of the title, each
    /// with an upper-case first letter. The identifier, the tags and the variants of a loop are not words.
    /// </summary>
    public static string PascalCase(string title)
    {
        title = Regex.Replace(title, @"^[A-Z]{2}-(?:[A-Z]{2,3}-)?\d{3}(\s|$)", string.Empty);
        title = Regex.Replace(title, @"\$\{[^{}]*\}|\{[^{}]*\}", " ");
        title = Regex.Replace(title, @"(^|\s)@[\w-]+", " ");
        var result = new StringBuilder();
        foreach (Match word in Regex.Matches(title, "[A-Za-z0-9]+"))
        {
            result.Append(char.ToUpperInvariant(word.Value[0])).Append(word.Value, 1, word.Value.Length - 1);
        }
        if (result.Length == 0 || char.IsDigit(result[0]))
        {
            result.Insert(0, "Test");
        }
        return result.ToString();
    }

    public static string UnescapeMarkdown(string text) => Regex.Replace(text, @"\\([\\`*_\[\]<>|{}#])", "$1");

    private static IReadOnlyDictionary<string, UpstreamTest> Read()
    {
        var tests = new Dictionary<string, UpstreamTest>();
        if (!File.Exists(Path))
        {
            return tests;
        }
        foreach (var line in File.ReadLines(Path))
        {
            var match = _row.Match(line);
            if (!match.Success)
            {
                continue;
            }
            var parts = match.Groups["title"].Value.Split(" › ").Select(UnescapeMarkdown).ToArray();
            tests[match.Groups["id"].Value] = new UpstreamTest
            {
                Id = match.Groups["id"].Value,
                File = match.Groups["file"].Value,
                Describes = parts.Take(parts.Length - 1).ToArray(),
                Title = parts[^1],
                Copies = match.Groups["copies"].Success ? int.Parse(match.Groups["copies"].Value, System.Globalization.CultureInfo.InvariantCulture) : 1,
            };
        }
        return tests;
    }
}
