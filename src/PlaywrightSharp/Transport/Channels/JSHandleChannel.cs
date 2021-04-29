using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class JSHandleChannel : Channel<JSHandle>
    {
        public JSHandleChannel(string guid, Connection connection, JSHandle owner) : base(guid, connection, owner)
        {
        }

        internal Task<JsonElement?> EvaluateExpressionAsync(string script, bool isFunction, object arg)
            => Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "evaluateExpression",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = ScriptsHelper.SerializedArgument(arg),
                });

        internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(string script, bool isFunction, object arg)
            => Connection.SendMessageToServerAsync<JSHandleChannel>(
                Guid,
                "evaluateExpressionHandle",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = ScriptsHelper.SerializedArgument(arg),
                });

        internal Task<JsonElement> JsonValueAsync() => Connection.SendMessageToServerAsync<JsonElement>(Guid, "jsonValue", null);

        internal Task DisposeAsync() => Connection.SendMessageToServerAsync(Guid, "dispose", null);

        internal Task<JSHandleChannel> GetPropertyAsync(string propertyName)
            => Connection.SendMessageToServerAsync<JSHandleChannel>(
                Guid,
                "getProperty",
                new Dictionary<string, object>
                {
                    ["name"] = propertyName,
                });

        internal async Task<List<JSElementProperty>> GetPropertiesAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "getPropertyList", null).ConfigureAwait(false))?
                .GetProperty("properties").ToObject<List<JSElementProperty>>(Connection.GetDefaultJsonSerializerOptions());

        internal class JSElementProperty
        {
            public string Name { get; set; }

            public JSHandleChannel Value { get; set; }
        }
    }
}
