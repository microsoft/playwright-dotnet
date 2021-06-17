using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core
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

        public Task SetFilesAsync(string files, FileChooserSetFilesOptions options = default)
            => ElementImpl.SetInputFilesAsync(files, Map(options));

        public Task SetFilesAsync(IEnumerable<string> files, FileChooserSetFilesOptions options = default)
            => ElementImpl.SetInputFilesAsync(files, Map(options));

        public Task SetFilesAsync(FilePayload files, FileChooserSetFilesOptions options = default)
            => ElementImpl.SetInputFilesAsync(files, Map(options));

        public Task SetFilesAsync(IEnumerable<FilePayload> files, FileChooserSetFilesOptions options = default)
            => ElementImpl.SetInputFilesAsync(files, Map(options));

        private ElementHandleSetInputFilesOptions Map(FileChooserSetFilesOptions options)
        {
            if (options == null)
            {
                return null;
            }

            return new()
            {
                NoWaitAfter = options?.NoWaitAfter,
                Timeout = options?.Timeout,
            };
        }
    }
}
