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
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>The <c>CDPSession</c> instances are used to talk raw Chrome Devtools Protocol:</para>
/// <list type="bullet">
/// <item><description>protocol methods can be called with <c>session.send</c> method.</description></item>
/// <item><description>protocol events can be subscribed to with <c>session.on</c> method.</description></item>
/// </list>
/// <para>Useful links:</para>
/// <list type="bullet">
/// <item><description>
/// Documentation on DevTools Protocol can be found here: <a href="https://chromedevtools.github.io/devtools-protocol/">DevTools
/// Protocol Viewer</a>.
/// </description></item>
/// <item><description>Getting Started with DevTools Protocol: https://github.com/aslushnikov/getting-started-with-cdp/blob/master/README.md</description></item>
/// </list>
/// <code>
/// var client = await Page.Context.NewCDPSessionAsync(Page);<br/>
/// await client.SendAsync("Runtime.enable");<br/>
/// client.AddEventListener("Animation.animationCreated", (_) =&gt; Console.WriteLine("Animation created!"));<br/>
/// var response = await client.SendAsync("Animation.getPlaybackRate");<br/>
/// var playbackRate = response.Value.Deserialize&lt;JsonNode&gt;()["result"]["playbackRate"].GetValue&lt;decimal&gt;();<br/>
/// Console.WriteLine("playback rate is " + playbackRate);<br/>
/// await client.SendAsync("Animation.setPlaybackRate", new() { { "playbackRate", playbackRate / 2 } });
/// </code>
/// </summary>
public partial interface ICDPSession
{
    /// <summary>
    /// <para>
    /// Detaches the CDPSession from the target. Once detached, the CDPSession object won't
    /// emit any events and can't be used to send messages.
    /// </para>
    /// </summary>
    Task DetachAsync();

    /// <param name="method">Protocol method name.</param>
    /// <param name="args">Optional method parameters.</param>
    Task<JsonElement?> SendAsync(string method, Dictionary<string, object>? args = default);

    /// <summary><para>Adds a listener for a named CDP event</para></summary>
    /// <param name="eventName">
    /// </param>
    /// <param name="eventHandler">
    /// </param>
    void AddEventListener(string eventName, Action<JsonElement?> eventHandler);

    /// <summary><para>Removes a listener for a named CDP event</para></summary>
    /// <param name="eventName">
    /// </param>
    /// <param name="eventHandler">
    /// </param>
    void RemoveEventListener(string eventName, Action<JsonElement?> eventHandler);
}

#nullable disable
