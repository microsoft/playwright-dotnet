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

#nullable enable

namespace Microsoft.Playwright;

/// <summary><para>The <see cref="IFormData"/> is used create form data that is sent via <see cref="IAPIRequestContext"/>.</para></summary>
public partial interface IFormData
{
    /// <summary>
    /// <para>
    /// Appends a new value onto an existing key inside a FormData object, or adds the key
    /// if it does not already exist. File values can be passed either as <c>Path</c> or
    /// as <c>FilePayload</c>. Multiple fields with the same name can be added.
    /// </para>
    /// <para>
    /// The difference between <see cref="IFormData.Set"/> and <see cref="IFormData.Append"/>
    /// is that if the specified key already exists, <see cref="IFormData.Set"/> will overwrite
    /// all existing values with the new one, whereas <see cref="IFormData.Append"/> will
    /// append the new value onto the end of the existing set of values.
    /// </para>
    /// <code>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// // Only name and value are set.<br/>
    /// multipart.Append("firstName", "John");<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Append("attachment", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "pic.jpg",<br/>
    ///     MimeType = "image/jpeg",<br/>
    ///     Buffer = File.ReadAllBytes("john.jpg")<br/>
    /// });<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Append("attachment", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "table.csv",<br/>
    ///     MimeType = "text/csv",<br/>
    ///     Buffer = File.ReadAllBytes("my-tble.csv")<br/>
    /// });<br/>
    /// await Page.APIRequest.PostAsync("https://localhost/submit", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Append(string name, string value);

    /// <summary>
    /// <para>
    /// Appends a new value onto an existing key inside a FormData object, or adds the key
    /// if it does not already exist. File values can be passed either as <c>Path</c> or
    /// as <c>FilePayload</c>. Multiple fields with the same name can be added.
    /// </para>
    /// <para>
    /// The difference between <see cref="IFormData.Set"/> and <see cref="IFormData.Append"/>
    /// is that if the specified key already exists, <see cref="IFormData.Set"/> will overwrite
    /// all existing values with the new one, whereas <see cref="IFormData.Append"/> will
    /// append the new value onto the end of the existing set of values.
    /// </para>
    /// <code>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// // Only name and value are set.<br/>
    /// multipart.Append("firstName", "John");<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Append("attachment", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "pic.jpg",<br/>
    ///     MimeType = "image/jpeg",<br/>
    ///     Buffer = File.ReadAllBytes("john.jpg")<br/>
    /// });<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Append("attachment", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "table.csv",<br/>
    ///     MimeType = "text/csv",<br/>
    ///     Buffer = File.ReadAllBytes("my-tble.csv")<br/>
    /// });<br/>
    /// await Page.APIRequest.PostAsync("https://localhost/submit", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Append(string name, bool value);

    /// <summary>
    /// <para>
    /// Appends a new value onto an existing key inside a FormData object, or adds the key
    /// if it does not already exist. File values can be passed either as <c>Path</c> or
    /// as <c>FilePayload</c>. Multiple fields with the same name can be added.
    /// </para>
    /// <para>
    /// The difference between <see cref="IFormData.Set"/> and <see cref="IFormData.Append"/>
    /// is that if the specified key already exists, <see cref="IFormData.Set"/> will overwrite
    /// all existing values with the new one, whereas <see cref="IFormData.Append"/> will
    /// append the new value onto the end of the existing set of values.
    /// </para>
    /// <code>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// // Only name and value are set.<br/>
    /// multipart.Append("firstName", "John");<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Append("attachment", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "pic.jpg",<br/>
    ///     MimeType = "image/jpeg",<br/>
    ///     Buffer = File.ReadAllBytes("john.jpg")<br/>
    /// });<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Append("attachment", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "table.csv",<br/>
    ///     MimeType = "text/csv",<br/>
    ///     Buffer = File.ReadAllBytes("my-tble.csv")<br/>
    /// });<br/>
    /// await Page.APIRequest.PostAsync("https://localhost/submit", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Append(string name, int value);

