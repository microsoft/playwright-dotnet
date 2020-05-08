using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Intermediate AX node.
    /// </summary>
    internal interface IAXNode
    {
        /// <summary>
        /// Children.
        /// </summary>
        public IEnumerable<IAXNode> Children { get; }

        /// <summary>
        /// Serialize action.
        /// </summary>
        /// <returns>Serialized node.</returns>
        public SerializedAXNode Serialize();

        /// <summary>
        /// Checks if the node is interesting.
        /// </summary>
        /// <param name="insideControl">Check inside the control.</param>
        /// <returns>Whether the node is interesting or not.</returns>
        public bool IsInteresting(bool insideControl);

        /// <summary>
        /// Checks if the node is a leaf node.
        /// </summary>
        /// <returns>Whether the node is leaf or not.</returns>
        public bool IsLeafNode();

        /// <summary>
        /// Checks if the node is a control.
        /// </summary>
        /// <returns>Whether the node is control or not.</returns>
        public bool IsControl();
    }
}
