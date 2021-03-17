using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IElementHandle
    {
        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync"/>
        Task SetInputFilesAsync(string file, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync"/>
        Task SetInputFilesAsync(IEnumerable<string> files, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync"/>
        Task SetInputFilesAsync(ElementHandleFiles file, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SelectOptionAsync" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SelectOptionAsync" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<string> values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SelectOptionAsync" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IElementHandle values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SelectOptionAsync" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SelectOptionAsync" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(ElementHandleValues values, bool? noWaitAfter, float? timeout);

        /// <inheritdoc cref="IElementHandle.SelectOptionAsync" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params string[] values);

        /// <inheritdoc cref="IElementHandle.SelectOptionAsync" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params ElementHandleValues[] values);

        /// <inheritdoc cref="IElementHandle.SelectOptionAsync" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(params IElementHandle[] values);
    }
}
