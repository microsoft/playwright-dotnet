namespace PlaywrightSharp.Tooling.Models.Mismatch
{
    internal class MismatchEntity
    {
        public string UpstreamClassName { get; set; }

        public string ClassName { get; set; }

        public MismatchMember[] Members { get; set; }

        public string Justification { get; set; }
    }
}
