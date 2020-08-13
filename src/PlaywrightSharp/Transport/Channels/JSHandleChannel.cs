using System;
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

        internal Task<JsonElement> GetJsonValue() => Scope.SendMessageToServer<JsonElement>(Guid, "jsonValue", null);

        internal Task DisposeAsync() => Scope.SendMessageToServer(Guid, "dispose", null);

        internal Task<JSHandleChannel> GetPropertyAsync(string propertyName)
            => Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "getProperty",
                new Dictionary<string, object>
                {
                    ["name"] = propertyName,
                });

        internal Task<List<JSElementProperty>> GetPropertiesAsync()
            => Scope.SendMessageToServer<List<JSElementProperty>>(Guid, "getPropertyList", null);

        internal class JSElementProperty
        {
            public string Name { get; set; }

            public JSHandleChannel Value { get; set; }
        }
    }
}
