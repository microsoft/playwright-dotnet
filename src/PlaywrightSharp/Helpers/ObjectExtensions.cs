using System;
using PlaywrightSharp.Contracts.Models;

namespace PlaywrightSharp.Helpers
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
