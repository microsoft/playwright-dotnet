using System.Collections.Generic;

namespace ApiChecker
{
    internal class PlaywrightEntity
    {
        public Dictionary<string, PlaywrightMember> Methods { get; set; }

        public Dictionary<string, PlaywrightMember> Events { get; set; }
    }
}
