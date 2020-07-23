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

        internal Task<string> GetTitleAsync() => Scope.SendMessageToServer<string>(Guid, "title", null);
    }
}
