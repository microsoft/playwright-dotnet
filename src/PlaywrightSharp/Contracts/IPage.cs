using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IPage
    {
        /// <summary><para>Returns frame with matching URL.</para></summary>
        /// <param name="urlString">
        /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
        /// cref="URL"/> object.
        /// </param>
        /// <returns>Matching frame.</returns>
        IFrame FrameByUrl(string urlString);

        /// <summary><para>Returns frame with matching URL.</para></summary>
        /// <param name="urlRegex">
        /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
        /// cref="URL"/> object.
        /// </param>
        /// <returns>Matching frame.</returns>
        IFrame FrameByUrl(Regex urlRegex);

        /// <summary><para>Returns frame with matching URL.</para></summary>
        /// <param name="urlFunc">
        /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
        /// cref="URL"/> object.
        /// </param>
        /// <returns>Matching frame.</returns>
        IFrame FrameByUrl(Func<string, bool> urlFunc);

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync();

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <param name="colorScheme">
        /// Emulates <c>'prefers-colors-scheme'</c> media feature, supported values are <c>'light'</c>,
        /// <c>'dark'</c>, <c>'no-preference'</c>. Passing <c>null</c> disables color scheme
        /// emulation.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync(ColorScheme colorScheme);
    }
}
