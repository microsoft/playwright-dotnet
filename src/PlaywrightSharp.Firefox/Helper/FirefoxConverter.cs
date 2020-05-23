using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using PlaywrightSharp.Firefox.Protocol.Browser;
using PlaywrightSharp.Firefox.Protocol.Network;
using PlaywrightSharp.Firefox.Protocol.Runtime;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Firefox.Helper
{
    internal static class FirefoxConverter
    {
        public static ConsoleType GetConsoleType(this RuntimeConsoleFirefoxEvent runtimeConsole)
            => runtimeConsole.Type == "warn" ? ConsoleType.Warning : runtimeConsole.Type.ToEnum<ConsoleType>();

        public static string ToStringValue(this RemoteObjectUnserializableValue value)
        {
            return typeof(RemoteObjectUnserializableValue).GetField(value.ToString()).GetCustomAttribute<EnumMemberAttribute>().Value;
        }

        public static ConsoleMessageLocation ToConsoleMessageLocation(this RuntimeConsoleFirefoxEvent runtimeConsole) => new ConsoleMessageLocation
        {
            ColumnNumber = (int?)runtimeConsole.Location.ColumnNumber,
            LineNumber = (int?)runtimeConsole.Location.LineNumber,
            URL = runtimeConsole.Location.Url,
        };

        public static ResourceType GetResourceType(this NetworkRequestWillBeSentFirefoxEvent networkRequestWillBeSent)
            => networkRequestWillBeSent.Cause switch
            {
                "TYPE_INVALID" => ResourceType.Other,
                "TYPE_OTHER" => ResourceType.Other,
                "TYPE_SCRIPT" => ResourceType.Script,
                "TYPE_IMAGE" => ResourceType.Image,
                "TYPE_STYLESHEET" => ResourceType.StyleSheet,
                "TYPE_OBJECT" => ResourceType.Other,
                "TYPE_DOCUMENT" => ResourceType.Document,
                "TYPE_SUBDOCUMENT" => ResourceType.Document,
                "TYPE_REFRESH" => ResourceType.Document,
                "TYPE_XBL" => ResourceType.Other,
                "TYPE_PING" => ResourceType.Other,
                "TYPE_XMLHTTPREQUEST" => ResourceType.Xhr,
                "TYPE_OBJECT_SUBREQUEST" => ResourceType.Other,
                "TYPE_DTD" => ResourceType.Other,
                "TYPE_FONT" => ResourceType.Font,
                "TYPE_MEDIA" => ResourceType.Media,
                "TYPE_WEBSOCKET" => ResourceType.WebSocket,
                "TYPE_CSP_REPORT" => ResourceType.Other,
                "TYPE_XSLT" => ResourceType.Other,
                "TYPE_BEACON" => ResourceType.Other,
                "TYPE_FETCH" => ResourceType.Fetch,
                "TYPE_IMAGESET" => ResourceType.Image,
                "TYPE_WEB_MANIFEST" => ResourceType.Manifest,
                _ => ResourceType.Other
            };

        public static int ToModifiersMask(this Modifier[] modifiers)
        {
            int mask = 0;
            if (modifiers.Contains(Modifier.Alt))
            {
                mask |= 1;
            }

            if (modifiers.Contains(Modifier.Control))
            {
                mask |= 2;
            }

            if (modifiers.Contains(Modifier.Shift))
            {
                mask |= 4;
            }

            if (modifiers.Contains(Modifier.Meta))
            {
                mask |= 8;
            }

            return mask;
        }

        public static int ToButtonNumber(this MouseButton button) => button switch
        {
            MouseButton.Left => 0,
            MouseButton.Middle => 1,
            MouseButton.Right => 2,
            _ => 0,
        };

        public static int ToButtonsMask(this List<MouseButton> buttons)
        {
            int mask = 0;
            if (buttons.Contains(MouseButton.Left))
            {
                mask |= 1;
            }

            if (buttons.Contains(MouseButton.Right))
            {
                mask |= 2;
            }

            if (buttons.Contains(MouseButton.Middle))
            {
                mask |= 4;
            }

            return mask;
        }

        public static PermissionsGrantPermissions ToPermissions(this ContextPermission permission)
            => permission switch
            {
                ContextPermission.Geolocation => PermissionsGrantPermissions.Geo,
                ContextPermission.Notifications => PermissionsGrantPermissions.DesktopNotifications,
                ContextPermission.Camera => PermissionsGrantPermissions.Camera,
                ContextPermission.Microphone => PermissionsGrantPermissions.Microphone,
                _ => throw new ArgumentOutOfRangeException(nameof(permission), "Unknown permission: " + permission)
            };

        public static HTTPHeader[] ToHttpHeaders(this IDictionary<string, string> headers) => headers.Select(pair => new HTTPHeader
        {
            Name = pair.Key,
            Value = pair.Value,
        }).ToArray();
    }
}
