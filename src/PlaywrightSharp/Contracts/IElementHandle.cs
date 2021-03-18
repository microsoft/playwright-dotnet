using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IElementHandle
    {
        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)"/>
        Task SetInputFilesAsync(string files, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)"/>
        Task SetInputFilesAsync(IEnumerable<string> files, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)"/>
        Task SetInputFilesAsync(ElementHandleFiles files, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<string> values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IElementHandle values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(ElementHandleValues values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params string[] values);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params ElementHandleValues[] values);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync(IEnumerable{ElementHandleFiles}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params IElementHandle[] values);

        /// <inheritdoc cref="EvalOnSelectorAsync{T}(string, string, object)" />
        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = null);
    }
}
