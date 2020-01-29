﻿using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PlaywrightSharp
{
    /// <summary>
    /// At every point of time, page exposes its current frame tree via the <see cref="IPage.MainFrame"/> and <see cref="IFrame.ChildFrames"/> methods.
    /// TODO
    /// </summary>
    public interface IFrame
    {
        /// <summary>
        /// Navigates to an URL
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        /// <remarks>
        /// <see cref="IFrame.GoToAsync(string)"/> will throw an error if:
        /// * There's an SSL error (e.g. in case of self-signed certificates).
        /// * Target URL is invalid.
        /// * The timeout is exceeded during navigation.
        /// * The remote server does not respond or is unreachable.
        /// * The main resource failed to load.
        ///
        /// <see cref="IFrame.GoToAsync(string)"/> will not throw an error when any valid HTTP status code is returned by the remote server, including 404 "Not Found" and 500 "Internal Server Error".
        /// The status code for such responses can be retrieved by calling response.status().
        ///
        /// NOTE <see cref="IFrame.GoToAsync(string)"/> either throws an error or returns a main resource response.
        /// The only exceptions are navigation to about:blank or navigation to the same URL with a different hash, which would succeed and return null.
        ///
        /// NOTE Headless mode doesn't support navigation to a PDF document. See the upstream issue.
        /// </remarks>
        Task<IResponse> GoToAsync(string url);

        /// <summary>
        /// Child frames of the this frame
        /// </summary>
        IFrame[] ChildFrames { get; }

        /// <summary>
        /// Gets the frame's name attribute as specified in the tag
        /// If the name is empty, returns the id attribute instead
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Gets the frame's url
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Adds a <c><![CDATA[<script>]]></c> tag into the page with the desired url or content
        /// </summary>
        /// <param name="options">add script tag options</param>
        /// <remarks>
        /// Shortcut for <c>page.MainFrame.AddScriptTagAsync(options)</c>
        /// </remarks>
        /// <returns>A <see cref="Task{IElementHandle}"/> that completes when the tag is added, yielding the added tag when the script's onload fires or when the script content was injected into frame</returns>
        Task<IElementHandle> AddScriptTagAsync(AddTagOptions options);
        /// <summary>
        /// Executes a function in browser context
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context</param>
        /// <param name="args">Arguments to pass to script</param>
        /// <remarks>
        /// TODO
        /// </remarks>
        /// <returns>Task that completes result of the script</returns>
        Task<T> EvaluateAsync<T>(string script, params object[] args);

        /// <summary>
        /// Executes a function in browser context
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context</param>
        /// <param name="args">Arguments to pass to script</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>Task that completes with the return value of the script</returns>
        Task<JToken> EvaluateAsync(string script, params object[] args);
    }
}