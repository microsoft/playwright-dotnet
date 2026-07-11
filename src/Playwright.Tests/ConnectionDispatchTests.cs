using System.Text.Json;
using Microsoft.Playwright.Transport;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class ConnectionDispatchTests
{
    [Test]
    public void CreateRemoteObjectWithUnknownTypeShouldCloseConnection()
    {
        using var connection = new Connection();
        connection.OnMessage = (_, _) => Task.CompletedTask;
        Exception? closeException = null;
        connection.Close += (_, ex) => closeException = ex;

        // "-1" is not a recognized ChannelOwnerType, so ToObject<ChannelOwnerType>
        // throws JsonException which Dispatch catches and calls DoClose.
        var message = new PlaywrightServerMessage
        {
            Method = "__create__",
            Guid = "test-guid",
            Params = JsonDocument.Parse("""
            {
                "type": "-1",
                "guid": "child-guid",
                "initializer": {}
            }
            """).RootElement,
        };

        // Dispatch catches the exception internally and calls DoClose.
        Assert.DoesNotThrow(() => connection.Dispatch(message));
        Assert.NotNull(closeException);
    }

    [Test]
    public void CreateRemoteObjectShouldHandleElectronAndAndroidAsNull()
    {
        using var connection = new Connection();
        connection.OnMessage = (_, _) => Task.CompletedTask;

        // Electron and Android are explicitly mapped to null (not yet supported)
        foreach (var typeName in new[] { "Electron", "Android" })
        {
            var message = new PlaywrightServerMessage
            {
                Method = "__create__",
                Guid = "test-guid",
                Params = JsonDocument.Parse($$"""
                {
                    "type": "{{typeName}}",
                    "guid": "child-guid",
                    "initializer": {}
                }
                """).RootElement,
            };

            Assert.DoesNotThrow(() => connection.Dispatch(message));
        }
    }

    [Test]
    public void DoCloseWithNullOrEmptyStringCauseShouldCreateTargetClosedException()
    {
        using var connection = new Connection();
        Exception? closeException = null;
        connection.Close += (_, ex) => closeException = ex;

        // Test null cause
        connection.DoClose(cause: (string?)null);
        Assert.NotNull(closeException);
        Assert.IsInstanceOf<TargetClosedException>(closeException);

        // Reset
        closeException = null;
        using var connection2 = new Connection();
        connection2.Close += (_, ex) => closeException = ex;

        // Test empty string cause
        connection2.DoClose(cause: string.Empty);
        Assert.NotNull(closeException);
        Assert.IsInstanceOf<TargetClosedException>(closeException);
    }

    [Test]
    public void DoCloseShouldCloseConnectionAndRejectSubsequentSends()
    {
        using var connection = new Connection();
        connection.OnMessage = (_, _) => Task.CompletedTask;
        connection.DoClose(cause: "test reason");

        var ex = Assert.ThrowsAsync<TargetClosedException>(() =>
            connection.SendMessageToServerAsync<JsonElement?>(null, "test"));

        Assert.That(ex!.Message, Does.Contain("test reason"));
    }

    [Test]
    public void DoCloseWithExceptionShouldCloseConnectionWithTargetClosedException()
    {
        using var connection = new Connection();
        connection.OnMessage = (_, _) => Task.CompletedTask;
        connection.DoClose(cause: new InvalidOperationException("inner error"));

        var ex = Assert.ThrowsAsync<TargetClosedException>(() =>
            connection.SendMessageToServerAsync<JsonElement?>(null, "test"));

        Assert.NotNull(ex);
    }

    [Test]
    public void SendAfterCloseShouldThrowClosedError()
    {
        using var connection = new Connection();
        connection.OnMessage = (_, _) => Task.CompletedTask;
        connection.DoClose(cause: "closed");

        var ex = Assert.ThrowsAsync<TargetClosedException>(() =>
            connection.SendMessageToServerAsync<JsonElement?>(null, "test"));

        Assert.That(ex!.Message, Does.Contain("closed"));
    }

    [Test]
    public void DispatchWithUnknownIdShouldThrow()
    {
        using var connection = new Connection();
        connection.OnMessage = (_, _) => Task.CompletedTask;

        var message = new PlaywrightServerMessage
        {
            Id = 99999,
        };

        var ex = Assert.Throws<PlaywrightException>(() => connection.Dispatch(message));
        Assert.That(ex!.Message, Does.Contain("Cannot find command to respond"));
    }

    [Test]
    public void RedactTraceMessageShouldHandleMalformedJsonGracefully()
    {
        var result = Connection.RedactTraceMessage("{invalid json here");

        Assert.AreEqual("{invalid json here", result);
    }

    [Test]
    public void RedactTraceMessageShouldRedactNestedSensitiveKeys()
    {
        var redacted = Connection.RedactTraceMessage("""
        {
            "params": {
                "credentials": { "username": "admin", "password": "secret123" },
                "storageState": "/path/to/state.json"
            }
        }
        """);

        using var doc = JsonDocument.Parse(redacted);
        var p = doc.RootElement.GetProperty("params");

        Assert.AreEqual("***REDACTED***", p.GetProperty("credentials").GetString());
        Assert.AreEqual("***REDACTED***", p.GetProperty("storageState").GetString());
    }

    [Test]
    public void RedactTraceMessageShouldRedactNumericSensitiveValues()
    {
        var redacted = Connection.RedactTraceMessage("""
        {
            "params": {
                "password": 12345,
                "apiKey": 999887766,
                "safeField": "visible"
            }
        }
        """);

        using var doc = JsonDocument.Parse(redacted);
        var p = doc.RootElement.GetProperty("params");

        Assert.AreEqual("***REDACTED***", p.GetProperty("password").GetString());
        Assert.AreEqual("***REDACTED***", p.GetProperty("apiKey").GetString());
        Assert.AreEqual("visible", p.GetProperty("safeField").GetString());
    }

    [Test]
    public void RedactTraceMessageShouldRedactArraySensitiveValues()
    {
        var redacted = Connection.RedactTraceMessage("""
        {
            "params": {
                "token": ["secret1", "secret2"],
                "safeField": [1, 2, 3]
            }
        }
        """);

        using var doc = JsonDocument.Parse(redacted);
        var p = doc.RootElement.GetProperty("params");

        Assert.AreEqual("***REDACTED***", p.GetProperty("token").GetString());
        Assert.AreEqual(3, p.GetProperty("safeField").GetArrayLength());
    }

    [Test]
    public void RedactTraceMessageShouldRedactObjectSensitiveValues()
    {
        var redacted = Connection.RedactTraceMessage("""
        {
            "params": {
                "credentials": { "user": "admin", "password": "s3cr3t" },
                "safeField": { "visible": true }
            }
        }
        """);

        using var doc = JsonDocument.Parse(redacted);
        var p = doc.RootElement.GetProperty("params");

        Assert.AreEqual("***REDACTED***", p.GetProperty("credentials").GetString());
        Assert.True(p.GetProperty("safeField").GetProperty("visible").GetBoolean());
    }

    [Test]
    public void RedactTraceMessageShouldRedactSensitiveKeysCaseInsensitively()
    {
        var redacted = Connection.RedactTraceMessage("""
        {
            "params": {
                "Authorization": "Bearer tok",
                "AUTHORIZATION": "Bearer tok2",
                "safe": "ok"
            }
        }
        """);

        using var doc = JsonDocument.Parse(redacted);
        var p = doc.RootElement.GetProperty("params");

        Assert.AreEqual("***REDACTED***", p.GetProperty("Authorization").GetString());
        Assert.AreEqual("***REDACTED***", p.GetProperty("AUTHORIZATION").GetString());
        Assert.AreEqual("ok", p.GetProperty("safe").GetString());
    }

    [Test]
    public void RedactTraceMessageShouldNotAffectNonSensitiveKeys()
    {
        var original = """{"params":{"url":"https://example.com","method":"GET","headers":{}}}""";
        var redacted = Connection.RedactTraceMessage(original);

        Assert.AreEqual(original, redacted);
    }
}
