using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IElementHandle
    {
        Task SetInputFilesAsync(string files, bool? noWaitAfter = default, float? timeout = default);

        Task SetInputFilesAsync(IEnumerable<string> files, bool? noWaitAfter = default, float? timeout = default);

        Task SetInputFilesAsync(FilePayload files, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string values, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<string> values, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(IElementHandle values, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(SelectOptionValue values, bool? noWaitAfter = default, float? timeout = default);

        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = null);
    }
}
