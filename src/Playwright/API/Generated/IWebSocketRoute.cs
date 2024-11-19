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
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Whenever a <a href="https://developer.mozilla.org/en-US/docs/Web/API/WebSocket"><c>WebSocket</c></a>
/// route is set up with <see cref="IPage.RouteWebSocketAsync"/> or <see cref="IBrowserContext.RouteWebSocketAsync"/>,
/// the <c>WebSocketRoute</c> object allows to handle the WebSocket, like an actual
/// server would do.
/// </para>
/// <para>**Mocking**</para>
/// <para>
/// By default, the routed WebSocket will not connect to the server. This way, you can
/// mock entire communcation over the WebSocket. Here is an example that responds to
/// a <c>"request"</c> with a <c>"response"</c>.
/// </para>
/// <code>
/// await page.RouteWebSocketAsync("wss://example.com/ws", ws =&gt; {<br/>
///   ws.OnMessage(frame =&gt; {<br/>
///     if (frame.Text == "request")<br/>
///       ws.Send("response");<br/>
///   });<br/>
/// });
/// </code>
/// <para>
/// Since we do not call <see cref="IWebSocketRoute.ConnectToServer"/> inside the WebSocket
/// route handler, Playwright assumes that WebSocket will be mocked, and opens the WebSocket
/// inside the page automatically.
/// </para>
/// <para>Here is another example that handles JSON messages:</para>
/// <code>
/// await page.RouteWebSocketAsync("wss://example.com/ws", ws =&gt; {<br/>
///   ws.OnMessage(frame =&gt; {<br/>
///     using var jsonDoc = JsonDocument.Parse(frame.Text);<br/>
///     JsonElement root = jsonDoc.RootElement;<br/>
///     if (root.TryGetProperty("request", out JsonElement requestElement) &amp;&amp; requestElement.GetString() == "question")<br/>
///     {<br/>
///       var response = new Dictionary&lt;string, string&gt; { ["response"] = "answer" };<br/>
///       string jsonResponse = JsonSerializer.Serialize(response);<br/>
///       ws.Send(jsonResponse);<br/>
///     }<br/>
///   });<br/>
/// });
/// </code>
/// <para>**Intercepting**</para>
/// <para>
/// Alternatively, you may want to connect to the actual server, but intercept messages
/// in-between and modify or block them. Calling <see cref="IWebSocketRoute.ConnectToServer"/>
/// returns a server-side <c>WebSocketRoute</c> instance that you can send messages
/// to, or handle incoming messages.
/// </para>
/// <para>
/// Below is an example that modifies some messages sent by the page to the server.
/// Messages sent from the server to the page are left intact, relying on the default
/// forwarding.
/// </para>
/// <code>
/// await page.RouteWebSocketAsync("/ws", ws =&gt; {<br/>
///   var server = ws.ConnectToServer();<br/>
///   ws.OnMessage(frame =&gt; {<br/>
///     if (frame.Text == "request")<br/>
///       server.Send("request2");<br/>
///     else<br/>
///       server.Send(frame.Text);<br/>
///   });<br/>
/// });
/// </code>
/// <para>
/// After connecting to the server, all **messages are forwarded** between the page
/// and the server by default.
/// </para>
/// <para>
/// However, if you call <see cref="IWebSocketRoute.OnMessage"/> on the original route,
/// messages from the page to the server **will not be forwarded** anymore, but should
/// instead be handled by the <see cref="IWebSocketRoute.OnMessage"/>.
/// </para>
/// <para>
/// Similarly, calling <see cref="IWebSocketRoute.OnMessage"/> on the server-side WebSocket
/// will **stop forwarding messages** from the server to the page, and <see cref="IWebSocketRoute.OnMessage"/>
/// should take care of them.
/// </para>
/// <para>
/// The following example blocks some messages in both directions. Since it calls <see
/// cref="IWebSocketRoute.OnMessage"/> in both directions, there is no automatic forwarding
/// at all.
/// </para>
/// <code>
/// await page.RouteWebSocketAsync("/ws", ws =&gt; {<br/>
///   var server = ws.ConnectToServer();<br/>
///   ws.OnMessage(frame =&gt; {<br/>
///     if (frame.Text != "blocked-from-the-page")<br/>
///       server.Send(frame.Text);<br/>
///   });<br/>
///   server.OnMessage(frame =&gt; {<br/>
///     if (frame.Text != "blocked-from-the-server")<br/>
///       ws.Send(frame.Text);<br/>
///   });<br/>
/// });
/// </code>
/// </summary>
public partial interface IWebSocketRoute
{
    /// <summary><para>Closes one side of the WebSocket connection.</para></summary>
    /// <param name="options">Call options</param>
    Task CloseAsync(WebSocketRouteCloseOptions? options = default);

