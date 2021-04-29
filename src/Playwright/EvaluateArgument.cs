using System.Collections.Generic;

namespace Microsoft.Playwright
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

        public List<EvaluateArgumentGuidElement> Handles { get; set; } = new List<EvaluateArgumentGuidElement>();
    }
}
