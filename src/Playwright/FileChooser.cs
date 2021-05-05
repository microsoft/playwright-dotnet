using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    /// <summary>
    /// see <see cref="IPage.FileChooser"/> arguments.
    /// </summary>
    public class FileChooser : IFileChooser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileChooser"/> class.
        /// </summary>
        /// <param name="page">The page this file chooser belongs to.</param>
        /// <param name="element">The input element.</param>
        /// <param name="multiple">The multiple option.</param>
        public FileChooser(IPage page, IElementHandle element, bool multiple)
        {
            Page = page;
            Element = element;
            IsMultiple = multiple;
        }

        /// <inheritdoc />
        public IPage Page { get; set; }

        /// <inheritdoc />
        public IElementHandle Element { get; set; }

        /// <inheritdoc />
        public bool IsMultiple { get; set; }

        /// <inheritdoc />
        public Task SetFilesAsync(string filesString, bool? noWaitAfter = default, float? timeout = default)
            => Element.SetInputFilesAsync(filesString, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetFilesAsync(IEnumerable<string> filesString, bool? noWaitAfter = default, float? timeout = default)
            => Element.SetInputFilesAsync(filesString, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetFilesAsync(FilePayload filesFilePayload, bool? noWaitAfter = default, float? timeout = default)
            => Element.SetInputFilesAsync(filesFilePayload, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetFilesAsync(IEnumerable<FilePayload> filesEnumerableFilePayload, bool? noWaitAfter = default, float? timeout = default)
            => Element.SetInputFilesAsync(filesEnumerableFilePayload, noWaitAfter, timeout);
    }
}
