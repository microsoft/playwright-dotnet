using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IFileChooser
    {
        /// <inheritdoc cref="SetFilesAsync(System.Collections.Generic.IEnumerable{string}, bool?, float?)"/>
        Task SetFilesAsync(string files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SetFilesAsync(System.Collections.Generic.IEnumerable{string}, bool?, float?)"/>
        Task SetFilesAsync(FilePayload files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SetFilesAsync(System.Collections.Generic.IEnumerable{string}, bool?, float?)"/>
        Task SetFilesAsync(IEnumerable<FilePayload> files, bool? noWaitAfter = default, float? timeout = default);
    }
}
