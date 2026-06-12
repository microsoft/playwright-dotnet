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

using System.Text.Json.Serialization;

namespace Microsoft.Playwright;

public class CredentialsCreateOptions
{
    public CredentialsCreateOptions() { }

    public CredentialsCreateOptions(CredentialsCreateOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Id = clone.Id;
        PrivateKey = clone.PrivateKey;
        PublicKey = clone.PublicKey;
        UserHandle = clone.UserHandle;
    }

    /// <summary><para>Base64url-encoded credential id. Auto-generated if omitted.</para></summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary><para>Base64url-encoded PKCS#8 (DER) private key. Auto-generated if omitted.</para></summary>
    [JsonPropertyName("privateKey")]
    public string? PrivateKey { get; set; }

    /// <summary><para>Base64url-encoded SPKI (DER) public key. Auto-generated if omitted.</para></summary>
    [JsonPropertyName("publicKey")]
    public string? PublicKey { get; set; }

    /// <summary><para>Base64url-encoded user handle. Auto-generated if omitted.</para></summary>
    [JsonPropertyName("userHandle")]
    public string? UserHandle { get; set; }
}
