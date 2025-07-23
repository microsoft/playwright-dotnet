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
using System.Threading.Tasks;

namespace Microsoft.Playwright.Tests;

/// <summary>
/// Test to reproduce and verify fix for the concurrency issue in Selectors
/// where SetTestIdAttribute could throw InvalidOperationException when
/// collection is modified during enumeration.
/// </summary>
public class SelectorsConcurrencyTests : PlaywrightTestEx
{
    [PlaywrightTest("selectors-concurrency.spec.ts", "should not throw when setting test id attribute concurrently")]
    public async Task ShouldNotThrowWhenSettingTestIdAttributeConcurrently()
    {
        // Reproduce the race condition by running operations concurrently
        var tasks = new Task[50]; // Increase the number to make race condition more likely
        
        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    // Just call SetTestIdAttribute repeatedly to trigger the race condition
                    // This should enumerate the _contextsForSelectors list
                    Playwright.Selectors.SetTestIdAttribute($"data-test-{index}");
                    
                    // Add a small delay to increase chance of race condition
                    await Task.Delay(1);
                    
                    Playwright.Selectors.SetTestIdAttribute($"data-test-{index}-2");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Collection was modified"))
                {
                    // This is the bug we're trying to fix
                    throw new Exception($"Race condition detected in task {index}: {ex.Message}", ex);
                }
            });
        }
        
        // Wait for all tasks to complete
        // If the bug exists, one or more tasks should throw InvalidOperationException
        await Task.WhenAll(tasks);
    }
}