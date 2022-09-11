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

using System.Runtime.Serialization;

namespace Microsoft.Playwright.Transport.Channels;

internal enum ChannelOwnerType
{
    [EnumMember(Value = "artifact")]
    Artifact,

    [EnumMember(Value = "bindingCall")]
    BindingCall,

    [EnumMember(Value = "browser")]
    Browser,

    [EnumMember(Value = "browserType")]
    BrowserType,

    [EnumMember(Value = "browserContext")]
    BrowserContext,

    [EnumMember(Value = "consoleMessage")]
    ConsoleMessage,

    [EnumMember(Value = "dialog")]
    Dialog,

    [EnumMember(Value = "download")]
    Download,

    [EnumMember(Value = "elementHandle")]
    ElementHandle,

    [EnumMember(Value = "frame")]
    Frame,

    [EnumMember(Value = "jsHandle")]
    JSHandle,

    [EnumMember(Value = "JsonPipe")]
    JsonPipe,
    [EnumMember(Value = "LocalUtils")]
    LocalUtils,

    [EnumMember(Value = "page")]
    Page,

    [EnumMember(Value = "request")]
    Request,

    [EnumMember(Value = "response")]
    Response,

    [EnumMember(Value = "route")]
    Route,

    [EnumMember(Value = "playwright")]
    Playwright,

    [EnumMember(Value = "browserServer")]
    BrowserServer,

    [EnumMember(Value = "worker")]
    Worker,

    [EnumMember(Value = "electron")]
    Electron,

    [EnumMember(Value = "selectors")]
    Selectors,

    [EnumMember(Value = "SocksSupport")]
    SocksSupport,

    [EnumMember(Value = "WebSocket")]
    WebSocket,

    [EnumMember(Value = "Android")]
    Android,

    [EnumMember(Value = "stream")]
    Stream,

    [EnumMember(Value = "WritableStream")]
    WritableStream,

    [EnumMember(Value = "tracing")]
    Tracing,

    [EnumMember(Value = "fetchRequest")]
    FetchRequest,

    [EnumMember(Value = "APIRequestContext")]
    APIRequestContext,
}
