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
using System.Threading.Tasks;

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// <c>Credentials</c> is a virtual WebAuthn authenticator scoped to a <see cref="IBrowserContext"/>.
/// It lets tests register passkeys and answer <c>navigator.credentials.create()</c>
/// / <c>navigator.credentials.get()</c> ceremonies in the page, without a real authenticator
/// or hardware security key.
/// </para>
/// <para>There are two common ways to use it:</para>
/// <para>**Usage: seed a known credential**</para>
/// <code>
/// var context = await browser.NewContextAsync();<br/>
/// <br/>
/// // A passkey your backend already provisioned for a test user.<br/>
/// await context.Credentials.CreateAsync("example.com", new()<br/>
/// {<br/>
///     Id = knownCredentialId, // base64url<br/>
///     UserHandle = knownUserHandle, // base64url<br/>
///     PrivateKey = knownPrivateKey, // base64url PKCS#8 (DER)<br/>
///     PublicKey = knownPublicKey, // base64url SPKI (DER)<br/>
/// });<br/>
/// await context.Credentials.InstallAsync();<br/>
/// <br/>
/// var page = await context.NewPageAsync();<br/>
/// await page.GotoAsync("https://example.com/login");<br/>
/// // The page's navigator.credentials.get() is answered with the seeded passkey.
/// </code>
/// <para>**Usage: capture a passkey, then reuse it**</para>
/// <code>
/// // setup test: let the app register a passkey, then save it.<br/>
/// var context = await browser.NewContextAsync();<br/>
/// await context.Credentials.InstallAsync();<br/>
/// <br/>
/// var page = await context.NewPageAsync();<br/>
/// await page.GotoAsync("https://example.com/register");<br/>
/// await page.GetByRole(AriaRole.Button, new() { Name = "Create a passkey" }).ClickAsync();<br/>
/// <br/>
/// // Read back the passkey the page registered — it includes the private key.<br/>
/// var credentials = await context.Credentials.GetAsync(new() { RpId = "example.com" });<br/>
/// File.WriteAllText("playwright/.auth/passkey.json", JsonSerializer.Serialize(credentials[0]));
/// </code>
/// <code>
/// // later test: seed the captured passkey so the app starts already enrolled.<br/>
/// var credential = JsonSerializer.Deserialize&lt;VirtualCredential&gt;(<br/>
///     File.ReadAllText("playwright/.auth/passkey.json"));<br/>
/// var context = await browser.NewContextAsync();<br/>
/// await context.Credentials.CreateAsync(credential.RpId, new()<br/>
/// {<br/>
///     Id = credential.Id,<br/>
///     UserHandle = credential.UserHandle,<br/>
///     PrivateKey = credential.PrivateKey,<br/>
///     PublicKey = credential.PublicKey,<br/>
/// });<br/>
/// await context.Credentials.InstallAsync();<br/>
/// <br/>
/// var page = await context.NewPageAsync();<br/>
/// await page.GotoAsync("https://example.com/login");<br/>
/// // navigator.credentials.get() resolves the captured passkey — already signed in.
/// </code>
/// <para>**Defaults**</para>
/// </summary>
public partial interface ICredentials
{
    /// <summary>
    /// <para>
    /// Installs the virtual WebAuthn authenticator into the context, overriding <c>navigator.credentials.create()</c>
    /// and <c>navigator.credentials.get()</c> in all current and future pages. Call this
    /// before the page first touches <c>navigator.credentials</c>.
    /// </para>
    /// <para>
    /// Required: until <see cref="ICredentials.InstallAsync"/> is called, no interception
    /// is in place and the page sees the platform's native (or absent) WebAuthn behaviour.
    /// Seeding credentials with <see cref="ICredentials.CreateAsync"/> without installing
    /// populates the authenticator, but the page will never see those credentials.
    /// </para>
    /// </summary>
    Task InstallAsync();

    /// <summary>
    /// <para>Seeds a virtual WebAuthn credential and returns it.</para>
    /// <para>
    /// With only <see cref="ICredentials.CreateAsync"/>, generates a fresh **ECDSA P-256**
    /// keypair, credential id and user handle. The seeded credential is discoverable (resident),
    /// so the page can resolve it from both username-then-passkey and usernameless passkey
    /// flows. The returned object carries the private and public keys, so it can be persisted
    /// to disk and re-seeded in a later test.
    /// </para>
    /// <para>
    /// To **import a known credential**, supply all four of <see cref="ICredentials.CreateAsync"/>,
    /// <see cref="ICredentials.CreateAsync"/>, <see cref="ICredentials.CreateAsync"/> and
    /// <see cref="ICredentials.CreateAsync"/> together.
    /// </para>
    /// <para>
    /// Call <see cref="ICredentials.InstallAsync"/> before navigating to a page that uses
    /// WebAuthn.
    /// </para>
    /// </summary>
    /// <param name="rpId">Relying party id (typically the site's effective domain).</param>
    /// <param name="options">Call options</param>
    Task<VirtualCredential> CreateAsync(string rpId, CredentialsCreateOptions? options = default);

    /// <summary>
    /// <para>
    /// Removes a credential from the authenticator by its id. Works for any credential
    /// currently held — both those seeded with <see cref="ICredentials.CreateAsync"/> and
    /// those the page registered itself by calling <c>navigator.credentials.create()</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Base64url-encoded credential id.</param>
    Task DeleteAsync(string id);

    /// <summary>
    /// <para>
    /// Returns every credential currently held by the authenticator, optionally filtered
    /// by <see cref="ICredentials.GetAsync"/> or <see cref="ICredentials.GetAsync"/>. This
    /// includes both credentials seeded with <see cref="ICredentials.CreateAsync"/> and
    /// credentials the page registered itself by calling <c>navigator.credentials.create()</c>.
    /// </para>
    /// <para>
    /// Each returned credential includes its private and public keys, so a passkey the
    /// app just registered can be saved and re-seeded into a later test with <see cref="ICredentials.CreateAsync"/>
    /// — see the second example in the class overview.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<VirtualCredential>> GetAsync(CredentialsGetOptions? options = default);
}
