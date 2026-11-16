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

using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// The helpers of the upstream tests, the port of <c>tests/config/utils.ts</c> and <c>tests/config/errors.ts</c>.
/// A test file imports them with <c>using static Microsoft.Playwright.Tests.Fixtures.Utils;</c>.
/// </summary>
public static partial class Utils
{
    /// <summary><c>kTargetClosedErrorMessage</c>.</summary>
    public const string TargetClosedErrorMessage = "Target page, context or browser has been closed";

    private static readonly Regex _ansiRegex = new(
        "[\\u001B\\u009B][[\\]()#?]*(?:(?:(?:[a-zA-Z\\d]*(?:;[-a-zA-Z\\d\\/#&.:=?%@~_]*)*)?\\u0007)|(?:(?:\\d{0,4}(?:;\\d{0,4})*)?[\\dA-PR-TZcf-ntqry=><~]))",
        RegexOptions.Compiled);

    private static readonly Lazy<string> _hostPlatform = new(ReadHostPlatform);

    /// <summary><c>hostPlatform</c> of the driver, as far as the tests need it: 'debian11...', 'ubuntu20.04...', 'mac...', 'win64'.</summary>
    public static string HostPlatform => _hostPlatform.Value;

    /// <summary><c>artifactsFolderName(workerIndex)</c> of Playwright Test: the folder of a worker in the output directory.</summary>
    public static string ArtifactsFolderName(int workerIndex) => $".playwright-artifacts-{workerIndex}";

    /// <summary><c>asset('file')</c>.</summary>
    public static string Asset(string path) => Path.Combine([Env.AssetsDir, .. path.Split('/')]);

