/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core;

internal class APIResponseAssertions : IAPIResponseAssertions
{
    private readonly APIResponse _actual;
    private readonly bool _isNot;

    public APIResponseAssertions(IAPIResponse response, bool isNot)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }
        _actual = (APIResponse)response;
        _isNot = isNot;
    }

    public IAPIResponseAssertions Not => new APIResponseAssertions(_actual, !_isNot);

    public async Task ToBeOKAsync()
    {
        if (_actual.Ok == !_isNot)
        {
            return;
        }
        var message = $"Response status expected to be within [200..299] range, was {_actual.Status}";
        if (!_isNot)
        {
            message = message.Replace("expected to", "expected not to");
        }
        var logList = await _actual.FetchLogAsync().ConfigureAwait(false);
        var log = string.Join("\n", logList);
        if (logList.Count > 0)
        {
            message += $"\nCall log:\n{log}";
        }

        var contentType = _actual.Headers.TryGetValue("content-type", out var contentTypeValue) ? contentTypeValue : string.Empty;
        bool isTextEncoding = IsTextualMimeType(contentType);
        var responseText = string.Empty;
        if (isTextEncoding)
        {
            var text = await _actual.TextAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(text))
            {
                var trimmedText = text.Length > 1000 ? text.Substring(0, 1000) : text;
                responseText = $"\nResponse text:\n{trimmedText}";
            }
        }
        throw new PlaywrightException(message + log + responseText);
    }

    private static bool IsTextualMimeType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }
        return new Regex("^(text/.*?|application/(json|(x-)?javascript|xml.*?|ecmascript|graphql|x-www-form-urlencoded)|image/svg(\\+xml)?|application/.*?(\\+json|\\+xml))(;\\s*charset=.*)?$").Matches(contentType).Count > 0;
    }
}
