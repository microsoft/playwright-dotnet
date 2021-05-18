using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    /// <summary>
    /// see <see cref="IPage.FileChooser"/> arguments.
    /// </summary>
    internal partial class FileChooser : IFileChooser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileChooser"/> class.
        /// </summary>
        /// <param name="page">The page this file chooser belongs to.</param>
        /// <param name="element">The input element.</param>
        /// <param name="multiple">The multiple option.</param>
        public FileChooser(IPage page, ElementHandle element, bool multiple)
        {
            Page = page;
            Element = element;
            ElementImpl = element;
            IsMultiple = multiple;
        }

        public IPage Page { get; set; }

        public IElementHandle Element { get; set; }

        public ElementHandle ElementImpl { get; set; }

        public bool IsMultiple { get; set; }

        public Task SetFilesAsync(string filesString, bool? noWaitAfter = default, float? timeout = default)
            => ElementImpl.SetInputFilesAsync(filesString, noWaitAfter, timeout);

        public Task SetFilesAsync(IEnumerable<string> filesString, bool? noWaitAfter = default, float? timeout = default)
            => ElementImpl.SetInputFilesAsync(filesString, noWaitAfter, timeout);

        public Task SetFilesAsync(FilePayload filesFilePayload, bool? noWaitAfter = default, float? timeout = default)
            => ElementImpl.SetInputFilesAsync(filesFilePayload, noWaitAfter, timeout);

        public Task SetFilesAsync(IEnumerable<FilePayload> filesEnumerableFilePayload, bool? noWaitAfter = default, float? timeout = default)
            => ElementImpl.SetInputFilesAsync(filesEnumerableFilePayload, noWaitAfter, timeout);
    }
}
