/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-webauthn.spec.ts.</summary>
public class BrowsercontextWebauthnTests
{
    // Runs an assertion ceremony with a discoverable credential, returns the signature counter from the authenticator data.
    private static async Task<int> AssertAndGetSignCount(IPage page, string rpId)
    {
        return await page.EvaluateAsync<int>(@"async ({ rpId }) => {
    const challenge = crypto.getRandomValues(new Uint8Array(32));
    const cred = await navigator.credentials.get({
      publicKey: { challenge, rpId, userVerification: 'preferred' },
    });
    const resp = cred.response;
    return new DataView(resp.authenticatorData).getUint32(33);
  }", new { rpId });
    }

    [UpstreamTest("AU-017 should not intercept navigator.credentials without install()")]
    public async Task ShouldNotInterceptNavigatorCredentialsWithoutInstall(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory();
        // Seed a credential, but do not install the interceptor.
        await context.Credentials.CreateAsync(server.Hostname);
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

        var intercepted = await page.EvaluateAsync<bool>("() => globalThis.__pwWebAuthnInstalled === true");
        Assert.That(intercepted, Is.EqualTo(false));
    }

    [UpstreamTest("AU-018 should seed a known credential and authenticate")]
    public async Task ShouldSeedAKnownCredentialAndAuthenticate(ContextFactory contextFactory, Server server)
    {
        // This is the easiest way to create credentials. In practice, this
        // probably comes from environment.
        var source = await contextFactory();
        var known = await source.Credentials.CreateAsync(server.Hostname);

        // A fresh context imports the known credential and signs in with it.
        var context = await contextFactory();
        // .NET: the options of CreateAsync are not a credential, they take its properties.
        await context.Credentials.CreateAsync(known.RpId, MergeOptions(new CredentialsCreateOptions(), known));
        await context.Credentials.InstallAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

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
  }", new { rpId = server.Hostname, credentialId = known.Id });

        Assert.That(result.GetProperty("id").GetString(), Is.EqualTo(known.Id));
        Assert.That(result.GetProperty("type").GetString(), Is.EqualTo("public-key"));
        Assert.That(result.GetProperty("hasClientData").GetBoolean(), Is.EqualTo(true));
        Assert.That(result.GetProperty("hasAuthData").GetBoolean(), Is.EqualTo(true));
        Assert.That(result.GetProperty("hasSignature").GetBoolean(), Is.EqualTo(true));
        // UP (0x01) | UV (0x04) = 0x05
        Assert.That(result.GetProperty("authDataFlags").GetInt32() & 0x05, Is.EqualTo(0x05));

