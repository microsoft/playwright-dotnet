using CommandLine;

namespace ApiChecker
{
    /// <summary>
    /// Describes the options for scaffolding the tests.
    /// </summary>
    [Verb("scaffold-test", HelpText = "Takes a spec.ts file and scaffolds the C# test.")]
    internal class ScaffoldTestOptions
    {
        [Option(Required = true, HelpText = "Name of the spec file to use.")]
        public string SpecFile { get; set; }

        [Option(Required = false, HelpText = "The location of the scaffold code. If not present, will output to console.")]
        public string OutputFile { get; set; }

        [Option(Required = false, HelpText = "The namespace of the generated class.", Default = "PlaywrightSharp.Tests")]
        public string Namespace { get; set; }
    }
}
