using System.Collections.Generic;

namespace PlaywrightSharp
{
    internal class EvaluateArgument
    {
        public object Value { get; set; }

        public List<EvaluateArgumentGuidElement> Guids { get; set; }
    }
}