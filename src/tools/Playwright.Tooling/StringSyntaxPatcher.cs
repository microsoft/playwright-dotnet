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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;

namespace Playwright.Tooling;

/// <summary>
/// Annotates the generated API with <c>[StringSyntax]</c>, so that IDEs inject and
/// highlight the right language inside string literals holding JavaScript, CSS, HTML
/// or URLs — for example the <c>expression</c> passed to <c>IPage.EvaluateAsync</c>.
/// <para>
/// The upstream generator (<c>utils/doclint/generateDotnetApi.js</c>) knows nothing
/// about this, and <c>API/Generated</c> is deleted and recreated on every roll, so
/// this runs as a post-generation step from <c>build.sh</c>. It is idempotent:
/// attributes it previously wrote are dropped and re-applied.
/// </para>
/// <para>
/// Every rule must match at least once. If the upstream generator renames a member or
/// changes a signature, the rule stops matching and the roll fails with the stale rule
/// named, rather than silently dropping the annotation.
/// </para>
/// </summary>
internal static class StringSyntaxPatcher
{
    private const string JavaScript = "javascript";
    private const string Css = "css";
    private const string Html = "html";

    // StringSyntaxAttribute.Uri. Unlike the three above it is a BCL-defined syntax
    // identifier, so it is honoured by Visual Studio as well as Rider.
    private const string Uri = "Uri";

    private const string AttributePrefix = "[StringSyntax(\"";
    private const string AttributeSuffix = "\")]";

    private const string UsingNamespace = "System.Diagnostics.CodeAnalysis";
    private const string UsingDirective = "using " + UsingNamespace + ";";

    /// <summary>
    /// Method parameters to annotate, matched against every interface in API/Generated.
    /// Declarations are one per line, so a line-based match is enough.
    /// </summary>
    private static readonly ParameterRule[] _parameterRules =
    {
        new("EvaluateAsync", "expression", JavaScript),
        new("EvaluateAllAsync", "expression", JavaScript),
        new("EvaluateHandleAsync", "expression", JavaScript),
        new("EvalOnSelectorAsync", "expression", JavaScript),
        new("EvalOnSelectorAllAsync", "expression", JavaScript),
        new("WaitForFunctionAsync", "expression", JavaScript),

        // The sibling scriptPath parameter is a file path and is deliberately left alone.
        new("AddInitScriptAsync", "script", JavaScript),

        new("SetContentAsync", "html", Html),
        new("ShowOverlayAsync", "html", Html),
    };

    /// <summary>
    /// Properties to annotate on the generated option classes. The owning class is the
    /// file name; "*" matches any option class, for options shared across several of them.
    /// </summary>
    private static readonly PropertyRule[] _propertyRules =
    {
        new("PageAddScriptTagOptions", "Content", JavaScript),
        new("FrameAddScriptTagOptions", "Content", JavaScript),
        new("SelectorsRegisterOptions", "Script", JavaScript),

        new("PageAddStyleTagOptions", "Content", Css),
        new("FrameAddStyleTagOptions", "Content", Css),

        new("PageAddScriptTagOptions", "Url", Uri),
        new("FrameAddScriptTagOptions", "Url", Uri),
        new("PageAddStyleTagOptions", "Url", Uri),
        new("FrameAddStyleTagOptions", "Url", Uri),
        new("*", "BaseURL", Uri),
    };

    public static void Run(PatchStringSyntaxOptions options)
    {
        var generatedDirectory = Path.Combine(options.BasePath, "src", "Playwright", "API", "Generated");
        if (!Directory.Exists(generatedDirectory))
        {
            Console.Error.WriteLine($"Generated API directory not found: {generatedDirectory}");
            Environment.Exit(1);
        }

        var counts = new Dictionary<Rule, int>();
        foreach (var rule in _parameterRules.Cast<Rule>().Concat(_propertyRules))
        {
            counts[rule] = 0;
        }

        var patchedFiles = 0;
        foreach (var file in Directory.EnumerateFiles(generatedDirectory, "*.cs", SearchOption.AllDirectories))
        {
            if (PatchFile(file, counts))
            {
                patchedFiles++;
            }
        }

        var staleRules = counts.Where(_ => _.Value == 0).Select(_ => _.Key).ToList();
        if (staleRules.Count > 0)
        {
            Console.Error.WriteLine("No [StringSyntax] target matched the following rules. The generated API most likely changed shape upstream; update StringSyntaxPatcher.");
            foreach (var rule in staleRules)
            {
                Console.Error.WriteLine($"  {rule}");
            }

            Environment.Exit(1);
        }

        Console.WriteLine($"Applied [StringSyntax] to {counts.Values.Sum()} declarations across {patchedFiles} files.");
    }

