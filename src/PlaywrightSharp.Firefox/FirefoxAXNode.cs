using System;
using System.Collections.Generic;
using System.Linq;
using PlaywrightSharp.Firefox.Protocol.Accessibility;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxAXNode : IAXNode
    {
        private static readonly IReadOnlyDictionary<string, string> FirefoxRoleToARIARole = new Dictionary<string, string>
        {
            ["pushbutton"] = "button",
            ["checkbutton"] = "checkbox",
            ["editcombobox"] = "combobox",
            ["content deletion"] = "deletion",
            ["footnote"] = "doc-footnote",
            ["non-native document"] = "document",
            ["grouping"] = "group",
            ["graphic"] = "img",
            ["content insertion"] = "insertion",
            ["animation"] = "marquee",
            ["flat equation"] = "math",
            ["menupopup"] = "menu",
            ["check menu item"] = "menuitemcheckbox",
            ["radio menu item"] = "menuitemradio",
            ["listbox option"] = "option",
            ["radiobutton"] = "radio",
            ["statusbar"] = "status",
            ["pagetab"] = "tab",
            ["pagetablist"] = "tablist",
            ["propertypage"] = "tabpanel",
            ["entry"] = "textbox",
            ["outline"] = "tree",
            ["tree table"] = "treegrid",
            ["outlineitem"] = "treeitem",
        };

        private readonly AXTree _payload;
        private readonly FirefoxAXNode[] _children;
        private readonly bool _editable;
        private readonly bool _richlyEditable;
        private readonly bool _focusable;
        private readonly bool _expanded;
        private readonly string _name;
        private readonly string _role;
        private bool? _cachedHasFocusableChild;

        public FirefoxAXNode(AXTree payload)
        {
            _payload = payload;
            _children = Array.ConvertAll(payload.Children ?? Array.Empty<AXTree>(), x => new FirefoxAXNode(x));
            _editable = payload.Editable == true;
            _richlyEditable = _editable && (payload.Tag != "textarea" && payload.Tag != "input");
            _focusable = payload.Focusable == true;
            _expanded = payload.Expanded == true;
            _name = payload.Name;
            _role = payload.Role;
        }

        public IEnumerable<IAXNode> Children => _children;

        public bool IsControl()
        {
            switch (_role)
            {
                case "checkbutton":
                case "check menu item":
                case "check rich option":
                case "combobox":
                case "combobox option":
                case "color chooser":
                case "listbox":
                case "listbox option":
                case "listbox rich option":
                case "popup menu":
                case "menupopup":
                case "menuitem":
                case "menubar":
                case "button":
                case "pushbutton":
                case "radiobutton":
                case "radio menuitem":
                case "scrollbar":
                case "slider":
                case "spinbutton":
                case "switch":
                case "pagetab":
                case "entry":
                case "tree table":
                    return true;
                default:
                    return false;
            }
        }

        public bool IsInteresting(bool insideControl)
        {
            if (_focusable || _richlyEditable)
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

            return IsLeafNode() && _name.Trim().Length > 0;
        }

        public bool IsLeafNode()
        {
            if (_children.Length == 0)
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
                case "graphic":
                case "scrollbar":
                case "slider":
                case "separator":
                case "progressbar":
                    return true;
                default:
                    break;
            }

            // Here and below: Android heuristics
            if (HasFocusableChild())
            {
                return false;
            }

            if (_focusable && !string.IsNullOrEmpty(_name))
            {
                return true;
            }

            if (_role == "heading" && !string.IsNullOrEmpty(_name))
            {
                return true;
            }

            return false;
        }

        public SerializedAXNode Serialize()
        {
            FirefoxRoleToARIARole.TryGetValue(_role, out string nodeRole);
            var node = new SerializedAXNode
            {
                Role = nodeRole ?? _role,
                Name = _name ?? string.Empty,
                Value = _payload.Value,
                Description = _payload.Description,
                RoleDescription = _payload.Roledescription,
                ValueText = _payload.Valuetext,
                KeyShortcuts = _payload.Keyshortcuts,
                Disabled = _payload.Disabled ?? false,
                Expanded = _payload.Expanded ?? false,
                Focused = _role != "document" && _payload.Focused == true,
                Modal = _payload.Modal ?? false,
                Multiline = _payload.Multiline ?? false,
                Multiselectable = _payload.Multiselectable ?? false,
                Readonly = _payload.Readonly ?? false,
                Required = _payload.Required ?? false,
                Selected = _payload.Selected ?? false,
                Checked = GetChecked(_payload.Checked),
                Pressed = GetPressed(_payload.Pressed),
                Level = _payload.Level.HasValue ? (int)_payload.Level : 0,
                AutoComplete = GetTokenProperty(_payload.Autocomplete),
                HasPopup = GetTokenProperty(_payload.Haspopup == true ? "true" : null),
                Invalid = GetTokenProperty(_payload.Invalid == true ? "true" : null),
                Orientation = GetTokenProperty(_payload.Orientation),
            };

            static CheckedState GetChecked(AXTreeChecked? value)
            {
                if (value != null)
                {
                    if (value == AXTreeChecked.Mixed)
                    {
                        return CheckedState.Mixed;
                    }
                    else if (value == AXTreeChecked.True)
                    {
                        return CheckedState.True;
                    }
                }

                return CheckedState.False;
            }

            static CheckedState GetPressed(bool? value) => value != null ? value == true ? CheckedState.True : CheckedState.False : CheckedState.False;

            static string GetTokenProperty(string value) => string.IsNullOrWhiteSpace(value) || value == "false" ? null : value;

            return node;
        }

        internal IAXNode FindNeedle()
        {
            if (_payload.FoundObject == true)
            {
                return this;
            }

            foreach (var child in _children)
            {
                var found = child.FindNeedle();
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private bool IsTextOnlyObject() => _role == "text leaf" || _role == "text" || _role == "statictext";

        private bool IsPlainTextField()
        {
            if (_richlyEditable)
            {
                return false;
            }

            if (_editable)
            {
                return true;
            }

            return _role == "entry";
        }

        private bool HasFocusableChild()
        {
            if (_cachedHasFocusableChild == null)
            {
                _cachedHasFocusableChild = _children.Any(child => child._focusable || child.HasFocusableChild());
            }

            return _cachedHasFocusableChild.Value;
        }
    }
}
