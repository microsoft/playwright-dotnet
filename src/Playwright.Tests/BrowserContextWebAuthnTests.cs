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

using System.Text.Json;

namespace Microsoft.Playwright.Tests;

///<playwright-file>browsercontext-webauthn.spec.ts</playwright-file>
public class BrowserContextWebAuthnTests : BrowserTestEx
{
    // Server.Prefix is http://localhost:<port>.
    private const string RpId = "localhost";

    [PlaywrightTest("browsercontext-webauthn.spec.ts", "should not intercept navigator.credentials without install()")]
    public async Task ShouldNotInterceptNavigatorCredentialsWithoutInstall()
    {
        await using var context = await Browser.NewContextAsync();
        // Seed a credential, but do not install the interceptor.
        await context.Credentials.CreateAsync(RpId);
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        Assert.IsFalse(await page.EvaluateAsync<bool>("() => globalThis.__pwWebAuthnInstalled === true"));
    }

    [PlaywrightTest("browsercontext-webauthn.spec.ts", "should seed a known credential and authenticate")]
    public async Task ShouldSeedAKnownCredentialAndAuthenticate()
    {
        // This is the easiest way to create credentials. In practice, this
        // probably comes from environment.
        await using var source = await Browser.NewContextAsync();
        var known = await source.Credentials.CreateAsync(RpId);

        // A fresh context imports the known credential and signs in with it.
        await using var context = await Browser.NewContextAsync();
        await context.Credentials.CreateAsync(known.RpId, new()
        {
            Id = known.Id,
            UserHandle = known.UserHandle,
            PrivateKey = known.PrivateKey,
            PublicKey = known.PublicKey,
        });
        await context.Credentials.InstallAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var result = await page.EvaluateAsync<JsonElement>(@"async ({ rpId, credentialId }) => {
            const b64UrlToBytes = (s) => {
                let str = s.replace(/-/g, '+').replace(/_/g, '/');
                while (str.length % 4)
                    str += '=';
                const bin = atob(str);
                const u8 = new Uint8Array(bin.length);
                for (let i = 0; i < bin.length; i++)
                    u8[i] = bin.charCodeAt(i);
                return u8;
            };
            const challenge = crypto.getRandomValues(new Uint8Array(32));
            const cred = await navigator.credentials.get({
                publicKey: {
                    challenge,
                    rpId,
                    allowCredentials: [{ type: 'public-key', id: b64UrlToBytes(credentialId) }],
                    userVerification: 'preferred',
                },
            });
            const resp = cred.response;
            return {
                id: cred.id,
                type: cred.type,
                hasClientData: resp.clientDataJSON.byteLength > 0,
                hasAuthData: resp.authenticatorData.byteLength > 0,
                hasSignature: resp.signature.byteLength > 0,
                authDataFlags: new Uint8Array(resp.authenticatorData)[32],
            };
        }", new { rpId = RpId, credentialId = known.Id });

        Assert.AreEqual(known.Id, result.GetProperty("id").GetString());
        Assert.AreEqual("public-key", result.GetProperty("type").GetString());
        Assert.IsTrue(result.GetProperty("hasClientData").GetBoolean());
        Assert.IsTrue(result.GetProperty("hasAuthData").GetBoolean());
        Assert.IsTrue(result.GetProperty("hasSignature").GetBoolean());
        // UP (0x01) | UV (0x04) = 0x05
        Assert.AreEqual(0x05, result.GetProperty("authDataFlags").GetInt32() & 0x05);

        // After the credential is deleted, the page can no longer authenticate with it.
        await context.Credentials.DeleteAsync(known.Id);
        Assert.IsEmpty(await context.Credentials.GetAsync());

        var error = await page.EvaluateAsync<string>(@"async ({ rpId, credentialId }) => {
            const b64UrlToBytes = (s) => {
                let str = s.replace(/-/g, '+').replace(/_/g, '/');
                while (str.length % 4)
                    str += '=';
                const bin = atob(str);
                const u8 = new Uint8Array(bin.length);
                for (let i = 0; i < bin.length; i++)
                    u8[i] = bin.charCodeAt(i);
                return u8;
            };
            const challenge = crypto.getRandomValues(new Uint8Array(32));
            try {
                await navigator.credentials.get({
                    publicKey: {
                        challenge,
                        rpId,
                        allowCredentials: [{ type: 'public-key', id: b64UrlToBytes(credentialId) }],
                    },
                });
                return 'no-error';
            } catch (e) {
                return e.name;
            }
        }", new { rpId = RpId, credentialId = known.Id });
        Assert.AreEqual("NotAllowedError", error);
    }

    [PlaywrightTest("browsercontext-webauthn.spec.ts", "should capture a page-created credential and reuse it in another context")]
    public async Task ShouldCaptureAPageCreatedCredentialAndReuseItInAnotherContext()
    {
        // Setup context: the app registers a passkey via navigator.credentials.create().
        await using var setupContext = await Browser.NewContextAsync();
        await setupContext.Credentials.InstallAsync();
        var setupPage = await setupContext.NewPageAsync();
        await setupPage.GotoAsync(Server.EmptyPage);

        var createdId = await setupPage.EvaluateAsync<string>(@"async ({ rpId }) => {
            const challenge = crypto.getRandomValues(new Uint8Array(32));
            const created = await navigator.credentials.create({
                publicKey: {
                    challenge,
                    rp: { id: rpId, name: 'Test RP' },
                    user: { id: new Uint8Array([1, 2, 3, 4]), name: 'u', displayName: 'User' },
                    pubKeyCredParams: [{ type: 'public-key', alg: -7 }],
                    authenticatorSelection: { residentKey: 'required', userVerification: 'preferred' },
                },
            });
            return created.id;
        }", new { rpId = RpId });

        var credentials = await setupContext.Credentials.GetAsync(new() { RpId = RpId });
        Assert.AreEqual(1, credentials.Count);
        var captured = credentials[0];
        Assert.AreEqual(createdId, captured.Id);
        StringAssert.IsMatch("^[A-Za-z0-9_-]+$", captured.PrivateKey);
        StringAssert.IsMatch("^[A-Za-z0-9_-]+$", captured.PublicKey);

        // Reuse the captured passkey in a fresh context and sign in with it.
        await using var context = await Browser.NewContextAsync();
        await context.Credentials.CreateAsync(captured.RpId, new()
        {
            Id = captured.Id,
            UserHandle = captured.UserHandle,
            PrivateKey = captured.PrivateKey,
            PublicKey = captured.PublicKey,
        });
        await context.Credentials.InstallAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var gotId = await page.EvaluateAsync<string>(@"async ({ rpId }) => {
            const challenge = crypto.getRandomValues(new Uint8Array(32));
            // No allowCredentials — relies on the re-seeded credential being discoverable.
            const cred = await navigator.credentials.get({
                publicKey: { challenge, rpId, userVerification: 'preferred' },
            });
            return cred.id;
        }", new { rpId = RpId });

        Assert.AreEqual(createdId, gotId);
    }
}
