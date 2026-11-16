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


using System.Globalization;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary><c>ComparatorResult</c>: what a comparator returns for two snapshots that differ. It returns null when they match.</summary>
public sealed class ComparatorResult
{
    public string ErrorMessage { get; set; }

    /// <summary>The diff image as a PNG, for the image comparators.</summary>
    public byte[] Diff { get; set; }
}

/// <summary><c>Comparator</c>: the actual value is a <c>byte[]</c> or a <c>string</c>.</summary>
internal delegate ComparatorResult SnapshotComparator(object actual, byte[] expected, SnapshotOptions options);

/// <summary>The port of <c>packages/utils/comparators.ts</c>.</summary>
internal static class ImageComparator
{
    private static readonly byte[] _pngMagicNumber = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] _jpegMagicNumber = [255, 216];
    private static readonly string[] _textExtensions = ["conf", "def", "in", "ini", "list", "log", "text", "txt"];

    /// <summary><c>getMimeTypeForPath(path)</c>, for the types that have a comparator of their own.</summary>
    public static string GetMimeTypeForPath(string path)
    {
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "webp" => "image/webp",
            _ => _textExtensions.Contains(extension) ? "text/plain" : null,
        };
    }

    public static SnapshotComparator GetComparator(string mimeType) => mimeType switch
    {
        "image/png" or "image/jpeg" or "image/webp" => (actual, expected, options) => CompareImages(mimeType, actual, expected, options),
        "text/plain" => (actual, expected, _) => CompareText(actual, expected),
        _ => (actual, expected, _) => CompareBuffersOrStrings(actual, expected),
    };

    public static ComparatorResult CompareBuffersOrStrings(object actualBuffer, byte[] expectedBuffer)
    {
        if (actualBuffer is string)
        {
            return CompareText(actualBuffer, expectedBuffer);
        }
        if (actualBuffer is not byte[] actualBytes)
        {
            return new() { ErrorMessage = "Actual result should be a Buffer or a string." };
        }
        if (!actualBytes.AsSpan().SequenceEqual(expectedBuffer))
        {
            return new() { ErrorMessage = "Buffers differ" };
        }
        return null;
    }

    public static ComparatorResult CompareImages(string mimeType, object actualBuffer, byte[] expectedBuffer, SnapshotOptions options = null)
    {
        options ??= new();
        if (actualBuffer is not byte[] actualBytes)
        {
            return new() { ErrorMessage = "Actual result should be a Buffer." };
        }
        ValidateBuffer(expectedBuffer, mimeType);

        var actual = Decode(mimeType, actualBytes);
        var expected = Decode(mimeType, expectedBuffer);
        var width = Math.Max(expected.Width, actual.Width);
        var height = Math.Max(expected.Height, actual.Height);
        var sizesMismatchError = string.Empty;
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            sizesMismatchError = $"Expected an image {expected.Width}px by {expected.Height}px, received {actual.Width}px by {actual.Height}px. ";
            actual = PadImageToSize(actual, width, height);
            expected = PadImageToSize(expected, width, height);
        }
        var diff = new byte[width * height * 4];
        int count;
        if (options.Comparator == "ssim-cie94")
        {
            // Upstream passes the expected image as `actual` and the actual one as `expected` too.
            count = ImageTools.Compare(expected.Data, actual.Data, diff, width, height, maxColorDeltaE94: 1.0);
        }
        else if ((options.Comparator ?? "pixelmatch") == "pixelmatch")
        {
            count = Pixelmatch.Compare(expected.Data, actual.Data, diff, width, height, options.Threshold ?? 0.2);
        }
        else
        {
            throw new ArgumentException($"Configuration specifies unknown comparator \"{options.Comparator}\"");
        }

        double? maxDiffPixels1 = options.MaxDiffPixels;
        double? maxDiffPixels2 = options.MaxDiffPixelRatio != null ? expected.Width * expected.Height * options.MaxDiffPixelRatio.Value : null;
        double maxDiffPixels;
        if (maxDiffPixels1 != null && maxDiffPixels2 != null)
        {
            maxDiffPixels = Math.Min(maxDiffPixels1.Value, maxDiffPixels2.Value);
        }
        else
        {
            maxDiffPixels = maxDiffPixels1 ?? maxDiffPixels2 ?? 0;
        }
        var ratio = Math.Ceiling((double)count / (expected.Width * expected.Height) * 100) / 100;
        var pixelsMismatchError = count > maxDiffPixels
            ? string.Create(CultureInfo.InvariantCulture, $"{count} pixels (ratio {ratio:F2} of all image pixels) are different.")
            : string.Empty;
        if (pixelsMismatchError.Length > 0 || sizesMismatchError.Length > 0)
        {
            return new() { ErrorMessage = sizesMismatchError + pixelsMismatchError, Diff = EncodePng(new ImageData(width, height, diff)) };
        }
        return null;
    }

    public static ComparatorResult CompareText(object actualValue, byte[] expectedBuffer)
    {
        if (actualValue is not string actual)
        {
            return new() { ErrorMessage = "Actual result should be a string" };
        }
        var expected = Encoding.UTF8.GetString(expectedBuffer);
        // Eliminate '\\ No newline at end of file'
        if (!actual.EndsWith('\n'))
        {
            actual += '\n';
        }
        if (!expected.EndsWith('\n'))
        {
            expected += '\n';
        }
        if (expected == actual)
        {
            return null;
        }
        // Upstream colors the lines for the terminal, the port does not.
        return new() { ErrorMessage = string.Join("\n", TextDiff.CreatePatch(expected, actual, context: 5)) + "\n" };
    }

    /// <summary>The RGBA pixels of an encoded image, as <c>PNG.sync.read</c> and <c>jpegjs.decode</c> give them.</summary>
    public static ImageData Decode(string mimeType, byte[] buffer)
    {
        IImageDecoder decoder = mimeType switch
        {
            "image/png" => new PngDecoder(),
            "image/webp" => new WebpDecoder(),
            _ => new JpegDecoder(),
        };
        using var image = Image.Load<Rgba32>(buffer, decoder);
        var data = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(data);
        return new ImageData(image.Width, image.Height, data);
    }

    /// <summary><c>PNG.sync.write</c>.</summary>
    public static byte[] EncodePng(ImageData image)
    {
        using var png = Image.LoadPixelData<Rgba32>(image.Data, image.Width, image.Height);
        using var stream = new MemoryStream();
        png.Save(stream, new PngEncoder { ColorType = PngColorType.RgbWithAlpha, BitDepth = PngBitDepth.Bit8 });
        return stream.ToArray();
    }

    private static void ValidateBuffer(byte[] buffer, string mimeType)
    {
        if (mimeType == "image/png")
        {
            if (!buffer.AsSpan().StartsWith(_pngMagicNumber))
            {
                throw new InvalidDataException("Could not decode expected image as PNG.");
            }
        }
        else if (mimeType == "image/jpeg")
        {
            if (!buffer.AsSpan().StartsWith(_jpegMagicNumber))
            {
                throw new InvalidDataException("Could not decode expected image as JPEG.");
            }
        }
        else if (mimeType == "image/webp")
        {
            // A WebP bitstream is a RIFF container tagged 'WEBP': "RIFF????WEBP".
            if (buffer.Length < 12 || !buffer.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !buffer.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            {
                throw new InvalidDataException("Could not decode expected image as WEBP.");
            }
        }
    }

    /// <summary><c>padImageToSize</c> of <c>packages/isomorphic/imageUtils.ts</c>: the new pixels are transparent black.</summary>
    private static ImageData PadImageToSize(ImageData image, int width, int height)
    {
        if (image.Width == width && image.Height == height)
        {
            return image;
        }
        var buffer = new byte[width * height * 4];
        for (var y = 0; y < image.Height; y++)
        {
            Array.Copy(image.Data, y * image.Width * 4, buffer, y * width * 4, image.Width * 4);
        }
        return new ImageData(width, height, buffer);
    }
}

