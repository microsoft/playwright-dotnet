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

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Microsoft.Playwright.Tests;

internal class ScreenshotHelper
{
    internal static bool PixelMatch(string screenShotFile, string fileName)
        => PixelMatch(screenShotFile, File.ReadAllBytes(fileName));

    internal static bool PixelMatch(string screenShotFile, byte[] screenshot)
    {
        const int pixelThreshold = 10;
        const decimal totalTolerance = 0.05m;

        var baseImage = Image.Load<Rgb24>(Path.Combine(TestUtils.FindParentDirectory("Screenshots"), TestConstants.BrowserName, screenShotFile));
        var compareImage = Image.Load<Rgb24>(screenshot);

        //Just for debugging purpose
        compareImage.Save(Path.Combine(TestUtils.FindParentDirectory("Screenshots"), TestConstants.BrowserName, "test.png"));

        if (baseImage.Width != compareImage.Width || baseImage.Height != compareImage.Height)
        {
            return false;
        }

        int invalidPixelsCount = 0;

        for (int y = 0; y < baseImage.Height; y++)
        {
            for (int x = 0; x < baseImage.Width; x++)
            {
                var pixelA = baseImage[x, y];
                var pixelB = compareImage[x, y];


                if (Math.Abs(pixelA.R - pixelB.R) > pixelThreshold ||
                    Math.Abs(pixelA.G - pixelB.G) > pixelThreshold ||
                    Math.Abs(pixelA.B - pixelB.B) > pixelThreshold)
                {
                    invalidPixelsCount++;
                }
            }
        }

        return (invalidPixelsCount / (baseImage.Height * baseImage.Width)) < totalTolerance;
    }
}
