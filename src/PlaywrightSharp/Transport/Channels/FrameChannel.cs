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
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

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
                    Handles = new List<EvaluateArgumentGuidElement>(),
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
                    Handles = new List<EvaluateArgumentGuidElement>(),
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
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

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
                    Handles = new List<EvaluateArgumentGuidElement>(),
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
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

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
            };

            if (timeout != null)
            {
                param["timeout"] = timeout;
            }

            if (url != null)
            {
                param["url"] = url;
            }

            if (waitUntil != null)
            {
                param["waitUntil"] = waitUntil;
            }

            return Scope.SendMessageToServer<ResponseChannel>(Guid, "waitForNavigation", param);
        }

        internal Task WaitForLoadStateAsync(LifecycleEvent? state, int? timeout, bool isPage)
        {
            var param = new Dictionary<string, object>
            {
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                param["timeout"] = timeout;
            }

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
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "setContent",
                args);
        }

        internal Task ClickAsync(
            string selector,
            int delay,
            MouseButton button,
            int clickCount,
            Modifier[] modifiers,
            Point? position,
            int? timeout,
            bool force,
            bool noWaitAfter,
            bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["delay"] = delay,
                ["button"] = button,
                ["clickCount"] = clickCount,
                ["force"] = force,
                ["noWaitAfter"] = noWaitAfter,
                ["isPage"] = isPage,
            };

            if (modifiers != null)
            {
                args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            }

            if (position != null)
            {
                args["position"] = position;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "click", args);
        }

        internal Task DoubleClickAsync(
            string selector,
            int delay,
            MouseButton button,
            Modifier[] modifiers,
            Point? position,
            int? timeout,
            bool force,
            bool noWaitAfter,
            bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["delay"] = delay,
                ["button"] = button,
                ["force"] = force,
                ["noWaitAfter"] = noWaitAfter,
                ["position"] = position,
                ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "dblclick", args);
        }

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

        internal Task FillAsync(string selector, string value, int? timeout, bool noWaitAfter, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["value"] = value,
                ["noWaitAfter"] = noWaitAfter,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer(Guid, "fill", args);
        }

        internal Task CheckAsync(string selector, int? timeout, bool force, bool noWaitAfter, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["force"] = force,
                ["noWaitAfter"] = noWaitAfter,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "check", args);
        }

        internal Task UncheckAsync(string selector, int? timeout, bool force, bool noWaitAfter, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["force"] = force,
                ["noWaitAfter"] = noWaitAfter,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "uncheck", args);
        }

        internal Task DispatchEventAsync(string selector, string type, object eventInit, int? timeout, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["type"] = type,
                ["eventInit"] = eventInit,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer(Guid, "dispatchEvent", args);
        }

        internal Task HoverAsync(string selector, Point? position, Modifier[] modifiers, bool force, int? timeout, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["position"] = position,
                ["modifiers"] = modifiers,
                ["force"] = force,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer(Guid, "hover", args);
        }

        internal Task<string[]> PressAsync(string selector, string text, int delay, bool? noWaitAfter, int? timeout, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["key"] = text,
                ["delay"] = delay,
                ["noWaitAfter"] = noWaitAfter,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<string[]>(Guid, "press", args);
        }

        internal Task<string[]> SelectOptionAsync(string selector, object values, bool? noWaitAfter, int? timeout, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["values"] = values,
                ["noWaitAfter"] = noWaitAfter,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<string[]>(Guid, "selectOption", args);
        }

        internal Task<string> GetAttributeAsync(string selector, string name, int? timeout, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["name"] = name,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<string>(Guid, "getAttribute", args);
        }

        internal Task<string> GetInnerHtmlAsync(string selector, int? timeout, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<string>(Guid, "innerHTML", args);
        }

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
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer(Guid, "focus", args);
        }

        internal Task<string> GetInnerTextAsync(string selector, int? timeout, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<string>(Guid, "innerText", args);
        }

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
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["isPage"] = isPage,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<string>(Guid, "textContent", args);
        }
    }
}
