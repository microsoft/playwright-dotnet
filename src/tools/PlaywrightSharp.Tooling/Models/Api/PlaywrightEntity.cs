using System.Collections.Generic;

namespace PlaywrightSharp.Tooling.Models.Api
{
    internal class PlaywrightEntity
    {
        public string Name { get; set; }

        public PlaywrightMember[] Members { get; set; }
    }
}
