using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class Accessibility : IAccessibility
    {
        private readonly Func<IElementHandle, Task<AccessibilityTree>> _getAXTreeAsync;

        public Accessibility(Func<IElementHandle, Task<AccessibilityTree>> getAXTreeAsync)
        {
            _getAXTreeAsync = getAXTreeAsync;
        }

        public async Task<SerializedAXNode> SnapshotAsync(AccessibilitySnapshotOptions options = null)
        {
            options ??= new AccessibilitySnapshotOptions();
            var accessibilityTree = await _getAXTreeAsync(options.Root).ConfigureAwait(false);

            if (!options.InterestingOnly)
            {
                if (options.Root != null)
                {
                    return accessibilityTree.Needle != null ? SerializeTree(accessibilityTree.Needle)[0] : null;
                }

                return SerializeTree(accessibilityTree.Tree)[0];
            }

            var interestingNodes = new List<IAXNode>();
            CollectInterestingNodes(interestingNodes, accessibilityTree.Tree, false);
            if (options.Root != null && (accessibilityTree.Needle == null || !interestingNodes.Contains(accessibilityTree.Needle)))
            {
                return null;
            }

            return SerializeTree(accessibilityTree.Needle ?? accessibilityTree.Tree, interestingNodes)[0];
        }

        private void CollectInterestingNodes(List<IAXNode> collection, IAXNode node, bool insideControl)
        {
            if (node.IsInteresting(insideControl))
            {
                collection.Add(node);
            }

            if (node.IsLeafNode())
            {
                return;
            }

            insideControl = insideControl || node.IsControl();
            foreach (var child in node.Children)
            {
                CollectInterestingNodes(collection, child, insideControl);
            }
        }

        private SerializedAXNode[] SerializeTree(IAXNode node, ICollection<IAXNode> whitelistedNodes = null)
        {
            var children = new List<SerializedAXNode>();
            foreach (var child in node.Children)
            {
                children.AddRange(SerializeTree(child, whitelistedNodes));
            }

            if (whitelistedNodes?.Contains(node) == false)
            {
                return children.ToArray();
            }

            var serializedNode = node.Serialize();
            if (children.Count > 0)
            {
                serializedNode.Children = children.ToArray();
            }

            return new[] { serializedNode };
        }
    }
}