    public static async Task<IFrame> AttachFrame(IPage page, string frameId, string url)
    {
        var handle = await page.EvaluateHandleAsync(
            @"async ({ frameId, url }) => {
                const frame = document.createElement('iframe');
                frame.src = url;
                frame.id = frameId;
                document.body.appendChild(frame);
                await new Promise(x => frame.onload = x);
                return frame;
            }",
            new { frameId, url }).ConfigureAwait(false);
        return await handle.AsElement().ContentFrameAsync().ConfigureAwait(false);
    }

    public static async Task DetachFrame(IPage page, string frameId)
    {
        await page.EvaluateAsync(
            @"frameId => {
                document.getElementById(frameId).remove();
            }",
            frameId).ConfigureAwait(false);
    }

    public static async Task VerifyViewport(IPage page, int width, int height)
    {
        Assert.That(page.ViewportSize.Width, Is.EqualTo(width));
        Assert.That(page.ViewportSize.Height, Is.EqualTo(height));
        Assert.That(await page.EvaluateAsync<int>("window.innerWidth").ConfigureAwait(false), Is.EqualTo(width));
        Assert.That(await page.EvaluateAsync<int>("window.innerHeight").ConfigureAwait(false), Is.EqualTo(height));
    }

    public static Regex ExpectedSSLError(string browserName, string platform, string channel)
    {
        if (browserName == "chromium")
        {
            return new Regex("net::(ERR_CERT_AUTHORITY_INVALID|ERR_CERT_INVALID)");
        }
        if (browserName == "webkit")
        {
            if (platform == "darwin")
            {
                return new Regex("The certificate for this server is invalid");
            }
            else if (platform == "win32" && channel != "webkit-wsl")
            {
                return new Regex("SSL peer certificate or SSH remote key was not OK");
            }
            else
            {
                return new Regex("Unacceptable TLS certificate|Operation was cancelled");
            }
        }
        if (browserName == "firefox" && IsBidiChannel(channel))
        {
            return new Regex("MOZILLA_PKIX_ERROR_SELF_SIGNED_CERT");
        }
        return new Regex("SSL_ERROR_UNKNOWN");
    }

    public static bool IsBidiChannel(string channel) => channel != null && (channel.StartsWith("bidi-chrom", StringComparison.Ordinal) || channel.StartsWith("moz-firefox", StringComparison.Ordinal));

    public static bool ChromiumVersionLessThan(string a, string b)
    {
        var left = a.Split('.').Select(e => int.Parse(e, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var right = b.Split('.').Select(e => int.Parse(e, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        for (var i = 0; i < 4; i++)
        {
            if (left[i] > right[i])
            {
                return false;
            }
            if (left[i] < right[i])
            {
                return true;
            }
        }
        return false;
    }

    /// <summary><c>parseHar(file)</c>: the entries of a zipped HAR.</summary>
    public static Dictionary<string, byte[]> ParseHar(string file)
    {
        var resources = new Dictionary<string, byte[]>();
        using var zip = ZipFile.OpenRead(file);
        foreach (var entry in zip.Entries)
        {
            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            resources[entry.FullName] = buffer.ToArray();
        }
        return resources;
    }

    /// <summary><c>waitForTestLog(page, prefix)</c>: the JSON after the prefix of the first console message that starts with it.</summary>
    public static Task<T> WaitForTestLog<T>(IPage page, string prefix)
    {
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        page.Console += (_, message) =>
        {
            var text = message.Text;
            if (text.StartsWith(prefix, StringComparison.Ordinal))
            {
                var json = text.Substring(prefix.Length);
                result.TrySetResult(JsonSerializer.Deserialize<T>(json));
            }
        };
        return result.Task;
    }

    public static Task Rafraf(IPage target, int count = 1) => Rafraf(target.MainFrame, count);

    public static async Task Rafraf(IFrame target, int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            await target.EvaluateAsync(
                @"async () => {
                    await new Promise(f => window.builtins.requestAnimationFrame(() => window.builtins.requestAnimationFrame(f)));
                }").ConfigureAwait(false);
        }
    }

    public static async Task EnsureSomeFrames(IPage page)
    {
        await Rafraf(page, 100).ConfigureAwait(false);
        await page.ScreenshotAsync().ConfigureAwait(false);
    }

    public static LocatorBoundingBoxResult RoundBox(LocatorBoundingBoxResult box) => new()
    {
        X = (float)Math.Round(box.X, MidpointRounding.AwayFromZero),
        Y = (float)Math.Round(box.Y, MidpointRounding.AwayFromZero),
        Width = (float)Math.Round(box.Width, MidpointRounding.AwayFromZero),
        Height = (float)Math.Round(box.Height, MidpointRounding.AwayFromZero),
    };

    public static ElementHandleBoundingBoxResult RoundBox(ElementHandleBoundingBoxResult box) => new()
    {
        X = (float)Math.Round(box.X, MidpointRounding.AwayFromZero),
        Y = (float)Math.Round(box.Y, MidpointRounding.AwayFromZero),
        Width = (float)Math.Round(box.Width, MidpointRounding.AwayFromZero),
        Height = (float)Math.Round(box.Height, MidpointRounding.AwayFromZero),
    };

    public static string Unshift(string snapshot)
    {
        var lines = snapshot.Split('\n');
        var whitespacePrefixLength = 100;
        foreach (var line in lines)
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }
            var match = Regex.Match(line, @"^(\s*)");
            if (match.Success && match.Groups[1].Length < whitespacePrefixLength)
            {
                whitespacePrefixLength = match.Groups[1].Length;
            }
        }
        return string.Join("\n", lines.Where(t => t.Trim().Length > 0).Select(line => line.Substring(whitespacePrefixLength)));
    }

    public static string StripAnsi(string str) => _ansiRegex.Replace(str, string.Empty);

    /// <summary><c>expect(received).toContainYaml(expected)</c> of <c>tests/page/pageTest.ts</c>.</summary>
    public static void ExpectToContainYaml(string received, string expected)
    {
        Assert.That(received, Does.Contain(TrimYaml(expected)));
    }

    /// <summary><c>expect(received).not.toContainYaml(expected)</c>.</summary>
    public static void ExpectNotToContainYaml(string received, string expected)
    {
        Assert.That(received, Does.Not.Contain(TrimYaml(expected)));
    }

    /// <summary><c>const error = await promise.catch(e => e)</c>: the exception of the call, or null when it does not throw.</summary>
    public static async Task<Exception> Catch(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>
    /// <c>await expect(promise).rejects.toThrow()</c>: the exception of the call, which must be a <typeparamref name="T"/>.
    /// NUnit's Assert.ThrowsAsync blocks the thread, so the ports use this one.
    /// </summary>
    public static async Task<T> ThrowsAsync<T>(Func<Task> action)
        where T : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (T e)
        {
            return e;
        }
        Assert.Fail($"Expected the call to throw {typeof(T).Name}, and it did not throw");
        return null;
    }

    /// <summary><c>new Promise(f => setTimeout(f, timeout))</c>.</summary>
    public static Task SetTimeout(int timeout) => Task.Delay(timeout);

    /// <summary>
    /// Adds the defaults to the options that upstream adds without a word: every property that
    /// <paramref name="options"/> does not set and <paramref name="defaults"/> has under the same name.
    /// </summary>
    public static T MergeOptions<T>(T options, object defaults)
        where T : class
    {
        if (defaults == null)
        {
            return options;
        }
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || property.GetValue(options) != null)
            {
                continue;
            }
            var source = defaults.GetType().GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
            var value = source?.CanRead == true ? source.GetValue(defaults) : null;
            if (value != null && property.PropertyType.IsInstanceOfType(value))
            {
                property.SetValue(options, value);
            }
        }
        return options;
    }

    /// <summary><c>removeFolders(dirs)</c>: a browser that is closing may still hold files of its profile.</summary>
    public static async Task RemoveFoldersAsync(IEnumerable<string> dirs)
    {
        foreach (var dir in dirs)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(200 * (attempt + 1)).ConfigureAwait(false);
                }
            }
        }
    }

    private static string TrimYaml(string expected)
    {
        var trimmed = expected.Split('\n').Where(a => a.Trim().Length > 0).ToArray();
        var maxPrefixLength = trimmed.Min(line => Regex.Match(line, @"^\s*").Length);
        return string.Join("\n", trimmed.Select(line => line.Substring(maxPrefixLength)));
    }

    private static string ReadHostPlatform()
    {
        if (Env.IsMac)
        {
            return "mac" + Env.MacVersion;
        }
        if (Env.IsWindows)
        {
            return "win64";
        }
        try
        {
            var fields = File.ReadAllLines("/etc/os-release")
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1].Trim('"'));
            return fields.GetValueOrDefault("ID", "linux") + fields.GetValueOrDefault("VERSION_ID", string.Empty);
        }
        catch (Exception)
        {
            return "linux";
        }
    }
}