    private static bool PatchFile(string file, Dictionary<Rule, int> counts)
    {
        var original = File.ReadAllText(file);
        var newline = original.Contains("\r\n") ? "\r\n" : "\n";
        var optionsClass = Path.GetFileNameWithoutExtension(file);

        var output = new List<string>();
        var annotated = false;
        foreach (var line in original.Split(new[] { newline }, StringSplitOptions.None))
        {
            // Drop what a previous run wrote, so re-running is a no-op rather than a pile-up.
            if (IsAttributeLine(line))
            {
                continue;
            }

            var patched = PatchParameters(line, counts);
            annotated |= patched != line;

            var propertyRule = MatchPropertyRule(optionsClass, patched);
            if (propertyRule != null)
            {
                counts[propertyRule]++;
                annotated = true;
                output.Add(Indentation(patched) + Attribute(propertyRule.Syntax));
            }

            output.Add(patched);
        }

        if (annotated)
        {
            AddUsingDirective(output);
        }

        var result = string.Join(newline, output);
        if (result == original)
        {
            return false;
        }

        File.WriteAllText(file, result);
        return true;
    }

    /// <summary>
    /// Adds the using for <see cref="UsingNamespace"/> to a file that now needs it, keeping the
    /// generator's ordinal ordering of the using block. Only files carrying an attribute get it,
    /// so no unused using is introduced.
    /// </summary>
    private static void AddUsingDirective(List<string> lines)
    {
        var insertAt = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (!line.StartsWith("using ", StringComparison.Ordinal) || !line.EndsWith(";", StringComparison.Ordinal))
            {
                continue;
            }

            if (line == UsingDirective)
            {
                return;
            }

            // Compare the namespaces rather than the whole lines, so that `using System;`
            // sorts before `using System.Diagnostics.CodeAnalysis;` rather than after it.
            insertAt = i + 1;
            if (string.CompareOrdinal(Namespace(line), UsingNamespace) > 0)
            {
                insertAt = i;
                break;
            }
        }

        if (insertAt < 0)
        {
            throw new InvalidOperationException("No using block to extend.");
        }

        lines.Insert(insertAt, UsingDirective);
    }

    private static string Namespace(string usingDirective) => usingDirective.Substring("using ".Length).TrimEnd(';');

    private static string PatchParameters(string line, Dictionary<Rule, int> counts)
    {
        // Documentation comments quote example calls; only real declarations are targets.
        if (line.TrimStart().StartsWith("//", StringComparison.Ordinal) || !line.TrimEnd().EndsWith(");", StringComparison.Ordinal))
        {
            return line;
        }

        foreach (var rule in _parameterRules)
        {
            if (!rule.Declaration.IsMatch(line))
            {
                continue;
            }

            var match = rule.Parameter.Match(line);
            if (match.Success)
            {
                // Counted on match rather than on rewrite, so that a line already carrying the
                // attribute still satisfies the rule instead of reporting it as stale.
                counts[rule]++;
                return line.Substring(0, match.Index)
                    + Attribute(rule.Syntax) + " " + match.Groups["type"].Value + " " + rule.ParameterName
                    + line.Substring(match.Index + match.Length);
            }
        }

        return line;
    }

    private static PropertyRule MatchPropertyRule(string optionsClass, string line)
        => _propertyRules.FirstOrDefault(_ => (_.OptionsClass == "*" || _.OptionsClass == optionsClass) && _.Declaration.IsMatch(line));

    private static bool IsAttributeLine(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith(AttributePrefix, StringComparison.Ordinal) && trimmed.EndsWith(AttributeSuffix, StringComparison.Ordinal);
    }

    private static string Indentation(string line) => line.Substring(0, line.Length - line.TrimStart().Length);

    private static string Attribute(string syntax) => AttributePrefix + syntax + AttributeSuffix;

    internal abstract class Rule
    {
        protected Rule(string syntax) => Syntax = syntax;

        public string Syntax { get; }
    }

    internal sealed class ParameterRule : Rule
    {
        public ParameterRule(string method, string parameterName, string syntax)
            : base(syntax)
        {
            Method = method;
            ParameterName = parameterName;
            Declaration = new Regex($@"\b{Regex.Escape(method)}(?:<T>)?\(");

            // An attribute left by a previous run is consumed, so it is rewritten rather than doubled.
            Parameter = new Regex($@"(?:{Regex.Escape(AttributePrefix)}[^""]*{Regex.Escape(AttributeSuffix)} )?\b(?<type>string\??) {Regex.Escape(parameterName)}\b");
        }

        public string Method { get; }

        public string ParameterName { get; }

        public Regex Declaration { get; }

        public Regex Parameter { get; }

        public override string ToString() => $"parameter {Method}({ParameterName}) -> {Syntax}";
    }

    internal sealed class PropertyRule : Rule
    {
        public PropertyRule(string optionsClass, string property, string syntax)
            : base(syntax)
        {
            OptionsClass = optionsClass;
            Property = property;
            Declaration = new Regex($@"^\s*public string\?? {Regex.Escape(property)} \{{ get; set; \}}");
        }

        public string OptionsClass { get; }

        public string Property { get; }

        public Regex Declaration { get; }

        public override string ToString() => $"property {OptionsClass}.{Property} -> {Syntax}";
    }
}

[Verb("patch-string-syntax")]
internal class PatchStringSyntaxOptions
{
    [Option(Required = true, HelpText = "Solution path.")]
    public string BasePath { get; set; }
}
