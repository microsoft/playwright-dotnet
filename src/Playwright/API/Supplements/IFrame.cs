using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IFrame
    {
        public Task<JsonElement?> EvaluateAsync(string expression, object arg = default);

        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg);

        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = default);
    }
}
