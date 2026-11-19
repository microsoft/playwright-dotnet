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
    private readonly string? _customMessage;

    public APIResponseAssertions(IAPIResponse response, bool isNot, string? customMessage = null)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }
        _actual = (APIResponse)response;
        _isNot = isNot;
        _customMessage = customMessage;
    }

    public IAPIResponseAssertions Not => new APIResponseAssertions(_actual, !_isNot, _customMessage);

    public async Task ToBeOKAsync()
    {
        if (_actual.Ok == !_isNot)
        {
            return;
        }
        var contentType = _actual.Headers.TryGetValue("content-type", out var contentTypeValue) ? contentTypeValue : string.Empty;
        var isTextEncoding = IsTextualMimeType(contentType);
        var log = await _actual.FetchLogAsync().ConfigureAwait(false);
        var text = isTextEncoding ? await _actual.TextAsync().ConfigureAwait(false) : null;

        var message = MatcherHint.FormatMatcherMessage(new()
        {
            IsNot = _isNot,
            MatcherName = "ToBeOKAsync",
            Receiver = "response",
            Expectation = string.Empty,
            Log = log,
        });
        if (isTextEncoding)
        {
            message += $"\nResponse text:\n{(text?.Length > 1000 ? text.Substring(0, 1000) : text)}";
        }
        if (!string.IsNullOrEmpty(_customMessage))
        {
            message = _customMessage + "\n\n" + message;
        }
        throw new PlaywrightAssertionException(message);
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
