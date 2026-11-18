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

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// The port of <c>tests/library/videoPlayer.ts</c>: <c>new VideoPlayer(fileName)</c> splits a video into
/// frames with the ffmpeg of the browser registry. A frame is a 10x10 square of the picture, at
/// <c>offset</c> (10, 10 when not given): <c>videoPlayer.seekLastFrame({ x: 95, y: 45 }).data</c> is
/// <c>videoPlayer.SeekLastFrame((x: 95, y: 45)).Data</c>.
/// </summary>
public sealed class VideoPlayer
{
    private static readonly Lazy<string> _ffmpeg = new(FindFfmpeg);
    private readonly Dictionary<int, ImageData> _cache = [];

    public VideoPlayer(string fileName)
    {
        FileName = fileName;
        var ffmpeg = _ffmpeg.Value;
        // Force output frame rate to 25 fps as otherwise it would produce one image per timebase unit
        // which is 1 / (25 * 1000).
        Output = SpawnSync(ffmpeg, ["-i", FileName, "-r", "25", $"{FileName}-%04d.png"]).Stderr;

        var lines = Output.Split('\n');
        var framesLine = lines.FirstOrDefault(l => l.StartsWith("frame=", StringComparison.Ordinal));
        if (string.IsNullOrEmpty(framesLine))
        {
            throw new InvalidOperationException($"No frame data in the output:\n{Output}");
        }
        framesLine = framesLine.Substring(framesLine.LastIndexOf("frame=", StringComparison.Ordinal));
        var framesMatch = Regex.Match(framesLine, @"frame=\s+(\d+)");
        var streamLine = lines.First(l => l.Trim().StartsWith("Stream #0:0", StringComparison.Ordinal));
        var resolutionMatch = Regex.Match(streamLine, @", (\d+)x(\d+),");
        var fpsMatch = Regex.Match(streamLine, @", ([\d.]+) fps,");
        var durationMatch = Regex.Match(lines.First(l => l.Trim().StartsWith("Duration", StringComparison.Ordinal)), @"Duration: (\d+):(\d\d):(\d\d.\d\d)");
        Duration = ((((ParseInt(durationMatch.Groups[1].Value) * 60) + ParseInt(durationMatch.Groups[2].Value)) * 60) + ParseFloat(durationMatch.Groups[3].Value)) * 1000;
        Frames = ParseInt(framesMatch.Groups[1].Value);
        VideoWidth = ParseInt(resolutionMatch.Groups[1].Value);
        VideoHeight = ParseInt(resolutionMatch.Groups[2].Value);
        Fps = ParseFloat(fpsMatch.Groups[1].Value);
    }

    public string FileName { get; }

    /// <summary>What ffmpeg printed.</summary>
    public string Output { get; }

    /// <summary>In milliseconds.</summary>
    public double Duration { get; }

    public int Frames { get; }

    public int VideoWidth { get; }

    public int VideoHeight { get; }

    public double Fps { get; }

    /// <summary><c>videoPlayer.findFrame(pixels => ..., offset)</c>: null when no frame matches.</summary>
    internal ImageData FindFrame(Func<byte[], bool> framePredicate, (int x, int y)? offset = null)
    {
        for (var f = 1; f <= Frames; ++f)
        {
            var frame = Frame(f, offset);
            if (framePredicate(frame.Data))
            {
                return frame;
            }
        }
        return null;
    }

    internal ImageData SeekLastFrame((int x, int y)? offset = null) => Frame(Frames, offset);

    internal ImageData Frame(int frame, (int x, int y)? offset = null)
    {
        var (x, y) = offset ?? (x: 10, y: 10);
        if (!_cache.TryGetValue(frame, out var decoded))
        {
            var gap = new string('0', 4 - frame.ToString(CultureInfo.InvariantCulture).Length);
            var buffer = File.ReadAllBytes($"{FileName}-{gap}{frame}.png");
            decoded = ImageComparator.Decode("image/png", buffer);
            _cache[frame] = decoded;
        }
        var dst = new ImageData(10, 10, new byte[10 * 10 * 4]);
        Bitblt(decoded, dst, x, y, 10, 10, 0, 0);
        return dst;
    }

    private static int ParseInt(string value) => int.Parse(value, CultureInfo.InvariantCulture);

    private static double ParseFloat(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    // registry.registry.findExecutable('ffmpeg').executablePathOrDie('javascript'), asked in the Node.js of the driver.
    private static string FindFfmpeg()
    {
        var bundle = Path.Combine(Path.GetDirectoryName(NodeProcess.CliPath), "lib", "coreBundle.js");
        var result = SpawnSync(NodeProcess.ExecPath, ["-e", "process.stdout.write(require(process.argv[1]).registry.registry.findExecutable('ffmpeg').executablePathOrDie('javascript'))", bundle]);
        if (result.ExitCode != 0 || result.Stdout.Length == 0)
        {
            throw new InvalidOperationException($"Could not find ffmpeg:\n{result.Stderr}");
        }
        return result.Stdout;
    }

    // spawnSync of 'child_process'.
    private static (string Stdout, string Stderr, int ExitCode) SpawnSync(string command, string[] args)
    {
        var startInfo = new ProcessStartInfo(command)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }
        using var process = Process.Start(startInfo);
        process.StandardInput.Close();
        string stderr = null;
        var stderrReader = new Thread(() => stderr = process.StandardError.ReadToEnd());
        stderrReader.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        stderrReader.Join();
        process.WaitForExit();
        return (stdout, stderr, process.ExitCode);
    }

    // PNG.bitblt of 'pngjs'.
    private static void Bitblt(ImageData src, ImageData dst, int srcX, int srcY, int width, int height, int deltaX, int deltaY)
    {
        if (srcX > src.Width || srcY > src.Height || srcX + width > src.Width || srcY + height > src.Height)
        {
            throw new ArgumentException("bitblt reading outside image");
        }
        if (deltaX > dst.Width || deltaY > dst.Height || deltaX + width > dst.Width || deltaY + height > dst.Height)
        {
            throw new ArgumentException("bitblt writing outside image");
        }
        for (var y = 0; y < height; y++)
        {
            Array.Copy(src.Data, (((srcY + y) * src.Width) + srcX) << 2, dst.Data, (((deltaY + y) * dst.Width) + deltaX) << 2, width << 2);
        }
    }
}
