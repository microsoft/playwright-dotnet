using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class JSHandleChannel : Channel<JSHandle>
    {
        public JSHandleChannel(string guid, ConnectionScope scope, JSHandle owner) : base(guid, scope, owner)
        {
        }

        internal Task<JsonElement?> EvaluateExpressionAsync(string script, bool isFunction, EvaluateArgument arg)
            => Scope.SendMessageToServer<JsonElement?>(
                Guid,
                "evaluateExpression",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                });
    }
}
