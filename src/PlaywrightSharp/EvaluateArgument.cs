using System.Collections.Generic;

namespace PlaywrightSharp
{
    internal class EvaluateArgument
    {
        public static EvaluateArgument Undefined
            => new EvaluateArgument
            {
                Value = new EvaluateArgumentValueElement.SpecialType
                {
                    V = "undefined",
                },
            };

        public object Value { get; set; }

        public List<EvaluateArgumentGuidElement> Guids { get; set; } = new List<EvaluateArgumentGuidElement>();
    }
}