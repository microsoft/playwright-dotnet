using System;
using Microsoft.Playwright.Contracts.Models;

namespace Microsoft.Playwright.Helpers
{
    internal static class ObjectExtensions
    {
        public static object ToEvaluateArgument(this object obj)
        {
            if (UndefinedEvaluationArgument.Undefined.Equals(obj))
            {
                return EvaluateArgument.Undefined;
            }

            return obj;
        }
    }
}
