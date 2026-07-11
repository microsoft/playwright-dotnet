using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright.Transport;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class ConnectionNormalizeValueTests
{
    [Test]
    public void NormalizeValueShouldRecursivelyNormalizeNestedProtocolArguments()
    {
        using var document = JsonDocument.Parse("""{"ok":true}""");
        var normalized = (JsonObject)Connection.NormalizeValue(new Dictionary<string, object?>
        {
            ["items"] = new object?[]
            {
                1,
                document.RootElement,
                new Dictionary<string, object?> { ["nested"] = "value" },
            },
        })!;

        var items = normalized["items"]!.AsArray();
        Assert.AreEqual(1, items[0]!.GetValue<int>());
        Assert.True(items[1]!["ok"]!.GetValue<bool>());
        Assert.AreEqual("value", items[2]!["nested"]!.GetValue<string>());
    }

    [Test]
    public void NormalizeValueShouldPreserveByteArrayAsBase64String()
    {
        var normalized = (JsonValue)Connection.NormalizeValue(new byte[] { 1, 2, 3 })!;

        Assert.AreEqual("AQID", normalized.GetValue<string>());
    }

    [Test]
    public void NormalizeValueShouldPreserveUnsignedAndSmallNumericPrimitives()
    {
        var normalized = (JsonObject)Connection.NormalizeValue(new Dictionary<string, object?>
        {
            ["sbyte"] = (sbyte)-2,
            ["ushort"] = (ushort)3,
            ["uint"] = 4u,
            ["ulong"] = 5ul,
            ["char"] = 'x',
        })!;

        Assert.AreEqual(-2, normalized["sbyte"]!.GetValue<int>());
        Assert.AreEqual(3, normalized["ushort"]!.GetValue<int>());
        Assert.AreEqual(4u, normalized["uint"]!.GetValue<uint>());
        Assert.AreEqual(5ul, normalized["ulong"]!.GetValue<ulong>());
        Assert.AreEqual("x", normalized["char"]!.GetValue<string>());
    }

    [Test]
    public void NormalizeValueShouldRejectNonStringDictionaryKeys()
    {
        var input = new Dictionary<object, object?>
        {
            ["ok"] = true,
            [1] = "bad",
        };

        var exception = Assert.Throws<PlaywrightException>(() => Connection.NormalizeValue(input));

        StringAssert.Contains("non-string key", exception!.Message);
    }

    [Test]
    public void NormalizeValueShouldRejectCyclicCollections()
    {
        var dictionary = new Dictionary<string, object?>();
        dictionary["self"] = dictionary;

        var list = new List<object?>();
        list.Add(list);

        var dictionaryException = Assert.Throws<PlaywrightException>(() => Connection.NormalizeValue(dictionary));
        var listException = Assert.Throws<PlaywrightException>(() => Connection.NormalizeValue(list));

        StringAssert.Contains("contains a cycle", dictionaryException!.Message);
        StringAssert.Contains("contains a cycle", listException!.Message);
    }

    [Test]
    public void NormalizeValueShouldAllowSharedNonCyclicReferences()
    {
        var shared = new Dictionary<string, object?> { ["value"] = 1 };
        var normalized = ((JsonArray)Connection.NormalizeValue(new object?[] { shared, shared })!);

        Assert.AreEqual(1, normalized[0]!["value"]!.GetValue<int>());
        Assert.AreEqual(1, normalized[1]!["value"]!.GetValue<int>());
    }

    [Test]
    public void NormalizeValueShouldTreatObjectKeyValuePairsAsObject()
    {
        var prefs = new List<KeyValuePair<string, object>>
        {
            new("browser.tabs.warnOnClose", false),
            new("network.proxy.type", 1),
        };

        var normalized = (JsonObject)Connection.NormalizeValue(prefs)!;

        Assert.False(normalized["browser.tabs.warnOnClose"]!.GetValue<bool>());
        Assert.AreEqual(1, normalized["network.proxy.type"]!.GetValue<int>());
    }

    [Test]
    public void NormalizeValueShouldNormalizeNonListEnumerablesAsArrays()
    {
        var input = new Dictionary<string, object?>
        {
            ["queue"] = new Queue<object?>(new object?[]
            {
                1,
                new Dictionary<string, object?> { ["nested"] = true },
            }),
        };

        var normalized = (JsonObject)Connection.NormalizeValue(input)!;
        var queue = normalized["queue"]!.AsArray();

        Assert.AreEqual(1, queue[0]!.GetValue<int>());
        Assert.True(queue[1]!["nested"]!.GetValue<bool>());
    }

    [Test]
    public async Task SendMessageToServerAsyncShouldPreserveTopLevelNullArgsWhenKeepNullsIsTrue()
    {
        using var connection = new Connection();
        Dictionary<string, object?>? sentMessage = null;
        bool? sentKeepNulls = null;

        connection.OnMessage = (message, keepNulls) =>
        {
            sentMessage = (Dictionary<string, object?>)message;
            sentKeepNulls = keepNulls;
            connection.Dispatch(new PlaywrightServerMessage { Id = (int)sentMessage["id"]! });
            return Task.CompletedTask;
        };

        await connection.SendMessageToServerAsync(
            null,
            "test",
            new Dictionary<string, object?>
            {
                ["nullable"] = null,
                ["defined"] = 1,
            },
            keepNulls: true).ConfigureAwait(false);

        var sentParams = (Dictionary<string, object?>)sentMessage!["params"]!;
        Assert.AreEqual(true, sentKeepNulls);
        Assert.True(sentParams.ContainsKey("nullable"));
        Assert.Null(sentParams["nullable"]);
        Assert.AreEqual(1, sentParams["defined"]);
    }

    [Test]
    public void NormalizeValueShouldRejectUnsupportedObjectsInsteadOfStringifyingThem()
    {
        var exception = Assert.Throws<PlaywrightException>(() => Connection.NormalizeValue(new Dictionary<string, object?>
        {
            ["unsupported"] = new UnsupportedProtocolArgument(),
        }));

        StringAssert.Contains("not registered for AOT-safe protocol argument serialization", exception!.Message);
    }

    [Test]
    public void RedactTraceMessageShouldRedactSensitiveJsonValuesStructurally()
    {
        var redacted = Connection.RedactTraceMessage("""
        {
          "method": "fetch",
          "params": {
            "token": { "value": "abc" },
            "headers": [
              { "authorization": "Bearer secret" }
            ],
            "cookie": 123,
            "apiKey": ["one", "two"],
            "safe": "visible"
          }
        }
        """);

        using var document = JsonDocument.Parse(redacted);
        var parameters = document.RootElement.GetProperty("params");
        Assert.AreEqual("***REDACTED***", parameters.GetProperty("token").GetString());
        Assert.AreEqual("***REDACTED***", parameters.GetProperty("headers")[0].GetProperty("authorization").GetString());
        Assert.AreEqual("***REDACTED***", parameters.GetProperty("cookie").GetString());
        Assert.AreEqual("***REDACTED***", parameters.GetProperty("apiKey").GetString());
        Assert.AreEqual("visible", parameters.GetProperty("safe").GetString());
        Assert.False(redacted.Contains("Bearer secret", StringComparison.Ordinal));
        Assert.False(redacted.Contains("\"value\":\"abc\"", StringComparison.Ordinal));
    }

    private sealed class UnsupportedProtocolArgument
    {
        public string Value { get; set; } = "bad";
    }
}
