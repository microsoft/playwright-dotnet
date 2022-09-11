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
        var hasLargeFile = files.Any(f => new FileInfo(f).Length > SizeLimitInBytes);
        if (hasLargeFile)
        {
            if (context.Channel.Connection.IsRemote)
            {
                var streams = await files.SelectAsync(async f =>
                {
                    var stream = await context.Channel.CreateTempFileAsync(Path.GetFileName(f)).ConfigureAwait(false);
                    using var fileStream = File.OpenRead(f);
                    await fileStream.CopyToAsync(stream.WritableStreamImpl).ConfigureAwait(false);
                    return stream;
                }).ConfigureAwait(false);
                return new() { Streams = streams.ToArray() };
            }
            return new() { LocalPaths = files.Select(f => Path.GetFullPath(f)).ToArray() };
        }
        return new()
        {
            Files = files.Select(file =>
            {
                var fileInfo = new FileInfo(file);
                return new InputFilesList()
                {
                    Name = fileInfo.Name,
                    Buffer = Convert.ToBase64String(File.ReadAllBytes(fileInfo.FullName)),
                    MimeType = file.MimeType(),
                };
            }),
        };
    }

    public static SetInputFilesFiles ConvertInputFiles(IEnumerable<FilePayload> files)
    {
        var hasLargeBuffer = files.Any(f => f.Buffer?.Length > SizeLimitInBytes);
        if (hasLargeBuffer)
        {
            throw new NotSupportedException("Cannot set buffer larger than 50Mb, please write it to a file and pass its path instead.");
        }
        return new()
        {
            Files = files.Select(f => new InputFilesList
            {
                Name = f.Name,
                Buffer = Convert.ToBase64String(f.Buffer),
                MimeType = f.MimeType,
            }),
        };
    }
}
