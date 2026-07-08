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

namespace Microsoft.Playwright;

/// <summary>
/// Options for resolving or downloading the Clearcote browser binary.
/// </summary>
public class ClearcoteDownloadOptions
{
    /// <summary><para>Explicit browser executable path. Used by <see cref="ClearcoteBrowser.ExecutablePathAsync"/>.</para></summary>
    public string? ExecutablePath { get; set; }

    /// <summary><para>Override the Clearcote browser cache directory.</para></summary>
    public string? CacheDir { get; set; }

    /// <summary><para>Suppress Clearcote download progress messages.</para></summary>
    public bool? Quiet { get; set; }

    /// <summary><para>Resolve the latest compatible Clearcote GitHub release instead of the pinned release.</para></summary>
    public bool? AutoUpdate { get; set; }
}
