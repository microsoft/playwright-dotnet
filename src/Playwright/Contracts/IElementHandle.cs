using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IElementHandle
    {
        /// <inheritdoc cref="SetInputFilesAsync(IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(string files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SetInputFilesAsync(IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(IEnumerable<string> files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SetInputFilesAsync(IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(FilePayload files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<string> values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IElementHandle values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(SelectOptionValue values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params string[] values);

        /// <inheritdoc cref="SelectOptionAsync(IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params SelectOptionValue[] values);

        /// <inheritdoc cref="SelectOptionAsync(IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params IElementHandle[] values);

        /// <inheritdoc cref="EvalOnSelectorAsync{T}(string, string, object)" />
        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = null);
    }
}
