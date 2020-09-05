using System.Collections.Generic;

namespace ApiChecker
{
    public class PlaywrightType
    {
        public Dictionary<string, PlaywrightArgument> Properties { get; set; }

        public string Name { get; set; }
    }
}