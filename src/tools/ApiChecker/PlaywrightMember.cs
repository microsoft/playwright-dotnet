using System.Collections.Generic;

namespace ApiChecker
{
    public class PlaywrightMember
    {
        public string Kind { get; set; }

        public Dictionary<string, PlaywrightArgument> Args { get; set; }
    }
}
