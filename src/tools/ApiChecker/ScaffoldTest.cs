using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApiChecker
{
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "CodeDom is complicated.")]
    internal static class ScaffoldTest
    {
        private static readonly TextInfo _textInfo = CultureInfo.InvariantCulture.TextInfo;

        public static void Run(ScaffoldTestOptions options)
        {
            if (!File.Exists(options.SpecFile))
            {
                throw new FileNotFoundException();
            }

            var fileInfo = new FileInfo(options.SpecFile);

            int dotSeparator = fileInfo.Name.IndexOf('.');
            string name = _textInfo.ToTitleCase(fileInfo.Name.Substring(0, dotSeparator)) + "Tests";
            var targetClass = GenerateClass(options.Namespace, name, fileInfo.Name);

            Regex rx = new Regex(@"it\(\'(.*)\',");
            foreach (string line in File.ReadAllLines(options.SpecFile))
            {
                var m = rx.Match(line);
                if (m?.Success == false)
                {
                    continue;
                }

                // keep in mind, group 0 is the entire match, but
                // first (and only group), should give us the describe value
                AddTest(targetClass, m.Groups[1].Value, fileInfo.Name);
            }

            using CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions codegenOptions = new CodeGeneratorOptions()
            {
                BracingStyle = "C",
            };

            using StreamWriter sourceWriter = new StreamWriter(options.OutputFile);
            provider.GenerateCodeFromCompileUnit(
                targetClass, sourceWriter, codegenOptions);
        }

        private static CodeCompileUnit GenerateClass(string @namespace, string @class, string fileOrigin)
        {
            var targetUnit = new CodeCompileUnit();
            var globalNamespace = new CodeNamespace();

            // add imports
            globalNamespace.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            globalNamespace.Imports.Add(new CodeNamespaceImport("PlaywrightSharp.Tests.BaseTests"));
            globalNamespace.Imports.Add(new CodeNamespaceImport("Xunit"));
            globalNamespace.Imports.Add(new CodeNamespaceImport("Xunit.Abstractions"));

            targetUnit.Namespaces.Add(globalNamespace);

            var codeNamespace = new CodeNamespace(@namespace);
            var targetClass = new CodeTypeDeclaration(@class)
            {
                IsClass = true,
                TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed,
            };

            targetClass.BaseTypes.Add(new CodeTypeReference("PlaywrightSharpPageBaseTest"));

            _ = targetClass.CustomAttributes.Add(new CodeAttributeDeclaration(
                "Collection",
                new CodeAttributeArgument[]
                {
                    new CodeAttributeArgument(
                        new CodeFieldReferenceExpression(
                            new CodeTypeReferenceExpression("TestConstants"),
                            "TestFixtureBrowserCollectionName")),
                }));

            targetClass.Comments.Add(new CodeCommentStatement($"<playwright-file>{fileOrigin}</playwright-file>", true));
            codeNamespace.Types.Add(targetClass);

            targetUnit.Namespaces.Add(codeNamespace);

            // add constructor
            var constructor = new CodeConstructor()
            {
                Attributes = MemberAttributes.Public,
            };

            constructor.Parameters.Add(new CodeParameterDeclarationExpression("ITestOutputHelper", "output"));
            constructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("output"));
            constructor.Comments.Add(new CodeCommentStatement("<inheritdoc/>", true));
            targetClass.Members.Add(constructor);

            return targetUnit;
        }

        private static void AddTest(CodeCompileUnit @class, string testDescribe, string testOrigin)
        {
            // make name out of the describe
            string name = _textInfo.ToTitleCase(testDescribe).Replace(" ", string.Empty);
            Console.WriteLine($"Adding {name}");

            CodeMemberMethod method = new CodeMemberMethod()
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference("async Task"),
                Name = name,
            };

            @class.Namespaces[1].Types[0].Members.Add(method);

            method.Comments.Add(new CodeCommentStatement($"<playwright-file>{testOrigin}</playwright-file>", true));
            method.Comments.Add(new CodeCommentStatement($"<playwright-it>{testDescribe}</playwright-it>", true));
            method.CustomAttributes.Add(new CodeAttributeDeclaration(
                "Fact",
                new CodeAttributeArgument[]
                {
                    new CodeAttributeArgument(
                        "Timeout",
                        new CodeFieldReferenceExpression(
                            new CodeTypeReferenceExpression("PlaywrightSharp.Playwright"),
                            "DefaultTimeout")),
                    new CodeAttributeArgument(
                        "Skip",
                        new CodePrimitiveExpression("This test is not yet implemented.")),
                }));
        }
    }
}
