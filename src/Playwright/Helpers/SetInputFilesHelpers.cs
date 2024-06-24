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

    private static (string[] LocalPaths, string LocalDirectory) ResolvePathsAndDirectoryForInputFiles(List<string> items)
    {
        List<string> localPaths = null;
        string localDirectory = null;
        foreach (var item in items)
        {
            if ((File.GetAttributes(item) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                if (localDirectory != null)
                {
                    throw new PlaywrightException("Multiple directories are not supported");
                }
                localDirectory = Path.GetFullPath(item);
            }
            else
            {
                localPaths ??= [];
                localPaths.Add(Path.GetFullPath(item));
            }
        }

        if (localPaths?.Count > 0 && localDirectory != null)
        {
            throw new PlaywrightException("File paths must be all files or a single directory");
        }

        return (localPaths?.ToArray(), localDirectory);
    }

    private static IEnumerable<string> GetFilesRecursive(string directory)
    {
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(directory));
        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            files.AddRange(GetFilesRecursive(subDirectory));
        }
        return files;
    }

    public static async Task<SetInputFilesFiles> ConvertInputFilesAsync(IEnumerable<string> files, BrowserContext context)
    {
        if (!files.Any())
        {
            return new() { Payloads = [] };
        }
        var (localPaths, localDirectory) = ResolvePathsAndDirectoryForInputFiles(files.ToList());
        if (context._connection.IsRemote)
        {
            files = localDirectory != null ? GetFilesRecursive(localDirectory) : localPaths;
            var result = await context.SendMessageToServerAsync("createTempFiles", new Dictionary<string, object>
            {
                ["rootDirName"] = localDirectory != null ? new DirectoryInfo(localDirectory).Name : null,
                ["items"] = files.Select(file => new Dictionary<string, object>
                {
                    // TODO: best-effort relative path, switch to Path.GetRelativePath once have netstandard2.1
                    ["name"] = localDirectory != null ? file.Substring(localDirectory.Length + 1) : Path.GetFileName(file),
                    ["lastModifiedMs"] = new DateTimeOffset(File.GetLastWriteTime(file)).ToUnixTimeMilliseconds(),
                }).ToArray(),
            }).ConfigureAwait(false);
            var writableStreams = result.Value.GetProperty("writableStreams").EnumerateArray();
            if (writableStreams.Count() != files.Count())
            {
                throw new Exception("Mismatch between the number of files and the number of writeable streams");
            }
            var streams = new List<WritableStream>();
            for (var i = 0; i < files.Count(); i++)
            {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                await using (var writeableStream = context._connection.GetObject(writableStreams.ElementAt(i).GetProperty("guid").ToString()) as WritableStream)
                {
                    streams.Add(writeableStream);
                    using var fileStream = File.OpenRead(files.ElementAt(i));
                    await fileStream.CopyToAsync(writeableStream.WritableStreamImpl).ConfigureAwait(false);
                }
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            }
            return new()
            {
                DirectoryStream = result.Value.TryGetProperty("rootDir", out var rootDir) ? context._connection.GetObject(rootDir.GetProperty("guid").ToString()) as WritableStream : null,
                Streams = localDirectory != null ? null : [.. streams],
            };
        }
        return new() { LocalPaths = localPaths, LocalDirectory = localDirectory };
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
