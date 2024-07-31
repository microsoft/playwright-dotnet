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

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public partial class ClientCertificate
{
    /// <summary>
    /// <para>
    /// Exact origin that the certificate is valid for. Origin includes <c>https</c> protocol,
    /// a hostname and optionally a port.
    /// </para>
    /// </summary>
    [Required]
    [JsonPropertyName("origin")]
    public string Origin { get; set; } = default!;

    /// <summary><para>Path to the file with the certificate in PEM format.</para></summary>
    [JsonPropertyName("certPath")]
    public string? CertPath { get; set; }

    /// <summary><para>Path to the file with the private key in PEM format.</para></summary>
    [JsonPropertyName("keyPath")]
    public string? KeyPath { get; set; }

    /// <summary><para>Path to the PFX or PKCS12 encoded private key and certificate chain.</para></summary>
    [JsonPropertyName("pfxPath")]
    public string? PfxPath { get; set; }

    /// <summary><para>Passphrase for the private key (PEM or PFX).</para></summary>
    [JsonPropertyName("passphrase")]
    public string? Passphrase { get; set; }
}

#nullable disable
