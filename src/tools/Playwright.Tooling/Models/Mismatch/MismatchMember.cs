namespace Playwright.Tooling.Models.Mismatch
{
    internal class MismatchMember
    {
        public string UpstreamMemberName { get; set; }

        public string MemberName { get; set; }

        public string Justification { get; set; }

        public MismatchArgument[] Arguments { get; set; }
    }
}