        // After the credential is deleted, the page can no longer authenticate with it.
        await context.Credentials.DeleteAsync(known.Id);
        Assert.That(await context.Credentials.GetAsync(), Has.Count.EqualTo(0));

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
  }", new { rpId = server.Hostname, credentialId = known.Id });
        Assert.That(error, Is.EqualTo("NotAllowedError"));
    }

    [UpstreamTest("AU-019 should capture a page-created credential and reuse it in another context")]
    public async Task ShouldCaptureAPageCreatedCredentialAndReuseItInAnotherContext(ContextFactory contextFactory, Server server)
    {
        // Setup context: the app registers a passkey via navigator.credentials.create().
        var setupContext = await contextFactory();
        await setupContext.Credentials.InstallAsync();
        var setupPage = await setupContext.NewPageAsync();
        await setupPage.GotoAsync(server.EmptyPage);

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
  }", new { rpId = server.Hostname });

        var captured = (await setupContext.Credentials.GetAsync(new() { RpId = server.Hostname }))[0];
        Assert.That(captured.Id, Is.EqualTo(createdId));
        Assert.That(captured.PrivateKey, Does.Match("^[A-Za-z0-9_-]+$"));
        Assert.That(captured.PublicKey, Does.Match("^[A-Za-z0-9_-]+$"));

        // Reuse the captured passkey in a fresh context and sign in with it.
        var context = await contextFactory();
        // .NET: the options of CreateAsync are not a credential, they take its properties.
        await context.Credentials.CreateAsync(captured.RpId, MergeOptions(new CredentialsCreateOptions(), captured));
        await context.Credentials.InstallAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

        var gotId = await page.EvaluateAsync<string>(@"async ({ rpId }) => {
    const challenge = crypto.getRandomValues(new Uint8Array(32));
    // No allowCredentials — relies on the re-seeded credential being discoverable.
    const cred = await navigator.credentials.get({
      publicKey: { challenge, rpId, userVerification: 'preferred' },
    });
    return cred.id;
  }", new { rpId = server.Hostname });

        Assert.That(gotId, Is.EqualTo(createdId));
    }

    [UpstreamTest("AU-020 should reuse a page-created credential via the storageState option")]
    public async Task ShouldReuseAPageCreatedCredentialViaTheStorageStateOption(ContextFactory contextFactory, Server server)
    {
        // Setup context: the app registers a passkey via navigator.credentials.create().
        var setupContext = await contextFactory();
        await setupContext.Credentials.InstallAsync();
        var setupPage = await setupContext.NewPageAsync();
        await setupPage.GotoAsync(server.EmptyPage);

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
  }", new { rpId = server.Hostname });

        // Capture the passkey as part of the storage state.
        var storageState = await setupContext.StorageStateAsync(new() { Credentials = true });

        // A context created from the storage state has the authenticator installed and signs in with
        // the captured passkey — without calling install() again.
        var context = await contextFactory(new() { StorageState = storageState });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

        var gotId = await page.EvaluateAsync<string>(@"async ({ rpId }) => {
    const challenge = crypto.getRandomValues(new Uint8Array(32));
    // No allowCredentials — relies on the re-seeded credential being discoverable.
    const cred = await navigator.credentials.get({
      publicKey: { challenge, rpId, userVerification: 'preferred' },
    });
    return cred.id;
  }", new { rpId = server.Hostname });

        Assert.That(gotId, Is.EqualTo(createdId));
    }

    [UpstreamTest("AU-021 should seed and report signCount")]
    public async Task ShouldSeedAndReportSignCount(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory();
        var fresh = await context.Credentials.CreateAsync("fresh.example.com");
        Assert.That(fresh.SignCount, Is.EqualTo(0));

        var seeded = await context.Credentials.CreateAsync(server.Hostname, new() { SignCount = 41 });
        Assert.That(seeded.SignCount, Is.EqualTo(41));
        await context.Credentials.InstallAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

        // Each assertion increments the counter and reports the new value to the page.
        Assert.That(await AssertAndGetSignCount(page, server.Hostname), Is.EqualTo(42));
        Assert.That(await AssertAndGetSignCount(page, server.Hostname), Is.EqualTo(43));
        ExpectToEqual(await context.Credentials.GetAsync(new() { Id = seeded.Id }), new[] { MergeOptions(new VirtualCredential { SignCount = 43 }, seeded) });
        ExpectToEqual(await context.Credentials.GetAsync(new() { Id = fresh.Id }), new[] { fresh });

        // A captured credential continues from the same counter in another context.
        var captured = (await context.Credentials.GetAsync(new() { Id = seeded.Id }))[0];
        var context2 = await contextFactory();
        // .NET: the options of CreateAsync are not a credential, they take its properties.
        await context2.Credentials.CreateAsync(captured.RpId, MergeOptions(new CredentialsCreateOptions(), captured));
        await context2.Credentials.InstallAsync();
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(server.EmptyPage);
        Assert.That(await AssertAndGetSignCount(page2, server.Hostname), Is.EqualTo(44));
    }

    [UpstreamTest("AU-022 should reject invalid signCount")]
    public async Task ShouldRejectInvalidSignCount(ContextFactory contextFactory)
    {
        var context = await contextFactory();
        var error = await Catch(() => context.Credentials.CreateAsync("example.com", new() { SignCount = -1 }));
        Assert.That(error.Message, Does.Contain("signCount must be between 0 and 4294967295, got -1"));
        ExpectToEqual(await context.Credentials.GetAsync(), Array.Empty<object>());
    }

    [UpstreamTest("AU-023 should preserve signCount via the storageState option")]
    public async Task ShouldPreserveSignCountViaTheStorageStateOption(ContextFactory contextFactory, Server server)
    {
        var setupContext = await contextFactory();
        await setupContext.Credentials.CreateAsync(server.Hostname);
        await setupContext.Credentials.InstallAsync();
        var setupPage = await setupContext.NewPageAsync();
        await setupPage.GotoAsync(server.EmptyPage);
        Assert.That(await AssertAndGetSignCount(setupPage, server.Hostname), Is.EqualTo(1));

        var storageState = await setupContext.StorageStateAsync(new() { Credentials = true });
        var captured = (await setupContext.Credentials.GetAsync())[0];
        Assert.That(captured.SignCount, Is.EqualTo(1));
        // .NET: StorageStateAsync gives the JSON text of the storage state.
        ExpectToEqual(JsonDocument.Parse(storageState).RootElement, new { cookies = Array.Empty<object>(), origins = Array.Empty<object>(), credentials = new[] { captured } });

        var context = await contextFactory(new() { StorageState = storageState });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await AssertAndGetSignCount(page, server.Hostname), Is.EqualTo(2));

        // Storage state saved by older versions has no signCount, the counter starts from zero.
        // .NET: the storage state is a JSON text, so the legacy one is built as JSON.
        var legacyCredential = JsonSerializer.SerializeToNode(captured).AsObject();
        legacyCredential.Remove("signCount");
        var legacyStorageState = JsonNode.Parse(storageState).AsObject();
        legacyStorageState["credentials"] = new JsonArray(legacyCredential);
        var legacyContext = await contextFactory(new() { StorageState = legacyStorageState.ToJsonString() });
        var legacyPage = await legacyContext.NewPageAsync();
        await legacyPage.GotoAsync(server.EmptyPage);
        Assert.That(await AssertAndGetSignCount(legacyPage, server.Hostname), Is.EqualTo(1));
    }

    [UpstreamTest("AU-024 should serialize credentials with toJSON")]
    public async Task ShouldSerializeCredentialsWithToJSON(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory();
        await context.Credentials.InstallAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

        // .NET: the objects of the result are dictionaries, so that expected.attestation can be spread below.
        IDictionary<string, object> result = await page.EvaluateAsync<ExpandoObject>(@"async ({ rpId }) => {
    const toB64Url = (buf) => btoa(String.fromCharCode(...new Uint8Array(buf))).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    const created = await navigator.credentials.create({
      publicKey: {
        challenge: crypto.getRandomValues(new Uint8Array(32)),
        rp: { id: rpId, name: 'Test RP' },
        user: { id: new Uint8Array([1, 2, 3, 4]), name: 'u', displayName: 'User' },
        pubKeyCredParams: [{ type: 'public-key', alg: -7 }],
        authenticatorSelection: { residentKey: 'required' },
      },
    });
    const attestation = created.response;
    const got = await navigator.credentials.get({
      publicKey: { challenge: crypto.getRandomValues(new Uint8Array(32)), rpId },
    });
    const assertion = got.response;
    return {
      registration: JSON.parse(JSON.stringify(created)),
      authentication: JSON.parse(JSON.stringify(got)),
      expected: {
        id: created.id,
        attestation: {
          clientDataJSON: toB64Url(attestation.clientDataJSON),
          authenticatorData: toB64Url(attestation.getAuthenticatorData()),
          publicKey: toB64Url(attestation.getPublicKey()),
          attestationObject: toB64Url(attestation.attestationObject),
        },
        assertion: {
          clientDataJSON: toB64Url(assertion.clientDataJSON),
          authenticatorData: toB64Url(assertion.authenticatorData),
          signature: toB64Url(assertion.signature),
          userHandle: toB64Url(assertion.userHandle),
        },
      },
    };
  }", new { rpId = server.Hostname });
        var registration = (IDictionary<string, object>)result["registration"];
        var authentication = (IDictionary<string, object>)result["authentication"];
        var expected = (IDictionary<string, object>)result["expected"];

        ExpectToEqual(registration, new Dictionary<string, object>
        {
            ["id"] = expected["id"],
            ["rawId"] = expected["id"],
            ["response"] = new Dictionary<string, object>((IDictionary<string, object>)expected["attestation"])
            {
                ["transports"] = new[] { "internal" },
                ["publicKeyAlgorithm"] = -7,
            },
            ["authenticatorAttachment"] = "platform",
            ["clientExtensionResults"] = new { },
            ["type"] = "public-key",
        });
        ExpectToEqual(authentication, new Dictionary<string, object>
        {
            ["id"] = expected["id"],
            ["rawId"] = expected["id"],
            ["response"] = expected["assertion"],
            ["authenticatorAttachment"] = "platform",
            ["clientExtensionResults"] = new { },
            ["type"] = "public-key",
        });

        // .NET: registration.response and authentication.response, and Buffer.from(text, 'base64url').
        var registrationResponse = (IDictionary<string, object>)registration["response"];
        var authenticationResponse = (IDictionary<string, object>)authentication["response"];
        byte[] fromBase64Url(object text) => Convert.FromBase64String(((string)text).Replace('-', '+').Replace('_', '/').PadRight(((string)text).Length + ((4 - (((string)text).Length % 4)) % 4), '='));

        var credential = (await context.Credentials.GetAsync())[0];
        Assert.That(registrationResponse["publicKey"], Is.EqualTo(credential.PublicKey));
        Assert.That(authenticationResponse["userHandle"], Is.EqualTo(credential.UserHandle));
        Assert.That(JsonDocument.Parse(Encoding.UTF8.GetString(fromBase64Url(registrationResponse["clientDataJSON"]))).RootElement.GetProperty("type").GetString(), Is.EqualTo("webauthn.create"));
        Assert.That(JsonDocument.Parse(Encoding.UTF8.GetString(fromBase64Url(authenticationResponse["clientDataJSON"]))).RootElement.GetProperty("type").GetString(), Is.EqualTo("webauthn.get"));

        // Registration authenticator data is embedded in the attestation object and carries attested credential data (AT flag).
        var authData = fromBase64Url(registrationResponse["authenticatorData"]);
        Assert.That(fromBase64Url(registrationResponse["attestationObject"]).AsSpan().IndexOf(authData) != -1, Is.EqualTo(true));
        Assert.That(authData[32] & 0x40, Is.EqualTo(0x40));
    }
}
