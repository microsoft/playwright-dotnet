namespace PlaywrightSharp.Tooling.Models.Mismatch
{
    public class MismatchMember
    {
        public string UpstreamMemberName { get; set; }

        public string MemberName { get; set; }

        public string Justification { get; set; }

        public MismatchArgument[] Arguments { get; set; }
    }
}
