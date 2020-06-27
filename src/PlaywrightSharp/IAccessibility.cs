using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// The Accessibility class provides methods for inspecting Chromium's accessibility tree.
    /// The accessibility tree is used by assistive technology such as screen readers or switches.
    /// <para/>
    /// Accessibility is a very platform-specific thing. On different platforms, there are different screen readers that might have wildly different output.
    /// Blink - Chrome's rendering engine - has a concept of "accessibility tree", which is than translated into different platform-specific APIs.
    /// Accessibility namespace gives users access to the Blink Accessibility Tree.
    /// Most of the accessibility tree gets filtered out when converting from Blink AX Tree to Platform-specific AX-Tree or by assistive technologies themselves.
    /// By default, PlaywrightSharp tries to approximate this filtering, exposing only the "interesting" nodes of the tree.
    /// </summary>
    public interface IAccessibility
    {
        /// <summary>
        /// Snapshots the async.
        /// </summary>
        /// <param name="options">Options.</param>
        /// <returns>A <see cref="Task"/> that completes when the snapshot is completed, yielding the serialized result.</returns>
        public Task<SerializedAXNode> SnapshotAsync(AccessibilitySnapshotOptions options = null);
    }
}
