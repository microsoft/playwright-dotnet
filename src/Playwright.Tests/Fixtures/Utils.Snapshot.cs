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

using NUnit.Framework.Internal;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>The options of <c>toMatchSnapshot</c> and <c>comparePNGs</c>, <c>ImageComparatorOptions</c> upstream.</summary>
public sealed class SnapshotOptions
{
    /// <summary>The color difference that 'pixelmatch' accepts for a pixel, from 0 (strict) to 1. The default is 0.2.</summary>
    public double? Threshold { get; set; }

    /// <summary>The number of different pixels that is accepted. The default is 0.</summary>
    public int? MaxDiffPixels { get; set; }

    /// <summary>The share of different pixels that is accepted, from 0 to 1.</summary>
    public double? MaxDiffPixelRatio { get; set; }

    /// <summary>'pixelmatch' (the default) or 'ssim-cie94'.</summary>
    public string Comparator { get; set; }
}

/// <summary><c>expect(value).toMatchSnapshot(name)</c> and <c>comparePNGs</c> of <c>tests/config/comparator.ts</c>.</summary>
public static partial class Utils
{
    /// <summary>
    /// <c>expect(buffer).toMatchSnapshot('x.png', options)</c>. The golden file is <c>Info.SnapshotPath(name)</c>,
    /// an image: the port does not compare snapshots of text or of other files.
    /// The port never writes a golden file, so a missing one is a failure.
    /// </summary>
    public static void ExpectToMatchSnapshot(byte[] actual, string name, SnapshotOptions options = null) => MatchSnapshot(actual, name, options, false);

    /// <summary><c>expect(buffer).not.toMatchSnapshot('x.png', options)</c>.</summary>
    public static void ExpectNotToMatchSnapshot(byte[] actual, string name, SnapshotOptions options = null) => MatchSnapshot(actual, name, options, true);

    /// <summary>
    /// <c>comparePNGs(actual, expected, options)</c>: null when the images match. The comparator is
    /// 'ssim-cie94' and the threshold is 0, unless the options say otherwise.
    /// </summary>
    public static ComparatorResult ComparePNGs(byte[] actual, byte[] expected, SnapshotOptions options = null)
    {
        return ImageComparator.CompareImages("image/png", actual, expected, new()
        {
            Comparator = options?.Comparator ?? "ssim-cie94",
            Threshold = options?.Threshold ?? 0,
            MaxDiffPixels = options?.MaxDiffPixels,
            MaxDiffPixelRatio = options?.MaxDiffPixelRatio,
        });
    }

    private static void MatchSnapshot(byte[] actual, string name, SnapshotOptions options, bool isNot)
    {
        options ??= new();
        if (options.MaxDiffPixels < 0)
        {
            throw new ArgumentException("`maxDiffPixels` option value must be non-negative integer");
        }
        if (options.MaxDiffPixelRatio is < 0 or > 1)
        {
            throw new ArgumentException("`maxDiffPixelRatio` option value must be between 0 and 1");
        }

        var info = ((UpstreamPlaywrightTest)TestExecutionContext.CurrentContext.TestObject).Info;
        var expectedPath = info.SnapshotPath(name);
        var mimeType = ImageComparator.GetMimeTypeForPath(expectedPath) ?? throw new NotSupportedException($"{name} is not an image: the port compares snapshots of images only");
        var hasSnapshot = File.Exists(expectedPath);

        if (isNot)
        {
            if (!hasSnapshot)
            {
                Assert.Fail($"A snapshot doesn't exist at {expectedPath}.");
            }
            if (ImageComparator.CompareImages(mimeType, actual, File.ReadAllBytes(expectedPath), options) == null)
            {
                Assert.Fail("Screenshot comparison failed:\n\n  Expected result should be different from the actual one.");
            }
            return;
        }

        var actualPath = SnapshotOutputPath(info, name, "-actual");
        if (!hasSnapshot)
        {
            WriteSnapshot(actualPath, actual);
            Assert.Fail($"A snapshot doesn't exist at {expectedPath}.\n\n    Received: {actualPath}\n");
        }

        var expected = File.ReadAllBytes(expectedPath);
        var result = ImageComparator.CompareImages(mimeType, actual, expected, options);
        if (result == null)
        {
            return;
        }

        // Upstream copies the expectation next to the actual one as well.
        WriteSnapshot(SnapshotOutputPath(info, name, "-expected"), expected);
        WriteSnapshot(actualPath, actual);
        var message = "expect(Buffer).toMatchSnapshot(expected) failed\n\n"
            + string.Join("\n", result.ErrorMessage.Split('\n').Select(line => line.Length > 0 ? "  " + line : line))
            + $"\n\n  Snapshot: {name}\n\n    Expected: {expectedPath}\n    Received: {actualPath}\n";
        if (result.Diff != null)
        {
            var diffPath = SnapshotOutputPath(info, name, "-diff");
            WriteSnapshot(diffPath, result.Diff);
            message += $"    Diff:     {diffPath}\n";
        }
        Assert.Fail(message);
    }

    /// <summary><c>addSuffixToFilePath(testInfo.outputPath(name), suffix)</c>: <c>x-actual.png</c> for <c>x.png</c>.</summary>
    private static string SnapshotOutputPath(TestInfo info, string name, string suffix)
    {
        var extension = Path.GetExtension(name);
        var path = info.OutputPath((name.Substring(0, name.Length - extension.Length) + suffix + extension).Split('/', '\\'));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        return path;
    }

    private static void WriteSnapshot(string path, byte[] content) => File.WriteAllBytes(path, content);
}
