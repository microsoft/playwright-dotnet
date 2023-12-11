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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class ElementHandle : JSHandle, IElementHandle
{
    internal ElementHandle(ChannelOwner parent, string guid, ElementHandleInitializer initializer) : base(parent, guid, initializer)
    {
    }

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "previewUpdated":
                Preview = serverParams.Value.GetProperty("preview").ToString();
                break;
        }
    }

    public async Task<IElementHandle> WaitForSelectorAsync(string selector, ElementHandleWaitForSelectorOptions options = default)
        => await SendMessageToServerAsync<ElementHandle>(
            "waitForSelector",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["timeout"] = options?.Timeout,
                ["state"] = options?.State,
                ["strict"] = options?.Strict,
            }).ConfigureAwait(false);

    public Task WaitForElementStateAsync(ElementState state, ElementHandleWaitForElementStateOptions options = default)
        => SendMessageToServerAsync("waitForElementState", new Dictionary<string, object>
        {
            ["state"] = state,
            ["timeout"] = options?.Timeout,
        });

    public Task PressAsync(string key, ElementHandlePressOptions options = default)
        => SendMessageToServerAsync("press", new Dictionary<string, object>
        {
            ["key"] = key,
            ["delay"] = options?.Delay,
            ["timeout"] = options?.Timeout,
            ["noWaitAfter"] = options?.NoWaitAfter,
        });

    public Task TypeAsync(string text, ElementHandleTypeOptions options = default)
        => SendMessageToServerAsync("type", new Dictionary<string, object>
        {
            ["text"] = text,
            ["delay"] = options?.Delay,
            ["timeout"] = options?.Timeout,
            ["noWaitAfter"] = options?.NoWaitAfter,
        });

    public async Task<byte[]> ScreenshotAsync(ElementHandleScreenshotOptions options = default)
    {
        options ??= new();
        if (options.Type == null && !string.IsNullOrEmpty(options.Path))
        {
            options.Type = DetermineScreenshotType(options.Path);
        }

        var args = new Dictionary<string, object>
        {
            ["type"] = options.Type,
            ["omitBackground"] = options.OmitBackground,
            ["path"] = options.Path,
            ["timeout"] = options.Timeout,
            ["animations"] = options.Animations,
            ["caret"] = options.Caret,
            ["scale"] = options.Scale,
            ["quality"] = options.Quality,
            ["maskColor"] = options.MaskColor,
        };
        if (options.Mask != null)
        {
            args["mask"] = options.Mask.Select(locator => new Dictionary<string, object>
            {
                ["frame"] = ((Locator)locator)._frame,
                ["selector"] = ((Locator)locator)._selector,
            }).ToArray();
        }

        var result = (await SendMessageToServerAsync("screenshot", args).ConfigureAwait(false))?.GetProperty("binary").GetBytesFromBase64();

        if (!string.IsNullOrEmpty(options.Path))
        {
            Directory.CreateDirectory(new FileInfo(options.Path).Directory.FullName);
            File.WriteAllBytes(options.Path, result);
        }

        return result;
    }

    public Task FillAsync(string value, ElementHandleFillOptions options = default)
        => SendMessageToServerAsync("fill", new Dictionary<string, object>
        {
            ["value"] = value,
            ["timeout"] = options?.Timeout,
            ["force"] = options?.Force,
            ["noWaitAfter"] = options?.NoWaitAfter,
        });

    public async Task<IFrame> ContentFrameAsync() => await SendMessageToServerAsync<Frame>("contentFrame").ConfigureAwait(false);

    public Task HoverAsync(ElementHandleHoverOptions options = default)
        => SendMessageToServerAsync<JsonElement?>("hover", new Dictionary<string, object>
        {
            ["force"] = options?.Force,
            ["position"] = options?.Position,
            ["timeout"] = options?.Timeout,
            ["trial"] = options?.Trial,
            ["modifiers"] = options?.Modifiers?.Select(m => m.ToValueString()),
            ["noWaitAfter"] = options?.NoWaitAfter,
        });

    public Task ScrollIntoViewIfNeededAsync(ElementHandleScrollIntoViewIfNeededOptions options = default)
        => SendMessageToServerAsync("scrollIntoViewIfNeeded", new Dictionary<string, object>
        {
            ["timeout"] = options?.Timeout,
        });

    public async Task<IFrame> OwnerFrameAsync() => await SendMessageToServerAsync<Frame>("ownerFrame").ConfigureAwait(false);

    public async Task<ElementHandleBoundingBoxResult> BoundingBoxAsync()
    {
        var result = (await SendMessageToServerAsync("boundingBox").ConfigureAwait(false)).Value;
        if (result.TryGetProperty("value", out var value))
        {
            return value.ToObject<ElementHandleBoundingBoxResult>();
        }
        return null;
    }

    public Task ClickAsync(ElementHandleClickOptions options = default)
        => SendMessageToServerAsync("click", new Dictionary<string, object>
        {
            ["delay"] = options?.Delay,
            ["button"] = options?.Button,
            ["clickCount"] = options?.ClickCount,
            ["force"] = options?.Force,
            ["noWaitAfter"] = options?.NoWaitAfter,
            ["timeout"] = options?.Timeout,
            ["trial"] = options?.Trial,
            ["position"] = options?.Position,
            ["modifiers"] = options?.Modifiers?.Select(m => m.ToValueString()),
        });

    public Task DblClickAsync(ElementHandleDblClickOptions options = default)
        => SendMessageToServerAsync("dblclick", new Dictionary<string, object>
        {
            ["delay"] = options?.Delay,
            ["button"] = options?.Button,
            ["force"] = options?.Force,
            ["noWaitAfter"] = options?.NoWaitAfter,
            ["timeout"] = options?.Timeout,
            ["trial"] = options?.Trial,
            ["position"] = options?.Position,
            ["modifiers"] = options?.Modifiers?.Select(m => m.ToValueString()),
        });

    public Task SetInputFilesAsync(string files, ElementHandleSetInputFilesOptions options = default)
        => SetInputFilesAsync(new[] { files }, options);

    public async Task SetInputFilesAsync(IEnumerable<string> files, ElementHandleSetInputFilesOptions options = default)
    {
        var frame = await OwnerFrameAsync().ConfigureAwait(false);
        if (frame == null)
        {
            throw new PlaywrightException("Cannot set input files to detached element.");
        }
        var converted = await SetInputFilesHelpers.ConvertInputFilesAsync(files, (BrowserContext)frame.Page.Context).ConfigureAwait(false);
        await SendMessageToServerAsync("setInputFiles", new Dictionary<string, object>
        {
            ["payloads"] = converted.Payloads,
            ["localPaths"] = converted.LocalPaths,
            ["streams"] = converted.Streams,
            ["timeout"] = options?.Timeout,
            ["noWaitAfter"] = options?.NoWaitAfter,
        }).ConfigureAwait(false);
    }

    public Task SetInputFilesAsync(FilePayload files, ElementHandleSetInputFilesOptions options = default)
        => SetInputFilesAsync(new[] { files }, options);

    public async Task SetInputFilesAsync(IEnumerable<FilePayload> files, ElementHandleSetInputFilesOptions options = default)
    {
        var converted = SetInputFilesHelpers.ConvertInputFiles(files);
        await SendMessageToServerAsync("setInputFiles", new Dictionary<string, object>
        {
            ["payloads"] = converted.Payloads,
            ["localPaths"] = converted.LocalPaths,
            ["streams"] = converted.Streams,
            ["timeout"] = options?.Timeout,
            ["noWaitAfter"] = options?.NoWaitAfter,
        }).ConfigureAwait(false);
    }

    public async Task<IElementHandle> QuerySelectorAsync(string selector)
        => await SendMessageToServerAsync<ElementHandle>(
            "querySelector",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
            }).ConfigureAwait(false);

    public async Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector)
        => (await SendMessageToServerAsync<ElementHandle[]>(
            "querySelectorAll",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
            }).ConfigureAwait(false)).ToList().AsReadOnly();

    public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await SendMessageToServerAsync<JsonElement?>(
            "evalOnSelector",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["expression"] = expression,
                ["arg"] = ScriptsHelper.SerializedArgument(arg),
            }).ConfigureAwait(false));

    public async Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await SendMessageToServerAsync<JsonElement?>(
            "evalOnSelector",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["expression"] = expression,
                ["arg"] = ScriptsHelper.SerializedArgument(arg),
            }).ConfigureAwait(false));

    public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await SendMessageToServerAsync<JsonElement?>(
            "evalOnSelectorAll",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["expression"] = expression,
                ["arg"] = ScriptsHelper.SerializedArgument(arg),
            }).ConfigureAwait(false));

    public Task FocusAsync() => SendMessageToServerAsync("focus");

    public Task DispatchEventAsync(string type, object eventInit = null)
        => SendMessageToServerAsync("dispatchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["eventInit"] = ScriptsHelper.SerializedArgument(eventInit),
        });

    public async Task<string> GetAttributeAsync(string name)
    {
        if ((await SendMessageToServerAsync("getAttribute", new Dictionary<string, object>
        {
            ["name"] = name,
        }).ConfigureAwait(false))?.TryGetProperty("value", out var value) ?? false)
        {
            return value.ToString();
        }
        return null;
    }

    public async Task<string> InnerHTMLAsync() => (await SendMessageToServerAsync("innerHTML").ConfigureAwait(false))?.GetProperty("value").ToString();

    public async Task<string> InnerTextAsync() => (await SendMessageToServerAsync("innerText").ConfigureAwait(false))?.GetProperty("value").ToString();

    public async Task<string> TextContentAsync() => (await SendMessageToServerAsync("textContent").ConfigureAwait(false))?.GetProperty("value").ToString();

    public Task SelectTextAsync(ElementHandleSelectTextOptions options = default)
        => SendMessageToServerAsync("selectText", new Dictionary<string, object>
        {
            ["force"] = options?.Force,
            ["timeout"] = options?.Timeout,
        });

    public Task<IReadOnlyList<string>> SelectOptionAsync(string value, ElementHandleSelectOptionOptions options = default)
        => _selectOptionAsync(new[] { new SelectOptionValueProtocol() { ValueOrLabel = value } }, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(IElementHandle values, ElementHandleSelectOptionOptions options = default)
        => _selectOptionAsync(new[] { values }, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, ElementHandleSelectOptionOptions options = default)
        => _selectOptionAsync(values.Select(x => new SelectOptionValueProtocol() { ValueOrLabel = x }), options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, ElementHandleSelectOptionOptions options = default)
        => _selectOptionAsync(new[] { SelectOptionValueProtocol.From(values) }, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, ElementHandleSelectOptionOptions options = default)
        => _selectOptionAsync(values, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, ElementHandleSelectOptionOptions options = default)
        => _selectOptionAsync(values.Select(v => SelectOptionValueProtocol.From(v)), options?.NoWaitAfter, options?.Force, options?.Timeout);

    private async Task<IReadOnlyList<string>> _selectOptionAsync(IEnumerable<SelectOptionValueProtocol> values, bool? noWaitAfter, bool? force, float? timeout)
    {
        return (await SendMessageToServerAsync("selectOption", new Dictionary<string, object>
        {
            ["options"] = values,
            ["noWaitAfter"] = noWaitAfter,
            ["force"] = force,
            ["timeout"] = timeout,
        }).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
    }

    private async Task<IReadOnlyList<string>> _selectOptionAsync(IEnumerable<IElementHandle> values, bool? noWaitAfter, bool? force, float? timeout)
    {
        return (await SendMessageToServerAsync("selectOption", new Dictionary<string, object>
        {
            ["elements"] = values,
            ["noWaitAfter"] = noWaitAfter,
            ["force"] = force,
            ["timeout"] = timeout,
        }).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
    }

    public Task CheckAsync(ElementHandleCheckOptions options = default)
        => SendMessageToServerAsync("check", new Dictionary<string, object>
        {
            ["force"] = options?.Force,
            ["position"] = options?.Position,
            ["trial"] = options?.Trial,
            ["timeout"] = options?.Timeout,
            ["noWaitAfter"] = options?.NoWaitAfter,
        });

    public Task UncheckAsync(ElementHandleUncheckOptions options = default)
        => SendMessageToServerAsync("uncheck", new Dictionary<string, object>
        {
            ["force"] = options?.Force,
            ["position"] = options?.Position,
            ["trial"] = options?.Trial,
            ["timeout"] = options?.Timeout,
            ["noWaitAfter"] = options?.NoWaitAfter,
        });

    public Task TapAsync(ElementHandleTapOptions options = default)
        => SendMessageToServerAsync("tap", new Dictionary<string, object>
        {
            ["force"] = options?.Force,
            ["noWaitAfter"] = options?.NoWaitAfter,
            ["position"] = options?.Position,
            ["modifiers"] = options?.Modifiers?.Select(m => m.ToValueString()),
            ["trial"] = options?.Trial,
            ["timeout"] = options?.Timeout,
        });

    public async Task<bool> IsCheckedAsync() => (await SendMessageToServerAsync("isChecked").ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    public async Task<bool> IsDisabledAsync() => (await SendMessageToServerAsync("isDisabled").ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    public async Task<bool> IsEditableAsync() => (await SendMessageToServerAsync("isEditable").ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    public async Task<bool> IsEnabledAsync() => (await SendMessageToServerAsync("isEnabled").ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    public async Task<bool> IsHiddenAsync() => (await SendMessageToServerAsync("isHidden").ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    public async Task<bool> IsVisibleAsync() => (await SendMessageToServerAsync("isVisible").ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    public async Task<string> InputValueAsync(ElementHandleInputValueOptions options = null)
        => (await SendMessageToServerAsync("inputValue", new Dictionary<string, object>()
            {
                { "timeout", options?.Timeout },
            }).ConfigureAwait(false))?.GetProperty("value").GetString();

    public Task SetCheckedAsync(bool checkedState, ElementHandleSetCheckedOptions options = null)
        => SendMessageToServerAsync(checkedState ? "check" : "uncheck", new Dictionary<string, object>
        {
            ["force"] = options?.Force,
            ["position"] = options?.Position,
            ["trial"] = options?.Trial,
            ["timeout"] = options?.Timeout,
            ["noWaitAfter"] = options?.NoWaitAfter,
        });

    internal static ScreenshotType DetermineScreenshotType(string path)
    {
        string mimeType = path.MimeType();
        return mimeType switch
        {
            "image/png" => ScreenshotType.Png,
            "image/jpeg" => ScreenshotType.Jpeg,
            _ => throw new ArgumentException($"path: unsupported mime type \"{mimeType}\""),
        };
    }
}
