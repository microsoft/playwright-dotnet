using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

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

        internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(string script, bool isFunction, object arg, bool isPage)
            => Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "evaluateExpressionHandle",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["isPage"] = isPage,
                });

        internal Task<JsonElement?> EvaluateExpressionAsync(string script, bool isFunction, EvaluateArgument arg, bool isPage)
            => Scope.SendMessageToServer<JsonElement?>(
                Guid,
                "evaluateExpression",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                    ["isPage"] = isPage,
                });

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
    }
}
