using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Transport.Converters;

namespace PlaywrightSharp.Transport.Channels
{
    internal class FrameChannel : Channel<Frame>
    {
        public FrameChannel(string guid, ConnectionScope scope, Frame owner) : base(guid, scope, owner)
        {
        }

        internal Task<ResponseChannel> GoToAsync(string url, int? timeout, LifecycleEvent? waitUntil, string referer, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["url"] = url,
                ["timeout"] = timeout,
                ["isPage"] = isPage,
            };

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            if (referer != null)
            {
                args["referer"] = referer;
            }

            return Scope.SendMessageToServer<ResponseChannel>(Guid, "goto", args);
        }

        internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(
            string script,
            bool isFunction,
            object arg,
            bool isPage,
            bool serializeArgument = false)
        {
            JsonSerializerOptions serializerOptions;

            if (serializeArgument)
            {
                serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions(false);
                arg = new EvaluateArgument
                {
                    Guids = new List<EvaluateArgumentGuidElement>(),
                    Value = arg,
                };
                serializerOptions.Converters.Add(new EvaluateArgumentConverter());
            }
            else
            {
                serializerOptions = Scope.Connection.GetDefaultJsonSerializerOptions(false);
            }

            return Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "evaluateExpressionHandle",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["isPage"] = isPage,
                },
                serializerOptions: serializerOptions);
        }

        internal Task<JSHandleChannel> WaitForFunctionAsync(
            string expression,
            bool isFunction,
            object arg,
            bool isPage,
            int? timeout,
            object polling,
            bool serializeArgument = false)
        {
            JsonSerializerOptions serializerOptions;

            if (serializeArgument)
            {
                serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions(false);
                arg = new EvaluateArgument
                {
                    Guids = new List<EvaluateArgumentGuidElement>(),
                    Value = arg,
                };
                serializerOptions.Converters.Add(new EvaluateArgumentConverter());
            }
            else
            {
                serializerOptions = Scope.Connection.GetDefaultJsonSerializerOptions(false);
            }

            var args = new Dictionary<string, object>
            {
                ["expression"] = expression,
                ["isFunction"] = isFunction,
                ["arg"] = arg,
                ["timeout"] = timeout,
                ["isPage"] = isPage,
            };

            if (polling != null)
            {
                args["polling"] = polling;
            }

            return Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "waitForFunction",
                args,
                serializerOptions: serializerOptions);
        }

        internal Task<JsonElement?> EvaluateExpressionAsync(
            string script,
            bool isFunction,
            object arg,
            bool isPage,
            bool serializeArgument = false)
        {
            JsonSerializerOptions serializerOptions;

            if (serializeArgument)
            {
                serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions(false);
                arg = new EvaluateArgument
                {
                    Guids = new List<EvaluateArgumentGuidElement>(),
                    Value = arg,
                };
                serializerOptions.Converters.Add(new EvaluateArgumentConverter());
            }
            else
            {
                serializerOptions = Scope.Connection.GetDefaultJsonSerializerOptions(false);
            }

            return Scope.SendMessageToServer<JsonElement?>(
                Guid,
                "evaluateExpression",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["isPage"] = isPage,
                },
                serializerOptions: serializerOptions);
        }

        internal Task<JSHandleChannel> EvalOnSelectorAsync(string selector, string script, bool isFunction, object arg, bool isPage)
            => Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "evalOnSelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["isPage"] = isPage,
                });

        internal Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, bool isFunction, EvaluateArgument arg, bool isPage)
            => Scope.SendMessageToServer<JsonElement?>(
                Guid,
                "evalOnSelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["isPage"] = isPage,
                });

        internal Task<JSHandleChannel> EvalOnSelectorAllAsync(string selector, string script, bool isFunction, object arg, bool isPage)
            => Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "evalOnSelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["isPage"] = isPage,
                });

        internal Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, bool isFunction, EvaluateArgument arg, bool isPage)
            => Scope.SendMessageToServer<JsonElement?>(
                Guid,
                "evalOnSelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["isPage"] = isPage,
                });

        internal Task<ElementHandleChannel> GetFrameElementAsync() => Scope.SendMessageToServer<ElementHandleChannel>(Guid, "frameElement", null);

        internal Task<string> GetTitleAsync() => Scope.SendMessageToServer<string>(Guid, "title", null);

        internal Task<ElementHandleChannel> WaitForSelector(string selector, WaitForState? state, int? timeout, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["timeout"] = timeout,
                ["isPage"] = isPage,
            };

            if (state != null)
            {
                args["state"] = state;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "waitForSelector",
                args);
        }

        internal Task<ElementHandleChannel> AddScriptTagAsync(string url, string path, string content, string type, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "addScriptTag",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["path"] = path,
                    ["content"] = content,
                    ["type"] = type,
                    ["isPage"] = isPage,
                });

        internal Task<ElementHandleChannel> AddStyleTagAsync(string url, string path, string content, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "addStyleTag",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["path"] = path,
                    ["content"] = content,
                    ["isPage"] = isPage,
                });

        internal Task<ResponseChannel> WaitForNavigationAsync(LifecycleEvent? waitUntil, string url, int? timeout, bool isPage)
        {
            var param = new Dictionary<string, object>
            {
                ["isPage"] = isPage,
                ["timeout"] = timeout,
            };

            if (url != null)
            {
                param["url"] = url;
            }

            if (waitUntil != null)
            {
                param["waitUntil"] = waitUntil;
            }

            return Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "waitForNavigation",
                param);
        }

        internal Task WaitForLoadStateAsync(LifecycleEvent? state, int? timeout, bool isPage)
        {
            var param = new Dictionary<string, object>
            {
                ["isPage"] = isPage,
                ["timeout"] = timeout,
            };

            if (state != null)
            {
                param["state"] = state;
            }

            return Scope.SendMessageToServer(
                Guid,
                "waitForLoadState",
                param);
        }

        internal Task SetcontentAsync(string html, int? timeout, LifecycleEvent? waitUntil, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["html"] = html,
                ["timeout"] = timeout,
                ["isPage"] = isPage,
            };

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "setContent",
                args);
        }

        internal Task ClickAsync(string selector, ClickOptions options, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "click",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["delay"] = options?.Delay,
                    ["button"] = options?.Button ?? Input.MouseButton.Left,
                    ["clickCount"] = options?.ClickCount ?? 1,
                    ["force"] = options?.Force,
                    ["timeout"] = options?.Timeout,
                    ["noWaitAfter"] = options?.NoWaitAfter,
                    ["position"] = options?.Position,
                    ["modifiers"] = options?.Modifiers?.Select(m => m.ToValueString()),
                    ["isPage"] = isPage,
                });

        internal Task<ElementHandleChannel> QuerySelectorAsync(string selector, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "querySelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["isPage"] = isPage,
                });

        internal Task<ChannelBase[]> QuerySelectorAllAsync(string selector, bool isPage)
            => Scope.SendMessageToServer<ChannelBase[]>(
                Guid,
                "querySelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["isPage"] = isPage,
                });

        internal Task FillAsync(string selector, string value, NavigatingActionWaitOptions options, bool isPage)
            => Scope.SendMessageToServer(
                Guid,
                "fill",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["value"] = value,
                    ["noWaitAfter"] = options?.NoWaitAfter,
                    ["timeout"] = options?.Timeout,
                    ["isPage"] = isPage,
                });

        internal Task CheckAsync(string selector, int? timeout, bool force, bool noWaitAfter, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "check",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["force"] = force,
                    ["timeout"] = timeout,
                    ["noWaitAfter"] = noWaitAfter,
                    ["isPage"] = isPage,
                });

        internal Task UncheckAsync(string selector, int? timeout, bool force, bool noWaitAfter, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "uncheck",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["force"] = force,
                    ["timeout"] = timeout,
                    ["noWaitAfter"] = noWaitAfter,
                    ["isPage"] = isPage,
                });

        internal Task DispatchEventAsync(string selector, string type, object eventInit, int? timeout, bool isPage)
            => Scope.SendMessageToServer(
                Guid,
                "dispatchEvent",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["type"] = type,
                    ["eventInit"] = eventInit,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task HoverAsync(string selector, Point? position, Modifier[] modifiers, bool force, int? timeout, bool isPage)
            => Scope.SendMessageToServer(
                Guid,
                "hover",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["position"] = position,
                    ["modifiers"] = modifiers,
                    ["force"] = force,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task<string[]> PressAsync(string selector, string text, int delay, bool? noWaitAfter, int? timeout, bool isPage)
            => Scope.SendMessageToServer<string[]>(
                Guid,
                "press",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["key"] = text,
                    ["delay"] = delay,
                    ["noWaitAfter"] = noWaitAfter,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task<string[]> SelectOptionAsync(string selector, object values, bool? noWaitAfter, int? timeout, bool isPage)
            => Scope.SendMessageToServer<string[]>(
                Guid,
                "selectOption",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["values"] = values,
                    ["noWaitAfter"] = noWaitAfter,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                },
                true);

        internal Task<string> GetAttributeAsync(string selector, string name, int? timeout, bool isPage)
            => Scope.SendMessageToServer<string>(
                Guid,
                "getAttribute",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["name"] = name,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task<string> GetInnerHtmlAsync(string selector, int? timeout, bool isPage)
            => Scope.SendMessageToServer<string>(
                Guid,
                "innerHTML",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task TypeAsync(string selector, string text, int? delay, bool isPage)
            => Scope.SendMessageToServer(
                Guid,
                "type",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["text"] = text,
                    ["delay"] = delay,
                    ["isPage"] = isPage,
                });

        internal Task<string> GetContentAsync(bool isPage)
            => Scope.SendMessageToServer<string>(
                Guid,
                "content",
                new Dictionary<string, object>
                {
                    ["isPage"] = isPage,
                });

        internal Task FocusAsync(string selector, int? timeout, bool isPage)
            => Scope.SendMessageToServer(
                Guid,
                "focus",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task<string> GetInnerTextAsync(string selector, int? timeout, bool isPage)
            => Scope.SendMessageToServer<string>(
                Guid,
                "innerText",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task SetInputFilesAsync(string selector, FilePayload[] files, bool isPage)
            => Scope.SendMessageToServer<string>(
                Guid,
                "setInputFiles",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["files"] = files,
                    ["isPage"] = isPage,
                });

        internal Task<string> GetTextContentAsync(string selector, int? timeout, bool isPage)
            => Scope.SendMessageToServer<string>(
                Guid,
                "textContent",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });
    }
}
