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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright;

public class APIRequestNewContextOptions
{
    public APIRequestNewContextOptions() { }

    public APIRequestNewContextOptions(APIRequestNewContextOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        BaseURL = clone.BaseURL;
        ClientCertificates = clone.ClientCertificates;
        ExtraHTTPHeaders = clone.ExtraHTTPHeaders;
        FailOnStatusCode = clone.FailOnStatusCode;
        HttpCredentials = clone.HttpCredentials;
        IgnoreHTTPSErrors = clone.IgnoreHTTPSErrors;
        MaxRedirects = clone.MaxRedirects;
        Proxy = clone.Proxy;
        StorageState = clone.StorageState;
        StorageStatePath = clone.StorageStatePath;
        Timeout = clone.Timeout;
        UserAgent = clone.UserAgent;
    }

    /// <summary>
    /// <para>
    /// Methods like <see cref="IAPIRequestContext.GetAsync"/> take the base URL into consideration
    /// by using the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>URL()</c></a>
    /// constructor for building the corresponding URL. Examples:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// baseURL: <c>http://localhost:3000</c> and sending request to <c>/bar.html</c> results
    /// in <c>http://localhost:3000/bar.html</c>
    /// </description></item>
    /// <item><description>
    /// baseURL: <c>http://localhost:3000/foo/</c> and sending request to <c>./bar.html</c>
    /// results in <c>http://localhost:3000/foo/bar.html</c>
    /// </description></item>
    /// <item><description>
    /// baseURL: <c>http://localhost:3000/foo</c> (without trailing slash) and navigating
    /// to <c>./bar.html</c> results in <c>http://localhost:3000/bar.html</c>
    /// </description></item>
    /// </list>
    /// </summary>
    [JsonPropertyName("baseURL")]
    public string? BaseURL { get; set; }

    /// <summary>
    /// <para>
    /// TLS Client Authentication allows the server to request a client certificate and
    /// verify it.
    /// </para>
    /// <para>**Details**</para>
    /// <para>
    /// An array of client certificates to be used. Each certificate object must have either
    /// both <c>certPath</c> and <c>keyPath</c>, a single <c>pfxPath</c>, or their corresponding
    /// direct value equivalents (<c>cert</c> and <c>key</c>, or <c>pfx</c>). Optionally,
    /// <c>passphrase</c> property should be provided if the certificate is encrypted. The
    /// <c>origin</c> property should be provided with an exact match to the request origin
    /// that the certificate is valid for.
    /// </para>
    /// <para>
    /// Client certificate authentication is only active when at least one client certificate
    /// is provided. If you want to reject all client certificates sent by the server, you
    /// need to provide a client certificate with an <c>origin</c> that does not match any
    /// of the domains you plan to visit.
    /// </para>
    /// <para>
    /// When using WebKit on macOS, accessing <c>localhost</c> will not pick up client certificates.
    /// You can make it work by replacing <c>localhost</c> with <c>local.playwright</c>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// When using WebKit on macOS, accessing <c>localhost</c> will not pick up client certificates.
    /// You can make it work by replacing <c>localhost</c> with <c>local.playwright</c>.
    ///
    /// </para>
    /// </remarks>
    [JsonPropertyName("clientCertificates")]
    public IEnumerable<ClientCertificate>? ClientCertificates { get; set; }

    /// <summary>
    /// <para>
    /// An object containing additional HTTP headers to be sent with every request. Defaults
    /// to none.
    /// </para>
    /// </summary>
    [JsonPropertyName("extraHTTPHeaders")]
    public IEnumerable<KeyValuePair<string, string>>? ExtraHTTPHeaders { get; set; }

    /// <summary>
    /// <para>
    /// Whether to throw on response codes other than 2xx and 3xx. By default response object
    /// is returned for all status codes.
    /// </para>
    /// </summary>
    [JsonPropertyName("failOnStatusCode")]
    public bool? FailOnStatusCode { get; set; }

    /// <summary>
    /// <para>
    /// Credentials for <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication">HTTP
    /// authentication</a>. If no origin is specified, the username and password are sent
    /// to any servers upon unauthorized responses.
    /// </para>
    /// </summary>
    [JsonPropertyName("httpCredentials")]
    public HttpCredentials? HttpCredentials { get; set; }

    /// <summary><para>Whether to ignore HTTPS errors when sending network requests. Defaults to <c>false</c>.</para></summary>
    [JsonPropertyName("ignoreHTTPSErrors")]
    public bool? IgnoreHTTPSErrors { get; set; }

    /// <summary>
    /// <para>
    /// Maximum number of request redirects that will be followed automatically. An error
    /// will be thrown if the number is exceeded. Defaults to <c>20</c>. Pass <c>0</c> to
    /// not follow redirects. This can be overwritten for each request individually.
    /// </para>
    /// </summary>
    [JsonPropertyName("maxRedirects")]
    public int? MaxRedirects { get; set; }

    /// <summary><para>Network proxy settings.</para></summary>
    [JsonPropertyName("proxy")]
    public Proxy? Proxy { get; set; }

    /// <summary>
    /// <para>
    /// Populates context with given storage state. This option can be used to initialize
    /// context with logged-in information obtained via <see cref="IBrowserContext.StorageStateAsync"/>
    /// or <see cref="IAPIRequestContext.StorageStateAsync"/>. Either a path to the file
    /// with saved storage, or the value returned by one of <see cref="IBrowserContext.StorageStateAsync"/>
    /// or <see cref="IAPIRequestContext.StorageStateAsync"/> methods.
    /// </para>
    /// </summary>
    [JsonPropertyName("storageState")]
    public string? StorageState { get; set; }

    /// <summary>
    /// <para>
    /// Populates context with given storage state. This option can be used to initialize
    /// context with logged-in information obtained via <see cref="IBrowserContext.StorageStateAsync"/>.
    /// Path to the file with saved storage state.
    /// </para>
    /// </summary>
    [JsonPropertyName("storageStatePath")]
    public string? StorageStatePath { get; set; }

    /// <summary>
    /// <para>
    /// Maximum time in milliseconds to wait for the response. Defaults to <c>30000</c>
    /// (30 seconds). Pass <c>0</c> to disable timeout.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }

    /// <summary><para>Specific user agent to use in this context.</para></summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }
}
