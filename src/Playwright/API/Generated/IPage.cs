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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Page provides methods to interact with a single tab in a <see cref="IBrowser"/>,
/// or an <a href="https://developer.chrome.com/extensions/background_pages">extension
/// background page</a> in Chromium. One <see cref="IBrowser"/> instance might have
/// multiple <see cref="IPage"/> instances.
/// </para>
/// <para>This example creates a page, navigates it to a URL, and then saves a screenshot:</para>
/// <code>
/// using Microsoft.Playwright;<br/>
/// using System.Threading.Tasks;<br/>
/// <br/>
/// class PageExamples<br/>
/// {<br/>
///     public static async Task Run()<br/>
///     {<br/>
///         using var playwright = await Playwright.CreateAsync();<br/>
///         await using var browser = await playwright.Webkit.LaunchAsync();<br/>
///         var page = await browser.NewPageAsync();<br/>
///         await page.GotoAsync("https://www.theverge.com");<br/>
///         await page.ScreenshotAsync(new PageScreenshotOptions { Path = "theverge.png" });<br/>
///     }<br/>
/// }
/// </code>
/// <para>
/// The Page class emits various events (described below) which can be handled using
/// any of Node's native <a href="https://nodejs.org/api/events.html#events_class_eventemitter"><c>EventEmitter</c></a>
/// methods, such as <c>on</c>, <c>once</c> or <c>removeListener</c>.
/// </para>
/// <para>This example logs a message for a single page <c>load</c> event:</para>
/// <code>page.Load += (_, _) =&gt; Console.WriteLine("Page loaded!");</code>
/// <para>To unsubscribe from events use the <c>removeListener</c> method:</para>
/// <code>
/// void PageLoadHandler(object _, IPage p) {<br/>
///     Console.WriteLine("Page loaded!");<br/>
/// };<br/>
/// <br/>
/// page.Load += PageLoadHandler;<br/>
/// // Do some work...<br/>
/// page.Load -= PageLoadHandler;
/// </code>
/// </summary>
public partial interface IPage
{
    /// <summary><para>Emitted when the page closes.</para></summary>
    event EventHandler<IPage> Close;

    /// <summary>
    /// <para>
    /// Emitted when JavaScript within the page calls one of console API methods, e.g. <c>console.log</c>
    /// or <c>console.dir</c>. Also emitted if the page throws an error or a warning.
    /// </para>
    /// <para>The arguments passed into <c>console.log</c> appear as arguments on the event handler.</para>
    /// <para>An example of handling <c>console</c> event:</para>
    /// <code>
    /// page.Console += async (_, msg) =&gt;<br/>
    /// {<br/>
    ///     foreach (var arg in msg.Args)<br/>
    ///         Console.WriteLine(await arg.JsonValueAsync&lt;object&gt;());<br/>
    /// };<br/>
    /// <br/>
    /// await page.EvaluateAsync("console.log('hello', 5, { foo: 'bar' })");
    /// </code>
    /// </summary>
    event EventHandler<IConsoleMessage> Console;

    /// <summary>
    /// <para>
    /// Emitted when the page crashes. Browser pages might crash if they try to allocate
    /// too much memory. When the page crashes, ongoing and subsequent operations will throw.
    /// </para>
    /// <para>The most common way to deal with crashes is to catch an exception:</para>
    /// <code>
    /// try {<br/>
    ///   // Crash might happen during a click.<br/>
    ///   await page.ClickAsync("button");<br/>
    ///   // Or while waiting for an event.<br/>
    ///   await page.WaitForPopup();<br/>
    /// } catch (PlaywrightException e) {<br/>
    ///   // When the page crashes, exception message contains "crash".<br/>
    /// }
    /// </code>
    /// </summary>
    event EventHandler<IPage> Crash;

    /// <summary>
    /// <para>
    /// Emitted when a JavaScript dialog appears, such as <c>alert</c>, <c>prompt</c>, <c>confirm</c>
    /// or <c>beforeunload</c>. Listener **must** either <see cref="IDialog.AcceptAsync"/>
    /// or <see cref="IDialog.DismissAsync"/> the dialog - otherwise the page will <a href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/EventLoop#never_blocking">freeze</a>
    /// waiting for the dialog, and actions like click will never finish.
    /// </para>
    /// <code>
    /// page.RequestFailed += (_, request) =&gt;<br/>
    /// {<br/>
    ///     Console.WriteLine(request.Url + " " + request.Failure);<br/>
    /// };
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// When no <see cref="IPage.Dialog"/> listeners are present, all dialogs are automatically
    /// dismissed.
    /// </para>
    /// </remarks>
    event EventHandler<IDialog> Dialog;

    /// <summary>
    /// <para>
    /// Emitted when the JavaScript <a href="https://developer.mozilla.org/en-US/docs/Web/Events/DOMContentLoaded"><c>DOMContentLoaded</c></a>
    /// event is dispatched.
    /// </para>
    /// </summary>
    event EventHandler<IPage> DOMContentLoaded;

    /// <summary>
    /// <para>
    /// Emitted when attachment download started. User can access basic file operations
    /// on downloaded content via the passed <see cref="IDownload"/> instance.
    /// </para>
    /// </summary>
    event EventHandler<IDownload> Download;

    /// <summary>
    /// <para>
    /// Emitted when a file chooser is supposed to appear, such as after clicking the  <c>&lt;input
    /// type=file&gt;</c>. Playwright can respond to it via setting the input files using
    /// <see cref="IFileChooser.SetFilesAsync"/> that can be uploaded after that.
    /// </para>
    /// <code>
    /// page.FileChooser += (_, fileChooser) =&gt;<br/>
    /// {<br/>
    ///     fileChooser.SetFilesAsync(@"C:\temp\myfile.pdf");<br/>
    /// };
    /// </code>
    /// </summary>
    event EventHandler<IFileChooser> FileChooser;

    /// <summary><para>Emitted when a frame is attached.</para></summary>
    event EventHandler<IFrame> FrameAttached;

    /// <summary><para>Emitted when a frame is detached.</para></summary>
    event EventHandler<IFrame> FrameDetached;

    /// <summary><para>Emitted when a frame is navigated to a new url.</para></summary>
    event EventHandler<IFrame> FrameNavigated;

    /// <summary>
    /// <para>
    /// Emitted when the JavaScript <a href="https://developer.mozilla.org/en-US/docs/Web/Events/load"><c>load</c></a>
    /// event is dispatched.
    /// </para>
    /// </summary>
    event EventHandler<IPage> Load;

    /// <summary>
    /// <para>Emitted when an uncaught exception happens within the page.</para>
    /// <code>
    /// // Log all uncaught errors to the terminal<br/>
    /// page.PageError += (_, exception) =&gt;<br/>
    /// {<br/>
    ///   Console.WriteLine("Uncaught exception: " + exception);<br/>
    /// };
    /// </code>
    /// </summary>
    event EventHandler<string> PageError;

    /// <summary>
    /// <para>
    /// Emitted when the page opens a new tab or window. This event is emitted in addition
    /// to the <see cref="IBrowserContext.Page"/>, but only for popups relevant to this
    /// page.
    /// </para>
    /// <para>
    /// The earliest moment that page is available is when it has navigated to the initial
    /// url. For example, when opening a popup with <c>window.open('http://example.com')</c>,
    /// this event will fire when the network request to "http://example.com" is done and
    /// its response has started loading in the popup.
    /// </para>
    /// <code>
    /// var popup = await page.RunAndWaitForPopupAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.EvaluateAsync("() =&gt; window.open('https://microsoft.com')");<br/>
    /// });<br/>
    /// Console.WriteLine(await popup.EvaluateAsync&lt;string&gt;("location.href"));
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="IPage.WaitForLoadStateAsync"/> to wait until the page gets to a particular
    /// state (you should not need it in most cases).
    /// </para>
    /// </remarks>
    event EventHandler<IPage> Popup;

    /// <summary>
    /// <para>
    /// Emitted when a page issues a request. The <see cref="request"/> object is read-only.
    /// In order to intercept and mutate requests, see <see cref="IPage.RouteAsync"/> or
    /// <see cref="IBrowserContext.RouteAsync"/>.
    /// </para>
    /// </summary>
    event EventHandler<IRequest> Request;

    /// <summary><para>Emitted when a request fails, for example by timing out.</para></summary>
    /// <remarks>
    /// <para>
    /// HTTP Error responses, such as 404 or 503, are still successful responses from HTTP
    /// standpoint, so request will complete with <see cref="IPage.RequestFinished"/> event
    /// and not with <see cref="IPage.RequestFailed"/>. A request will only be considered
    /// failed when the client cannot get an HTTP response from the server, e.g. due to
    /// network error net::ERR_FAILED.
    /// </para>
    /// </remarks>
    event EventHandler<IRequest> RequestFailed;

    /// <summary>
    /// <para>
    /// Emitted when a request finishes successfully after downloading the response body.
    /// For a successful response, the sequence of events is <c>request</c>, <c>response</c>
    /// and <c>requestfinished</c>.
    /// </para>
    /// </summary>
    event EventHandler<IRequest> RequestFinished;

    /// <summary>
    /// <para>
    /// Emitted when <see cref="response"/> status and headers are received for a request.
    /// For a successful response, the sequence of events is <c>request</c>, <c>response</c>
    /// and <c>requestfinished</c>.
    /// </para>
    /// </summary>
    event EventHandler<IResponse> Response;

    /// <summary><para>Emitted when <see cref="IWebSocket"/> request is sent.</para></summary>
    event EventHandler<IWebSocket> WebSocket;

    /// <summary>
    /// <para>
    /// Emitted when a dedicated <a href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API">WebWorker</a>
    /// is spawned by the page.
    /// </para>
    /// </summary>
    event EventHandler<IWorker> Worker;

    /// <summary>
    /// <para>
    /// **DEPRECATED** This property is deprecated. Please use other libraries such as <a
    /// href="https://www.deque.com/axe/">Axe</a> if you need to test page accessibility.
    /// See our Node.js <a href="https://playwright.dev/docs/accessibility-testing">guide</a>
    /// for integration with Axe.
    /// </para>
    /// </summary>
    [System.Obsolete]
    public IAccessibility Accessibility { get; }

    /// <summary>
    /// <para>Adds a script which would be evaluated in one of the following scenarios:</para>
    /// <list type="bullet">
    /// <item><description>Whenever the page is navigated.</description></item>
    /// <item><description>
    /// Whenever the child frame is attached or navigated. In this case, the script is evaluated
    /// in the context of the newly attached frame.
    /// </description></item>
    /// </list>
    /// <para>
    /// The script is evaluated after the document was created but before any of its scripts
    /// were run. This is useful to amend the JavaScript environment, e.g. to seed <c>Math.random</c>.
    /// </para>
    /// <para>An example of overriding <c>Math.random</c> before the page loads:</para>
    /// <code>await page.AddInitScriptAsync(new PageAddInitScriptOption { ScriptPath = "./preload.js" });</code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order of evaluation of multiple scripts installed via <see cref="IBrowserContext.AddInitScriptAsync"/>
    /// and <see cref="IPage.AddInitScriptAsync"/> is not defined.
    /// </para>
    /// </remarks>
    /// <param name="script">Script to be evaluated in all pages in the browser context.</param>
    /// <param name="scriptPath">Instead of specifying <paramref name="script"/>, gives the file name to load from.</param>
    Task AddInitScriptAsync(string? script = default, string? scriptPath = default);

