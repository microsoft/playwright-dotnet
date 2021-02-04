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
 *
 *
 * ------------------------------------------------------------------------------ 
 * <auto-generated> 
 * This code was generated by a tool at:
 * /utils/doclint/generateDotnetApi.js
 * 
 * Changes to this file may cause incorrect behavior and will be lost if 
 * the code is regenerated. 
 * </auto-generated> 
 * ------------------------------------------------------------------------------
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
	/// Whenever the page sends a request for a network resource the following sequence of events are emitted by 
	/// <see cref="IPage"/>:
	/// <list>
	/// <item><description><see cref="IPage.Request"/> emitted when the request is issued by the page.</description>
	/// </item>
	/// <item><description><see cref="IPage.Response"/> emitted when/if the response status and headers are received for the request.
	/// </description></item>
	/// <item><description><see cref="IPage.Requestfinished"/> emitted when the response body is downloaded and the request is complete.
	/// </description></item>
	/// </list>
	/// If request fails at some point, then instead of `'requestfinished'` event (and possibly instead of 'response' event), the
	/// <see cref="IPage.Requestfailed"/> event is emitted.
	/// If request gets a 'redirect' response, the request is successfully finished with the 'requestfinished' event, and a new request
	/// is  issued to a redirected url.
	/// </summary>
	public partial interface IRequest
	{
		/// <summary>
		/// The method returns `null` unless this request has failed, as reported by `requestfailed` event.
		/// Example of logging of all the failed requests:
		/// </summary>
		RequestFailureResult GetFailure();
		/// <summary>
		/// Returns the <see cref="IFrame"/> that initiated this request.
		/// </summary>
		IFrame GetFrame();
		/// <summary>
		/// An object with HTTP headers associated with the request. All header names are lower-case.
		/// </summary>
		IEnumerable<KeyValuePair<string, string>> GetHeaders();
		/// <summary>
		/// Whether this request is driving frame's navigation.
		/// </summary>
		bool IsNavigationRequest();
		/// <summary>
		/// Request's method (GET, POST, etc.)
		/// </summary>
		string GetMethod();
		/// <summary>
		/// Request's post body, if any.
		/// </summary>
		string GetPostData();
		/// <summary>
		/// Request's post body in a binary form, if any.
		/// </summary>
		byte[] GetPostDataBuffer();
		/// <summary>
		/// Returns parsed request's body for `form-urlencoded` and JSON as a fallback if any.
		/// When the response is `application/x-www-form-urlencoded` then a key/value object of the values will be returned. Otherwise
		/// it will be parsed as JSON.
		/// </summary>
		T GetPostDataJSON<T>();
		/// <summary>
		/// Request that was redirected by the server to this one, if any.
		/// When the server responds with a redirect, Playwright creates a new <see cref="IRequest"/> object. The two requests are connected
		/// by `redirectedFrom()` and `redirectedTo()` methods. When multiple server redirects has happened, it is possible to construct
		/// the whole redirect chain by repeatedly calling `redirectedFrom()`.
		/// For example, if the website `http://example.com` redirects to `https://example.com`:
		/// If the website `https://google.com` has no redirects:
		/// </summary>
		IRequest GetRedirectedFrom();
		/// <summary>
		/// New request issued by the browser if the server responded with redirect.
		/// This method is the opposite of <see cref="IRequest.RedirectedFrom"/>:
		/// </summary>
		IRequest GetRedirectedTo();
		/// <summary>
		/// Contains the request's resource type as it was perceived by the rendering engine. ResourceType will be one of the following:
		/// `document`, `stylesheet`, `image`, `media`, `font`, `script`, `texttrack`, `xhr`, `fetch`, `eventsource`, `websocket`, `manifest`,
		/// `other`.
		/// </summary>
		string GetResourceType();
		/// <summary>
		/// Returns the matching <see cref="IResponse"/> object, or `null` if the response was not received due to error.
		/// </summary>
		Task<IResponse> GetResponseAsync();
		/// <summary>
		/// Returns resource timing information for given request. Most of the timing values become available upon the response, `responseEnd`
		/// becomes available when request finishes. Find more information at <a href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceResourceTiming">Resource Timing API</a>.
		/// </summary>
		RequestTimingResult GetTiming();
		/// <summary>
		/// URL of the request.
		/// </summary>
		string GetUrl();
	}
}