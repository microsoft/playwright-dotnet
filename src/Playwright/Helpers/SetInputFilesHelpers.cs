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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Helpers;

internal static class SetInputFilesHelpers
{
    private const int SizeLimitInBytes = 50 * 1024 * 1024;

    public static async Task<SetInputFilesFiles> ConvertInputFilesAsync(IEnumerable<string> files, BrowserContext context)
    {
        if (!files.Any())
        {
            return new()
            {
                Payloads = Array.Empty<InputFilesList>(),
            };
        }
        if (context.Channel.Connection.IsRemote)
        {
            var streams = await files.SelectAsync(async f =>
            {
                var lastModifiedMs = new DateTimeOffset(File.GetLastWriteTime(f)).ToUnixTimeMilliseconds();
#pragma warning disable CA2007 // Upstream bug: https://github.com/dotnet/roslyn-analyzers/issues/5712
                await using var stream = await context.Channel.CreateTempFileAsync(Path.GetFileName(f), lastModifiedMs).ConfigureAwait(false);
#pragma warning restore CA2007
                using var fileStream = File.OpenRead(f);
                await fileStream.CopyToAsync(stream.WritableStreamImpl).ConfigureAwait(false);
                return stream;
            }).ConfigureAwait(false);
            return new() { Streams = streams.ToArray() };
        }
        return new() { LocalPaths = files.Select(f => Path.GetFullPath(f)).ToArray() };
    }

    public static SetInputFilesFiles ConvertInputFiles(IEnumerable<FilePayload> files)
    {
        var filePayloadExceedsSizeLimit = files.Sum(f => f.Buffer.Length) > SizeLimitInBytes;
        if (filePayloadExceedsSizeLimit)
        {
            throw new NotSupportedException("Cannot set buffer larger than 50Mb, please write it to a file and pass its path instead.");
        }
        return new()
        {
            Payloads = files.Select(f => new InputFilesList
            {
                Name = f.Name,
                Buffer = Convert.ToBase64String(f.Buffer),
                MimeType = f.MimeType,
            }).ToArray(),
        };
    }
}
