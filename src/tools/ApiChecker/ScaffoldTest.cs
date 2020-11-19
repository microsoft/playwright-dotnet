using System;
using System.CodeDom;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using System.Globalization;
using System.CodeDom.Compiler;

namespace ApiChecker
{
    static class ScaffoldTest
    {
        static readonly TextInfo _textInfo = CultureInfo.InvariantCulture.TextInfo;

        [Verb("scaffold-test", HelpText = "Takes a spec.ts file and scaffolds the C# test.")]
        public class ScaffoldTestOptions
        {
            [Option(Required = true, HelpText = "Name of the spec file to use.")]
            public string SpecFile { get; set; }

            [Option(Required = false, HelpText = "The location of the scaffold code. If not present, will output to console.")]
            public string OutputFile { get; set; }

            [Option(Required = false, HelpText = "The namespace of the generated class.", Default = "PlaywrightSharp.Tests")]
            public string Namespace { get; set; }
        }

        public static Task Run(ScaffoldTestOptions options)
        {
            if (!File.Exists(options.SpecFile))
                return Task.FromException(new FileNotFoundException());

            var fileInfo = new FileInfo(options.SpecFile);
            string specName = fileInfo.Name; // we want this when generating the code

            int dotSeparator = fileInfo.Name.IndexOf('.');
            string name = _textInfo.ToTitleCase(fileInfo.Name.Substring(0, dotSeparator)) + "Tests";
            var targetClass = GenerateClass(options.Namespace, name, fileInfo.Name);

            // this is a naive implementation, but it'll do
            string[] lines = File.ReadAllLines(options.SpecFile);

            Regex rx = new Regex(@"it\(\'(.*)\',");
            foreach (string line in lines)
            {
                var m = rx.Match(line);
                if (m?.Success == false)
                    continue;

                // keep in mind, group 0 is the entire match, but 
                // first (and only group), should give us the describe value
                AddTest(targetClass, m.Groups[1].Value, fileInfo.Name);
            }

            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions codegenOptions = new CodeGeneratorOptions()
            {
                BracingStyle = "C",
            };

            using (StreamWriter sourceWriter = new StreamWriter(options.OutputFile))
            {
                provider.GenerateCodeFromCompileUnit(
                    targetClass, sourceWriter, codegenOptions);
            }

            return Task.CompletedTask;
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

            targetClass.BaseTypes.Add(new CodeTypeReference("PlaywrightSharpBrowserBaseTest"));

            ///
            targetClass.CustomAttributes.Add(new CodeAttributeDeclaration("Collection",
            new CodeAttributeArgument[] {
                new CodeAttributeArgument(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("TestConstants"), "TestFixtureBrowserCollectionName"))
            }));

            targetClass.Comments.Add(new CodeCommentStatement($"<playwright-file>{fileOrigin}</playwright-file>", true));
            codeNamespace.Types.Add(targetClass);

            targetUnit.Namespaces.Add(codeNamespace);

            // add constructor
            var constructor = new CodeConstructor()
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final
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
            string name = _textInfo.ToTitleCase(testDescribe).Replace(" ", String.Empty);
            Console.WriteLine($"Adding {name}");

            CodeMemberMethod method = new CodeMemberMethod()
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference("async Task"),
                Name = name
            };

            @class.Namespaces[1].Types[0].Members.Add(method);

            method.Comments.Add(new CodeCommentStatement($"<playwright-file>{testOrigin}</playwright-file>", true));
            method.Comments.Add(new CodeCommentStatement($"<playwright-it>{testDescribe}</playwright-it>", true));
            method.CustomAttributes.Add(new CodeAttributeDeclaration("Fact",
            new CodeAttributeArgument[] {
                new CodeAttributeArgument("Timeout", new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("PlaywrightSharp.Playwright"), "DefaultTimeout"))
            }));

            method.Statements.Add(
            new CodeThrowExceptionStatement(
                new CodeObjectCreateExpression(
                    new CodeTypeReference(typeof(NotImplementedException)),
                    new CodeExpression[] { })));
        }
    }
}
