using System;

namespace PlaywrightSharp.BuildTasks.Models.Api
{
    public class PlaywrightType
    {
        public PlaywrightMember[] Properties { get; set; }

        public string Name { get; set; }

        public string Expression { get; set; }

        public PlaywrightType[] Union { get; set; } = Array.Empty<PlaywrightType>();

        public PlaywrightType[] Templates { get; set; } = Array.Empty<PlaywrightType>();
    }
}
