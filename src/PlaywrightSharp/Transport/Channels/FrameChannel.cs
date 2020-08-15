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

        internal Task<ResponseChannel> GoToAsync(string url, GoToOptions options, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["referrer"] = options?.Referer,
                ["timeout"] = options?.Timeout,
                ["isPage"] = isPage,
            };

            if (options?.WaitUntil != null)
            {
                args["waitUntil"] = options.WaitUntil;
            }

            return Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "goto",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["options"] = options ?? new GoToOptions(),
                });
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
            WaitForFunctionPollingOption? polling,
            int? pollingInterval,
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
                "waitForFunction",
                new Dictionary<string, object>
                {
                    ["expression"] = expression,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["timeout"] = timeout,
                    ["polling"] = polling,
                    ["pollingInterval"] = pollingInterval,
                    ["isPage"] = isPage,
                },
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

        internal Task<ElementHandleChannel> WaitForSelector(string selector, WaitForSelectorOptions options, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "waitForSelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["timeout"] = options.Timeout,
                    ["state"] = options.State,
                    ["isPage"] = isPage,
                });

        internal Task<ElementHandleChannel> AddScriptTagAsync(AddTagOptions options, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "addScriptTag",
                new Dictionary<string, object>
                {
                    ["url"] = options.Url,
                    ["path"] = options.Path,
                    ["content"] = options.Content,
                    ["type"] = options.Type,
                    ["isPage"] = isPage,
                });

        internal Task<ElementHandleChannel> AddStyleTagAsync(AddTagOptions options, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "addStyleTag",
                new Dictionary<string, object>
                {
                    ["url"] = options.Url,
                    ["path"] = options.Path,
                    ["content"] = options.Content,
                    ["type"] = options.Type,
                    ["isPage"] = isPage,
                });

        internal Task WaitForLoadStateAsync(LifecycleEvent state, int? timeout, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "waitForLoadState",
                new Dictionary<string, object>
                {
                    ["state"] = state.ToValueString(),
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task SetcontentAsync(string html, NavigationOptions options, bool isPage)
        {
            var args = new Dictionary<string, object>
            {
                ["html"] = html,
                ["timeout"] = options?.Timeout,
                ["isPage"] = isPage,
            };

            if (options != null)
            {
                args["waitUntil"] = options.WaitUntil;
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

        internal Task<ResponseChannel> WaitForNavigationAsync(WaitForNavigationOptions options, bool isPage)
        {
            var param = new Dictionary<string, object>
            {
                ["isPage"] = isPage,
            };

            if (options?.WaitUntil != null)
            {
                param["waitUntil"] = options.WaitUntil?.ToValueString();
            }

            return Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "waitForNavigation",
                param);
        }

        internal Task CheckAsync(string selector, CheckOptions options, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "check",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["force"] = options?.Force,
                    ["timeout"] = options?.Timeout,
                    ["noWaitAfter"] = options?.NoWaitAfter,
                    ["isPage"] = isPage,
                });

        internal Task UncheckAsync(string selector, CheckOptions options, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "uncheck",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["force"] = options?.Force,
                    ["timeout"] = options?.Timeout,
                    ["noWaitAfter"] = options?.NoWaitAfter,
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
