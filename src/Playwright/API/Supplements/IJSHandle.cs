using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IJSHandle
    {
        /// <inheritdoc cref="EvaluateAsync{T}(string, object)" />
        Task<JsonElement?> EvaluateAsync(string expression, object arg = null);
    }
}
