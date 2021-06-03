using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Converters;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class WorkerChannel : Channel<Worker>
    {
        public WorkerChannel(string guid, Connection connection, Worker owner) : base(guid, connection, owner)
        {
        }

        internal event EventHandler Closed;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(string script, object arg)
            => Connection.SendMessageToServerAsync<JSHandleChannel>(
                Guid,
                "evaluateExpressionHandle",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["arg"] = arg,
                });

        internal Task<JsonElement?> EvaluateExpressionAsync(
            string script,
            object arg)
        {
            return Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "evaluateExpression",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["arg"] = arg,
                });
        }
    }
}
