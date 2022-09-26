/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
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

using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

internal static class FrameUtils
{
    public static async Task<IFrame> AttachFrameAsync(IPage page, string frameId, string url)
    {
        var handle = (IElementHandle)await page.EvaluateHandleAsync(@" async ({frameId, url}) => {
              const frame = document.createElement('iframe');
              frame.src = url;
              frame.id = frameId;
              document.body.appendChild(frame);
              await new Promise(x => frame.onload = x);
              return frame
            }", new { frameId, url });
        return await handle.ContentFrameAsync();
    }

    public static Task DetachFrameAsync(IPage page, string frameId)
    {
        return page.EvaluateAsync(@"function detachFrame(frameId) {
              const frame = document.getElementById(frameId);
              frame.remove();
            }", frameId);
    }

    public static IEnumerable<string> DumpFrames(IFrame frame, string indentation = "")
    {
        string description = indentation + Regex.Replace(frame.Url, @":\d{4}", ":<PORT>");
        if (!string.IsNullOrEmpty(frame.Name))
        {
            description += $" ({frame.Name})";
        }
        var result = new List<string>() { description };
        foreach (var child in frame.ChildFrames)
        {
            result.AddRange(DumpFrames(child, "    " + indentation));
        }

        return result;
    }
}
