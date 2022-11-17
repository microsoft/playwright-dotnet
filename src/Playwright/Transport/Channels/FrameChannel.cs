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

internal class FrameChannel : Channel<Frame>
{
    public FrameChannel(string guid, Connection connection, Frame owner) : base(guid, connection, owner)
    {
    }

    internal event EventHandler<FrameNavigatedEventArgs> Navigated;

    internal event EventHandler<FrameChannelLoadStateEventArgs> LoadState;

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "navigated":
                var e = serverParams?.ToObject<FrameNavigatedEventArgs>(Connection.DefaultJsonSerializerOptions);

                if (serverParams.Value.TryGetProperty("newDocument", out var documentElement))
                {
                    e.NewDocument = documentElement.ToObject<NavigateDocument>(Connection.DefaultJsonSerializerOptions);
                }

                Navigated?.Invoke(this, e);
                break;
            case "loadstate":
                LoadState?.Invoke(
                    this,
                    serverParams?.ToObject<FrameChannelLoadStateEventArgs>(Connection.DefaultJsonSerializerOptions));
                break;
        }
    }

    internal Task<ElementHandleChannel> QuerySelectorAsync(string selector, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync<ElementHandleChannel>("querySelector", args);
    }

    internal Task<ResponseChannel> GotoAsync(string url, float? timeout, WaitUntilState? waitUntil, string referer)
    {
        var args = new Dictionary<string, object>
        {
            ["url"] = url,
            ["timeout"] = timeout,
            ["waitUntil"] = waitUntil,
            ["referer"] = referer,
        };
        return Object.SendMessageToServerAsync<ResponseChannel>("goto", args);
    }

    internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(
        string script,
        object arg)
    {
        return Object.SendMessageToServerAsync<JSHandleChannel>(
            "evaluateExpressionHandle",
            new Dictionary<string, object>
            {
                ["expression"] = script,
                ["arg"] = arg,
            });
    }

    internal Task<JSHandleChannel> WaitForFunctionAsync(
        string expression,
        object arg,
        float? timeout,
        float? polling)
    {
        var args = new Dictionary<string, object>
        {
            ["expression"] = expression,
            ["arg"] = arg,
            ["timeout"] = timeout,
            ["pollingInterval"] = polling,
        };
        return Object.SendMessageToServerAsync<JSHandleChannel>(
            "waitForFunction",
            args);
    }

    internal Task<JsonElement?> EvaluateExpressionAsync(
        string script,
        object arg)
    {
        return Object.SendMessageToServerAsync<JsonElement?>(
            "evaluateExpression",
            new Dictionary<string, object>
            {
                ["expression"] = script,
                ["arg"] = arg,
            });
    }

    internal Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, object arg, bool? strict)
        => Object.SendMessageToServerAsync<JsonElement?>(
            "evalOnSelector",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["expression"] = script,
                ["arg"] = arg,
                ["strict"] = strict,
            });

    internal Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, object arg)
        => Object.SendMessageToServerAsync<JsonElement?>(
            "evalOnSelectorAll",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["expression"] = script,
                ["arg"] = arg,
            });

    internal Task<ElementHandleChannel> FrameElementAsync() => Object.SendMessageToServerAsync<ElementHandleChannel>("frameElement");

    internal async Task<string> TitleAsync()
        => (await Object.SendMessageToServerAsync("title", null).ConfigureAwait(false))?.GetProperty("value").ToString();

    internal Task<ElementHandleChannel> WaitForSelectorAsync(string selector, WaitForSelectorState? state, float? timeout, bool? strict, bool? omitReturnValue = default)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["state"] = state,
            ["strict"] = strict,
            ["omitReturnValue"] = omitReturnValue,
        };
        return Object.SendMessageToServerAsync<ElementHandleChannel>(
            "waitForSelector",
            args);
    }

    internal Task WaitForTimeoutAsync(float timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["timeout"] = timeout,
        };
        return Object.SendMessageToServerAsync<ElementHandleChannel>(
            "waitForTimeout",
            args);
    }

    internal Task<ElementHandleChannel> AddScriptTagAsync(string url, string path, string content, string type)
    {
        var args = new Dictionary<string, object>
        {
            ["url"] = url,
            ["path"] = path,
            ["content"] = content,
            ["type"] = type,
        };
        return Object.SendMessageToServerAsync<ElementHandleChannel>("addScriptTag", args);
    }

    internal Task BlurAsync(string selector, bool strict, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["strict"] = strict,
            ["timeout"] = timeout,
        };
        return Object.SendMessageToServerAsync("blur", args);
    }

    internal Task<ElementHandleChannel> AddStyleTagAsync(string url, string path, string content)
    {
        var args = new Dictionary<string, object>
        {
            ["url"] = url,
            ["path"] = path,
            ["content"] = content,
        };
        return Object.SendMessageToServerAsync<ElementHandleChannel>("addStyleTag", args);
    }

    internal Task<ResponseChannel> WaitForNavigationAsync(LoadState? waitUntil, string url, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["timeout"] = timeout,
            ["url"] = url,
            ["waitUntil"] = waitUntil,
        };

        return Object.SendMessageToServerAsync<ResponseChannel>("waitForNavigation", args);
    }

    internal Task WaitForLoadStateAsync(LoadState? state, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["timeout"] = timeout,
            ["state"] = state,
        };

        return Object.SendMessageToServerAsync(
            "waitForLoadState",
            args);
    }

    internal async Task<int> QueryCountAsync(string selector)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
        };

        var result = await Object.SendMessageToServerAsync("queryCount", args).ConfigureAwait(false);
        return result.Value.GetProperty("value").GetInt32();
    }

    internal Task SetContentAsync(string html, float? timeout, WaitUntilState? waitUntil)
    {
        var args = new Dictionary<string, object>
        {
            ["html"] = html,
            ["waitUntil"] = waitUntil,
            ["timeout"] = timeout,
        };

        return Object.SendMessageToServerAsync("setContent", args);
    }

    internal Task ClickAsync(
        string selector,
        float? delay,
        MouseButton? button,
        int? clickCount,
        IEnumerable<KeyboardModifier> modifiers,
        Position position,
        float? timeout,
        bool? force,
        bool? noWaitAfter,
        bool? trial,
        bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["button"] = button,
            ["force"] = force,
            ["delay"] = delay,
            ["clickCount"] = clickCount,
            ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
            ["position"] = position,
            ["noWaitAfter"] = noWaitAfter,
            ["trial"] = trial,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync<ElementHandleChannel>("click", args);
    }

    internal Task DblClickAsync(
        string selector,
        float? delay,
        MouseButton? button,
        Position position,
        IEnumerable<KeyboardModifier> modifiers,
        float? timeout,
        bool? force,
        bool? noWaitAfter,
        bool? trial,
        bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["button"] = button,
            ["delay"] = delay,
            ["force"] = force,
            ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
            ["position"] = position,
            ["trial"] = trial,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync<ElementHandleChannel>("dblclick", args);
    }

    internal Task<ElementHandleChannel> QuerySelectorAsync(string selector)
        => Object.SendMessageToServerAsync<ElementHandleChannel>(
            "querySelector",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
            });

    internal Task<ChannelBase[]> QuerySelectorAllAsync(string selector)
        => Object.SendMessageToServerAsync<ChannelBase[]>(
            "querySelectorAll",
            new Dictionary<string, object>
            {
                ["selector"] = selector,
            });

    internal Task FillAsync(string selector, string value, bool? force, float? timeout, bool? noWaitAfter, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["value"] = value,
            ["force"] = force,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync("fill", args);
    }

    internal Task CheckAsync(string selector, Position position, float? timeout, bool? force, bool? noWaitAfter, bool? trial, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["force"] = force,
            ["position"] = position,
            ["noWaitAfter"] = noWaitAfter,
            ["trial"] = trial,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync<ElementHandleChannel>("check", args);
    }

    internal Task UncheckAsync(string selector, Position position, float? timeout, bool? force, bool? noWaitAfter, bool? trial, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["force"] = force,
            ["position"] = position,
            ["noWaitAfter"] = noWaitAfter,
            ["trial"] = trial,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync<ElementHandleChannel>("uncheck", args);
    }

    internal Task DispatchEventAsync(string selector, string type, object eventInit, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["type"] = type,
            ["eventInit"] = eventInit,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync("dispatchEvent", args);
    }

    internal Task HoverAsync(
        string selector,
        Position position,
        IEnumerable<KeyboardModifier> modifiers,
        bool? force,
        bool? noWaitAfter,
        float? timeout,
        bool? trial,
        bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["force"] = force,
            ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
            ["position"] = position,
            ["trial"] = trial,
            ["timeout"] = timeout,
            ["strict"] = strict,
            ["noWaitAfter"] = noWaitAfter,
        };

        return Object.SendMessageToServerAsync("hover", args);
    }

    internal Task PressAsync(string selector, string text, float? delay, float? timeout, bool? noWaitAfter, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["key"] = text,
            ["delay"] = delay,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync("press", args);
    }

    internal async Task<string[]> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, bool? noWaitAfter, bool? strict, bool? force, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["options"] = values,
            ["noWaitAfter"] = noWaitAfter,
            ["strict"] = strict,
            ["force"] = force,
            ["timeout"] = timeout,
        };

        return (await Object.SendMessageToServerAsync("selectOption", args).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
    }

    internal async Task<string[]> SelectOptionAsync(string selector, IEnumerable<ElementHandle> values, bool? noWaitAfter, bool? strict, bool? force, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["elements"] = values,
            ["noWaitAfter"] = noWaitAfter,
            ["strict"] = strict,
            ["force"] = force,
            ["timeout"] = timeout,
        };

        return (await Object.SendMessageToServerAsync("selectOption", args).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
    }

    internal async Task<string> GetAttributeAsync(string selector, string name, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["name"] = name,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        JsonElement retValue = default;
        if ((await Object.SendMessageToServerAsync("getAttribute", args).ConfigureAwait(false))?.TryGetProperty("value", out retValue) ?? false)
        {
            return retValue.ToString();
        }

        return null;
    }

    internal async Task<string> InnerHTMLAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("innerHTML", args).ConfigureAwait(false))?.GetProperty("value").ToString();
    }

    internal Task TypeAsync(string selector, string text, float? delay, float? timeout, bool? noWaitAfter, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["text"] = text,
            ["delay"] = delay,
            ["noWaitAfter"] = noWaitAfter,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync("type", args);
    }

    internal async Task<string> ContentAsync()
        => (await Object.SendMessageToServerAsync("content").ConfigureAwait(false))?.GetProperty("value").ToString();

    internal Task FocusAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync("focus", args);
    }

    internal async Task<string> InnerTextAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("innerText", args).ConfigureAwait(false))?.GetProperty("value").ToString();
    }

    internal Task SetInputFilesAsync(string selector, IEnumerable<InputFilesList> files, bool? noWaitAfter, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["files"] = files,
            ["noWaitAfter"] = noWaitAfter,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync("setInputFiles", args);
    }

    internal Task SetInputFilePathsAsync(string selector, IEnumerable<string> localPaths, IEnumerable<WritableStream> streams, bool? noWaitAfter, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["localPaths"] = localPaths,
            ["streams"] = streams,
            ["timeout"] = timeout,
            ["noWaitAfter"] = noWaitAfter,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync("setInputFilePaths", args);
    }

    internal async Task<string> TextContentAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("textContent", args).ConfigureAwait(false))?.GetProperty("value").ToString();
    }

    internal Task TapAsync(
        string selector,
        IEnumerable<KeyboardModifier> modifiers,
        Position position,
        float? timeout,
        bool? force,
        bool? noWaitAfter,
        bool? trial,
        bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["force"] = force,
            ["modifiers"] = modifiers?.Select(m => m.ToValueString()),
            ["noWaitAfter"] = noWaitAfter,
            ["trial"] = trial,
            ["timeout"] = timeout,
            ["position"] = position,
            ["strict"] = strict,
        };

        return Object.SendMessageToServerAsync("tap", args);
    }

    internal async Task<bool> IsCheckedAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("isChecked", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
    }

    internal async Task<bool> IsDisabledAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("isDisabled", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
    }

    internal async Task<bool> IsEditableAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("isEditable", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
    }

    internal async Task<bool> IsEnabledAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("isEnabled", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
    }

    internal async Task<bool> IsHiddenAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("isHidden", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
    }

    internal async Task<bool> IsVisibleAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("isVisible", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
    }

    internal async Task<string> InputValueAsync(string selector, float? timeout, bool? strict)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["timeout"] = timeout,
            ["strict"] = strict,
        };

        return (await Object.SendMessageToServerAsync("inputValue", args).ConfigureAwait(false))?.GetProperty("value").ToString();
    }

    internal Task DragAndDropAsync(string source, string target, bool? force, bool? noWaitAfter, float? timeout, bool? trial, bool? strict, SourcePosition sourcePosition, TargetPosition targetPosition)
    {
        var args = new Dictionary<string, object>
        {
            ["source"] = source,
            ["target"] = target,
            ["force"] = force,
            ["noWaitAfter"] = noWaitAfter,
            ["timeout"] = timeout,
            ["trial"] = trial,
            ["strict"] = strict,
            ["sourcePosition"] = sourcePosition,
            ["targetPosition"] = targetPosition,
        };

        return Object.SendMessageToServerAsync("dragAndDrop", args);
    }

    internal async Task<FrameExpectResult> ExpectAsync(string selector, string expression, object expressionArg, ExpectedTextValue[] expectedText, int? expectedNumber, object expectedValue, bool? useInnerText, bool? isNot, float? timeout)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
            ["expression"] = expression,
            ["expressionArg"] = expressionArg,
            ["expectedText"] = expectedText,
            ["expectedNumber"] = expectedNumber,
            ["expectedValue"] = expectedValue,
            ["useInnerText"] = useInnerText,
            ["isNot"] = isNot,
            ["timeout"] = timeout,
        };
        var result = await Object.SendMessageToServerAsync("expect", args).ConfigureAwait(false);
        var parsed = result.Value.ToObject<FrameExpectResult>();
        if (result.Value.TryGetProperty("received", out var received))
        {
            var outs = ScriptsHelper.ParseEvaluateResult<object>(received);
            parsed.Received = outs;
        }
        return parsed;
    }

    internal async Task HighlightAsync(string selector)
    {
        var args = new Dictionary<string, object>
        {
            ["selector"] = selector,
        };

        await Object.SendMessageToServerAsync("highlight", args).ConfigureAwait(false);
    }
}
