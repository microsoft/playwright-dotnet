using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Microsoft.Playwright.Tests
{
    internal class ScreenshotHelper
    {
        internal static bool PixelMatch(string screenShotFile, string fileName)
            => PixelMatch(screenShotFile, File.ReadAllBytes(fileName));

        internal static bool PixelMatch(string screenShotFile, byte[] screenshot)
        {
            const int pixelThreshold = 10;
            const decimal totalTolerance = 0.05m;

            var baseImage = Image.Load<Rgb24>(Path.Combine(TestUtils.FindParentDirectory("Screenshots"), TestConstants.Product, screenShotFile));
            var compareImage = Image.Load<Rgb24>(screenshot);

            //Just for debugging purpose
            compareImage.Save(Path.Combine(TestUtils.FindParentDirectory("Screenshots"), TestConstants.Product, "test.png"));

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
}
