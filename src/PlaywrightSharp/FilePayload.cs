using System.IO;

namespace PlaywrightSharp
{
    /// <summary>
    /// Payload for <see cref="IElementHandle.SetInputFilesAsync(FilePayload, int?, bool?)"/>, <see cref="IPage.SetInputFilesAsync(string, FilePayload, int?, bool?)"/> and <see cref="IFrame.SetInputFilesAsync(string, FilePayload, int?, bool?)"/>.
    /// </summary>
    public class FilePayload
    {
        /// <summary>
        /// File name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Mime type.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// File content as a base64 string.
        /// </summary>
        public string Buffer { get; set; }
    }
}