    /// <summary>
    /// <para>
    /// Appends a new value onto an existing key inside a FormData object, or adds the key
    /// if it does not already exist. File values can be passed either as <c>Path</c> or
    /// as <c>FilePayload</c>. Multiple fields with the same name can be added.
    /// </para>
    /// <para>
    /// The difference between <see cref="IFormData.Set"/> and <see cref="IFormData.Append"/>
    /// is that if the specified key already exists, <see cref="IFormData.Set"/> will overwrite
    /// all existing values with the new one, whereas <see cref="IFormData.Append"/> will
    /// append the new value onto the end of the existing set of values.
    /// </para>
    /// <code>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// // Only name and value are set.<br/>
    /// multipart.Append("firstName", "John");<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Append("attachment", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "pic.jpg",<br/>
    ///     MimeType = "image/jpeg",<br/>
    ///     Buffer = File.ReadAllBytes("john.jpg")<br/>
    /// });<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Append("attachment", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "table.csv",<br/>
    ///     MimeType = "text/csv",<br/>
    ///     Buffer = File.ReadAllBytes("my-tble.csv")<br/>
    /// });<br/>
    /// await Page.APIRequest.PostAsync("https://localhost/submit", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Append(string name, AppendValue value);

    /// <summary>
    /// <para>
    /// Sets a field on the form. File values can be passed either as <c>Path</c> or as
    /// <c>FilePayload</c>.
    /// </para>
    /// <code>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// // Only name and value are set.<br/>
    /// multipart.Set("firstName", "John");<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Set("profilePicture", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "john.jpg",<br/>
    ///     MimeType = "image/jpeg",<br/>
    ///     Buffer = File.ReadAllBytes("john.jpg")<br/>
    /// });<br/>
    /// multipart.Set("age", 30);<br/>
    /// await Page.APIRequest.PostAsync("https://localhost/submit", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Set(string name, string value);

    /// <summary>
    /// <para>
    /// Sets a field on the form. File values can be passed either as <c>Path</c> or as
    /// <c>FilePayload</c>.
    /// </para>
    /// <code>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// // Only name and value are set.<br/>
    /// multipart.Set("firstName", "John");<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Set("profilePicture", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "john.jpg",<br/>
    ///     MimeType = "image/jpeg",<br/>
    ///     Buffer = File.ReadAllBytes("john.jpg")<br/>
    /// });<br/>
    /// multipart.Set("age", 30);<br/>
    /// await Page.APIRequest.PostAsync("https://localhost/submit", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Set(string name, bool value);

    /// <summary>
    /// <para>
    /// Sets a field on the form. File values can be passed either as <c>Path</c> or as
    /// <c>FilePayload</c>.
    /// </para>
    /// <code>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// // Only name and value are set.<br/>
    /// multipart.Set("firstName", "John");<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Set("profilePicture", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "john.jpg",<br/>
    ///     MimeType = "image/jpeg",<br/>
    ///     Buffer = File.ReadAllBytes("john.jpg")<br/>
    /// });<br/>
    /// multipart.Set("age", 30);<br/>
    /// await Page.APIRequest.PostAsync("https://localhost/submit", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Set(string name, int value);

    /// <summary>
    /// <para>
    /// Sets a field on the form. File values can be passed either as <c>Path</c> or as
    /// <c>FilePayload</c>.
    /// </para>
    /// <code>
    /// var multipart = Context.APIRequest.CreateFormData();<br/>
    /// // Only name and value are set.<br/>
    /// multipart.Set("firstName", "John");<br/>
    /// // Name, value, filename and Content-Type are set.<br/>
    /// multipart.Set("profilePicture", new FilePayload()<br/>
    /// {<br/>
    ///     Name = "john.jpg",<br/>
    ///     MimeType = "image/jpeg",<br/>
    ///     Buffer = File.ReadAllBytes("john.jpg")<br/>
    /// });<br/>
    /// multipart.Set("age", 30);<br/>
    /// await Page.APIRequest.PostAsync("https://localhost/submit", new() { Multipart = multipart });
    /// </code>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Set(string name, FilePayload value);
}

#nullable disable
