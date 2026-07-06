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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PlaywrightServerMessage))]
[JsonSerializable(typeof(ErrorEntry))]
[JsonSerializable(typeof(PlaywrightServerError))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, object?>), TypeInfoPropertyName = "DictionaryOfStringToObject")]
[JsonSerializable(typeof(List<Dictionary<string, object?>>), TypeInfoPropertyName = "ListOfDictionaryStringToObject")]
[JsonSerializable(typeof(List<object?>), TypeInfoPropertyName = "ListOfObject")]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(JsonArray))]
[JsonSerializable(typeof(JsonValue))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(System.Text.Json.JsonElement))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ViewportSize))]
[JsonSerializable(typeof(ScreencastFrame))]
[JsonSerializable(typeof(BrowserContextCookiesResult))]
[JsonSerializable(typeof(ResponseSecurityDetailsResult))]
[JsonSerializable(typeof(ResponseServerAddrResult))]
[JsonSerializable(typeof(RequestSizesResult))]
[JsonSerializable(typeof(WebErrorLocation))]
[JsonSerializable(typeof(ElementHandleBoundingBoxResult))]
[JsonSerializable(typeof(Core.ConsoleMessageInitializer))]
[JsonSerializable(typeof(Core.FrameNavigatedEventArgs))]
[JsonSerializable(typeof(Core.NavigateDocument))]
[JsonSerializable(typeof(Core.JSElementProperty))]
[JsonSerializable(typeof(DebuggerPausedDetails))]
[JsonSerializable(typeof(WebStorageItem))]
[JsonSerializable(typeof(List<Core.ConsoleMessageInitializer>), TypeInfoPropertyName = "ListOfConsoleMessageInitializer")]
[JsonSerializable(typeof(List<Core.JSElementProperty>), TypeInfoPropertyName = "ListOfJSElementProperty")]
[JsonSerializable(typeof(List<NameValue>), TypeInfoPropertyName = "ListOfNameValue")]
[JsonSerializable(typeof(List<SerializedError>), TypeInfoPropertyName = "ListOfSerializedError")]
[JsonSerializable(typeof(List<VirtualCredential>), TypeInfoPropertyName = "ListOfVirtualCredential")]
[JsonSerializable(typeof(List<WebStorageItem>), TypeInfoPropertyName = "ListOfWebStorageItem")]
[JsonSerializable(typeof(IReadOnlyList<BrowserContextCookiesResult>), TypeInfoPropertyName = "IReadOnlyListOfBrowserContextCookiesResult")]
[JsonSerializable(typeof(Channels.BrowserContextChannelRequestEventArgs))]
[JsonSerializable(typeof(Channels.BrowserContextChannelResponseEventArgs))]
[JsonSerializable(typeof(RequestSizesResult))]
// Protocol initializer types (68 types)
[JsonSerializable(typeof(AndroidDeviceInitializer))]
[JsonSerializable(typeof(AndroidElementInfo))]
[JsonSerializable(typeof(AndroidInitializer))]
[JsonSerializable(typeof(AndroidSelector))]
[JsonSerializable(typeof(AndroidSelectorHasChild))]
[JsonSerializable(typeof(AndroidSelectorHasDescendant))]
[JsonSerializable(typeof(AndroidSocketInitializer))]
[JsonSerializable(typeof(AndroidWebView))]
[JsonSerializable(typeof(APIRequestContextInitializer))]
[JsonSerializable(typeof(APIResponse))]
[JsonSerializable(typeof(ArtifactInitializer))]
[JsonSerializable(typeof(BindingCallInitializer))]
[JsonSerializable(typeof(BrowserContextInitializer))]
[JsonSerializable(typeof(BrowserInitializer))]
[JsonSerializable(typeof(BrowserTypeInitializer))]
[JsonSerializable(typeof(CDPSessionInitializer))]
[JsonSerializable(typeof(ClientSideCallMetadata))]
[JsonSerializable(typeof(DebugControllerInitializer))]
[JsonSerializable(typeof(DebuggerInitializer))]
[JsonSerializable(typeof(DialogInitializer))]
[JsonSerializable(typeof(DisposableInitializer))]
[JsonSerializable(typeof(ElectronApplicationInitializer))]
[JsonSerializable(typeof(ElectronInitializer))]
[JsonSerializable(typeof(ElementHandleInitializer))]
[JsonSerializable(typeof(ExpectedTextValue))]
[JsonSerializable(typeof(FormField))]
[JsonSerializable(typeof(FormFieldFile))]
[JsonSerializable(typeof(FrameInitializer))]
[JsonSerializable(typeof(JSHandleInitializer))]
[JsonSerializable(typeof(JsonPipeInitializer))]
[JsonSerializable(typeof(LocalUtilsInitializer))]
[JsonSerializable(typeof(Metadata))]
[JsonSerializable(typeof(MetadataLocation))]
[JsonSerializable(typeof(NameValue))]
[JsonSerializable(typeof(PageInitializer))]
[JsonSerializable(typeof(PlaywrightInitializer))]
[JsonSerializable(typeof(Point))]
[JsonSerializable(typeof(RecorderSource))]
[JsonSerializable(typeof(RecorderSourceHighlight))]
[JsonSerializable(typeof(RecordHarOptions))]
[JsonSerializable(typeof(Rect))]
[JsonSerializable(typeof(RemoteAddr))]
[JsonSerializable(typeof(RequestInitializer))]
[JsonSerializable(typeof(RequestSizes))]
[JsonSerializable(typeof(ResourceTiming))]
[JsonSerializable(typeof(ResponseInitializer))]
[JsonSerializable(typeof(RootInitializer))]
[JsonSerializable(typeof(RouteInitializer))]
[JsonSerializable(typeof(SecurityDetails))]
[JsonSerializable(typeof(SelectorEngine))]
[JsonSerializable(typeof(SerializedError))]
[JsonSerializable(typeof(SerializedErrorError))]
[JsonSerializable(typeof(SerializedValue))]
[JsonSerializable(typeof(SerializedValueE))]
[JsonSerializable(typeof(SerializedValueO))]
[JsonSerializable(typeof(SerializedValueR))]
[JsonSerializable(typeof(SerializedValueTa))]
[JsonSerializable(typeof(SocksSupportInitializer))]
[JsonSerializable(typeof(StackFrame))]
[JsonSerializable(typeof(StreamInitializer))]
[JsonSerializable(typeof(TracingInitializer))]
[JsonSerializable(typeof(URLPattern))]
[JsonSerializable(typeof(VirtualCredential))]
[JsonSerializable(typeof(WaitInfo))]
[JsonSerializable(typeof(WebSocketInitializer))]
[JsonSerializable(typeof(WebSocketRouteInitializer))]
[JsonSerializable(typeof(WorkerInitializer))]
[JsonSerializable(typeof(WritableStreamInitializer))]
internal partial class PlaywrightJsonContext : JsonSerializerContext
{
}
