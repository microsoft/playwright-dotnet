using System.Collections.Generic;

namespace PlaywrightSharp.BuildTasks.ApiCheckerModels
{
    public class PlaywrightType
    {
        public Dictionary<string, PlaywrightArgument> Properties { get; set; }

        public string Name { get; set; }
    }
}
