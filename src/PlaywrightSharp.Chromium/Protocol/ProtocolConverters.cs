using System;
using System.Linq;

namespace PlaywrightSharp.Chromium.Protocol
{
    internal static class ProtocolConverters
    {
        internal static Input.MouseButton ToMouseButtonProtocol(this PlaywrightSharp.Input.MouseButton button)
            => button switch
            {
                PlaywrightSharp.Input.MouseButton.Left => Input.MouseButton.Left,
                PlaywrightSharp.Input.MouseButton.Middle => Input.MouseButton.Middle,
                PlaywrightSharp.Input.MouseButton.Right => Input.MouseButton.Right,
                _ => Input.MouseButton.None,
            };

        internal static int ToModifiersMask(this PlaywrightSharp.Input.Modifier[] modifiers)
        {
            int mask = 0;
            if (modifiers.Contains(PlaywrightSharp.Input.Modifier.Alt))
            {
                mask |= 1;
            }

            if (modifiers.Contains(PlaywrightSharp.Input.Modifier.Control))
            {
                mask |= 2;
            }

            if (modifiers.Contains(PlaywrightSharp.Input.Modifier.Meta))
            {
                mask |= 4;
            }

            if (modifiers.Contains(PlaywrightSharp.Input.Modifier.Shift))
            {
                mask |= 8;
            }

            return mask;
        }

        internal static string ToStringFormat(this ScreenshotFormat format)
            => format switch
            {
                ScreenshotFormat.Jpeg => "jpeg",
                ScreenshotFormat.Png => "png",
                _ => string.Empty,
            };

        internal static Page.Viewport ToViewportProtocol(this Rect clip)
            => new Page.Viewport
            {
                Height = clip.Height,
                Width = clip.Width,
                X = clip.X,
                Y = clip.Y,
                Scale = 1,
            };

        internal static string ToExceptionMessage(this Runtime.ExceptionDetails exceptionDetails)
        {
            if (exceptionDetails.Exception != null)
            {
                return exceptionDetails.Exception.Description ?? exceptionDetails.Exception.Value.ToString();
            }

            string message = exceptionDetails.Text;
            if (exceptionDetails.StackTrace != null)
            {
                foreach (var callframe in exceptionDetails.StackTrace.CallFrames)
                {
                    string location = $"{callframe.Url}:{callframe.LineNumber}:{callframe.ColumnNumber}";
                    string functionName = string.IsNullOrEmpty(callframe.FunctionName) ? "<anonymous>" : callframe.FunctionName;
                    message += $"\n at ${functionName} (${location})";
                }
            }

            return message;
        }

        internal static ResourceType ToPlaywrightResourceType(this Network.ResourceType resourceType)
            => resourceType switch
            {
                Network.ResourceType.Document => ResourceType.Document,
                Network.ResourceType.Stylesheet => ResourceType.StyleSheet,
                Network.ResourceType.Image => ResourceType.Image,
                Network.ResourceType.Media => ResourceType.Media,
                Network.ResourceType.Font => ResourceType.Font,
                Network.ResourceType.Script => ResourceType.Script,
                Network.ResourceType.TextTrack => ResourceType.TextTrack,
                Network.ResourceType.XHR => ResourceType.Xhr,
                Network.ResourceType.Fetch => ResourceType.Fetch,
                Network.ResourceType.EventSource => ResourceType.EventSource,
                Network.ResourceType.WebSocket => ResourceType.WebSocket,
                Network.ResourceType.Manifest => ResourceType.Manifest,
                Network.ResourceType.SignedExchange => ResourceType.SignedExchange,
                Network.ResourceType.Ping => ResourceType.Ping,
                Network.ResourceType.CSPViolationReport => ResourceType.CSPViolationReport,
                Network.ResourceType.Other => ResourceType.Other,
                _ => ResourceType.Other
            };

        internal static DialogType ToDialogType(this PlaywrightSharp.Chromium.Protocol.Page.DialogType dialogType)
            => dialogType switch
            {
                Page.DialogType.Alert => DialogType.Alert,
                Page.DialogType.Confirm => DialogType.Confirm,
                Page.DialogType.Prompt => DialogType.Prompt,
                Page.DialogType.Beforeunload => DialogType.BeforeUnload,
                _ => DialogType.Alert
            };
    }
}
