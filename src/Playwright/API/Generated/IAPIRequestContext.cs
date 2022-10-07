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

using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// This API is used for the Web API testing. You can use it to trigger API endpoints,
/// configure micro-services, prepare environment or the service to your e2e test.
/// </para>
/// <para>
/// Each Playwright browser context has associated with it <see cref="IAPIRequestContext"/>
/// instance which shares cookie storage with the browser context and can be accessed
/// via <see cref="IBrowserContext.APIRequest"/> or <see cref="IPage.APIRequest"/>.
/// It is also possible to create a new APIRequestContext instance manually by calling
/// <see cref="IAPIRequest.NewContextAsync"/>.
/// </para>
/// <para>**Cookie management**</para>
/// <para>
/// <see cref="IAPIRequestContext"/> returned by <see cref="IBrowserContext.APIRequest"/>
/// and <see cref="IPage.APIRequest"/> shares cookie storage with the corresponding
/// <see cref="IBrowserContext"/>. Each API request will have <c>Cookie</c> header populated
/// with the values from the browser context. If the API response contains <c>Set-Cookie</c>
/// header it will automatically update <see cref="IBrowserContext"/> cookies and requests
/// made from the page will pick them up. This means that if you log in using this API,
/// your e2e test will be logged in and vice versa.
/// </para>
/// <para>
/// If you want API requests to not interfere with the browser cookies you should create
/// a new <see cref="IAPIRequestContext"/> by calling <see cref="IAPIRequest.NewContextAsync"/>.
/// Such <c>APIRequestContext</c> object will have its own isolated cookie storage.
/// </para>
/// </summary>
public partial interface IAPIRequestContext
{
    /// <summary>
    /// <para>
    /// Creates a new <see cref="IFormData"/> instance which is used for providing form
    /// and multipart data when making HTTP requests.
    /// </para>
    /// </summary>
    IFormData CreateFormData();

