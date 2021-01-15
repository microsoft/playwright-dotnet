namespace PlaywrightSharp.BuildTasks.Models.Api
{
    public class PlaywrightMember
    {
        public PlaywrightMemberKind Kind { get; set; }

        public string Name { get; set; }

        public bool Deprecated { get; set; }

        public PlaywrightType Type { get; set; }

        public PlaywrightMember[] Args { get; set; }

        public PlaywrightLangs Langs { get; set; }
    }
}