    /// <summary>
    /// <para>
    /// Adds a <c>&lt;script&gt;</c> tag into the page with the desired url or content.
    /// Returns the added tag when the script's onload fires or when the script content
    /// was injected into frame.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.AddScriptTagAsync"/>.</para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IElementHandle> AddScriptTagAsync(PageAddScriptTagOptions? options = default);

    /// <summary>
    /// <para>
    /// Adds a <c>&lt;link rel="stylesheet"&gt;</c> tag into the page with the desired url
    /// or a <c>&lt;style type="text/css"&gt;</c> tag with the content. Returns the added
    /// tag when the stylesheet's onload fires or when the CSS content was injected into
    /// frame.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.AddStyleTagAsync"/>.</para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IElementHandle> AddStyleTagAsync(PageAddStyleTagOptions? options = default);

    /// <summary><para>Brings page to front (activates tab).</para></summary>
    Task BringToFrontAsync();

    /// <summary>
    /// <para>
    /// This method checks an element matching <paramref name="selector"/> by performing
    /// the following steps:
    /// </para>
    /// <list type="ordinal">
    /// <item><description>
    /// Find an element matching <paramref name="selector"/>. If there is none, wait until
    /// a matching element is attached to the DOM.
    /// </description></item>
    /// <item><description>
    /// Ensure that matched element is a checkbox or a radio input. If not, this method
    /// throws. If the element is already checked, this method returns immediately.
    /// </description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the matched element, unless <paramref name="force"/> option is set. If
    /// the element is detached during the checks, the whole action is retried.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// <item><description>Ensure that the element is now checked. If not, this method throws.</description></item>
    /// </list>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.CheckAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task CheckAsync(string selector, PageCheckOptions? options = default);

    /// <summary>
    /// <para>
    /// This method clicks an element matching <paramref name="selector"/> by performing
    /// the following steps:
    /// </para>
    /// <list type="ordinal">
    /// <item><description>
    /// Find an element matching <paramref name="selector"/>. If there is none, wait until
    /// a matching element is attached to the DOM.
    /// </description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the matched element, unless <paramref name="force"/> option is set. If
    /// the element is detached during the checks, the whole action is retried.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>
    /// Use <see cref="IPage.Mouse"/> to click in the center of the element, or the specified
    /// <paramref name="position"/>.
    /// </description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// </list>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.ClickAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task ClickAsync(string selector, PageClickOptions? options = default);

    /// <summary>
    /// <para>
    /// If <paramref name="runBeforeUnload"/> is <c>false</c>, does not run any unload handlers
    /// and waits for the page to be closed. If <paramref name="runBeforeUnload"/> is <c>true</c>
    /// the method will run unload handlers, but will **not** wait for the page to close.
    /// </para>
    /// <para>By default, <c>page.close()</c> **does not** run <c>beforeunload</c> handlers.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// if <paramref name="runBeforeUnload"/> is passed as true, a <c>beforeunload</c> dialog
    /// might be summoned and should be handled manually via <see cref="IPage.Dialog"/>
    /// event.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    Task CloseAsync(PageCloseOptions? options = default);

    /// <summary><para>Gets the full HTML contents of the page, including the doctype.</para></summary>
    Task<string> ContentAsync();

    /// <summary><para>Get the browser context that the page belongs to.</para></summary>
    IBrowserContext Context { get; }

    /// <summary>
    /// <para>
    /// This method double clicks an element matching <paramref name="selector"/> by performing
    /// the following steps:
    /// </para>
    /// <list type="ordinal">
    /// <item><description>
    /// Find an element matching <paramref name="selector"/>. If there is none, wait until
    /// a matching element is attached to the DOM.
    /// </description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the matched element, unless <paramref name="force"/> option is set. If
    /// the element is detached during the checks, the whole action is retried.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>
    /// Use <see cref="IPage.Mouse"/> to double click in the center of the element, or the
    /// specified <paramref name="position"/>.
    /// </description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set. Note that if the first click of the <c>dblclick()</c> triggers a
    /// navigation event, this method will throw.
    /// </description></item>
    /// </list>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.DblClickAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>page.dblclick()</c> dispatches two <c>click</c> events and a single <c>dblclick</c>
    /// event.
    /// </para>
    /// </remarks>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task DblClickAsync(string selector, PageDblClickOptions? options = default);

    /// <summary>
    /// <para>
    /// The snippet below dispatches the <c>click</c> event on the element. Regardless of
    /// the visibility state of the element, <c>click</c> is dispatched. This is equivalent
    /// to calling <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/click">element.click()</a>.
    /// </para>
    /// <code>await page.DispatchEventAsync("button#submit", "click");</code>
    /// <para>
    /// Under the hood, it creates an instance of an event based on the given <paramref
    /// name="type"/>, initializes it with <paramref name="eventInit"/> properties and dispatches
    /// it on the element. Events are <c>composed</c>, <c>cancelable</c> and bubble by default.
    /// </para>
    /// <para>
    /// Since <paramref name="eventInit"/> is event-specific, please refer to the events
    /// documentation for the lists of initial properties:
    /// </para>
    /// <list type="bullet">
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/DragEvent/DragEvent">DragEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/FocusEvent/FocusEvent">FocusEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/KeyboardEvent">KeyboardEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/MouseEvent/MouseEvent">MouseEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent/PointerEvent">PointerEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/TouchEvent/TouchEvent">TouchEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/Event/Event">Event</a></description></item>
    /// </list>
    /// <para>
    /// You can also specify <c>JSHandle</c> as the property value if you want live objects
    /// to be passed into the event:
    /// </para>
    /// <code>
    /// var dataTransfer = await page.EvaluateHandleAsync("() =&gt; new DataTransfer()");<br/>
    /// await page.DispatchEventAsync("#source", "dragstart", new { dataTransfer });
    /// </code>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="type">DOM event type: <c>"click"</c>, <c>"dragstart"</c>, etc.</param>
    /// <param name="eventInit">Optional event-specific initialization properties.</param>
    /// <param name="options">Call options</param>
    Task DispatchEventAsync(string selector, string type, object? eventInit = default, PageDispatchEventOptions? options = default);

    /// <summary>
    /// <para>
    /// This method drags the source element to the target element. It will first move to
    /// the source element, perform a <c>mousedown</c>, then move to the target element
    /// and perform a <c>mouseup</c>.
    /// </para>
    /// <code>
    /// await Page.DragAndDropAsync("#source", "#target");<br/>
    /// // or specify exact positions relative to the top-left corners of the elements:<br/>
    /// await Page.DragAndDropAsync("#source", "#target", new()<br/>
    /// {<br/>
    ///     SourcePosition = new() { X = 34, Y = 7 },<br/>
    ///     TargetPosition = new() { X = 10, Y = 20 },<br/>
    /// });
    /// </code>
    /// </summary>
    /// <param name="source">
    /// A selector to search for an element to drag. If there are multiple elements satisfying
    /// the selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="target">
    /// A selector to search for an element to drop onto. If there are multiple elements
    /// satisfying the selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task DragAndDropAsync(string source, string target, PageDragAndDropOptions? options = default);

    /// <summary>
    /// <para>
    /// This method changes the <c>CSS media type</c> through the <c>media</c> argument,
    /// and/or the <c>'prefers-colors-scheme'</c> media feature, using the <c>colorScheme</c>
    /// argument.
    /// </para>
    /// <code>
    /// await page.EvaluateAsync("() =&gt; matchMedia('screen').matches");<br/>
    /// // → true<br/>
    /// await page.EvaluateAsync("() =&gt; matchMedia('print').matches");<br/>
    /// // → false<br/>
    /// <br/>
    /// await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });<br/>
    /// await page.EvaluateAsync("() =&gt; matchMedia('screen').matches");<br/>
    /// // → false<br/>
    /// await page.EvaluateAsync("() =&gt; matchMedia('print').matches");<br/>
    /// // → true<br/>
    /// <br/>
    /// await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Screen });<br/>
    /// await page.EvaluateAsync("() =&gt; matchMedia('screen').matches");<br/>
    /// // → true<br/>
    /// await page.EvaluateAsync("() =&gt; matchMedia('print').matches");<br/>
    /// // → false
    /// </code>
    /// <code>
    /// await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });<br/>
    /// await page.EvaluateAsync("matchMedia('(prefers-color-scheme: dark)').matches");<br/>
    /// // → true<br/>
    /// await page.EvaluateAsync("matchMedia('(prefers-color-scheme: light)').matches");<br/>
    /// // → false<br/>
    /// await page.EvaluateAsync("matchMedia('(prefers-color-scheme: no-preference)').matches");<br/>
    /// // → false
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task EmulateMediaAsync(PageEmulateMediaOptions? options = default);

