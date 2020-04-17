using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
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

        public static HTTPHeader[] ToHeadersArray(this IDictionary<string, string> headers) => headers.Select(pair => new HTTPHeader
        {
            Name = pair.Key,
            Value = pair.Value,
        }).ToArray();

        public static string ToStatusText(this HttpStatusCode httpStatus) => httpStatus switch
        {
            HttpStatusCode.Continue => "Continue",
            HttpStatusCode.SwitchingProtocols => "Switching Protocols",
            (HttpStatusCode)102 => "Processing",
            (HttpStatusCode)103 => "Early Hints",
            HttpStatusCode.OK => "OK",
            HttpStatusCode.Created => "Created",
            HttpStatusCode.Accepted => "Accepted",
            (HttpStatusCode)203 => "Non-Authoritative Information",
            HttpStatusCode.NoContent => "No Content",
            HttpStatusCode.ResetContent => "Reset Content",
            HttpStatusCode.PartialContent => "Partial Content",
            (HttpStatusCode)207 => "Multi-Status",
            (HttpStatusCode)208 => "Already Reported",
            (HttpStatusCode)226 => "IM Used",
            HttpStatusCode.MultipleChoices => "Multiple Choices",
            HttpStatusCode.MovedPermanently => "Moved Permanently",
            HttpStatusCode.Found => "Found",
            HttpStatusCode.SeeOther => "See Other",
            HttpStatusCode.NotModified => "Not Modified",
            HttpStatusCode.UseProxy => "Use Proxy",
            (HttpStatusCode)306 => "Switch Proxy",
            HttpStatusCode.TemporaryRedirect => "Temporary Redirect",
            (HttpStatusCode)308 => "Permanent Redirect",
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.PaymentRequired => "Payment Required",
            HttpStatusCode.Forbidden => "Forbidden",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.MethodNotAllowed => "Method Not Allowed",
            HttpStatusCode.NotAcceptable => "Not Acceptable",
            HttpStatusCode.ProxyAuthenticationRequired => "Proxy Authentication Required",
            HttpStatusCode.RequestTimeout => "Request Timeout",
            HttpStatusCode.Conflict => "Conflict",
            HttpStatusCode.Gone => "Gone",
            HttpStatusCode.LengthRequired => "Length Required",
            HttpStatusCode.PreconditionFailed => "Precondition Failed",
            HttpStatusCode.RequestEntityTooLarge => "Payload Too Large",
            HttpStatusCode.RequestUriTooLong => "URI Too Long",
            HttpStatusCode.UnsupportedMediaType => "Unsupported Media Type",
            HttpStatusCode.RequestedRangeNotSatisfiable => "Range Not Satisfiable",
            HttpStatusCode.ExpectationFailed => "Expectation Failed",
            (HttpStatusCode)418 => "I\'m a teapot",
            (HttpStatusCode)421 => "Misdirected Request",
            (HttpStatusCode)422 => "Unprocessable Entity",
            (HttpStatusCode)423 => "Locked",
            (HttpStatusCode)424 => "Failed Dependency",
            (HttpStatusCode)425 => "Too Early",
            HttpStatusCode.UpgradeRequired => "Upgrade Required",
            (HttpStatusCode)428 => "Precondition Required",
            (HttpStatusCode)429 => "Too Many Requests",
            (HttpStatusCode)431 => "Request Header Fields Too Large",
            (HttpStatusCode)451 => "Unavailable For Legal Reasons",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            HttpStatusCode.NotImplemented => "Not Implemented",
            HttpStatusCode.BadGateway => "Bad Gateway",
            HttpStatusCode.ServiceUnavailable => "Service Unavailable",
            HttpStatusCode.GatewayTimeout => "Gateway Timeout",
            HttpStatusCode.HttpVersionNotSupported => "HTTP Version Not Supported",
            (HttpStatusCode)506 => "Variant Also Negotiates",
            (HttpStatusCode)507 => "Insufficient Storage",
            (HttpStatusCode)508 => "Loop Detected",
            (HttpStatusCode)510 => "Not Extended",
            (HttpStatusCode)511 => "Network Authentication Required",
            _ => "OK"
        };
    }
}
