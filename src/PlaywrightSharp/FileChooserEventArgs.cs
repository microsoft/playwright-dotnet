using System;
using System.IO;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// see <see cref="IPage.FileChooser"/> arguments.
    /// </summary>
    public class FileChooserEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileChooserEventArgs"/> class.
        /// </summary>
        /// <param name="page">The page this file chooser belongs to.</param>
        /// <param name="element">The input element.</param>
        /// <param name="multiple">The multiple option.</param>
        public FileChooserEventArgs(IPage page, IElementHandle element, bool multiple)
        {
            Page = page;
            Element = element;
            IsMultiple = multiple;
        }

        /// <summary>
        /// The page this file chooser belongs to.
        /// </summary>
        public IPage Page { get; set; }

        /// <summary>
        /// Handle to the input element that was clicked.
        /// </summary>
        public IElementHandle Element { get; set; }

        /// <summary>
        /// Whether file chooser allow for multiple file selection.
        /// </summary>
        /// <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/file#attr-multiple"/>
        public bool IsMultiple { get; set; }

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="file"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="file">The file path.</param>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        public Task SetFilesAsync(string file) => Element.SetInputFilesAsync(file);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="files"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="files">File paths.</param>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        public Task SetFilesAsync(string[] files) => Element.SetInputFilesAsync(files);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="file"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="file">The file payload.</param>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        public Task SetFilesAsync(FilePayload file) => Element.SetInputFilesAsync(file);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="files"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="files">File payloads.</param>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        public Task SetFilesAsync(FilePayload[] files) => Element.SetInputFilesAsync(files);
    }
}
