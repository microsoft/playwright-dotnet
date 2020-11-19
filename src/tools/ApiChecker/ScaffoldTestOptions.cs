using CommandLine;

namespace ApiChecker
{
#pragma warning disable SA1600 // Elements are self-documenting and don't need additional docs
    /// <summary>
    /// Describes the options for scaffolding the tests.
    /// </summary>
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
#pragma warning restore SA1600 // Elements should be documented
}
