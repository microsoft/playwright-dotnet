using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol.Accessibility;
using PlaywrightSharp.Chromium.Protocol.DOM;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumAXNode : IAXNode
    {
        private readonly ChromiumSession _client;
        private readonly string _name;
        private readonly string _role;
        private readonly bool _richlyEditable;
        private readonly bool _editable;
        private readonly bool _hidden;
        private bool? _cachedHasFocusableChild;

        private ChromiumAXNode(ChromiumSession client, AXNode payload)
        {
            _client = client;
            Payload = payload;

            _name = payload.Name != null ? payload.Name.Value.ToString() : string.Empty;
            _role = payload.Role != null ? payload.Role.Value.ToString() : "Unknown";

            _richlyEditable = GetPropertyElement(payload, "editable")?.ToString() == "richtext";
            _editable |= _richlyEditable;
            _hidden = GetPropertyElement(payload, "hidden")?.ToObject<bool>() ?? false;
            Focusable = GetPropertyElement(payload, "focusable")?.ToObject<bool>() ?? false;
        }

        IEnumerable<IAXNode> IAXNode.Children => Children;

        private bool Focusable { get; }

        private AXNode Payload { get; }

        private List<ChromiumAXNode> Children { get; } = new List<ChromiumAXNode>();

        public static ChromiumAXNode CreateTree(ChromiumSession client, AXNode[] payloads)
        {
            var nodeById = new Dictionary<string, ChromiumAXNode>();

            foreach (var payload in payloads)
            {
                nodeById[payload.NodeId] = new ChromiumAXNode(client, payload);
            }

            foreach (var node in nodeById.Values)
            {
                if (node.Payload.ChildIds == null)
                {
                    continue;
                }

                foreach (string childId in node.Payload.ChildIds)
                {
                    node.Children.Add(nodeById[childId]);
                }
            }

            return nodeById.Values.FirstOrDefault();
        }

        public SerializedAXNode Serialize()
        {
            var properties = new Dictionary<string, JsonElement?>();
            foreach (var property in Payload.Properties)
            {
                properties[property.Name.ToString().ToLower()] = (JsonElement?)property?.Value?.Value;
            }

            if (Payload.Name != null)
            {
                properties["name"] = (JsonElement)Payload.Name.Value;
            }

            if (Payload.Value != null)
            {
                properties["value"] = (JsonElement)Payload.Value.Value;
            }

            if (Payload.Description != null)
            {
                properties["description"] = (JsonElement)Payload.Description.Value;
            }

            var node = new SerializedAXNode
            {
                Role = _role,
                Name = properties.GetValueOrDefault("name")?.ToString(),
                Value = properties.GetValueOrDefault("value")?.ToString(),
                Description = properties.GetValueOrDefault("description")?.ToString(),
                KeyShortcuts = properties.GetValueOrDefault("keyshortcuts")?.ToString(),
                RoleDescription = properties.GetValueOrDefault("roledescription")?.ToString(),
                ValueText = properties.GetValueOrDefault("valuetext")?.ToString(),
                Disabled = properties.GetValueOrDefault("disabled")?.ToObject<bool>() ?? false,
                Expanded = properties.GetValueOrDefault("expanded")?.ToObject<bool>() ?? false,
                Focused = properties.GetValueOrDefault("focused")?.ToObject<bool>() == true && _role != "WebArea",
                Modal = properties.GetValueOrDefault("modal")?.ToObject<bool>() ?? false,
                Multiline = properties.GetValueOrDefault("multiline")?.ToObject<bool>() ?? false,
                Multiselectable = properties.GetValueOrDefault("multiselectable")?.ToObject<bool>() ?? false,
                Readonly = properties.GetValueOrDefault("readonly")?.ToObject<bool>() ?? false,
                Required = properties.GetValueOrDefault("required")?.ToObject<bool>() ?? false,
                Selected = properties.GetValueOrDefault("selected")?.ToObject<bool>() ?? false,
                Checked = GetCheckedState(properties.GetValueOrDefault("checked")?.ToString()),
                Pressed = GetCheckedState(properties.GetValueOrDefault("pressed")?.ToString()),
                Level = properties.GetValueOrDefault("level")?.ToObject<int>() ?? 0,
                ValueMax = properties.GetValueOrDefault("valuemax")?.ToObject<int>() ?? 0,
                ValueMin = properties.GetValueOrDefault("valuemin")?.ToObject<int>() ?? 0,
                AutoComplete = GetIfNotFalse(properties.GetValueOrDefault("autocomplete")?.ToString()),
                HasPopup = GetIfNotFalse(properties.GetValueOrDefault("haspopup")?.ToString()),
                Invalid = GetIfNotFalse(properties.GetValueOrDefault("invalid")?.ToString()),
                Orientation = GetIfNotFalse(properties.GetValueOrDefault("orientation")?.ToString()),
            };

            return node;
        }

        /// <inheritdoc />
        public bool IsInteresting(bool insideControl)
        {
            if (_role == "Ignored" || _hidden)
            {
                return false;
            }

            if (Focusable || _richlyEditable)
            {
                return true;
            }

            // If it's not focusable but has a control role, then it's interesting.
            if (IsControl())
            {
                return true;
            }

            // A non focusable child of a control is not interesting
            if (insideControl)
            {
                return false;
            }

            return IsLeafNode() && !string.IsNullOrEmpty(_name);
        }

        public bool IsLeafNode()
        {
            if (Children.Count == 0)
            {
                return true;
            }

            // These types of objects may have children that we use as internal
            // implementation details, but we want to expose them as leaves to platform
            // accessibility APIs because screen readers might be confused if they find
            // any children.
            if (IsPlainTextField() || IsTextOnlyObject())
            {
                return true;
            }

            // Roles whose children are only presentational according to the ARIA and
            // HTML5 Specs should be hidden from screen readers.
            // (Note that whilst ARIA buttons can have only presentational children, HTML5
            // buttons are allowed to have content.)
            switch (_role)
            {
                case "doc-cover":
                case "graphics-symbol":
                case "img":
                case "Meter":
                case "scrollbar":
                case "slider":
                case "separator":
                case "progressbar":
                    return true;
            }

            // Here and below: Android heuristics
            if (HasFocusableChild())
            {
                return false;
            }

            if (Focusable && !string.IsNullOrEmpty(_name))
            {
                return true;
            }

            if (_role == "heading" && !string.IsNullOrEmpty(_name))
            {
                return true;
            }

            return false;
        }

        public async Task<IAXNode> FindElementAsync(ElementHandle element)
        {
            var remoteObject = element.RemoteObject;
            var result = await _client.SendAsync(new DOMDescribeNodeRequest { ObjectId = remoteObject.ObjectId }).ConfigureAwait(false);
            var needle = Find(node => node.Payload.BackendDOMNodeId == result.Node.BackendNodeId);
            return needle;
        }

        public bool IsControl()
        {
            switch (_role)
            {
                case "button":
                case "checkbox":
                case "ColorWell":
                case "combobox":
                case "DisclosureTriangle":
                case "listbox":
                case "menu":
                case "menubar":
                case "menuitem":
                case "menuitemcheckbox":
                case "menuitemradio":
                case "radio":
                case "scrollbar":
                case "searchbox":
                case "slider":
                case "spinbutton":
                case "switch":
                case "tab":
                case "textbox":
                case "tree":
                    return true;
            }

            return false;
        }

        private bool HasFocusableChild()
        {
            if (!_cachedHasFocusableChild.HasValue)
            {
                _cachedHasFocusableChild = Children.Any(c => c.Focusable || c.HasFocusableChild());
            }

            return _cachedHasFocusableChild.Value;
        }

        private bool IsTextOnlyObject()
            => _role == "LineBreak" ||
               _role == "text" ||
               _role == "InlineTextBox";

        private bool IsPlainTextField()
            => !_richlyEditable && (_editable || _role == "textbox" || _role == "ComboBox" || _role == "searchbox");

        private IAXNode Find(Func<ChromiumAXNode, bool> predicate)
            => predicate(this) ? this : Children.Select(child => child.Find(predicate)).FirstOrDefault(result => result != null);

        private string GetIfNotFalse(string value) => value != null && value != "false" ? value : null;

        private CheckedState GetCheckedState(string value) =>
            value switch
            {
                "mixed" => CheckedState.Mixed,
                "true" => CheckedState.True,
                _ => CheckedState.False
            };

        private JsonElement? GetPropertyElement(AXNode payload, string propertyName)
            => (JsonElement?)Array.Find(payload.Properties, p => string.Equals(p.Name.ToString(), propertyName, StringComparison.OrdinalIgnoreCase))
                ?.Value?.Value;
    }
}
