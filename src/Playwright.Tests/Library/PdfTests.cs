/**
 * Copyright 2017 Google Inc. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/pdf.spec.ts.</summary>
public class PdfTests
{
    [UpstreamTest("PD-001 should be able to save file")]
    public async Task ShouldBeAbleToSaveFile(ContextFactory contextFactory, string browserName, TestInfo testInfo)
    {
        It.Skip(browserName != "chromium", "Printing to pdf is currently only supported in chromium.");
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        var outputFile = testInfo.OutputPath("output.pdf");
        await page.PdfAsync(new() { Path = outputFile });
        Assert.That(File.ReadAllBytes(outputFile).Length, Is.GreaterThan(0));
    }

    [UpstreamTest("PD-002 should be able to generate outline")]
    public async Task ShouldBeAbleToGenerateOutline(ContextFactory contextFactory, Server server, string browserName, TestInfo testInfo)
    {
        It.Skip(browserName != "chromium", "Printing to pdf is currently only supported in chromium.");
        var context = await contextFactory(new()
        {
            BaseURL = server.Prefix,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("/headings.html");
        var outputFileNoOutline = testInfo.OutputPath("outputNoOutline.pdf");
        var outputFileOutline = testInfo.OutputPath("outputOutline.pdf");
        await page.PdfAsync(new() { Path = outputFileNoOutline });
        await page.PdfAsync(new() { Path = outputFileOutline, Tagged = true, Outline = true });
        Assert.That(File.ReadAllBytes(outputFileOutline).Length, Is.GreaterThan(File.ReadAllBytes(outputFileNoOutline).Length));
    }
}
