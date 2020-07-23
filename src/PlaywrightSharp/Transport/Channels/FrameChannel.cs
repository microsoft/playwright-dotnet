using System;
using System.Collections.Generic;
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

        internal Task<ResponseChannel> GoToAsync(string url, GoToOptions options)
            => Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "goto",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["options"] = options ?? new GoToOptions(),
                });

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

        internal Task WaitForLoadStateAsync(LifecycleEvent lifeCycleEvent, int? timeout, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "waitForLoadState",
                new Dictionary<string, object>
                {
                    ["state"] = lifeCycleEvent,
                    ["timeout"] = timeout,
                    ["isPage"] = isPage,
                });

        internal Task SetcontentAsync(string html, NavigationOptions options, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "waitForLoadState",
                new Dictionary<string, object>
                {
                    ["html"] = html,
                    ["waitUntil"] = options?.WaitUntil,
                    ["timeout"] = options?.Timeout,
                    ["isPage"] = isPage,
                });

        internal Task ClickAsync(string selector, ClickOptions options, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "click",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["delay"] = options?.Delay,
                    ["button"] = options?.Button,
                    ["clickCount"] = options?.ClickCount,
                    ["force"] = options?.Force,
                    ["timeout"] = options?.Timeout,
                    ["noWaitAfter"] = options?.NoWaitAfter,
                    ["isPage"] = isPage,
                });

        internal Task<ElementHandleChannel> QuerySelectorAsync(string selector, bool isPage)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "$",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["isPage"] = isPage,
                });
    }
}
