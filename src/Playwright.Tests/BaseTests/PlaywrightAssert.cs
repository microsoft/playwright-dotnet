/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System.Text.Json;
using NUnit.Framework.Constraints;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Microsoft.Playwright.Tests;

internal static class PlaywrightAssert
{
    /// This functions replaces the <see cref="Assert.ThrowsAsync(IResolveConstraint, AsyncTestDelegate,string,object[])"/> because that
    /// particular function does not actually work correctly for Playwright Tests as it completely blocks the calling thread.
    /// For a more detailed read on the subject, see <see href="https://github.com/nunit/nunit/issues/464"/>.
    internal static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
            Assert.Fail($"Expected exception of type '{typeof(T).Name}' which was not thrown");
            return null;
        }
        catch (T t)
        {
            return t;
        }
    }

    internal static void AreJsonEqual(object expected, object actual)
    {
        Assert.AreEqual(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
    }

    internal static void ToMatchSnapshot(string expectedImageName, string actual)
        => ToMatchSnapshot(expectedImageName, File.ReadAllBytes(actual));

    internal static void ToMatchSnapshot(string expectedImageName, byte[] actual)
    {
        const int pixelThreshold = 10;
        const decimal totalTolerance = 0.05m;

        var goldenPathDir = Path.Combine(TestUtils.FindParentDirectory("Screenshots"), TestConstants.BrowserName);
        var expectedImagePath = Path.Combine(goldenPathDir, expectedImageName);
        if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
        {
            File.WriteAllBytes(expectedImagePath, actual);
        }

        var expectedImage = Image.Load<Rgb24>(expectedImagePath);
        var actualImage = Image.Load<Rgb24>(actual);

        if (expectedImage.Width != actualImage.Width || expectedImage.Height != actualImage.Height)
        {
            Assert.Fail("Expected image dimensions do not match actual image dimensions.\n" +
                $"Expected: {expectedImage.Width}x{expectedImage.Height}\n" +
                $"Actual: {actualImage.Width}x{actualImage.Height}");
            return;
        }

        int invalidPixelsCount = 0;
        for (int y = 0; y < expectedImage.Height; y++)
        {
            for (int x = 0; x < expectedImage.Width; x++)
            {
                var pixelA = expectedImage[x, y];
                var pixelB = actualImage[x, y];


                if (Math.Abs(pixelA.R - pixelB.R) > pixelThreshold ||
                    Math.Abs(pixelA.G - pixelB.G) > pixelThreshold ||
                    Math.Abs(pixelA.B - pixelB.B) > pixelThreshold)
                {
                    invalidPixelsCount++;
                }
            }
        }
        // cast invalidPixelsCount to decimal to avoid integer division
        if (((decimal)invalidPixelsCount / (expectedImage.Height * expectedImage.Width)) > totalTolerance)
        {
            Assert.Fail($"Expected image to match snapshot but it did not. {invalidPixelsCount} pixels do not match.\n" +
                $"Set the environment variable 'UPDATE_SNAPSHOTS' to '1' to update the snapshot.");
        }
    }
}
