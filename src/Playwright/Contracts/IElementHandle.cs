using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IElementHandle
    {
        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = null);
    }
}