    /// <summary>
    /// <para>
    /// Sends HTTP(S) <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/DELETE">DELETE</a>
    /// request and returns its response. The method will populate request cookies from
    /// the context and update context cookies from the response. The method will automatically
    /// follow redirects.
    /// </para>
    /// </summary>
    /// <param name="url">Target URL.</param>
    /// <param name="options">Call options</param>
    Task<IAPIResponse> DeleteAsync(string url, APIRequestContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Sends HTTP(S) request and returns its response. The method will populate request
    /// cookies from the context and update context cookies from the response. The method
    /// will automatically follow redirects.
    /// </para>
    /// <para>JSON objects can be passed directly to the request:</para>
    /// <code>
    /// var data = new Dictionary&lt;string, object&gt;() {<br/>
    ///   { "title", "Book Title" },<br/>
    ///   { "body", "John Doe" }<br/>
    /// };<br/>
    /// await Request.FetchAsync("https://example.com/api/createBook", new() { Method = "post", DataObject = data });
    /// </code>
    /// <para>
    /// The common way to send file(s) in the body of a request is to encode it as form
    /// fields with <c>multipart/form-data</c> encoding. You can achieve that with Playwright
    /// API like this:
    /// </para>
    /// <code>
    /// var file = new FilePayload()<br/>
    /// {<br/>
    ///     Name = "f.js",<br/>
    ///     MimeType = "text/javascript",<br/>
    ///     Buffer = System.Text.Encoding.UTF8.GetBytes("console.log(2022);")<br/>
    /// };<br/>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// multipart.Set("fileField", file);<br/>
    /// await Request.FetchAsync("https://example.com/api/uploadScript", new() { Method = "post", Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="urlOrRequest">Target URL or Request to get all parameters from.</param>
    /// <param name="options">Call options</param>
    Task<IAPIResponse> FetchAsync(string urlOrRequest, APIRequestContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Sends HTTP(S) request and returns its response. The method will populate request
    /// cookies from the context and update context cookies from the response. The method
    /// will automatically follow redirects.
    /// </para>
    /// <para>JSON objects can be passed directly to the request:</para>
    /// <code>
    /// var data = new Dictionary&lt;string, object&gt;() {<br/>
    ///   { "title", "Book Title" },<br/>
    ///   { "body", "John Doe" }<br/>
    /// };<br/>
    /// await Request.FetchAsync("https://example.com/api/createBook", new() { Method = "post", DataObject = data });
    /// </code>
    /// <para>
    /// The common way to send file(s) in the body of a request is to encode it as form
    /// fields with <c>multipart/form-data</c> encoding. You can achieve that with Playwright
    /// API like this:
    /// </para>
    /// <code>
    /// var file = new FilePayload()<br/>
    /// {<br/>
    ///     Name = "f.js",<br/>
    ///     MimeType = "text/javascript",<br/>
    ///     Buffer = System.Text.Encoding.UTF8.GetBytes("console.log(2022);")<br/>
    /// };<br/>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// multipart.Set("fileField", file);<br/>
    /// await Request.FetchAsync("https://example.com/api/uploadScript", new() { Method = "post", Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="urlOrRequest">Target URL or Request to get all parameters from.</param>
    /// <param name="options">Call options</param>
    Task<IAPIResponse> FetchAsync(IRequest urlOrRequest, APIRequestContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Sends HTTP(S) <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/GET">GET</a>
    /// request and returns its response. The method will populate request cookies from
    /// the context and update context cookies from the response. The method will automatically
    /// follow redirects.
    /// </para>
    /// <para>
    /// Request parameters can be configured with <c>params</c> option, they will be serialized
    /// into the URL search parameters:
    /// </para>
    /// <code>
    /// var params = new Dictionary&lt;string, object&gt;()<br/>
    /// {<br/>
    ///   { "isbn", "1234" },<br/>
    ///   { "page", 23 },<br/>
    /// }<br/>
    /// await request.GetAsync("https://example.com/api/getText", new() { Params = params });
    /// </code>
    /// </summary>
    /// <param name="url">Target URL.</param>
    /// <param name="options">Call options</param>
    Task<IAPIResponse> GetAsync(string url, APIRequestContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Sends HTTP(S) <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/HEAD">HEAD</a>
    /// request and returns its response. The method will populate request cookies from
    /// the context and update context cookies from the response. The method will automatically
    /// follow redirects.
    /// </para>
    /// </summary>
    /// <param name="url">Target URL.</param>
    /// <param name="options">Call options</param>
    Task<IAPIResponse> HeadAsync(string url, APIRequestContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Sends HTTP(S) <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/PATCH">PATCH</a>
    /// request and returns its response. The method will populate request cookies from
    /// the context and update context cookies from the response. The method will automatically
    /// follow redirects.
    /// </para>
    /// </summary>
    /// <param name="url">Target URL.</param>
    /// <param name="options">Call options</param>
    Task<IAPIResponse> PatchAsync(string url, APIRequestContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Sends HTTP(S) <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/POST">POST</a>
    /// request and returns its response. The method will populate request cookies from
    /// the context and update context cookies from the response. The method will automatically
    /// follow redirects.
    /// </para>
    /// <para>JSON objects can be passed directly to the request:</para>
    /// <code>
    /// var data = new Dictionary&lt;string, object&gt;() {<br/>
    ///   { "firstNam", "John" },<br/>
    ///   { "lastName", "Doe" }<br/>
    /// };<br/>
    /// await request.PostAsync("https://example.com/api/createBook", new() { DataObject = data });
    /// </code>
    /// <para>
    /// To send form data to the server use <c>form</c> option. Its value will be encoded
    /// into the request body with <c>application/x-www-form-urlencoded</c> encoding (see
    /// below how to use <c>multipart/form-data</c> form encoding to send files):
    /// </para>
    /// <code>
    /// var formData = Context.APIRequest.CreateFormData();<br/>
    /// formData.Set("title", "Book Title");<br/>
    /// formData.Set("body", "John Doe");<br/>
    /// await request.PostAsync("https://example.com/api/findBook", new() { Form = formData });
    /// </code>
    /// <para>
    /// The common way to send file(s) in the body of a request is to upload them as form
    /// fields with <c>multipart/form-data</c> encoding. You can achieve that with Playwright
    /// API like this:
    /// </para>
    /// <code>
    /// var file = new FilePayload()<br/>
    /// {<br/>
    ///     Name = "f.js",<br/>
    ///     MimeType = "text/javascript",<br/>
    ///     Buffer = System.Text.Encoding.UTF8.GetBytes("console.log(2022);")<br/>
    /// };<br/>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// multipart.Set("fileField", file);<br/>
    /// await request.PostAsync("https://example.com/api/uploadScript", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="url">Target URL.</param>
    /// <param name="options">Call options</param>
    Task<IAPIResponse> PostAsync(string url, APIRequestContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Sends HTTP(S) <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/PUT">PUT</a>
    /// request and returns its response. The method will populate request cookies from
    /// the context and update context cookies from the response. The method will automatically
    /// follow redirects.
    /// </para>
    /// </summary>
    /// <param name="url">Target URL.</param>
    /// <param name="options">Call options</param>
    Task<IAPIResponse> PutAsync(string url, APIRequestContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns storage state for this request context, contains current cookies and local
    /// storage snapshot if it was passed to the constructor.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<string> StorageStateAsync(APIRequestContextStorageStateOptions? options = default);
}

#nullable disable
