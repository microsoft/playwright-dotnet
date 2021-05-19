using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IElementHandle
    {
        Task SetInputFilesAsync(string files, ElementHandleSetInputFilesOptions options = default);

        Task SetInputFilesAsync(IEnumerable<string> files, ElementHandleSetInputFilesOptions options = default);

        Task SetInputFilesAsync(FilePayload files, ElementHandleSetInputFilesOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string values, ElementHandleSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<string> values, ElementHandleSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(IElementHandle values, ElementHandleSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, ElementHandleSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(SelectOptionValue values, ElementHandleSelectOptionOptions options = default);

        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = null);
    }
}
