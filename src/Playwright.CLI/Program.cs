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

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Playwright.CLI
{
    static class Program
    {
        static int Main(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            if (args.Length > 1 && args[0] == "-p")
            {
                root = Path.Combine(Directory.GetCurrentDirectory(), args[1]);
                args = args[2..];
            }

            var file = Traverse(new DirectoryInfo(root));

            if (string.IsNullOrEmpty(file))
            {
                Console.WriteLine("Please make sure Playwright is installed and built:");
                Console.WriteLine("   dotnet add package Microsoft.Playwright");
                Console.WriteLine("   dotnet build");
                return 1;
            }

            var dll = Assembly.LoadFile(file);
            foreach (Type type in dll.GetExportedTypes())
            {
                if (type.FullName == "Microsoft.Playwright.Program")
                {
                    dynamic c = Activator.CreateInstance(type);
                    return c.Run(args);
                }
            }

            return 0;
        }

        private static string Traverse(DirectoryInfo root)
        {
            foreach (var dir in root.EnumerateDirectories())
            {
                var candidate = Path.Combine(dir.ToString(), "Microsoft.Playwright.dll");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                string result = Traverse(dir);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
            return null;
        }
    }
}
