/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Text.Json;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright.Tests;

/// <summary>
/// Regression coverage for expect errorDetails attached to Exception.Data.
/// On .NET Framework, Exception.Data rejects non-serializable values (e.g. JsonElement),
/// which used to orphan the callback and kill the driver connection (issue #3342).
/// </summary>
public class ConnectionTests
{
    [Test]
    public async Task Dispatch_ExpectErrorDetails_MustBeSerializableForExceptionData()
    {
        var connection = new Connection();
        var errorDetailsJson = "{\"received\":{\"value\":{\"v\":\"n\",\"n\":0}},\"customErrorMessage\":\"nope\"}";
        using var errorDetailsDocument = JsonDocument.Parse(errorDetailsJson);
        var errorDetails = errorDetailsDocument.RootElement.Clone();

        connection.OnMessage = (message, _) =>
        {
            var dict = (Dictionary<string, object>)message;
            var id = (int)dict["id"];
            connection.Dispatch(new PlaywrightServerMessage
            {
                Id = id,
                Error = new ErrorEntry
                {
                    Error = new PlaywrightServerError
                    {
                        Message = "Error: expect failed",
                        Name = "Error",
                    },
                },
                ErrorDetails = errorDetails,
                Log = new[] { "waiting for locator" },
            });
            return Task.CompletedTask;
        };

        var sendTask = connection.SendMessageToServerAsync(
            null,
            "expect",
            new Dictionary<string, object> { ["selector"] = "#missing" });

        var completed = await Task.WhenAny(sendTask, Task.Delay(5000));
        Assert.AreSame(sendTask, completed, "expect error callback must complete (must not hang after Dispatch)");

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => sendTask);
        StringAssert.Contains("expect failed", exception.Message);

        Assert.True(exception.Data.Contains(Connection.ErrorDetailsDataKey));
        var detailsValue = exception.Data[Connection.ErrorDetailsDataKey];
        // .NET Framework Exception.Data requires serializable values. JsonElement is not serializable.
        Assert.That(detailsValue, Is.Null.Or.TypeOf<string>(),
            "ErrorDetails must be stored as a serializable string, not JsonElement");

        if (detailsValue is string detailsText)
        {
            StringAssert.Contains("received", detailsText);
            StringAssert.Contains("customErrorMessage", detailsText);
        }

        Assert.True(exception.Data.Contains(Connection.LogDataKey));
        Assert.That(exception.Data[Connection.LogDataKey], Is.Null.Or.TypeOf<string[]>());
    }
}
