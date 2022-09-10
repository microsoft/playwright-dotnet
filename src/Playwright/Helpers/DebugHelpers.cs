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
using System.Diagnostics;

namespace Microsoft.Playwright.Helpers
{
    internal static class DebugHelpers
    {
        internal static string StackTrace(string name = null)
        {
            var output = $"Microsoft.Playwright.Helpers.DebugHelpers::StackTrace({name})\n";
            var prefix = string.IsNullOrEmpty(name) ? string.Empty : $"{name} ";
            var stackTrace = new StackTrace(true);
            for (int i = 0; i < stackTrace.FrameCount; ++i)
            {
                var stackFrame = stackTrace.GetFrame(i);
                var fileName = stackFrame.GetFileName();
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }
                var methodName = stackFrame.GetMethod().Name;
                output += $"{prefix}{methodName} {fileName}:{stackFrame.GetFileLineNumber()}\n";
            }
            output += new string('-', 80) + "\n";
            return output;
        }

        [Conditional("DEBUG")]
        internal static void PrintStack(string name = null)
        {
            var output = StackTrace(name);
            Console.Error.WriteLine(output);
        }
    }
}
