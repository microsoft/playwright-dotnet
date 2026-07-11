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
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;

namespace Microsoft.Playwright;

/// <summary>
/// Render-backend coherence verdict from a live Clearcote page.
/// </summary>
public sealed class ClearcoteRenderVerdict
{
    public string Vendor { get; init; } = string.Empty;

    public string Renderer { get; init; } = string.Empty;

    public bool Webgl { get; init; }

    public bool Webgl2 { get; init; }

    public int MaxTextureSize { get; init; }

    public bool SoftwareSuspected { get; init; }

    public bool Coherent { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = System.Array.Empty<string>();
}
