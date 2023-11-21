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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport.Channels;

internal class ElementHandleChannel : JSHandleChannel, IChannel<ElementHandle>
{
    public ElementHandleChannel(string guid, Connection connection, ElementHandle owner) : base(guid, connection, owner)
    {
        Object = owner;
    }

    internal event EventHandler<string> PreviewUpdated;

    public new ElementHandle Object { get; set; }

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "previewUpdated":
                PreviewUpdated?.Invoke(this, serverParams.Value.GetProperty("preview").ToString());
                break;
        }
    }

    internal Task<ElementHandleChannel> WaitForSelectorAsync(string selector, WaitForSelectorState? state, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["state"] = state,
            ["strict"] = strict,
        };

        return Connection.SendMessageToServerAsync<ElementHandleChannel>(
            Object,
            "waitForSelector",
            args);
    }

    internal Task<ElementHandleChannel> QuerySelectorAsync(string selector)
        => Connection.SendMessageToServerAsync<ElementHandleChannel>(
            Object,
            "querySelector",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
            });

    internal Task WaitForElementStateAsync(ElementState state, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["state"] = state,
            ["timeout"] = timeout,
        };

        return Connection.SendMessageToServerAsync<ElementHandleChannel>(Object, "waitForElementState", args);
    }

    internal Task<ChannelBase[]> QuerySelectorAllAsync(string selector)
        => Connection.SendMessageToServerAsync<ChannelBase[]>(
            Object,
            "querySelectorAll",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
            });

    internal async Task<byte[]> ScreenshotAsync(string path, bool? omitBackground, ScreenshotType? type, int? quality, IEnumerable<ILocator> mask, string maskColor, ScreenshotAnimations? animations, ScreenshotCaret? caret, ScreenshotScale? scale, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["type"] = type,
            ["omitBackground"] = omitBackground,
            ["path"] = path,
            ["timeout"] = timeout,
            ["animations"] = animations,
            ["caret"] = caret,
            ["scale"] = scale,
            ["quality"] = quality,
            ["maskColor"] = maskColor,
        };
        if (mask != null)
        {
            args["mask"] = mask.Select(locator => new Dictionary<string, object>
            {
                ["frame"] = ((Locator)locator)._frame._channel,
                ["selector"] = ((Locator)locator)._selector,
            }).ToArray();
        }

        return (await Connection.SendMessageToServerAsync(Object, "screenshot", args).ConfigureAwait(false))?.GetProperty("binary").GetBytesFromBase64();
    }

    internal Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, object arg)
        => Connection.SendMessageToServerAsync<JsonElement?>(
            Object,
            "evalOnSelector",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["expression"] = script,
                ["arg"] = arg,
            });

    internal Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, object arg)
        => Connection.SendMessageToServerAsync<JsonElement?>(
            Object,
            "evalOnSelectorAll",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["expression"] = script,
                ["arg"] = arg,
            });

    internal Task<FrameChannel> ContentFrameAsync() => Connection.SendMessageToServerAsync<FrameChannel>(Object, "contentFrame", null);

    internal Task<FrameChannel> OwnerFrameAsync() => Connection.SendMessageToServerAsync<FrameChannel>(Object, "ownerFrame", null);

    internal Task HoverAsync(
        IEnumerable<KeyboardModifier> modifiers,
        Position position,
        float? timeout,
        bool? force,
        bool? noWaitAfter,
        bool? trial)
    {
        var args = new Dictionary<string, object>
        {
            ["force"] = force,
            ["position"] = position,
            ["timeout"] = timeout,
            ["trial"] = trial,
            ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
            ["noWaitAfter"] = noWaitAfter,
        };

        return Connection.SendMessageToServerAsync<JsonElement?>(Object, "hover", args);
    }

    internal Task FocusAsync() => Connection.SendMessageToServerAsync(Object, "focus", null);

    internal Task ClickAsync(
        float? delay,
        MouseButton? button,
        int? clickCount,
        IEnumerable<KeyboardModifier> modifiers,
        Position position,
        float? timeout,
        bool? force,
        bool? noWaitAfter,
        bool? trial)
    {
        var args = new Dictionary<string, object>
        {
            ["delay"] = delay,
            ["button"] = button,
            ["clickCount"] = clickCount,
            ["force"] = force,
            ["noWaitAfter"] = noWaitAfter,
            ["timeout"] = timeout,
            ["trial"] = trial,
            ["position"] = position,
            ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
        };

        return Connection.SendMessageToServerAsync(Object, "click", args);
    }

    internal Task DblClickAsync(
        float? delay,
        MouseButton? button,
        IEnumerable<KeyboardModifier> modifiers,
        Position position,
        float? timeout,
        bool? force,
        bool? noWaitAfter,
        bool? trial)
    {
        var args = new Dictionary<string, object>
        {
            ["delay"] = delay,
            ["button"] = button,
            ["force"] = force,
            ["noWaitAfter"] = noWaitAfter,
            ["timeout"] = timeout,
            ["trial"] = trial,
            ["position"] = position,
            ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
        };

        return Connection.SendMessageToServerAsync(Object, "dblclick", args);
    }

    internal async Task<ElementHandleBoundingBoxResult> BoundingBoxAsync()
    {
        var result = (await Connection.SendMessageToServerAsync(Object, "boundingBox", null).ConfigureAwait(false)).Value;

        if (result.TryGetProperty("value", out var value))
        {
            return value.ToObject<ElementHandleBoundingBoxResult>();
        }

        return null;
    }

    internal Task ScrollIntoViewIfNeededAsync(float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["timeout"] = timeout,
        };

        return Connection.SendMessageToServerAsync<ElementHandleChannel>(Object, "scrollIntoViewIfNeeded", args);
    }

    internal Task FillAsync(string value, bool? noWaitAfter, bool? force, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value,
            ["timeout"] = timeout,
            ["force"] = force,
            ["noWaitAfter"] = noWaitAfter,
        };

        return Connection.SendMessageToServerAsync(Object, "fill", args);
    }

    internal Task DispatchEventAsync(string type, object eventInit)
    {
        var args = new Dictionary<string, object>
        {
            ["type"] = type,
            ["eventInit"] = eventInit,
        };

        return Connection.SendMessageToServerAsync<ElementHandleChannel>(Object, "dispatchEvent", args);
    }

    internal Task SetInputFilesAsync(SetInputFilesFiles files, bool? noWaitAfter, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["payloads"] = files.Payloads,
            ["localPaths"] = files.LocalPaths,
            ["streams"] = files.Streams,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
        };

        return Connection.SendMessageToServerAsync(Object, "setInputFiles", args);
    }

    internal async Task<string> GetAttributeAsync(string name)
    {
        var args = new Dictionary<string, object>
        {
            ["name"] = name,
        };

        if ((await Connection.SendMessageToServerAsync(Object, "getAttribute", args).ConfigureAwait(false))?.TryGetProperty("value", out var value) ?? false)
        {
            return value.ToString();
        }
        return null;
    }

    internal async Task<string> InnerHTMLAsync()
        => (await Connection.SendMessageToServerAsync(Object, "innerHTML").ConfigureAwait(false))?.GetProperty("value").ToString();

    internal async Task<string> InnerTextAsync()
        => (await Connection.SendMessageToServerAsync(Object, "innerText").ConfigureAwait(false))?.GetProperty("value").ToString();

    internal async Task<string> TextContentAsync()
        => (await Connection.SendMessageToServerAsync(Object, "textContent").ConfigureAwait(false))?.GetProperty("value").ToString();

    internal Task SelectTextAsync(bool? force = null, float? timeout = null)
    {
        var args = new Dictionary<string, object>
        {
            ["force"] = force,
            ["timeout"] = timeout,
        };

        return Connection.SendMessageToServerAsync<ElementHandleChannel>(Object, "selectText", args);
    }

    internal async Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValueProtocol> values, bool? noWaitAfter, bool? force, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["options"] = values,
            ["noWaitAfter"] = noWaitAfter,
            ["force"] = force,
            ["timeout"] = timeout,
        };

        return (await Connection.SendMessageToServerAsync(Object, "selectOption", args).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
    }

    internal async Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, bool? noWaitAfter, bool? force, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["elements"] = values,
            ["noWaitAfter"] = noWaitAfter,
            ["force"] = force,
            ["timeout"] = timeout,
        };

        return (await Connection.SendMessageToServerAsync(Object, "selectOption", args).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
    }

    internal async Task<bool> IsVisibleAsync()
        => (await Connection.SendMessageToServerAsync(Object, "isVisible", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    internal async Task<bool> IsHiddenAsync()
        => (await Connection.SendMessageToServerAsync(Object, "isHidden", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    internal async Task<bool> IsEnabledAsync()
        => (await Connection.SendMessageToServerAsync(Object, "isEnabled", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    internal async Task<bool> IsEditableAsync()
        => (await Connection.SendMessageToServerAsync(Object, "isEditable", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    internal async Task<bool> IsDisabledAsync()
        => (await Connection.SendMessageToServerAsync(Object, "isDisabled", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    internal async Task<string> InputValueAsync(float? timeout)
    {
        var args = new Dictionary<string, object>()
            {
                { "timeout", timeout },
            };

        return (await Connection.SendMessageToServerAsync(Object, "inputValue", args).ConfigureAwait(false))?.GetProperty("value").GetString();
    }

    internal async Task<bool> IsCheckedAsync()
        => (await Connection.SendMessageToServerAsync(Object, "isChecked", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

    internal Task CheckAsync(Position position, float? timeout, bool? force, bool? noWaitAfter, bool? trial)
    {
        var args = new Dictionary<string, object>
        {
            ["force"] = force,
            ["position"] = position,
            ["trial"] = trial,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
        };

        return Connection.SendMessageToServerAsync<ElementHandleChannel>(Object, "check", args);
    }

    internal Task UncheckAsync(Position position, float? timeout, bool? force, bool? noWaitAfter, bool? trial)
    {
        var args = new Dictionary<string, object>
        {
            ["force"] = force,
            ["position"] = position,
            ["trial"] = trial,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
        };

        return Connection.SendMessageToServerAsync<ElementHandleChannel>(Object, "uncheck", args);
    }

    internal Task TypeAsync(string text, float? delay, float? timeout, bool? noWaitAfter)
    {
        var args = new Dictionary<string, object>
        {
            ["text"] = text,
            ["delay"] = delay,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
        };

        return Connection.SendMessageToServerAsync(Object, "type", args);
    }

    internal Task PressAsync(string key, float? delay, float? timeout, bool? noWaitAfter)
    {
        var args = new Dictionary<string, object>
        {
            ["key"] = key,
            ["delay"] = delay,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
        };

        return Connection.SendMessageToServerAsync(Object, "press", args);
    }

    internal Task TapAsync(
        Position position,
        IEnumerable<KeyboardModifier> modifiers,
        float? timeout,
        bool? force,
        bool? noWaitAfter,
        bool? trial)
    {
        var args = new Dictionary<string, object>
        {
            ["force"] = force,
            ["noWaitAfter"] = noWaitAfter,
            ["position"] = position,
            ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
            ["trial"] = trial,
            ["timeout"] = timeout,
        };

        return Connection.SendMessageToServerAsync(Object, "tap", args);
    }
}
