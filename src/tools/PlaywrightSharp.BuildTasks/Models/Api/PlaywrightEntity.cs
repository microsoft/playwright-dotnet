using System.Collections.Generic;

namespace PlaywrightSharp.BuildTasks.Models.Api
{
    internal class PlaywrightEntity
    {
        public string Name { get; set; }

        public PlaywrightMember[] Members { get; set; }
    }
}
