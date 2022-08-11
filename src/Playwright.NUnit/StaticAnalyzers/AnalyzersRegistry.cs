using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Playwright.NUnit.StaticAnalyzers
{
    public static class AnalyzersRegistry
    {
        public static DiagnosticDescriptor NonSupportedParallelScopesDiagnostic => new DiagnosticDescriptor(
            id: "PWNunit1001",
            title: "Playwright only supports ParallelScope.Self",
            messageFormat: "Playwright only supports ParallelScope.Self",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}

