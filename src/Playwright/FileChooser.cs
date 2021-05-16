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

        public IPage Page { get; set; }

        public IElementHandle Element { get; set; }

        public bool IsMultiple { get; set; }

        public Task SetFilesAsync(string filesString, bool? noWaitAfter = default, float? timeout = default)
            => Element.SetInputFilesAsync(filesString, noWaitAfter, timeout);

        public Task SetFilesAsync(IEnumerable<string> filesString, bool? noWaitAfter = default, float? timeout = default)
            => Element.SetInputFilesAsync(filesString, noWaitAfter, timeout);

        public Task SetFilesAsync(FilePayload filesFilePayload, bool? noWaitAfter = default, float? timeout = default)
            => Element.SetInputFilesAsync(filesFilePayload, noWaitAfter, timeout);

        public Task SetFilesAsync(IEnumerable<FilePayload> filesEnumerableFilePayload, bool? noWaitAfter = default, float? timeout = default)
            => Element.SetInputFilesAsync(filesEnumerableFilePayload, noWaitAfter, timeout);
    }
}