    /// <summary>
    /// <para>
    /// By default, routed WebSocket does not connect to the server, so you can mock entire
    /// WebSocket communication. This method connects to the actual WebSocket server, and
    /// returns the server-side <see cref="IWebSocketRoute"/> instance, giving the ability
    /// to send and receive messages from the server.
    /// </para>
    /// <para>Once connected to the server:</para>
    /// <list type="bullet">
    /// <item><description>
    /// Messages received from the server will be **automatically forwarded** to the WebSocket
    /// in the page, unless <see cref="IWebSocketRoute.OnMessage"/> is called on the server-side
    /// <c>WebSocketRoute</c>.
    /// </description></item>
    /// <item><description>
    /// Messages sent by the <a href="https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/send"><c>WebSocket.send()</c></a>
    /// call in the page will be **automatically forwarded** to the server, unless <see
    /// cref="IWebSocketRoute.OnMessage"/> is called on the original <c>WebSocketRoute</c>.
    /// </description></item>
    /// </list>
    /// <para>See examples at the top for more details.</para>
    /// </summary>
    IWebSocketRoute ConnectToServer();

    /// <summary>
    /// <para>Allows to handle <a href="https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/close"><c>WebSocket.close</c></a>.</para>
    /// <para>
    /// By default, closing one side of the connection, either in the page or on the server,
    /// will close the other side. However, when <see cref="IWebSocketRoute.OnClose"/> handler
    /// is set up, the default forwarding of closure is disabled, and handler should take
    /// care of it.
    /// </para>
    /// </summary>
    /// <param name="handler">
    /// Function that will handle WebSocket closure. Received an optional <a href="https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/close#code">close
    /// code</a> and an optional <a href="https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/close#reason">close
    /// reason</a>.
    /// </param>
    void OnClose(Action<int?, string> handler);

    /// <summary>
    /// <para>
    /// This method allows to handle messages that are sent by the WebSocket, either from
    /// the page or from the server.
    /// </para>
    /// <para>
    /// When called on the original WebSocket route, this method handles messages sent from
    /// the page. You can handle this messages by responding to them with <see cref="IWebSocketRoute.Send"/>,
    /// forwarding them to the server-side connection returned by <see cref="IWebSocketRoute.ConnectToServer"/>
    /// or do something else.
    /// </para>
    /// <para>
    /// Once this method is called, messages are not automatically forwarded to the server
    /// or to the page - you should do that manually by calling <see cref="IWebSocketRoute.Send"/>.
    /// See examples at the top for more details.
    /// </para>
    /// <para>Calling this method again will override the handler with a new one.</para>
    /// </summary>
    /// <param name="handler">Function that will handle messages.</param>
    void OnMessage(Action<IWebSocketFrame> handler);

    /// <summary>
    /// <para>
    /// Sends a message to the WebSocket. When called on the original WebSocket, sends the
    /// message to the page. When called on the result of <see cref="IWebSocketRoute.ConnectToServer"/>,
    /// sends the message to the server. See examples at the top for more details.
    /// </para>
    /// </summary>
    /// <param name="message">Message to send.</param>
    void Send(byte[] message);

    /// <summary><para>URL of the WebSocket created in the page.</para></summary>
    string Url { get; }
}

#nullable disable
