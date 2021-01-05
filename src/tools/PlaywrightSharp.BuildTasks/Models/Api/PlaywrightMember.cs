using System.Collections.Generic;

namespace PlaywrightSharp.BuildTasks.Models.Api
{
    public class PlaywrightMember
    {
        public string Kind { get; set; }

        public Dictionary<string, PlaywrightArgument> Args { get; set; }
    }
}