/// <summary><c>ImageData</c>: 4 bytes per pixel, RGBA.</summary>
internal sealed record ImageData(int Width, int Height, byte[] Data);

/// <summary>
/// The port of <c>packages/utils/third_party/pixelmatch.js</c> with its default options but the threshold,
/// as upstream calls it. The arithmetic is in doubles and in the order of the JavaScript, and a value
/// that goes into the diff is truncated, as an assignment to a <c>Uint8Array</c> does.
/// </summary>
internal static class Pixelmatch
{
    private const double Alpha = 0.1;

    public static int Compare(byte[] img1, byte[] img2, byte[] output, int width, int height, double threshold)
    {
        if (img1.Length != img2.Length || (output != null && output.Length != img1.Length))
        {
            throw new ArgumentException("Image sizes do not match.");
        }
        if (img1.Length != width * height * 4)
        {
            throw new ArgumentException("Image data size does not match width/height.");
        }

        var len = width * height;
        if (img1.AsSpan().SequenceEqual(img2))
        {
            if (output != null)
            {
                for (var i = 0; i < len; i++)
                {
                    DrawGrayPixel(img1, 4 * i, Alpha, output);
                }
            }
            return 0;
        }

        // maximum acceptable square distance between two colors;
        // 35215 is the maximum possible value for the YIQ difference metric
        var maxDelta = 35215 * threshold * threshold;
        var diff = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pos = ((y * width) + x) * 4;

                // squared YUV distance between colors at this pixel position, negative if the img2 pixel is darker
                var delta = ColorDelta(img1, img2, pos, pos, false);

                if (Math.Abs(delta) > maxDelta)
                {
                    // check it's a real rendering difference or just anti-aliasing
                    if (Antialiased(img1, x, y, width, height, img2) || Antialiased(img2, x, y, width, height, img1))
                    {
                        // one of the pixels is anti-aliasing; draw as yellow and do not count as difference
                        if (output != null)
                        {
                            DrawPixel(output, pos, 255, 255, 0);
                        }
                    }
                    else
                    {
                        if (output != null)
                        {
                            DrawPixel(output, pos, 255, 0, 0);
                        }
                        diff++;
                    }
                }
                else if (output != null)
                {
                    // pixels are similar; draw background as grayscale image blended with white
                    DrawGrayPixel(img1, pos, Alpha, output);
                }
            }
        }

        return diff;
    }

    // check if a pixel is likely a part of anti-aliasing;
    // based on "Anti-aliased Pixel and Intensity Slope Detector" paper by V. Vysniauskas, 2009
    private static bool Antialiased(byte[] img, int x1, int y1, int width, int height, byte[] img2)
    {
        var x0 = Math.Max(x1 - 1, 0);
        var y0 = Math.Max(y1 - 1, 0);
        var x2 = Math.Min(x1 + 1, width - 1);
        var y2 = Math.Min(y1 + 1, height - 1);
        var pos = ((y1 * width) + x1) * 4;
        var zeroes = x1 == x0 || x1 == x2 || y1 == y0 || y1 == y2 ? 1 : 0;
        double min = 0;
        double max = 0;
        int minX = 0, minY = 0, maxX = 0, maxY = 0;

        // go through 8 adjacent pixels
        for (var x = x0; x <= x2; x++)
        {
            for (var y = y0; y <= y2; y++)
            {
                if (x == x1 && y == y1)
                {
                    continue;
                }

                // brightness delta between the center pixel and adjacent one
                var delta = ColorDelta(img, img, pos, ((y * width) + x) * 4, true);

                // count the number of equal, darker and brighter adjacent pixels
                if (delta == 0)
                {
                    zeroes++;
                    // if found more than 2 equal siblings, it's definitely not anti-aliasing
                    if (zeroes > 2)
                    {
                        return false;
                    }
                }
                else if (delta < min)
                {
                    min = delta;
                    minX = x;
                    minY = y;
                }
                else if (delta > max)
                {
                    max = delta;
                    maxX = x;
                    maxY = y;
                }
            }
        }

        // if there are no both darker and brighter pixels among siblings, it's not anti-aliasing
        if (min == 0 || max == 0)
        {
            return false;
        }

        // if either the darkest or the brightest pixel has 3+ equal siblings in both images
        // (definitely not anti-aliased), this pixel is anti-aliased
        return (HasManySiblings(img, minX, minY, width, height) && HasManySiblings(img2, minX, minY, width, height)) ||
               (HasManySiblings(img, maxX, maxY, width, height) && HasManySiblings(img2, maxX, maxY, width, height));
    }

    // check if a pixel has 3+ adjacent pixels of the same color.
    private static bool HasManySiblings(byte[] img, int x1, int y1, int width, int height)
    {
        var x0 = Math.Max(x1 - 1, 0);
        var y0 = Math.Max(y1 - 1, 0);
        var x2 = Math.Min(x1 + 1, width - 1);
        var y2 = Math.Min(y1 + 1, height - 1);
        var pos = ((y1 * width) + x1) * 4;
        var zeroes = x1 == x0 || x1 == x2 || y1 == y0 || y1 == y2 ? 1 : 0;

        // go through 8 adjacent pixels
        for (var x = x0; x <= x2; x++)
        {
            for (var y = y0; y <= y2; y++)
            {
                if (x == x1 && y == y1)
                {
                    continue;
                }

                var pos2 = ((y * width) + x) * 4;
                if (img[pos] == img[pos2] &&
                    img[pos + 1] == img[pos2 + 1] &&
                    img[pos + 2] == img[pos2 + 2] &&
                    img[pos + 3] == img[pos2 + 3])
                {
                    zeroes++;
                }

                if (zeroes > 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // calculate color difference according to the paper "Measuring perceived color difference
    // using YIQ NTSC transmission color space in mobile applications" by Y. Kotsarenko and F. Ramos
    private static double ColorDelta(byte[] img1, byte[] img2, int k, int m, bool yOnly)
    {
        double r1 = img1[k + 0];
        double g1 = img1[k + 1];
        double b1 = img1[k + 2];
        double a1 = img1[k + 3];

        double r2 = img2[m + 0];
        double g2 = img2[m + 1];
        double b2 = img2[m + 2];
        double a2 = img2[m + 3];

        if (a1 == a2 && r1 == r2 && g1 == g2 && b1 == b2)
        {
            return 0;
        }

        if (a1 < 255)
        {
            a1 /= 255;
            r1 = Blend(r1, a1);
            g1 = Blend(g1, a1);
            b1 = Blend(b1, a1);
        }

        if (a2 < 255)
        {
            a2 /= 255;
            r2 = Blend(r2, a2);
            g2 = Blend(g2, a2);
            b2 = Blend(b2, a2);
        }

        var y1 = Rgb2y(r1, g1, b1);
        var y2 = Rgb2y(r2, g2, b2);
        var y = y1 - y2;

        if (yOnly)
        {
            // brightness difference only
            return y;
        }

        var i = Rgb2i(r1, g1, b1) - Rgb2i(r2, g2, b2);
        var q = Rgb2q(r1, g1, b1) - Rgb2q(r2, g2, b2);

        var delta = (0.5053 * y * y) + (0.299 * i * i) + (0.1957 * q * q);

        // encode whether the pixel lightens or darkens in the sign
        return y1 > y2 ? -delta : delta;
    }

    private static double Rgb2y(double r, double g, double b) => (r * 0.29889531) + (g * 0.58662247) + (b * 0.11448223);

    private static double Rgb2i(double r, double g, double b) => (r * 0.59597799) - (g * 0.27417610) - (b * 0.32180189);

    private static double Rgb2q(double r, double g, double b) => (r * 0.21147017) - (g * 0.52261711) + (b * 0.31114694);

    // blend semi-transparent color with white
    private static double Blend(double c, double a) => 255 + ((c - 255) * a);

    private static void DrawPixel(byte[] output, int pos, double r, double g, double b)
    {
        output[pos + 0] = (byte)(int)r;
        output[pos + 1] = (byte)(int)g;
        output[pos + 2] = (byte)(int)b;
        output[pos + 3] = 255;
    }

    private static void DrawGrayPixel(byte[] img, int i, double alpha, byte[] output)
    {
        double r = img[i + 0];
        double g = img[i + 1];
        double b = img[i + 2];
        var val = Blend(Rgb2y(r, g, b), alpha * img[i + 3] / 255);
        DrawPixel(output, i, val, val, val);
    }
}

/// <summary>
/// The port of <c>packages/utils/image_tools</c>, the comparator 'ssim-cie94'. The arithmetic is in
/// doubles and in the order of the JavaScript.
/// </summary>
internal static class ImageTools
{
    private const int SsimWindowRadius = 15;
    private const int VarianceWindowRadius = 1;

    // Image channel has a 8-bit depth.
    private const double DynamicRange = 255;

    private const double SigmaPow2 = 6.0 * 6 / 29 / 29;
    private const double SigmaPow3 = 6.0 * 6 * 6 / 29 / 29 / 29;

    /// <summary><c>compare</c> of <c>compare.ts</c>: the number of pixels that differ.</summary>
    public static int Compare(byte[] actual, byte[] expected, byte[] diff, int width, int height, double maxColorDeltaE94 = 1.0)
    {
        var paddingSize = Math.Max(VarianceWindowRadius, SsimWindowRadius);
        int[] paddingColorEven = [255, 0, 255];
        int[] paddingColorOdd = [0, 255, 0];
        var (r1, g1, b1) = ImageChannel.IntoRGB(width, height, expected, paddingSize, paddingColorOdd, paddingColorEven);
        var (r2, g2, b2) = ImageChannel.IntoRGB(width, height, actual, paddingSize, paddingColorOdd, paddingColorEven);

        void DrawRedPixel(int x, int y) => DrawPixel(width, diff, x - paddingSize, y - paddingSize, 255, 0, 0);
        void DrawYellowPixel(int x, int y) => DrawPixel(width, diff, x - paddingSize, y - paddingSize, 255, 255, 0);
        void DrawGrayPixel(int x, int y)
        {
            var gray = Rgb2gray(r1.Get(x, y), g1.Get(x, y), b1.Get(x, y));
            var value = BlendWithWhite(gray, 0.1);
            DrawPixel(width, diff, x - paddingSize, y - paddingSize, value, value, value);
        }

        FastStats fastR = null;
        FastStats fastG = null;
        FastStats fastB = null;

        var diffCount = 0;
        for (var y = paddingSize; y < r1.Height - paddingSize; ++y)
        {
            for (var x = paddingSize; x < r1.Width - paddingSize; ++x)
            {
                // Fast-path: equal pixels.
                if (r1.Get(x, y) == r2.Get(x, y) && g1.Get(x, y) == g2.Get(x, y) && b1.Get(x, y) == b2.Get(x, y))
                {
                    DrawGrayPixel(x, y);
                    continue;
                }

                // Compare pixel colors using the dE94 color difference formulae.
                // The dE94 is normalized so that the value of 1.0 is the "just-noticeable-difference".
                // Color difference below 1.0 is not noticeable to a human eye, so we can disregard it.
                // See https://en.wikipedia.org/wiki/Color_difference
                var delta = ColorDeltaE94(
                    [r1.Get(x, y), g1.Get(x, y), b1.Get(x, y)],
                    [r2.Get(x, y), g2.Get(x, y), b2.Get(x, y)]);

                if (delta <= maxColorDeltaE94)
                {
                    DrawGrayPixel(x, y);
                    continue;
                }

                if (fastR == null || fastG == null || fastB == null)
                {
                    fastR = new FastStats(r1, r2);
                    fastG = new FastStats(g1, g2);
                    fastB = new FastStats(b1, b2);
                }
                var (varX1, varY1) = r1.BoundXY(x - VarianceWindowRadius, y - VarianceWindowRadius);
                var (varX2, varY2) = r1.BoundXY(x + VarianceWindowRadius, y + VarianceWindowRadius);
                var var1 = fastR.VarianceC1(varX1, varY1, varX2, varY2) + fastG.VarianceC1(varX1, varY1, varX2, varY2) + fastB.VarianceC1(varX1, varY1, varX2, varY2);
                var var2 = fastR.VarianceC2(varX1, varY1, varX2, varY2) + fastG.VarianceC2(varX1, varY1, varX2, varY2) + fastB.VarianceC2(varX1, varY1, varX2, varY2);
                // if this pixel is a part of a flood fill of a 3x3 square of either of the images, then it cannot be
                // anti-aliasing pixel so it must be a pixel difference.
                if (var1 == 0 || var2 == 0)
                {
                    DrawRedPixel(x, y);
                    ++diffCount;
                    continue;
                }

                var (ssimX1, ssimY1) = r1.BoundXY(x - SsimWindowRadius, y - SsimWindowRadius);
                var (ssimX2, ssimY2) = r1.BoundXY(x + SsimWindowRadius, y + SsimWindowRadius);
                var ssimRGB = (Ssim(fastR, ssimX1, ssimY1, ssimX2, ssimY2) + Ssim(fastG, ssimX1, ssimY1, ssimX2, ssimY2) + Ssim(fastB, ssimX1, ssimY1, ssimX2, ssimY2)) / 3.0;
                var isAntialiased = ssimRGB >= 0.99;
                if (isAntialiased)
                {
                    DrawYellowPixel(x, y);
                }
                else
                {
                    DrawRedPixel(x, y);
                    ++diffCount;
                }
            }
        }

        return diffCount;
    }

    private static void DrawPixel(int width, byte[] data, int x, int y, double r, double g, double b)
    {
        if (data == null)
        {
            return;
        }
        var idx = ((y * width) + x) * 4;
        data[idx + 0] = (byte)(int)r;
        data[idx + 1] = (byte)(int)g;
        data[idx + 2] = (byte)(int)b;
        data[idx + 3] = 255;
    }

    private static double BlendWithWhite(double c, double a) => 255 + ((c - 255) * a);

    // NOTE: this is the exact integer formula from SSIM.js.
    private static int Rgb2gray(int r, int g, int b) => ((77 * r) + (150 * g) + (29 * b) + 128) >> 8;

    // Perceived color difference defined by CIE94, where 1.0 is a "just-noticeable difference".
    // See https://en.wikipedia.org/wiki/Color_difference#CIE94
    private static double ColorDeltaE94(int[] rgb1, int[] rgb2)
    {
        var (l1, a1, b1) = Xyz2lab(Srgb2xyz(rgb1));
        var (l2, a2, b2) = Xyz2lab(Srgb2xyz(rgb2));
        var deltaL = l1 - l2;
        var deltaA = a1 - a2;
        var deltaB = b1 - b2;
        var c1 = Math.Sqrt((a1 * a1) + (b1 * b1));
        var c2 = Math.Sqrt((a2 * a2) + (b2 * b2));
        var deltaC = c1 - c2;
        var deltaH = (deltaA * deltaA) + (deltaB * deltaB) - (deltaC * deltaC);
        deltaH = deltaH < 0 ? 0 : Math.Sqrt(deltaH);
        // The k1, k2, kL, kC, kH values for "graphic arts" applications.
        const double k1 = 0.045;
        const double k2 = 0.015;
        const double kL = 1;
        const double kC = 1;
        const double kH = 1;

        var sC = 1.0 + (k1 * c1);
        var sH = 1.0 + (k2 * c1);
        const double sL = 1;

        var l = deltaL / sL / kL;
        var c = deltaC / sC / kC;
        var h = deltaH / sH / kH;
        return Math.Sqrt((l * l) + (c * c) + (h * h));
    }

    // sRGB -> 1-normalized XYZ (i.e. Y ∈ [0, 1]) with D65 illuminant
    // See https://en.wikipedia.org/wiki/SRGB#From_sRGB_to_CIE_XYZ
    private static (double X, double Y, double Z) Srgb2xyz(int[] rgb)
    {
        var r = rgb[0] / 255.0;
        var g = rgb[1] / 255.0;
        var b = rgb[2] / 255.0;
        r = (r > 0.04045) ? Math.Pow((r + 0.055) / 1.055, 2.4) : r / 12.92;
        g = (g > 0.04045) ? Math.Pow((g + 0.055) / 1.055, 2.4) : g / 12.92;
        b = (b > 0.04045) ? Math.Pow((b + 0.055) / 1.055, 2.4) : b / 12.92;
        return (
            (r * 0.4124) + (g * 0.3576) + (b * 0.1805),
            (r * 0.2126) + (g * 0.7152) + (b * 0.0722),
            (r * 0.0193) + (g * 0.1192) + (b * 0.9505));
    }

    // 1-normalized CIE XYZ with D65 to L*a*b*
    // See https://en.wikipedia.org/wiki/CIELAB_color_space#From_CIEXYZ_to_CIELAB
    private static (double L, double A, double B) Xyz2lab((double X, double Y, double Z) xyz)
    {
        var x = xyz.X / 0.950489;
        var y = xyz.Y;
        var z = xyz.Z / 1.088840;

        var fx = x > SigmaPow3 ? Math.Pow(x, 1.0 / 3) : (x / 3 / SigmaPow2) + (4.0 / 29);
        var fy = y > SigmaPow3 ? Math.Pow(y, 1.0 / 3) : (y / 3 / SigmaPow2) + (4.0 / 29);
        var fz = z > SigmaPow3 ? Math.Pow(z, 1.0 / 3) : (z / 3 / SigmaPow2) + (4.0 / 29);

        var l = (116 * fy) - 16;
        var a = 500 * (fx - fy);
        var b = 200 * (fy - fz);

        return (l, a, b);
    }

    private static double Ssim(FastStats stats, int x1, int y1, int x2, int y2)
    {
        var mean1 = stats.MeanC1(x1, y1, x2, y2);
        var mean2 = stats.MeanC2(x1, y1, x2, y2);
        var var1 = stats.VarianceC1(x1, y1, x2, y2);
        var var2 = stats.VarianceC2(x1, y1, x2, y2);
        var cov = stats.Covariance(x1, y1, x2, y2);
        var c1 = 0.01 * DynamicRange * (0.01 * DynamicRange);
        var c2 = 0.03 * DynamicRange * (0.03 * DynamicRange);
        return ((2 * mean1 * mean2) + c1) * ((2 * cov) + c2) / ((mean1 * mean1) + (mean2 * mean2) + c1) / (var1 + var2 + c2);
    }

    /// <summary><c>ImageChannel</c> of <c>imageChannel.ts</c>: one channel of an image blended with white, with a padding.</summary>
    private sealed class ImageChannel(int width, int height, byte[] data)
    {
        public byte[] Data { get; } = data;

        public int Width { get; } = width;

        public int Height { get; } = height;

        public static (ImageChannel R, ImageChannel G, ImageChannel B) IntoRGB(int width, int height, byte[] data, int paddingSize, int[] paddingColorOdd, int[] paddingColorEven)
        {
            var newWidth = width + (2 * paddingSize);
            var newHeight = height + (2 * paddingSize);
            var r = new byte[newWidth * newHeight];
            var g = new byte[newWidth * newHeight];
            var b = new byte[newWidth * newHeight];
            for (var y = 0; y < newHeight; ++y)
            {
                for (var x = 0; x < newWidth; ++x)
                {
                    var index = (y * newWidth) + x;
                    if (y >= paddingSize && y < newHeight - paddingSize && x >= paddingSize && x < newWidth - paddingSize)
                    {
                        var offset = (((y - paddingSize) * width) + (x - paddingSize)) * 4;
                        var alpha = data[offset + 3] == 255 ? 1 : data[offset + 3] / 255.0;
                        r[index] = (byte)(int)BlendWithWhite(data[offset], alpha);
                        g[index] = (byte)(int)BlendWithWhite(data[offset + 1], alpha);
                        b[index] = (byte)(int)BlendWithWhite(data[offset + 2], alpha);
                    }
                    else
                    {
                        var color = (y + x) % 2 == 0 ? paddingColorEven : paddingColorOdd;
                        r[index] = (byte)color[0];
                        g[index] = (byte)color[1];
                        b[index] = (byte)color[2];
                    }
                }
            }
            return (new(newWidth, newHeight, r), new(newWidth, newHeight, g), new(newWidth, newHeight, b));
        }

        public int Get(int x, int y) => Data[(y * Width) + x];

        public (int X, int Y) BoundXY(int x, int y) => (Math.Min(Math.Max(x, 0), Width - 1), Math.Min(Math.Max(y, 0), Height - 1));
    }

    /// <summary><c>FastStats</c> of <c>stats.ts</c>: mean, population variance and covariance of a window from summed-area tables.</summary>
    private sealed class FastStats
    {
        private readonly int _width;
        private readonly double[] _partialSumC1;
        private readonly double[] _partialSumC2;
        private readonly double[] _partialSumMult;
        private readonly double[] _partialSumSq1;
        private readonly double[] _partialSumSq2;

        public FastStats(ImageChannel c1, ImageChannel c2)
        {
            var width = c1.Width;
            var height = c1.Height;
            _width = width;

            _partialSumC1 = new double[width * height];
            _partialSumC2 = new double[width * height];
            _partialSumSq1 = new double[width * height];
            _partialSumSq2 = new double[width * height];
            _partialSumMult = new double[width * height];

            void Recalc(double[] mx, int idx, double initial, int x, int y)
            {
                mx[idx] = initial;
                if (y > 0)
                {
                    mx[idx] += mx[((y - 1) * width) + x];
                }
                if (x > 0)
                {
                    mx[idx] += mx[(y * width) + x - 1];
                }
                if (x > 0 && y > 0)
                {
                    mx[idx] -= mx[((y - 1) * width) + x - 1];
                }
            }

            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    var idx = (y * width) + x;
                    Recalc(_partialSumC1, idx, c1.Data[idx], x, y);
                    Recalc(_partialSumC2, idx, c2.Data[idx], x, y);
                    Recalc(_partialSumSq1, idx, c1.Data[idx] * c1.Data[idx], x, y);
                    Recalc(_partialSumSq2, idx, c2.Data[idx] * c2.Data[idx], x, y);
                    Recalc(_partialSumMult, idx, c1.Data[idx] * c2.Data[idx], x, y);
                }
            }
        }

        public double MeanC1(int x1, int y1, int x2, int y2)
        {
            double n = (y2 - y1 + 1) * (x2 - x1 + 1);
            return Sum(_partialSumC1, x1, y1, x2, y2) / n;
        }

        public double MeanC2(int x1, int y1, int x2, int y2)
        {
            double n = (y2 - y1 + 1) * (x2 - x1 + 1);
            return Sum(_partialSumC2, x1, y1, x2, y2) / n;
        }

        public double VarianceC1(int x1, int y1, int x2, int y2)
        {
            double n = (y2 - y1 + 1) * (x2 - x1 + 1);
            var sum = Sum(_partialSumC1, x1, y1, x2, y2);
            return (Sum(_partialSumSq1, x1, y1, x2, y2) - (sum * sum / n)) / n;
        }

        public double VarianceC2(int x1, int y1, int x2, int y2)
        {
            double n = (y2 - y1 + 1) * (x2 - x1 + 1);
            var sum = Sum(_partialSumC2, x1, y1, x2, y2);
            return (Sum(_partialSumSq2, x1, y1, x2, y2) - (sum * sum / n)) / n;
        }

        public double Covariance(int x1, int y1, int x2, int y2)
        {
            double n = (y2 - y1 + 1) * (x2 - x1 + 1);
            return (Sum(_partialSumMult, x1, y1, x2, y2) - (Sum(_partialSumC1, x1, y1, x2, y2) * Sum(_partialSumC2, x1, y1, x2, y2) / n)) / n;
        }

        private double Sum(double[] partialSum, int x1, int y1, int x2, int y2)
        {
            var width = _width;
            var result = partialSum[(y2 * width) + x2];
            if (y1 > 0)
            {
                result -= partialSum[((y1 - 1) * width) + x2];
            }
            if (x1 > 0)
            {
                result -= partialSum[(y2 * width) + x1 - 1];
            }
            if (x1 > 0 && y1 > 0)
            {
                result += partialSum[((y1 - 1) * width) + x1 - 1];
            }
            return result;
        }
    }
}

/// <summary>
/// <c>diff.createPatch('file', expected, actual, undefined, undefined, { context })</c> without the four
/// lines of the file header: the hunks of a unified diff by lines, as the package 'diff' formats them.
/// The lines are aligned by a longest common subsequence, so among the alignments of the same length
/// the port can pick another one than the package does.
/// </summary>
internal static class TextDiff
{
    public static List<string> CreatePatch(string oldText, string newText, int context)
    {
        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);

        // The longest common subsequence of the lines: lcs[i, j] is its length for oldLines[i..] and newLines[j..].
        var lcs = new int[oldLines.Length + 1, newLines.Length + 1];
        for (var i = oldLines.Length - 1; i >= 0; i--)
        {
            for (var j = newLines.Length - 1; j >= 0; j--)
            {
                lcs[i, j] = oldLines[i] == newLines[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        // Runs of lines that are common (' '), removed ('-') or added ('+'), a removal before an addition.
        var changes = new List<(char Kind, List<string> Lines)>();
        var removed = new List<string>();
        var added = new List<string>();
        void FlushChange()
        {
            if (removed.Count > 0)
            {
                changes.Add(('-', removed));
                removed = [];
            }
            if (added.Count > 0)
            {
                changes.Add(('+', added));
                added = [];
            }
        }
        for (int i = 0, j = 0; i < oldLines.Length || j < newLines.Length;)
        {
            if (i < oldLines.Length && j < newLines.Length && oldLines[i] == newLines[j])
            {
                FlushChange();
                if (changes.Count == 0 || changes[^1].Kind != ' ')
                {
                    changes.Add((' ', []));
                }
                changes[^1].Lines.Add(oldLines[i]);
                i++;
                j++;
            }
            else if (i < oldLines.Length && (j == newLines.Length || lcs[i + 1, j] >= lcs[i, j + 1]))
            {
                removed.Add(oldLines[i++]);
            }
            else
            {
                added.Add(newLines[j++]);
            }
        }
        FlushChange();
        changes.Add((' ', []));

        var result = new List<string>();
        var oldRangeStart = 0;
        var newRangeStart = 0;
        var curRange = new List<string>();
        var oldLine = 1;
        var newLine = 1;
        for (var i = 0; i < changes.Count; i++)
        {
            var (kind, lines) = changes[i];
            if (kind != ' ')
            {
                if (oldRangeStart == 0)
                {
                    oldRangeStart = oldLine;
                    newRangeStart = newLine;
                    if (i > 0)
                    {
                        curRange = changes[i - 1].Lines.TakeLast(context).Select(line => " " + line).ToList();
                        oldRangeStart -= curRange.Count;
                        newRangeStart -= curRange.Count;
                    }
                }
                curRange.AddRange(lines.Select(line => kind + line));
                if (kind == '+')
                {
                    newLine += lines.Count;
                }
                else
                {
                    oldLine += lines.Count;
                }
                continue;
            }
            if (oldRangeStart != 0)
            {
                if (lines.Count <= context * 2 && i < changes.Count - 2)
                {
                    curRange.AddRange(lines.Select(line => " " + line));
                }
                else
                {
                    var contextSize = Math.Min(lines.Count, context);
                    curRange.AddRange(lines.Take(contextSize).Select(line => " " + line));
                    var oldCount = oldLine - oldRangeStart + contextSize;
                    var newCount = newLine - newRangeStart + contextSize;
                    var oldStart = oldCount == 0 ? oldRangeStart - 1 : oldRangeStart;
                    var newStart = newCount == 0 ? newRangeStart - 1 : newRangeStart;
                    result.Add(string.Create(CultureInfo.InvariantCulture, $"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@"));
                    result.AddRange(curRange);
                    oldRangeStart = 0;
                    newRangeStart = 0;
                    curRange = [];
                }
            }
            oldLine += lines.Count;
            newLine += lines.Count;
        }
        return result;
    }

    private static string[] SplitLines(string text)
    {
        var lines = text.Split('\n');
        return lines[^1].Length == 0 ? lines[..^1] : lines;
    }
}
