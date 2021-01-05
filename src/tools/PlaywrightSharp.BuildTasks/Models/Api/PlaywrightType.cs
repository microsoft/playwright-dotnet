using System.Collections.Generic;

namespace PlaywrightSharp.BuildTasks.Models.Api
{
    public class PlaywrightType
    {
        public Dictionary<string, PlaywrightArgument> Properties { get; set; }

        public string Name { get; set; }
    }
}
