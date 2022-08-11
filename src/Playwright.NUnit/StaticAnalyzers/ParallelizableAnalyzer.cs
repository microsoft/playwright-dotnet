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

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Playwright.NUnit.StaticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ParallelizableAnalyzer : DiagnosticAnalyzer
    {

        private const string PlaywrigtTestClassName = "Microsoft.Playwright.NUnit.PlaywrightTest";
        public const string FullNameOfTypeParallelizableAttribute = "NUnit.Framework.ParallelizableAttribute";
        private const int ParallelScopeSelf = 1;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(AnalyzersRegistry.NonSupportedParallelScopesDiagnostic);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
        }

        private static void AnalyzeClass(SymbolAnalysisContext context)
        {
            
            if (!TryGetAttributeEnumValue(context.Compilation, context.Symbol,
                out int enumValue,
                out var attributeData))
            {
                return;
            }

            var classSymbol = (INamedTypeSymbol)context.Symbol;
            var playwrightTest = context.Compilation.GetTypeByMetadataName(PlaywrigtTestClassName)!;


            if (!IsPlaywrightTest(classSymbol, playwrightTest))
            {
                return;
            }

            if (enumValue != ParallelScopeSelf)
            {
                var syntaxTree = attributeData?.ApplicationSyntaxReference?.SyntaxTree;
                var location = syntaxTree?.GetLocation(attributeData!.ApplicationSyntaxReference!.Span);
                context.ReportDiagnostic(Diagnostic.Create(AnalyzersRegistry.NonSupportedParallelScopesDiagnostic, location));

            }
        }

        private static bool IsPlaywrightTest(INamedTypeSymbol type, INamedTypeSymbol playwrightTest)
        {
            while (type.BaseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type.BaseType, playwrightTest))
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }

        private static bool TryGetAttributeEnumValue(Compilation compilation, ISymbol symbol,
            out int enumValue,
            out AttributeData? attributeData)
        {
            enumValue = 0;
            attributeData = null;

            var parallelizableAttributeType = compilation.GetTypeByMetadataName(FullNameOfTypeParallelizableAttribute);
            if (parallelizableAttributeType is null)
            {
                return false;
            }
            attributeData = symbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, parallelizableAttributeType));

            if (attributeData?.ApplicationSyntaxReference is null)
            {
                return false;
            }
            var optionalEnumValue = GetOptionalEnumValue(attributeData);
            if (optionalEnumValue is null)
                return false;

            enumValue = optionalEnumValue.Value;
            return true;
        }
    
        private static int? GetOptionalEnumValue(AttributeData attributeData)
        {
            var attributePositionalArguments = attributeData.ConstructorArguments;
            var noExplicitEnumArgument = attributePositionalArguments.Length == 0;
            if (noExplicitEnumArgument)
            {
                return ParallelScopeSelf;
            }
            else
            {
                var arg = attributePositionalArguments[0];
                return arg.Value as int?;
            }
        }
    }
}

