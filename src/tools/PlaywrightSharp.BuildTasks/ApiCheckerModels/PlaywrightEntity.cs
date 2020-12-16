using System.Collections.Generic;

namespace PlaywrightSharp.BuildTasks.ApiCheckerModels
{
    internal class PlaywrightEntity
    {
        public Dictionary<string, PlaywrightMember> Methods { get; set; }

        public Dictionary<string, PlaywrightMember> Events { get; set; }
    }
}
