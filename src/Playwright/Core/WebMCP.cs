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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class WebMCP : IWebMCP
{
    private readonly Frame _frame;

    public WebMCP(Frame frame)
    {
        _frame = frame;
    }

    public async Task<IReadOnlyList<WebMCPTool>> ToolsAsync(WebMCPToolsOptions? options = null)
    {
        var result = await _frame.SendMessageToServerAsync("webmcpTools", timeout: _frame.Timeout(options?.Timeout)).ConfigureAwait(false);
        return result!.Value.GetProperty("tools").ToObject<List<WebMCPTool>>(_frame._connection.DefaultJsonSerializerOptions).AsReadOnly();
    }

    public async Task<T> CallToolAsync<T>(string name, object? input = null, WebMCPCallToolOptions? options = null)
    {
        var result = await _frame.SendMessageToServerAsync(
            "webmcpCallTool",
            new Dictionary<string, object?>
            {
                ["name"] = name,
                ["input"] = ScriptsHelper.SerializedArgument(input),
            },
            timeout: _frame.Timeout(options?.Timeout)).ConfigureAwait(false);
        return ScriptsHelper.ParseEvaluateResult<T>(result!.Value.GetProperty("result"));
    }
}
