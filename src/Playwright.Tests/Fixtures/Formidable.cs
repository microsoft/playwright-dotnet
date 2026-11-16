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

using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>The options of <c>new formidable.IncomingForm(options)</c> that the tests use.</summary>
public sealed class IncomingFormOptions
{
    /// <summary><c>uploadDir</c>: the directory of the uploaded files, <c>os.tmpdir()</c> when not set.</summary>
    public string UploadDir { get; set; }
}

/// <summary><c>formidable.File</c>.</summary>
public sealed class FormidableFile
{
    internal FormidableFile(string filepath, string originalFilename, string mimetype, long size)
    {
        Filepath = filepath;
        OriginalFilename = originalFilename;
        Mimetype = mimetype;
        Size = size;
    }

    /// <summary><c>file.filepath</c>: the file in <c>uploadDir</c>, under a random name.</summary>
    public string Filepath { get; }

    /// <summary><c>file.originalFilename</c>: null when the part has no file name.</summary>
    public string OriginalFilename { get; }

    /// <summary><c>file.mimetype</c>: the Content-Type header of the part.</summary>
    public string Mimetype { get; }

    /// <summary><c>file.size</c>.</summary>
    public long Size { get; }
}

/// <summary>
/// The <c>fields</c> and the <c>files</c> of formidable, which are JavaScript objects: <c>Keys</c> is
/// <c>Object.keys()</c>, in the order of the body, a name that the form does not have gives null, and
/// the last part of a name wins.
/// </summary>
public sealed class FormidableMap<T> : IReadOnlyDictionary<string, T>
    where T : class
{
    private readonly List<string> _keys = [];
    private readonly Dictionary<string, T> _values = new(StringComparer.Ordinal);

    public IEnumerable<string> Keys => _keys;

    public IEnumerable<T> Values => _keys.Select(key => _values[key]);

    public int Count => _keys.Count;

    public T this[string name] => _values.GetValueOrDefault(name);

    public bool ContainsKey(string name) => _values.ContainsKey(name);

    public bool TryGetValue(string name, out T value) => _values.TryGetValue(name, out value);

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => _keys.Select(key => new KeyValuePair<string, T>(key, _values[key])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Set(string name, T value)
    {
        if (!_values.ContainsKey(name))
        {
            _keys.Add(name);
        }
        _values[name] = value;
    }
}

/// <summary>
/// <c>formidable.IncomingForm</c> of formidable 2, for a multipart body.
/// <c>form.parse(req, (err, fields, files) => { ... })</c> is <c>var (err, fields, files) = await form.Parse(req);</c>.
/// </summary>
public sealed class IncomingForm
{
    private static readonly byte[] _crlf = "\r\n"u8.ToArray();
    private static readonly byte[] _dashes = "--"u8.ToArray();

    // The regular expressions of formidable.
    private static readonly Regex _boundaryRegex = new("boundary=(?:\"([^\"]+)\"|([^;]+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _nameRegex = new("(?<![A-Za-z0-9_])name=(\"([^\"]*)\"|([^()<>@,;:\\\\\"/\\[\\]?={}\\s]+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _filenameRegex = new("(?<![A-Za-z0-9_])filename=(\"(.*?)\"|([^()<>{}\\[\\]@,;:\"?=\\s/]+))($|;\\s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _charCodeRegex = new("&#([0-9]{4});");

    // filename*=UTF-8''%E2%82%AC.txt of RFC 5987, which formidable does not read.
    private static readonly Regex _filenameStarRegex = new("(?<![A-Za-z0-9_])filename\\*=UTF-8'[^']*'([^;\\s]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly string _uploadDir;

    public IncomingForm(IncomingFormOptions options = null)
    {
        _uploadDir = Path.GetFullPath(options?.UploadDir ?? Path.GetTempPath());
    }

    /// <summary><c>form.parse(req, (err, fields, files) => { ... })</c>.</summary>
    public async Task<(Exception Error, FormidableMap<string> Fields, FormidableMap<FormidableFile> Files)> Parse(ServerRequest req)
        => await Parse(req.Headers["content-type"], await req.PostBody.ConfigureAwait(false)).ConfigureAwait(false);

    /// <summary>Parses a body that the test has: the value of the Content-Type header of the request and the bytes.</summary>
    public async Task<(Exception Error, FormidableMap<string> Fields, FormidableMap<FormidableFile> Files)> Parse(string contentType, byte[] body)
    {
        var fields = new FormidableMap<string>();
        var files = new FormidableMap<FormidableFile>();
        var filepaths = new List<string>();
        try
        {
            if (body.Length > 0)
            {
                foreach (var part in ReadParts(body, Delimiter(contentType)))
                {
                    if (string.IsNullOrEmpty(part.Mimetype))
                    {
                        fields.Set(part.Name, Encoding.UTF8.GetString(body, part.Start, part.Length));
                        continue;
                    }
                    var filepath = Path.Combine(_uploadDir, Guid.NewGuid().ToString("N"));
                    filepaths.Add(filepath);
                    using var handle = File.OpenHandle(filepath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous);
                    await RandomAccess.WriteAsync(handle, body.AsMemory(part.Start, part.Length), 0).ConfigureAwait(false);
                    files.Set(part.Name, new FormidableFile(filepath, part.OriginalFilename, part.Mimetype, part.Length));
                }
            }
            return (null, fields, files);
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            foreach (var filepath in filepaths.Where(File.Exists))
            {
                File.Delete(filepath);
            }
            return (e, fields, files);
        }
    }

    private static byte[] Delimiter(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            throw new InvalidDataException("bad content-type header, no content-type");
        }
        if (!contentType.Contains("multipart", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("no parser found");
        }
        var match = _boundaryRegex.Match(contentType);
        if (!match.Success)
        {
            throw new InvalidDataException("bad content-type header, no multipart boundary");
        }
        return Encoding.UTF8.GetBytes("\r\n--" + match.Groups[1].Value + match.Groups[2].Value);
    }

    private static IEnumerable<Part> ReadParts(byte[] body, byte[] delimiter)
    {
        var pos = delimiter.Length - 2;
        if (!StartsWith(body, 0, delimiter[2..]))
        {
            pos = IndexOf(body, delimiter, 0) + delimiter.Length;
        }
        while (!StartsWith(body, pos, _dashes))
        {
            if (!StartsWith(body, pos, _crlf))
            {
                throw Malformed();
            }
            pos += 2;
            string name = null;
            string originalFilename = null;
            string mimetype = null;
            while (true)
            {
                var lineEnd = IndexOf(body, _crlf, pos);
                var line = Encoding.UTF8.GetString(body, pos, lineEnd - pos);
                pos = lineEnd + 2;
                if (line.Length == 0)
                {
                    break;
                }
                var colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    throw Malformed();
                }
                var value = line.Substring(colon + 1).TrimStart(' ');
                switch (line.Substring(0, colon).ToLowerInvariant())
                {
                    case "content-disposition":
                        name = GetName(value);
                        originalFilename = GetFileName(value);
                        break;
                    case "content-type":
                        mimetype = value;
                        break;
                }
            }
            var start = pos;
            int end;
            do
            {
                // The bytes of the boundary are content when a line break or '--' does not follow them.
                end = IndexOf(body, delimiter, pos);
                pos = end + delimiter.Length;
            }
            while (!StartsWith(body, pos, _crlf) && !StartsWith(body, pos, _dashes));

            // A part without a name is fields[null] in JavaScript.
            yield return new Part(name ?? "null", originalFilename, mimetype, start, end - start);
        }
    }

    private static string GetName(string header)
    {
        var match = _nameRegex.Match(header);
        return match.Success ? Unescape(match.Groups[2].Value + match.Groups[3].Value) : null;
    }

    private static string GetFileName(string header)
    {
        var star = _filenameStarRegex.Match(header);
        if (star.Success)
        {
            return Uri.UnescapeDataString(star.Groups[1].Value);
        }
        var match = _filenameRegex.Match(header);
        if (!match.Success)
        {
            return null;
        }
        var filename = match.Groups[2].Value + match.Groups[3].Value;
        filename = Unescape(filename.Substring(filename.LastIndexOf('\\') + 1));
        return _charCodeRegex.Replace(filename, code => ((char)int.Parse(code.Groups[1].Value, CultureInfo.InvariantCulture)).ToString());
    }

    // The escapes of the multipart/form-data encoding algorithm of HTML. formidable reads %22 in a file name only.
    private static string Unescape(string value) => value.Replace("%0D", "\r", StringComparison.Ordinal).Replace("%0A", "\n", StringComparison.Ordinal).Replace("%22", "\"", StringComparison.Ordinal);

    private static bool StartsWith(byte[] body, int at, byte[] value) => body.AsSpan(at).StartsWith(value);

    private static int IndexOf(byte[] body, byte[] value, int from)
    {
        var index = body.AsSpan(from).IndexOf(value);
        return index == -1 ? throw Malformed() : from + index;
    }

    private static InvalidDataException Malformed() => new("malformed multipart body: stream ended unexpectedly");

    private readonly record struct Part(string Name, string OriginalFilename, string Mimetype, int Start, int Length);
}