    /// <summary>
    /// <para>
    /// The method finds an element matching the specified selector within the page and
    /// passes it as a first argument to <paramref name="expression"/>. If no elements match
    /// the selector, the method throws an error. Returns the value of <paramref name="expression"/>.
    /// </para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, then <see cref="IPage.EvalOnSelectorAsync"/>
    /// would wait for the promise to resolve and return its value.
    /// </para>
    /// <para>Examples:</para>
    /// <code>
    /// var searchValue = await page.EvalOnSelectorAsync&lt;string&gt;("#search", "el =&gt; el.value");<br/>
    /// var preloadHref = await page.EvalOnSelectorAsync&lt;string&gt;("link[rel=preload]", "el =&gt; el.href");<br/>
    /// var html = await page.EvalOnSelectorAsync(".main-container", "(e, suffix) =&gt; e.outerHTML + suffix", "hello");
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.EvalOnSelectorAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method does not wait for the element to pass actionability checks and therefore
    /// can lead to the flaky tests. Use <see cref="ILocator.EvaluateAsync"/>, other <see
    /// cref="ILocator"/> helper methods or web-first assertions instead.
    /// </para>
    /// </remarks>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    /// <param name="options">Call options</param>
    Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object? arg = default, PageEvalOnSelectorOptions? options = default);

    /// <summary>
    /// <para>
    /// The method finds all elements matching the specified selector within the page and
    /// passes an array of matched elements as a first argument to <paramref name="expression"/>.
    /// Returns the result of <paramref name="expression"/> invocation.
    /// </para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, then <see cref="IPage.EvalOnSelectorAllAsync"/>
    /// would wait for the promise to resolve and return its value.
    /// </para>
    /// <para>Examples:</para>
    /// <code>var divsCount = await page.EvalOnSelectorAllAsync&lt;bool&gt;("div", "(divs, min) =&gt; divs.length &gt;= min", 10);</code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// In most cases, <see cref="ILocator.EvaluateAllAsync"/>, other <see cref="ILocator"/>
    /// helper methods and web-first assertions do a better job.
    /// </para>
    /// </remarks>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object? arg = default);

    /// <summary>
    /// <para>Returns the value of the <paramref name="expression"/> invocation.</para>
    /// <para>
    /// If the function passed to the <see cref="IPage.EvaluateAsync"/> returns a <see cref="Task"/>,
    /// then <see cref="IPage.EvaluateAsync"/> would wait for the promise to resolve and
    /// return its value.
    /// </para>
    /// <para>
    /// If the function passed to the <see cref="IPage.EvaluateAsync"/> returns a non-<see
    /// cref="Serializable"/> value, then <see cref="IPage.EvaluateAsync"/> resolves to
    /// <c>undefined</c>. Playwright also supports transferring some additional values that
    /// are not serializable by <c>JSON</c>: <c>-0</c>, <c>NaN</c>, <c>Infinity</c>, <c>-Infinity</c>.
    /// </para>
    /// <para>Passing argument to <paramref name="expression"/>:</para>
    /// <code>
    /// var result = await page.EvaluateAsync&lt;int&gt;("([x, y]) =&gt; Promise.resolve(x * y)", new[] { 7, 8 });<br/>
    /// Console.WriteLine(result);
    /// </code>
    /// <para>A string can also be passed in instead of a function:</para>
    /// <code>Console.WriteLine(await page.EvaluateAsync&lt;int&gt;("1 + 2")); // prints "3"</code>
    /// <para>
    /// <see cref="IElementHandle"/> instances can be passed as an argument to the <see
    /// cref="IPage.EvaluateAsync"/>:
    /// </para>
    /// <code>
    /// var bodyHandle = await page.EvaluateAsync("document.body");<br/>
    /// var html = await page.EvaluateAsync&lt;string&gt;("([body, suffix]) =&gt; body.innerHTML + suffix", new object [] { bodyHandle, "hello" });<br/>
    /// await bodyHandle.DisposeAsync();
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.EvaluateAsync"/>.</para>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<T> EvaluateAsync<T>(string expression, object? arg = default);

    /// <summary>
    /// <para>Returns the value of the <paramref name="expression"/> invocation as a <see cref="IJSHandle"/>.</para>
    /// <para>
    /// The only difference between <see cref="IPage.EvaluateAsync"/> and <see cref="IPage.EvaluateHandleAsync"/>
    /// is that <see cref="IPage.EvaluateHandleAsync"/> returns <see cref="IJSHandle"/>.
    /// </para>
    /// <para>
    /// If the function passed to the <see cref="IPage.EvaluateHandleAsync"/> returns a
    /// <see cref="Task"/>, then <see cref="IPage.EvaluateHandleAsync"/> would wait for
    /// the promise to resolve and return its value.
    /// </para>
    /// <code>
    /// // Handle for the window object.<br/>
    /// var aWindowHandle = await page.EvaluateHandleAsync("() =&gt; Promise.resolve(window)");
    /// </code>
    /// <para>A string can also be passed in instead of a function:</para>
    /// <code>var docHandle = await page.EvaluateHandleAsync("document"); // Handle for the `document`</code>
    /// <para><see cref="IJSHandle"/> instances can be passed as an argument to the <see cref="IPage.EvaluateHandleAsync"/>:</para>
    /// <code>
    /// var handle = await page.EvaluateHandleAsync("() =&gt; document.body");<br/>
    /// var resultHandle = await page.EvaluateHandleAsync("([body, suffix]) =&gt; body.innerHTML + suffix", new object[] { handle, "hello" });<br/>
    /// Console.WriteLine(await resultHandle.JsonValueAsync&lt;string&gt;());<br/>
    /// await resultHandle.DisposeAsync();
    /// </code>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<IJSHandle> EvaluateHandleAsync(string expression, object? arg = default);

    /// <summary>
    /// <para>
    /// The method adds a function called <paramref name="name"/> on the <c>window</c> object
    /// of every frame in this page. When called, the function executes <paramref name="callback"/>
    /// and returns a <see cref="Task"/> which resolves to the return value of <paramref
    /// name="callback"/>. If the <paramref name="callback"/> returns a <see cref="Promise"/>,
    /// it will be awaited.
    /// </para>
    /// <para>
    /// The first argument of the <paramref name="callback"/> function contains information
    /// about the caller: <c>{ browserContext: BrowserContext, page: Page, frame: Frame
    /// }</c>.
    /// </para>
    /// <para>See <see cref="IBrowserContext.ExposeBindingAsync"/> for the context-wide version.</para>
    /// <para>An example of exposing page URL to all frames in a page:</para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// using System.Threading.Tasks;<br/>
    /// <br/>
    /// class PageExamples<br/>
    /// {<br/>
    ///     public static async Task Main()<br/>
    ///     {<br/>
    ///         using var playwright = await Playwright.CreateAsync();<br/>
    ///         await using var browser = await playwright.Webkit.LaunchAsync(new()<br/>
    ///         {<br/>
    ///             Headless: false<br/>
    ///         });<br/>
    ///         var page = await browser.NewPageAsync();<br/>
    /// <br/>
    ///         await page.ExposeBindingAsync("pageUrl", (source) =&gt; source.Page.Url);<br/>
    ///         await page.SetContentAsync("&lt;script&gt;\n" +<br/>
    ///         "  async function onClick() {\n" +<br/>
    ///         "    document.querySelector('div').textContent = await window.pageURL();\n" +<br/>
    ///         "  }\n" +<br/>
    ///         "&lt;/script&gt;\n" +<br/>
    ///         "&lt;button onclick=\"onClick()\"&gt;Click me&lt;/button&gt;\n" +<br/>
    ///         "&lt;div&gt;&lt;/div&gt;");<br/>
    /// <br/>
    ///         await page.ClickAsync("button");<br/>
    ///     }<br/>
    /// }
    /// </code>
    /// <para>An example of passing an element handle:</para>
    /// <code>
    /// var result = new TaskCompletionSource&lt;string&gt;();<br/>
    /// await page.ExposeBindingAsync("clicked", async (BindingSource _, IJSHandle t) =&gt;<br/>
    /// {<br/>
    ///     return result.TrySetResult(await t.AsElement().TextContentAsync());<br/>
    /// });<br/>
    /// <br/>
    /// await page.SetContentAsync("&lt;script&gt;\n" +<br/>
    ///   "  document.addEventListener('click', event =&gt; window.clicked(event.target));\n" +<br/>
    ///   "&lt;/script&gt;\n" +<br/>
    ///   "&lt;div&gt;Click me&lt;/div&gt;\n" +<br/>
    ///   "&lt;div&gt;Or click me&lt;/div&gt;\n");<br/>
    /// <br/>
    /// await page.ClickAsync("div");<br/>
    /// Console.WriteLine(await result.Task);
    /// </code>
    /// </summary>
    /// <remarks><para>Functions installed via <see cref="IPage.ExposeBindingAsync"/> survive navigations.</para></remarks>
    /// <param name="name">Name of the function on the window object.</param>
    /// <param name="callback">Callback function that will be called in the Playwright's context.</param>
    /// <param name="options">Call options</param>
    Task ExposeBindingAsync(string name, Action callback, PageExposeBindingOptions? options = default);

    /// <summary>
    /// <para>
    /// The method adds a function called <paramref name="name"/> on the <c>window</c> object
    /// of every frame in the page. When called, the function executes <paramref name="callback"/>
    /// and returns a <see cref="Task"/> which resolves to the return value of <paramref
    /// name="callback"/>.
    /// </para>
    /// <para>If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.</para>
    /// <para>See <see cref="IBrowserContext.ExposeFunctionAsync"/> for context-wide exposed function.</para>
    /// <para>An example of adding a <c>sha256</c> function to the page:</para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// using System;<br/>
    /// using System.Security.Cryptography;<br/>
    /// using System.Threading.Tasks;<br/>
    /// <br/>
    /// class PageExamples<br/>
    /// {<br/>
    ///     public static async Task Main()<br/>
    ///     {<br/>
    ///         using var playwright = await Playwright.CreateAsync();<br/>
    ///         await using var browser = await playwright.Webkit.LaunchAsync(new()<br/>
    ///         {<br/>
    ///             Headless: false<br/>
    ///         });<br/>
    ///         var page = await browser.NewPageAsync();<br/>
    /// <br/>
    ///         await page.ExposeFunctionAsync("sha256", (string input) =&gt;<br/>
    ///         {<br/>
    ///             return Convert.ToBase64String(<br/>
    ///                 SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));<br/>
    ///         });<br/>
    /// <br/>
    ///         await page.SetContentAsync("&lt;script&gt;\n" +<br/>
    ///         "  async function onClick() {\n" +<br/>
    ///         "    document.querySelector('div').textContent = await window.sha256('PLAYWRIGHT');\n" +<br/>
    ///         "  }\n" +<br/>
    ///         "&lt;/script&gt;\n" +<br/>
    ///         "&lt;button onclick=\"onClick()\"&gt;Click me&lt;/button&gt;\n" +<br/>
    ///         "&lt;div&gt;&lt;/div&gt;");<br/>
    /// <br/>
    ///         await page.ClickAsync("button");<br/>
    ///         Console.WriteLine(await page.TextContentAsync("div"));<br/>
    ///     }<br/>
    /// }
    /// </code>
    /// </summary>
    /// <remarks><para>Functions installed via <see cref="IPage.ExposeFunctionAsync"/> survive navigations.</para></remarks>
    /// <param name="name">Name of the function on the window object</param>
    /// <param name="callback">Callback function which will be called in Playwright's context.</param>
    Task ExposeFunctionAsync(string name, Action callback);

    /// <summary>
    /// <para>
    /// This method waits for an element matching <paramref name="selector"/>, waits for
    /// <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a> checks,
    /// focuses the element, fills it and triggers an <c>input</c> event after filling.
    /// Note that you can pass an empty string to clear the input field.
    /// </para>
    /// <para>
    /// If the target element is not an <c>&lt;input&gt;</c>, <c>&lt;textarea&gt;</c> or
    /// <c>[contenteditable]</c> element, this method throws an error. However, if the element
    /// is inside the <c>&lt;label&gt;</c> element that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be filled instead.
    /// </para>
    /// <para>To send fine-grained keyboard events, use <see cref="IPage.TypeAsync"/>.</para>
    /// <para>Shortcut for main frame's <see cref="IFrame.FillAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="value">
    /// Value to fill for the <c>&lt;input&gt;</c>, <c>&lt;textarea&gt;</c> or <c>[contenteditable]</c>
    /// element.
    /// </param>
    /// <param name="options">Call options</param>
    Task FillAsync(string selector, string value, PageFillOptions? options = default);

    /// <summary>
    /// <para>
    /// This method fetches an element with <paramref name="selector"/> and focuses it.
    /// If there's no element matching <paramref name="selector"/>, the method waits until
    /// a matching element appears in the DOM.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.FocusAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task FocusAsync(string selector, PageFocusOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns frame matching the specified criteria. Either <c>name</c> or <c>url</c>
    /// must be specified.
    /// </para>
    /// <code>var frame = page.Frame("frame-name");</code>
    /// <code>var frame = page.FrameByUrl(".*domain.*");</code>
    /// </summary>
    /// <param name="name">Frame name specified in the <c>iframe</c>'s <c>name</c> attribute.</param>
    IFrame? Frame(string name);

    /// <summary><para>Returns frame with matching URL.</para></summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
    /// cref="URL"/> object.
    /// </param>
    IFrame? FrameByUrl(string url);

    /// <summary><para>Returns frame with matching URL.</para></summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
    /// cref="URL"/> object.
    /// </param>
    IFrame? FrameByUrl(Regex url);

    /// <summary><para>Returns frame with matching URL.</para></summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
    /// cref="URL"/> object.
    /// </param>
    IFrame? FrameByUrl(Func<string, bool> url);

    /// <summary>
    /// <para>
    /// When working with iframes, you can create a frame locator that will enter the iframe
    /// and allow selecting elements in that iframe. Following snippet locates element with
    /// text "Submit" in the iframe with id <c>my-frame</c>, like <c>&lt;iframe id="my-frame"&gt;</c>:
    /// </para>
    /// <code>
    /// var locator = page.FrameLocator("#my-iframe").GetByText("Submit");<br/>
    /// await locator.ClickAsync();
    /// </code>
    /// </summary>
    /// <param name="selector">
    /// A selector to use when resolving DOM element. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    IFrameLocator FrameLocator(string selector);

    /// <summary><para>An array of all frames attached to the page.</para></summary>
    IReadOnlyList<IFrame> Frames { get; }

    /// <summary><para>Returns element attribute value.</para></summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="name">Attribute name to get the value for.</param>
    /// <param name="options">Call options</param>
    Task<string?> GetAttributeAsync(string selector, string name, PageGetAttributeOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their alt text. For example, this method will find the
    /// image by alt text "Castle":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByAltText(string text, PageGetByAltTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their alt text. For example, this method will find the
    /// image by alt text "Castle":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByAltText(Regex text, PageGetByAltTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the text of the associated label. For example,
    /// this method will find the input by label text Password in the following DOM:
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByLabel(string text, PageGetByLabelOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the text of the associated label. For example,
    /// this method will find the input by label text Password in the following DOM:
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByLabel(Regex text, PageGetByLabelOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the placeholder text. For example, this method
    /// will find the input by placeholder "Country":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByPlaceholder(string text, PageGetByPlaceholderOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the placeholder text. For example, this method
    /// will find the input by placeholder "Country":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByPlaceholder(Regex text, PageGetByPlaceholderOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their <a href="https://www.w3.org/TR/wai-aria-1.2/#roles">ARIA
    /// role</a>, <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-attributes">ARIA attributes</a>
    /// and <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible name</a>.
    /// Note that role selector **does not replace** accessibility audits and conformance
    /// tests, but rather gives early feedback about the ARIA guidelines.
    /// </para>
    /// <para>
    /// Note that many html elements have an implicitly <a href="https://w3c.github.io/html-aam/#html-element-role-mappings">defined
    /// role</a> that is recognized by the role selector. You can find all the <a href="https://www.w3.org/TR/wai-aria-1.2/#role_definitions">supported
    /// roles here</a>. ARIA guidelines **do not recommend** duplicating implicit roles
    /// and attributes by setting <c>role</c> and/or <c>aria-*</c> attributes to default
    /// values.
    /// </para>
    /// </summary>
    /// <param name="role">Required aria role.</param>
    /// <param name="options">Call options</param>
    ILocator GetByRole(AriaRole role, PageGetByRoleOptions? options = default);

    /// <summary>
    /// <para>
    /// Locate element by the test id. By default, the <c>data-testid</c> attribute is used
    /// as a test id. Use <see cref="ISelectors.SetTestIdAttribute"/> to configure a different
    /// test id attribute if necessary.
    /// </para>
    /// </summary>
    /// <param name="testId">Id to locate the element by.</param>
    ILocator GetByTestId(string testId);

    /// <summary><para>Allows locating elements that contain given text.</para></summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByText(string text, PageGetByTextOptions? options = default);

    /// <summary><para>Allows locating elements that contain given text.</para></summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByText(Regex text, PageGetByTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their title. For example, this method will find the
    /// button by its title "Submit":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByTitle(string text, PageGetByTitleOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their title. For example, this method will find the
    /// button by its title "Submit":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByTitle(Regex text, PageGetByTitleOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the main resource response. In case of multiple redirects, the navigation
    /// will resolve with the response of the last redirect. If can not go back, returns
    /// <c>null</c>.
    /// </para>
    /// <para>Navigate to the previous page in history.</para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IResponse?> GoBackAsync(PageGoBackOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the main resource response. In case of multiple redirects, the navigation
    /// will resolve with the response of the last redirect. If can not go forward, returns
    /// <c>null</c>.
    /// </para>
    /// <para>Navigate to the next page in history.</para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IResponse?> GoForwardAsync(PageGoForwardOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the main resource response. In case of multiple redirects, the navigation
    /// will resolve with the first non-redirect response.
    /// </para>
    /// <para>The method will throw an error if:</para>
    /// <list type="bullet">
    /// <item><description>there's an SSL error (e.g. in case of self-signed certificates).</description></item>
    /// <item><description>target URL is invalid.</description></item>
    /// <item><description>the <paramref name="timeout"/> is exceeded during navigation.</description></item>
    /// <item><description>the remote server does not respond or is unreachable.</description></item>
    /// <item><description>the main resource failed to load.</description></item>
    /// </list>
    /// <para>
    /// The method will not throw an error when any valid HTTP status code is returned by
    /// the remote server, including 404 "Not Found" and 500 "Internal Server Error".  The
    /// status code for such responses can be retrieved by calling <see cref="IResponse.Status"/>.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.GotoAsync"/></para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method either throws an error or returns a main resource response. The only
    /// exceptions are navigation to <c>about:blank</c> or navigation to the same URL with
    /// a different hash, which would succeed and return <c>null</c>.
    /// </para>
    /// <para>
    /// Headless mode doesn't support navigation to a PDF document. See the <a href="https://bugs.chromium.org/p/chromium/issues/detail?id=761295">upstream
    /// issue</a>.
    /// </para>
    /// </remarks>
    /// <param name="url">
    /// URL to navigate page to. The url should include scheme, e.g. <c>https://</c>. When
    /// a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IResponse?> GotoAsync(string url, PageGotoOptions? options = default);

    /// <summary>
    /// <para>
    /// This method hovers over an element matching <paramref name="selector"/> by performing
    /// the following steps:
    /// </para>
    /// <list type="ordinal">
    /// <item><description>
    /// Find an element matching <paramref name="selector"/>. If there is none, wait until
    /// a matching element is attached to the DOM.
    /// </description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the matched element, unless <paramref name="force"/> option is set. If
    /// the element is detached during the checks, the whole action is retried.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>
    /// Use <see cref="IPage.Mouse"/> to hover over the center of the element, or the specified
    /// <paramref name="position"/>.
    /// </description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <c>noWaitAfter</c>
    /// option is set.
    /// </description></item>
    /// </list>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.HoverAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task HoverAsync(string selector, PageHoverOptions? options = default);

    /// <summary><para>Returns <c>element.innerHTML</c>.</para></summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<string> InnerHTMLAsync(string selector, PageInnerHTMLOptions? options = default);

    /// <summary><para>Returns <c>element.innerText</c>.</para></summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<string> InnerTextAsync(string selector, PageInnerTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns <c>input.value</c> for the selected <c>&lt;input&gt;</c> or <c>&lt;textarea&gt;</c>
    /// or <c>&lt;select&gt;</c> element.
    /// </para>
    /// <para>
    /// Throws for non-input elements. However, if the element is inside the <c>&lt;label&gt;</c>
    /// element that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// returns the value of the control.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<string> InputValueAsync(string selector, PageInputValueOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns whether the element is checked. Throws if the element is not a checkbox
    /// or radio input.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<bool> IsCheckedAsync(string selector, PageIsCheckedOptions? options = default);

    /// <summary><para>Indicates that the page has been closed.</para></summary>
    bool IsClosed { get; }

    /// <summary><para>Returns whether the element is disabled, the opposite of <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</para></summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<bool> IsDisabledAsync(string selector, PageIsDisabledOptions? options = default);

    /// <summary><para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#editable">editable</a>.</para></summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<bool> IsEditableAsync(string selector, PageIsEditableOptions? options = default);

    /// <summary><para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</para></summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<bool> IsEnabledAsync(string selector, PageIsEnabledOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns whether the element is hidden, the opposite of <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.
    /// <paramref name="selector"/> that does not match any elements is considered hidden.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<bool> IsHiddenAsync(string selector, PageIsHiddenOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.
    /// <paramref name="selector"/> that does not match any elements is considered not visible.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<bool> IsVisibleAsync(string selector, PageIsVisibleOptions? options = default);

    public IKeyboard Keyboard { get; }

    /// <summary>
    /// <para>
    /// The method returns an element locator that can be used to perform actions on this
    /// page / frame. Locator is resolved to the element immediately before performing an
    /// action, so a series of actions on the same locator can in fact be performed on different
    /// DOM elements. That would happen if the DOM structure between those actions has changed.
    /// </para>
    /// <para><a href="https://playwright.dev/dotnet/docs/locators">Learn more about locators</a>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to use when resolving DOM element. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    ILocator Locator(string selector, PageLocatorOptions? options = default);

    /// <summary>
    /// <para>
    /// The page's main frame. Page is guaranteed to have a main frame which persists during
    /// navigations.
    /// </para>
    /// </summary>
    IFrame MainFrame { get; }

    public IMouse Mouse { get; }

    /// <summary>
    /// <para>
    /// Returns the opener for popup pages and <c>null</c> for others. If the opener has
    /// been closed already the returns <c>null</c>.
    /// </para>
    /// </summary>
    Task<IPage?> OpenerAsync();

    /// <summary>
    /// <para>
    /// Pauses script execution. Playwright will stop executing the script and wait for
    /// the user to either press 'Resume' button in the page overlay or to call <c>playwright.resume()</c>
    /// in the DevTools console.
    /// </para>
    /// <para>
    /// User can inspect selectors or perform manual steps while paused. Resume will continue
    /// running the original script from the place it was paused.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method requires Playwright to be started in a headed mode, with a falsy <paramref
    /// name="headless"/> value in the <see cref="IBrowserType.LaunchAsync"/>.
    /// </para>
    /// </remarks>
    Task PauseAsync();

    /// <summary>
    /// <para>Returns the PDF buffer.</para>
    /// <para>
    /// <c>page.pdf()</c> generates a pdf of the page with <c>print</c> css media. To generate
    /// a pdf with <c>screen</c> media, call <see cref="IPage.EmulateMediaAsync"/> before
    /// calling <c>page.pdf()</c>:
    /// </para>
    /// <code>
    /// // Generates a PDF with 'screen' media type<br/>
    /// await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Screen });<br/>
    /// await page.PdfAsync(new PagePdfOptions { Path = "page.pdf" });
    /// </code>
    /// <para>
    /// The <paramref name="width"/>, <paramref name="height"/>, and <paramref name="margin"/>
    /// options accept values labeled with units. Unlabeled values are treated as pixels.
    /// </para>
    /// <para>A few examples:</para>
    /// <list type="bullet">
    /// <item><description><c>page.pdf({width: 100})</c> - prints with width set to 100 pixels</description></item>
    /// <item><description><c>page.pdf({width: '100px'})</c> - prints with width set to 100 pixels</description></item>
    /// <item><description><c>page.pdf({width: '10cm'})</c> - prints with width set to 10 centimeters.</description></item>
    /// </list>
    /// <para>All possible units are:</para>
    /// <list type="bullet">
    /// <item><description><c>px</c> - pixel</description></item>
    /// <item><description><c>in</c> - inch</description></item>
    /// <item><description><c>cm</c> - centimeter</description></item>
    /// <item><description><c>mm</c> - millimeter</description></item>
    /// </list>
    /// <para>The <paramref name="format"/> options are:</para>
    /// <list type="bullet">
    /// <item><description><c>Letter</c>: 8.5in x 11in</description></item>
    /// <item><description><c>Legal</c>: 8.5in x 14in</description></item>
    /// <item><description><c>Tabloid</c>: 11in x 17in</description></item>
    /// <item><description><c>Ledger</c>: 17in x 11in</description></item>
    /// <item><description><c>A0</c>: 33.1in x 46.8in</description></item>
    /// <item><description><c>A1</c>: 23.4in x 33.1in</description></item>
    /// <item><description><c>A2</c>: 16.54in x 23.4in</description></item>
    /// <item><description><c>A3</c>: 11.7in x 16.54in</description></item>
    /// <item><description><c>A4</c>: 8.27in x 11.7in</description></item>
    /// <item><description><c>A5</c>: 5.83in x 8.27in</description></item>
    /// <item><description><c>A6</c>: 4.13in x 5.83in</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>Generating a pdf is currently only supported in Chromium headless.</para>
    /// <para>
    /// By default, <c>page.pdf()</c> generates a pdf with modified colors for printing.
    /// Use the <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/-webkit-print-color-adjust"><c>-webkit-print-color-adjust</c></a>
    /// property to force rendering of exact colors.
    /// </para>
    /// <para>
    /// <paramref name="headerTemplate"/> and <paramref name="footerTemplate"/> markup have
    /// the following limitations: > 1. Script tags inside templates are not evaluated.
    /// > 2. Page styles are not visible inside templates.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    Task<byte[]> PdfAsync(PagePdfOptions? options = default);

    /// <summary>
    /// <para>Focuses the element, and then uses <see cref="IKeyboard.DownAsync"/> and <see cref="IKeyboard.UpAsync"/>.</para>
    /// <para>
    /// <paramref name="key"/> can specify the intended <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key">keyboardEvent.key</a>
    /// value or a single character to generate the text for. A superset of the <paramref
    /// name="key"/> values can be found <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key/Key_Values">here</a>.
    /// Examples of the keys are:
    /// </para>
    /// <para>
    /// <c>F1</c> - <c>F12</c>, <c>Digit0</c>- <c>Digit9</c>, <c>KeyA</c>- <c>KeyZ</c>,
    /// <c>Backquote</c>, <c>Minus</c>, <c>Equal</c>, <c>Backslash</c>, <c>Backspace</c>,
    /// <c>Tab</c>, <c>Delete</c>, <c>Escape</c>, <c>ArrowDown</c>, <c>End</c>, <c>Enter</c>,
    /// <c>Home</c>, <c>Insert</c>, <c>PageDown</c>, <c>PageUp</c>, <c>ArrowRight</c>, <c>ArrowUp</c>,
    /// etc.
    /// </para>
    /// <para>
    /// Following modification shortcuts are also supported: <c>Shift</c>, <c>Control</c>,
    /// <c>Alt</c>, <c>Meta</c>, <c>ShiftLeft</c>.
    /// </para>
    /// <para>
    /// Holding down <c>Shift</c> will type the text that corresponds to the <paramref name="key"/>
    /// in the upper case.
    /// </para>
    /// <para>
    /// If <paramref name="key"/> is a single character, it is case-sensitive, so the values
    /// <c>a</c> and <c>A</c> will generate different respective texts.
    /// </para>
    /// <para>
    /// Shortcuts such as <c>key: "Control+o"</c> or <c>key: "Control+Shift+T"</c> are supported
    /// as well. When specified with the modifier, modifier is pressed and being held while
    /// the subsequent key is being pressed.
    /// </para>
    /// <code>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.GotoAsync("https://keycode.info");<br/>
    /// await page.PressAsync("body", "A");<br/>
    /// await page.ScreenshotAsync(new PageScreenshotOptions { Path = "A.png" });<br/>
    /// await page.PressAsync("body", "ArrowLeft");<br/>
    /// await page.ScreenshotAsync(new PageScreenshotOptions { Path = "ArrowLeft.png" });<br/>
    /// await page.PressAsync("body", "Shift+O");<br/>
    /// await page.ScreenshotAsync(new PageScreenshotOptions { Path = "O.png" });
    /// </code>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="key">
    /// Name of the key to press or a character to generate, such as <c>ArrowLeft</c> or
    /// <c>a</c>.
    /// </param>
    /// <param name="options">Call options</param>
    Task PressAsync(string selector, string key, PagePressOptions? options = default);

    /// <summary>
    /// <para>
    /// The method finds an element matching the specified selector within the page. If
    /// no elements match the selector, the return value resolves to <c>null</c>. To wait
    /// for an element on the page, use <see cref="ILocator.WaitForAsync"/>.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.QuerySelectorAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The use of <see cref="IElementHandle"/> is discouraged, use <see cref="ILocator"/>
    /// objects and web-first assertions instead.
    /// </para>
    /// </remarks>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IElementHandle?> QuerySelectorAsync(string selector, PageQuerySelectorOptions? options = default);

    /// <summary>
    /// <para>
    /// The method finds all elements matching the specified selector within the page. If
    /// no elements match the selector, the return value resolves to <c>[]</c>.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.QuerySelectorAllAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The use of <see cref="IElementHandle"/> is discouraged, use <see cref="ILocator"/>
    /// objects and web-first assertions instead.
    /// </para>
    /// </remarks>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector);

    /// <summary>
    /// <para>
    /// This method reloads the current page, in the same way as if the user had triggered
    /// a browser refresh. Returns the main resource response. In case of multiple redirects,
    /// the navigation will resolve with the response of the last redirect.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IResponse?> ReloadAsync(PageReloadOptions? options = default);

    /// <summary>
    /// <para>
    /// API testing helper associated with this page. This method returns the same instance
    /// as <see cref="IBrowserContext.APIRequest"/> on the page's context. See <see cref="IBrowserContext.APIRequest"/>
    /// for more details.
    /// </para>
    /// </summary>
    public IAPIRequestContext APIRequest { get; }

    /// <summary>
    /// <para>Routing provides the capability to modify network requests that are made by a page.</para>
    /// <para>
    /// Once routing is enabled, every request matching the url pattern will stall unless
    /// it's continued, fulfilled or aborted.
    /// </para>
    /// <para>An example of a naive handler that aborts all image requests:</para>
    /// <code>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.RouteAsync("**/*.{png,jpg,jpeg}", async r =&gt; await r.AbortAsync());<br/>
    /// await page.GotoAsync("https://www.microsoft.com");
    /// </code>
    /// <para>or the same snippet using a regex pattern instead:</para>
    /// <code>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.RouteAsync(new Regex("(\\.png$)|(\\.jpg$)"), async r =&gt; await r.AbortAsync());<br/>
    /// await page.GotoAsync("https://www.microsoft.com");
    /// </code>
    /// <para>
    /// It is possible to examine the request to decide the route action. For example, mocking
    /// all requests that contain some post data, and leaving all other requests as is:
    /// </para>
    /// <code>
    /// await page.RouteAsync("/api/**", async r =&gt;<br/>
    /// {<br/>
    ///   if (r.Request.PostData.Contains("my-string"))<br/>
    ///       await r.FulfillAsync(new RouteFulfillOptions { Body = "mocked-data" });<br/>
    ///   else<br/>
    ///       await r.ContinueAsync();<br/>
    /// });
    /// </code>
    /// <para>
    /// Page routes take precedence over browser context routes (set up with <see cref="IBrowserContext.RouteAsync"/>)
    /// when request matches both handlers.
    /// </para>
    /// <para>To remove a route with its handler you can use <see cref="IPage.UnrouteAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>The handler will only be called for the first url if the response is a redirect.</para>
    /// <para>
    /// <see cref="IPage.RouteAsync"/> will not intercept requests intercepted by Service
    /// Worker. See <a href="https://github.com/microsoft/playwright/issues/1090">this</a>
    /// issue. We recommend disabling Service Workers when using request interception by
    /// setting <paramref name="Browser.newContext.serviceWorkers"/> to <c>'block'</c>.
    /// </para>
    /// <para>Enabling routing disables http cache.</para>
    /// </remarks>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing. When a <paramref name="baseURL"/> via the context options was provided
    /// and the passed URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="handler">handler function to route the request.</param>
    /// <param name="options">Call options</param>
    Task RouteAsync(string url, Action<IRoute> handler, PageRouteOptions? options = default);

    /// <summary>
    /// <para>Routing provides the capability to modify network requests that are made by a page.</para>
    /// <para>
    /// Once routing is enabled, every request matching the url pattern will stall unless
    /// it's continued, fulfilled or aborted.
    /// </para>
    /// <para>An example of a naive handler that aborts all image requests:</para>
    /// <code>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.RouteAsync("**/*.{png,jpg,jpeg}", async r =&gt; await r.AbortAsync());<br/>
    /// await page.GotoAsync("https://www.microsoft.com");
    /// </code>
    /// <para>or the same snippet using a regex pattern instead:</para>
    /// <code>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.RouteAsync(new Regex("(\\.png$)|(\\.jpg$)"), async r =&gt; await r.AbortAsync());<br/>
    /// await page.GotoAsync("https://www.microsoft.com");
    /// </code>
    /// <para>
    /// It is possible to examine the request to decide the route action. For example, mocking
    /// all requests that contain some post data, and leaving all other requests as is:
    /// </para>
    /// <code>
    /// await page.RouteAsync("/api/**", async r =&gt;<br/>
    /// {<br/>
    ///   if (r.Request.PostData.Contains("my-string"))<br/>
    ///       await r.FulfillAsync(new RouteFulfillOptions { Body = "mocked-data" });<br/>
    ///   else<br/>
    ///       await r.ContinueAsync();<br/>
    /// });
    /// </code>
    /// <para>
    /// Page routes take precedence over browser context routes (set up with <see cref="IBrowserContext.RouteAsync"/>)
    /// when request matches both handlers.
    /// </para>
    /// <para>To remove a route with its handler you can use <see cref="IPage.UnrouteAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>The handler will only be called for the first url if the response is a redirect.</para>
    /// <para>
    /// <see cref="IPage.RouteAsync"/> will not intercept requests intercepted by Service
    /// Worker. See <a href="https://github.com/microsoft/playwright/issues/1090">this</a>
    /// issue. We recommend disabling Service Workers when using request interception by
    /// setting <paramref name="Browser.newContext.serviceWorkers"/> to <c>'block'</c>.
    /// </para>
    /// <para>Enabling routing disables http cache.</para>
    /// </remarks>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing. When a <paramref name="baseURL"/> via the context options was provided
    /// and the passed URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="handler">handler function to route the request.</param>
    /// <param name="options">Call options</param>
    Task RouteAsync(Regex url, Action<IRoute> handler, PageRouteOptions? options = default);

    /// <summary>
    /// <para>Routing provides the capability to modify network requests that are made by a page.</para>
    /// <para>
    /// Once routing is enabled, every request matching the url pattern will stall unless
    /// it's continued, fulfilled or aborted.
    /// </para>
    /// <para>An example of a naive handler that aborts all image requests:</para>
    /// <code>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.RouteAsync("**/*.{png,jpg,jpeg}", async r =&gt; await r.AbortAsync());<br/>
    /// await page.GotoAsync("https://www.microsoft.com");
    /// </code>
    /// <para>or the same snippet using a regex pattern instead:</para>
    /// <code>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.RouteAsync(new Regex("(\\.png$)|(\\.jpg$)"), async r =&gt; await r.AbortAsync());<br/>
    /// await page.GotoAsync("https://www.microsoft.com");
    /// </code>
    /// <para>
    /// It is possible to examine the request to decide the route action. For example, mocking
    /// all requests that contain some post data, and leaving all other requests as is:
    /// </para>
    /// <code>
    /// await page.RouteAsync("/api/**", async r =&gt;<br/>
    /// {<br/>
    ///   if (r.Request.PostData.Contains("my-string"))<br/>
    ///       await r.FulfillAsync(new RouteFulfillOptions { Body = "mocked-data" });<br/>
    ///   else<br/>
    ///       await r.ContinueAsync();<br/>
    /// });
    /// </code>
    /// <para>
    /// Page routes take precedence over browser context routes (set up with <see cref="IBrowserContext.RouteAsync"/>)
    /// when request matches both handlers.
    /// </para>
    /// <para>To remove a route with its handler you can use <see cref="IPage.UnrouteAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>The handler will only be called for the first url if the response is a redirect.</para>
    /// <para>
    /// <see cref="IPage.RouteAsync"/> will not intercept requests intercepted by Service
    /// Worker. See <a href="https://github.com/microsoft/playwright/issues/1090">this</a>
    /// issue. We recommend disabling Service Workers when using request interception by
    /// setting <paramref name="Browser.newContext.serviceWorkers"/> to <c>'block'</c>.
    /// </para>
    /// <para>Enabling routing disables http cache.</para>
    /// </remarks>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing. When a <paramref name="baseURL"/> via the context options was provided
    /// and the passed URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="handler">handler function to route the request.</param>
    /// <param name="options">Call options</param>
    Task RouteAsync(Func<string, bool> url, Action<IRoute> handler, PageRouteOptions? options = default);

    /// <summary>
    /// <para>
    /// If specified the network requests that are made in the page will be served from
    /// the HAR file. Read more about <a href="https://playwright.dev/dotnet/docs/network#replaying-from-har">Replaying
    /// from HAR</a>.
    /// </para>
    /// <para>
    /// Playwright will not serve requests intercepted by Service Worker from the HAR file.
    /// See <a href="https://github.com/microsoft/playwright/issues/1090">this</a> issue.
    /// We recommend disabling Service Workers when using request interception by setting
    /// <paramref name="Browser.newContext.serviceWorkers"/> to <c>'block'</c>.
    /// </para>
    /// </summary>
    /// <param name="har">
    /// Path to a <a href="http://www.softwareishard.com/blog/har-12-spec">HAR</a> file
    /// with prerecorded network data. If <c>path</c> is a relative path, then it is resolved
    /// relative to the current working directory.
    /// </param>
    /// <param name="options">Call options</param>
    Task RouteFromHARAsync(string har, PageRouteFromHAROptions? options = default);

    /// <summary><para>Returns the buffer with the captured screenshot.</para></summary>
    /// <param name="options">Call options</param>
    Task<byte[]> ScreenshotAsync(PageScreenshotOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for an element matching <paramref name="selector"/>, waits for
    /// <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a> checks,
    /// waits until all specified options are present in the <c>&lt;select&gt;</c> element
    /// and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "blue" });<br/>
    /// // single selection matching both the value and the label<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "red", "green", "blue" });
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.SelectOptionAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(string selector, string values, PageSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for an element matching <paramref name="selector"/>, waits for
    /// <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a> checks,
    /// waits until all specified options are present in the <c>&lt;select&gt;</c> element
    /// and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "blue" });<br/>
    /// // single selection matching both the value and the label<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "red", "green", "blue" });
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.SelectOptionAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IElementHandle values, PageSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for an element matching <paramref name="selector"/>, waits for
    /// <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a> checks,
    /// waits until all specified options are present in the <c>&lt;select&gt;</c> element
    /// and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "blue" });<br/>
    /// // single selection matching both the value and the label<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "red", "green", "blue" });
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.SelectOptionAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<string> values, PageSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for an element matching <paramref name="selector"/>, waits for
    /// <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a> checks,
    /// waits until all specified options are present in the <c>&lt;select&gt;</c> element
    /// and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "blue" });<br/>
    /// // single selection matching both the value and the label<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "red", "green", "blue" });
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.SelectOptionAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(string selector, SelectOptionValue values, PageSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for an element matching <paramref name="selector"/>, waits for
    /// <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a> checks,
    /// waits until all specified options are present in the <c>&lt;select&gt;</c> element
    /// and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "blue" });<br/>
    /// // single selection matching both the value and the label<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "red", "green", "blue" });
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.SelectOptionAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, PageSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for an element matching <paramref name="selector"/>, waits for
    /// <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a> checks,
    /// waits until all specified options are present in the <c>&lt;select&gt;</c> element
    /// and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "blue" });<br/>
    /// // single selection matching both the value and the label<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple<br/>
    /// await page.SelectOptionAsync("select#colors", new[] { "red", "green", "blue" });
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.SelectOptionAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, PageSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method checks or unchecks an element matching <paramref name="selector"/> by
    /// performing the following steps:
    /// </para>
    /// <list type="ordinal">
    /// <item><description>
    /// Find an element matching <paramref name="selector"/>. If there is none, wait until
    /// a matching element is attached to the DOM.
    /// </description></item>
    /// <item><description>
    /// Ensure that matched element is a checkbox or a radio input. If not, this method
    /// throws.
    /// </description></item>
    /// <item><description>If the element already has the right checked state, this method returns immediately.</description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the matched element, unless <paramref name="force"/> option is set. If
    /// the element is detached during the checks, the whole action is retried.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// <item><description>Ensure that the element is now checked or unchecked. If not, this method throws.</description></item>
    /// </list>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.SetCheckedAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="checkedState">Whether to check or uncheck the checkbox.</param>
    /// <param name="options">Call options</param>
    Task SetCheckedAsync(string selector, bool checkedState, PageSetCheckedOptions? options = default);

    /// <param name="html">HTML markup to assign to the page.</param>
    /// <param name="options">Call options</param>
    Task SetContentAsync(string html, PageSetContentOptions? options = default);

    /// <summary>
    /// <para>
    /// This setting will change the default maximum navigation time for the following methods
    /// and related shortcuts:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="IPage.GoBackAsync"/></description></item>
    /// <item><description><see cref="IPage.GoForwardAsync"/></description></item>
    /// <item><description><see cref="IPage.GotoAsync"/></description></item>
    /// <item><description><see cref="IPage.ReloadAsync"/></description></item>
    /// <item><description><see cref="IPage.SetContentAsync"/></description></item>
    /// <item><description><see cref="IPage.RunAndWaitForNavigationAsync"/></description></item>
    /// <item><description><see cref="IPage.WaitForURLAsync"/></description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPage.SetDefaultNavigationTimeout"/> takes priority over <see cref="IPage.SetDefaultTimeout"/>,
    /// <see cref="IBrowserContext.SetDefaultTimeout"/> and <see cref="IBrowserContext.SetDefaultNavigationTimeout"/>.
    /// </para>
    /// </remarks>
    /// <param name="timeout">Maximum navigation time in milliseconds</param>
    void SetDefaultNavigationTimeout(float timeout);

    /// <summary>
    /// <para>
    /// This setting will change the default maximum time for all the methods accepting
    /// <paramref name="timeout"/> option.
    /// </para>
    /// </summary>
    /// <remarks><para><see cref="IPage.SetDefaultNavigationTimeout"/> takes priority over <see cref="IPage.SetDefaultTimeout"/>.</para></remarks>
    /// <param name="timeout">Maximum time in milliseconds</param>
    void SetDefaultTimeout(float timeout);

    /// <summary><para>The extra HTTP headers will be sent with every request the page initiates.</para></summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPage.SetExtraHTTPHeadersAsync"/> does not guarantee the order of headers
    /// in the outgoing requests.
    /// </para>
    /// </remarks>
    /// <param name="headers">
    /// An object containing additional HTTP headers to be sent with every request. All
    /// header values must be strings.
    /// </param>
    Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <paramref name="selector"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(string selector, string files, PageSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <paramref name="selector"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(string selector, IEnumerable<string> files, PageSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <paramref name="selector"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(string selector, FilePayload files, PageSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <paramref name="selector"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, PageSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// In the case of multiple pages in a single browser, each page can have its own viewport
    /// size. However, <see cref="IBrowser.NewContextAsync"/> allows to set viewport size
    /// (and more) for all pages in the context at once.
    /// </para>
    /// <para>
    /// <see cref="IPage.SetViewportSizeAsync"/> will resize the page. A lot of websites
    /// don't expect phones to change size, so you should set the viewport size before navigating
    /// to the page. <see cref="IPage.SetViewportSizeAsync"/> will also reset <c>screen</c>
    /// size, use <see cref="IBrowser.NewContextAsync"/> with <c>screen</c> and <c>viewport</c>
    /// parameters if you need better control of these properties.
    /// </para>
    /// <code>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.SetViewportSizeAsync(640, 480);<br/>
    /// await page.GotoAsync("https://www.microsoft.com");
    /// </code>
    /// </summary>
    /// <param name="width">
    /// </param>
    /// <param name="height">
    /// </param>
    Task SetViewportSizeAsync(int width, int height);

    /// <summary>
    /// <para>
    /// This method taps an element matching <paramref name="selector"/> by performing the
    /// following steps:
    /// </para>
    /// <list type="ordinal">
    /// <item><description>
    /// Find an element matching <paramref name="selector"/>. If there is none, wait until
    /// a matching element is attached to the DOM.
    /// </description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the matched element, unless <paramref name="force"/> option is set. If
    /// the element is detached during the checks, the whole action is retried.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>
    /// Use <see cref="IPage.Touchscreen"/> to tap the center of the element, or the specified
    /// <paramref name="position"/>.
    /// </description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// </list>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.TapAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPage.TapAsync"/> requires that the <paramref name="hasTouch"/> option
    /// of the browser context be set to true.
    /// </para>
    /// </remarks>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task TapAsync(string selector, PageTapOptions? options = default);

    /// <summary><para>Returns <c>element.textContent</c>.</para></summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<string?> TextContentAsync(string selector, PageTextContentOptions? options = default);

    /// <summary><para>Returns the page's title. Shortcut for main frame's <see cref="IFrame.TitleAsync"/>.</para></summary>
    Task<string> TitleAsync();

    public ITouchscreen Touchscreen { get; }

    /// <summary>
    /// <para>
    /// Sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>, and <c>keyup</c> event for
    /// each character in the text. <c>page.type</c> can be used to send fine-grained keyboard
    /// events. To fill values in form fields, use <see cref="IPage.FillAsync"/>.
    /// </para>
    /// <para>To press a special key, like <c>Control</c> or <c>ArrowDown</c>, use <see cref="IKeyboard.PressAsync"/>.</para>
    /// <code>
    /// await page.TypeAsync("#mytextarea", "hello"); // types instantly<br/>
    /// await page.TypeAsync("#mytextarea", "world", new() { Delay = 100 }); // types slower, like a user
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.TypeAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="text">A text to type into a focused element.</param>
    /// <param name="options">Call options</param>
    Task TypeAsync(string selector, string text, PageTypeOptions? options = default);

    /// <summary>
    /// <para>
    /// This method unchecks an element matching <paramref name="selector"/> by performing
    /// the following steps:
    /// </para>
    /// <list type="ordinal">
    /// <item><description>
    /// Find an element matching <paramref name="selector"/>. If there is none, wait until
    /// a matching element is attached to the DOM.
    /// </description></item>
    /// <item><description>
    /// Ensure that matched element is a checkbox or a radio input. If not, this method
    /// throws. If the element is already unchecked, this method returns immediately.
    /// </description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the matched element, unless <paramref name="force"/> option is set. If
    /// the element is detached during the checks, the whole action is retried.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// <item><description>Ensure that the element is now unchecked. If not, this method throws.</description></item>
    /// </list>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// <para>Shortcut for main frame's <see cref="IFrame.UncheckAsync"/>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to search for an element. If there are multiple elements satisfying the
    /// selector, the first will be used. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task UncheckAsync(string selector, PageUncheckOptions? options = default);

    /// <summary>
    /// <para>
    /// Removes a route created with <see cref="IPage.RouteAsync"/>. When <paramref name="handler"/>
    /// is not specified, removes all routes for the <paramref name="url"/>.
    /// </para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing.
    /// </param>
    /// <param name="handler">Optional handler function to route the request.</param>
    Task UnrouteAsync(string url, Action<IRoute>? handler = default);

    /// <summary>
    /// <para>
    /// Removes a route created with <see cref="IPage.RouteAsync"/>. When <paramref name="handler"/>
    /// is not specified, removes all routes for the <paramref name="url"/>.
    /// </para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing.
    /// </param>
    /// <param name="handler">Optional handler function to route the request.</param>
    Task UnrouteAsync(Regex url, Action<IRoute>? handler = default);

    /// <summary>
    /// <para>
    /// Removes a route created with <see cref="IPage.RouteAsync"/>. When <paramref name="handler"/>
    /// is not specified, removes all routes for the <paramref name="url"/>.
    /// </para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing.
    /// </param>
    /// <param name="handler">Optional handler function to route the request.</param>
    Task UnrouteAsync(Func<string, bool> url, Action<IRoute>? handler = default);

    /// <summary><para>Shortcut for main frame's <see cref="IFrame.Url"/>.</para></summary>
    string Url { get; }

    /// <summary><para>Video object associated with this page.</para></summary>
    IVideo? Video { get; }

    PageViewportSizeResult? ViewportSize { get; }

    /// <summary>
    /// <para>
    /// Performs action and waits for a <see cref="IConsoleMessage"/> to be logged by in
    /// the page. If predicate is provided, it passes <see cref="IConsoleMessage"/> value
    /// into the <c>predicate</c> function and waits for <c>predicate(message)</c> to return
    /// a truthy value. Will throw an error if the page is closed before the <see cref="IPage.Console"/>
    /// event is fired.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IConsoleMessage> WaitForConsoleMessageAsync(PageWaitForConsoleMessageOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a <see cref="IConsoleMessage"/> to be logged by in
    /// the page. If predicate is provided, it passes <see cref="IConsoleMessage"/> value
    /// into the <c>predicate</c> function and waits for <c>predicate(message)</c> to return
    /// a truthy value. Will throw an error if the page is closed before the <see cref="IPage.Console"/>
    /// event is fired.
    /// </para>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IConsoleMessage> RunAndWaitForConsoleMessageAsync(Func<Task> action, PageRunAndWaitForConsoleMessageOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IDownload"/>. If predicate is provided,
    /// it passes <see cref="IDownload"/> value into the <c>predicate</c> function and waits
    /// for <c>predicate(download)</c> to return a truthy value. Will throw an error if
    /// the page is closed before the download event is fired.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IDownload> WaitForDownloadAsync(PageWaitForDownloadOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IDownload"/>. If predicate is provided,
    /// it passes <see cref="IDownload"/> value into the <c>predicate</c> function and waits
    /// for <c>predicate(download)</c> to return a truthy value. Will throw an error if
    /// the page is closed before the download event is fired.
    /// </para>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IDownload> RunAndWaitForDownloadAsync(Func<Task> action, PageRunAndWaitForDownloadOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IFileChooser"/> to be created. If
    /// predicate is provided, it passes <see cref="IFileChooser"/> value into the <c>predicate</c>
    /// function and waits for <c>predicate(fileChooser)</c> to return a truthy value. Will
    /// throw an error if the page is closed before the file chooser is opened.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IFileChooser> WaitForFileChooserAsync(PageWaitForFileChooserOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IFileChooser"/> to be created. If
    /// predicate is provided, it passes <see cref="IFileChooser"/> value into the <c>predicate</c>
    /// function and waits for <c>predicate(fileChooser)</c> to return a truthy value. Will
    /// throw an error if the page is closed before the file chooser is opened.
    /// </para>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IFileChooser> RunAndWaitForFileChooserAsync(Func<Task> action, PageRunAndWaitForFileChooserOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns when the <paramref name="expression"/> returns a truthy value. It resolves
    /// to a JSHandle of the truthy value.
    /// </para>
    /// <para>
    /// The <see cref="IPage.WaitForFunctionAsync"/> can be used to observe viewport size
    /// change:
    /// </para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// using System.Threading.Tasks;<br/>
    /// <br/>
    /// class FrameExamples<br/>
    /// {<br/>
    ///   public static async Task WaitForFunction()<br/>
    ///   {<br/>
    ///     using var playwright = await Playwright.CreateAsync();<br/>
    ///     await using var browser = await playwright.Webkit.LaunchAsync();<br/>
    ///     var page = await browser.NewPageAsync();<br/>
    ///     await page.SetViewportSizeAsync(50, 50);<br/>
    ///     await page.MainFrame.WaitForFunctionAsync("window.innerWidth &lt; 100");<br/>
    ///   }<br/>
    /// }
    /// </code>
    /// <para>
    /// To pass an argument to the predicate of <see cref="IPage.WaitForFunctionAsync"/>
    /// function:
    /// </para>
    /// <code>
    /// var selector = ".foo";<br/>
    /// await page.WaitForFunctionAsync("selector =&gt; !!document.querySelector(selector)", selector);
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.WaitForFunctionAsync"/>.</para>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    /// <param name="options">Call options</param>
    Task<IJSHandle> WaitForFunctionAsync(string expression, object? arg = default, PageWaitForFunctionOptions? options = default);

    /// <summary>
    /// <para>Returns when the required load state has been reached.</para>
    /// <para>
    /// This resolves when the page reaches a required load state, <c>load</c> by default.
    /// The navigation must have been committed when this method is called. If current document
    /// has already reached the required state, resolves immediately.
    /// </para>
    /// <code>
    /// await page.GetByRole("button").ClickAsync(); // Click triggers navigation.<br/>
    /// await page.WaitForLoadStateAsync(); // The promise resolves after 'load' event.
    /// </code>
    /// <code>
    /// var popup = await page.RunAndWaitForPopupAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.GetByRole("button").ClickAsync(); // click triggers the popup/<br/>
    /// });<br/>
    /// await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);<br/>
    /// Console.WriteLine(await popup.TitleAsync()); // popup is ready to use.
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.WaitForLoadStateAsync"/>.</para>
    /// </summary>
    /// <param name="state">
    /// Optional load state to wait for, defaults to <c>load</c>. If the state has been
    /// already reached while loading current document, the method resolves immediately.
    /// Can be one of:
    /// <list type="bullet">
    /// <item><description><c>'load'</c> - wait for the <c>load</c> event to be fired.</description></item>
    /// <item><description><c>'domcontentloaded'</c> - wait for the <c>DOMContentLoaded</c> event to be fired.</description></item>
    /// <item><description>
    /// <c>'networkidle'</c> - wait until there are no network connections for at least
    /// <c>500</c> ms.
    /// </description></item>
    /// </list>
    /// </param>
    /// <param name="options">Call options</param>
    Task WaitForLoadStateAsync(LoadState? state = default, PageWaitForLoadStateOptions? options = default);

    /// <summary>
    /// <para>
    /// Waits for the main frame navigation and returns the main resource response. In case
    /// of multiple redirects, the navigation will resolve with the response of the last
    /// redirect. In case of navigation to a different anchor or navigation due to History
    /// API usage, the navigation will resolve with <c>null</c>.
    /// </para>
    /// <para>
    /// This resolves when the page navigates to a new URL or reloads. It is useful for
    /// when you run code which will indirectly cause the page to navigate. e.g. The click
    /// target has an <c>onclick</c> handler that triggers navigation from a <c>setTimeout</c>.
    /// Consider this example:
    /// </para>
    /// <code>
    /// await page.RunAndWaitForNavigationAsync(async () =&gt;<br/>
    /// {<br/>
    ///     // Clicking the link will indirectly cause a navigation.<br/>
    ///     await page.ClickAsync("a.delayed-navigation");<br/>
    /// });<br/>
    /// <br/>
    /// // The method continues after navigation has finished
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.RunAndWaitForNavigationAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Usage of the <a href="https://developer.mozilla.org/en-US/docs/Web/API/History_API">History
    /// API</a> to change the URL is considered a navigation.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    Task<IResponse?> WaitForNavigationAsync(PageWaitForNavigationOptions? options = default);

    /// <summary>
    /// <para>
    /// Waits for the main frame navigation and returns the main resource response. In case
    /// of multiple redirects, the navigation will resolve with the response of the last
    /// redirect. In case of navigation to a different anchor or navigation due to History
    /// API usage, the navigation will resolve with <c>null</c>.
    /// </para>
    /// <para>
    /// This resolves when the page navigates to a new URL or reloads. It is useful for
    /// when you run code which will indirectly cause the page to navigate. e.g. The click
    /// target has an <c>onclick</c> handler that triggers navigation from a <c>setTimeout</c>.
    /// Consider this example:
    /// </para>
    /// <code>
    /// await page.RunAndWaitForNavigationAsync(async () =&gt;<br/>
    /// {<br/>
    ///     // Clicking the link will indirectly cause a navigation.<br/>
    ///     await page.ClickAsync("a.delayed-navigation");<br/>
    /// });<br/>
    /// <br/>
    /// // The method continues after navigation has finished
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.RunAndWaitForNavigationAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Usage of the <a href="https://developer.mozilla.org/en-US/docs/Web/API/History_API">History
    /// API</a> to change the URL is considered a navigation.
    /// </para>
    /// </remarks>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IResponse?> RunAndWaitForNavigationAsync(Func<Task> action, PageRunAndWaitForNavigationOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a popup <see cref="IPage"/>. If predicate is provided,
    /// it passes <see cref="Popup"/> value into the <c>predicate</c> function and waits
    /// for <c>predicate(page)</c> to return a truthy value. Will throw an error if the
    /// page is closed before the popup event is fired.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IPage> WaitForPopupAsync(PageWaitForPopupOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a popup <see cref="IPage"/>. If predicate is provided,
    /// it passes <see cref="Popup"/> value into the <c>predicate</c> function and waits
    /// for <c>predicate(page)</c> to return a truthy value. Will throw an error if the
    /// page is closed before the popup event is fired.
    /// </para>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IPage> RunAndWaitForPopupAsync(Func<Task> action, PageRunAndWaitForPopupOptions? options = default);

    /// <summary>
    /// <para>
    /// Waits for the matching request and returns it. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next request with the specified url.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, request =&gt; request.Url == "https://example.com" &amp;&amp; request.Method == "GET");
    /// </code>
    /// </summary>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IRequest"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IRequest> WaitForRequestAsync(string urlOrPredicate, PageWaitForRequestOptions? options = default);

    /// <summary>
    /// <para>
    /// Waits for the matching request and returns it. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next request with the specified url.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, request =&gt; request.Url == "https://example.com" &amp;&amp; request.Method == "GET");
    /// </code>
    /// </summary>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IRequest"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IRequest> WaitForRequestAsync(Regex urlOrPredicate, PageWaitForRequestOptions? options = default);

    /// <summary>
    /// <para>
    /// Waits for the matching request and returns it. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next request with the specified url.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, request =&gt; request.Url == "https://example.com" &amp;&amp; request.Method == "GET");
    /// </code>
    /// </summary>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IRequest"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IRequest> WaitForRequestAsync(Func<IRequest, bool> urlOrPredicate, PageWaitForRequestOptions? options = default);

    /// <summary>
    /// <para>
    /// Waits for the matching request and returns it. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next request with the specified url.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, request =&gt; request.Url == "https://example.com" &amp;&amp; request.Method == "GET");
    /// </code>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IRequest"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, string urlOrPredicate, PageRunAndWaitForRequestOptions? options = default);

    /// <summary>
    /// <para>
    /// Waits for the matching request and returns it. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next request with the specified url.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, request =&gt; request.Url == "https://example.com" &amp;&amp; request.Method == "GET");
    /// </code>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IRequest"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, Regex urlOrPredicate, PageRunAndWaitForRequestOptions? options = default);

    /// <summary>
    /// <para>
    /// Waits for the matching request and returns it. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next request with the specified url.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForRequestAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, request =&gt; request.Url == "https://example.com" &amp;&amp; request.Method == "GET");
    /// </code>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IRequest"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, Func<IRequest, bool> urlOrPredicate, PageRunAndWaitForRequestOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a <see cref="IRequest"/> to finish loading. If predicate
    /// is provided, it passes <see cref="IRequest"/> value into the <c>predicate</c> function
    /// and waits for <c>predicate(request)</c> to return a truthy value. Will throw an
    /// error if the page is closed before the <see cref="IPage.RequestFinished"/> event
    /// is fired.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IRequest> WaitForRequestFinishedAsync(PageWaitForRequestFinishedOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a <see cref="IRequest"/> to finish loading. If predicate
    /// is provided, it passes <see cref="IRequest"/> value into the <c>predicate</c> function
    /// and waits for <c>predicate(request)</c> to return a truthy value. Will throw an
    /// error if the page is closed before the <see cref="IPage.RequestFinished"/> event
    /// is fired.
    /// </para>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IRequest> RunAndWaitForRequestFinishedAsync(Func<Task> action, PageRunAndWaitForRequestFinishedOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the matched response. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next response with the specified url.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button.triggers-response");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, response =&gt; response.Url == "https://example.com" &amp;&amp; response.Status == 200);
    /// </code>
    /// </summary>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IResponse"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IResponse> WaitForResponseAsync(string urlOrPredicate, PageWaitForResponseOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the matched response. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next response with the specified url.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button.triggers-response");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, response =&gt; response.Url == "https://example.com" &amp;&amp; response.Status == 200);
    /// </code>
    /// </summary>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IResponse"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IResponse> WaitForResponseAsync(Regex urlOrPredicate, PageWaitForResponseOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the matched response. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next response with the specified url.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button.triggers-response");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, response =&gt; response.Url == "https://example.com" &amp;&amp; response.Status == 200);
    /// </code>
    /// </summary>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IResponse"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IResponse> WaitForResponseAsync(Func<IResponse, bool> urlOrPredicate, PageWaitForResponseOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the matched response. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next response with the specified url.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button.triggers-response");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, response =&gt; response.Url == "https://example.com" &amp;&amp; response.Status == 200);
    /// </code>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IResponse"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, string urlOrPredicate, PageRunAndWaitForResponseOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the matched response. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next response with the specified url.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button.triggers-response");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, response =&gt; response.Url == "https://example.com" &amp;&amp; response.Status == 200);
    /// </code>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IResponse"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, Regex urlOrPredicate, PageRunAndWaitForResponseOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the matched response. See <a href="https://playwright.dev/dotnet/docs/events#waiting-for-event">waiting
    /// for event</a> for more details about events.
    /// </para>
    /// <code>
    /// // Waits for the next response with the specified url.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button.triggers-response");<br/>
    /// }, "http://example.com/resource");<br/>
    /// <br/>
    /// // Alternative way with a predicate.<br/>
    /// await page.RunAndWaitForResponseAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.ClickAsync("button");<br/>
    /// }, response =&gt; response.Url == "https://example.com" &amp;&amp; response.Status == 200);
    /// </code>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="urlOrPredicate">
    /// Request URL string, regex or predicate receiving <see cref="IResponse"/> object.
    /// When a <paramref name="baseURL"/> via the context options was provided and the passed
    /// URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, Func<IResponse, bool> urlOrPredicate, PageRunAndWaitForResponseOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns when element specified by selector satisfies <paramref name="state"/> option.
    /// Returns <c>null</c> if waiting for <c>hidden</c> or <c>detached</c>.
    /// </para>
    /// <para>
    /// Wait for the <paramref name="selector"/> to satisfy <paramref name="state"/> option
    /// (either appear/disappear from dom, or become visible/hidden). If at the moment of
    /// calling the method <paramref name="selector"/> already satisfies the condition,
    /// the method will return immediately. If the selector doesn't satisfy the condition
    /// for the <paramref name="timeout"/> milliseconds, the function will throw.
    /// </para>
    /// <para>This method works across navigations:</para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// using System;<br/>
    /// using System.Threading.Tasks;<br/>
    /// <br/>
    /// class FrameExamples<br/>
    /// {<br/>
    ///   public static async Task Images()<br/>
    ///   {<br/>
    ///       using var playwright = await Playwright.CreateAsync();<br/>
    ///       await using var browser = await playwright.Chromium.LaunchAsync();<br/>
    ///       var page = await browser.NewPageAsync();<br/>
    /// <br/>
    ///       foreach (var currentUrl in new[] { "https://www.google.com", "https://bbc.com" })<br/>
    ///       {<br/>
    ///           await page.GotoAsync(currentUrl);<br/>
    ///           var element = await page.WaitForSelectorAsync("img");<br/>
    ///           Console.WriteLine($"Loaded image: {await element.GetAttributeAsync("src")}");<br/>
    ///       }<br/>
    /// <br/>
    ///       await browser.CloseAsync();<br/>
    ///   }<br/>
    /// }
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Playwright automatically waits for element to be ready before performing an action.
    /// Using <see cref="ILocator"/> objects and web-first assertions makes the code wait-for-selector-free.
    /// </para>
    /// </remarks>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IElementHandle?> WaitForSelectorAsync(string selector, PageWaitForSelectorOptions? options = default);

    /// <summary>
    /// <para>Waits for the given <paramref name="timeout"/> in milliseconds.</para>
    /// <para>
    /// Note that <c>page.waitForTimeout()</c> should only be used for debugging. Tests
    /// using the timer in production are going to be flaky. Use signals such as network
    /// events, selectors becoming visible and others instead.
    /// </para>
    /// <code>
    /// // Wait for 1 second<br/>
    /// await page.WaitForTimeoutAsync(1000);
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.WaitForTimeoutAsync"/>.</para>
    /// </summary>
    /// <param name="timeout">A timeout to wait for</param>
    Task WaitForTimeoutAsync(float timeout);

    /// <summary>
    /// <para>Waits for the main frame to navigate to the given URL.</para>
    /// <code>
    /// await page.ClickAsync("a.delayed-navigation"); // clicking the link will indirectly cause a navigation<br/>
    /// await page.WaitForURLAsync("**/target.html");
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.WaitForURLAsync"/>.</para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while waiting for the navigation. Note that if the parameter is a string without
    /// wildcard characters, the method will wait for navigation to URL that is exactly
    /// equal to the string.
    /// </param>
    /// <param name="options">Call options</param>
    Task WaitForURLAsync(string url, PageWaitForURLOptions? options = default);

    /// <summary>
    /// <para>Waits for the main frame to navigate to the given URL.</para>
    /// <code>
    /// await page.ClickAsync("a.delayed-navigation"); // clicking the link will indirectly cause a navigation<br/>
    /// await page.WaitForURLAsync("**/target.html");
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.WaitForURLAsync"/>.</para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while waiting for the navigation. Note that if the parameter is a string without
    /// wildcard characters, the method will wait for navigation to URL that is exactly
    /// equal to the string.
    /// </param>
    /// <param name="options">Call options</param>
    Task WaitForURLAsync(Regex url, PageWaitForURLOptions? options = default);

    /// <summary>
    /// <para>Waits for the main frame to navigate to the given URL.</para>
    /// <code>
    /// await page.ClickAsync("a.delayed-navigation"); // clicking the link will indirectly cause a navigation<br/>
    /// await page.WaitForURLAsync("**/target.html");
    /// </code>
    /// <para>Shortcut for main frame's <see cref="IFrame.WaitForURLAsync"/>.</para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while waiting for the navigation. Note that if the parameter is a string without
    /// wildcard characters, the method will wait for navigation to URL that is exactly
    /// equal to the string.
    /// </param>
    /// <param name="options">Call options</param>
    Task WaitForURLAsync(Func<string, bool> url, PageWaitForURLOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IWebSocket"/>. If predicate is provided,
    /// it passes <see cref="IWebSocket"/> value into the <c>predicate</c> function and
    /// waits for <c>predicate(webSocket)</c> to return a truthy value. Will throw an error
    /// if the page is closed before the WebSocket event is fired.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IWebSocket> WaitForWebSocketAsync(PageWaitForWebSocketOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IWebSocket"/>. If predicate is provided,
    /// it passes <see cref="IWebSocket"/> value into the <c>predicate</c> function and
    /// waits for <c>predicate(webSocket)</c> to return a truthy value. Will throw an error
    /// if the page is closed before the WebSocket event is fired.
    /// </para>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IWebSocket> RunAndWaitForWebSocketAsync(Func<Task> action, PageRunAndWaitForWebSocketOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IWorker"/>. If predicate is provided,
    /// it passes <see cref="IWorker"/> value into the <c>predicate</c> function and waits
    /// for <c>predicate(worker)</c> to return a truthy value. Will throw an error if the
    /// page is closed before the worker event is fired.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IWorker> WaitForWorkerAsync(PageWaitForWorkerOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IWorker"/>. If predicate is provided,
    /// it passes <see cref="IWorker"/> value into the <c>predicate</c> function and waits
    /// for <c>predicate(worker)</c> to return a truthy value. Will throw an error if the
    /// page is closed before the worker event is fired.
    /// </para>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IWorker> RunAndWaitForWorkerAsync(Func<Task> action, PageRunAndWaitForWorkerOptions? options = default);

    /// <summary>
    /// <para>
    /// This method returns all of the dedicated <a href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API">WebWorkers</a>
    /// associated with the page.
    /// </para>
    /// </summary>
    /// <remarks><para>This does not contain ServiceWorkers</para></remarks>
    IReadOnlyList<IWorker> Workers { get; }
}

#nullable disable
