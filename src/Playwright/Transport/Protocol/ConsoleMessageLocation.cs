namespace Microsoft.Playwright.Transport.Protocol
{
    internal class ConsoleMessageLocation
    {
        public string URL { get; set; }

        public int LineNumber { get; set; }

        public int ColumnNumber { get; set; }

        public override string ToString() => $"{URL}:{LineNumber}:{ColumnNumber}";
    }
}
