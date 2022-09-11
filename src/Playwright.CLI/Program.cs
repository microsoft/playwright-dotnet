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

namespace Microsoft.Playwright.CLI;

static class Program
{
    static int Main(string[] args)
    {
        var path = Directory.GetCurrentDirectory();
        if (args.Length > 1 && args[0] == "-p")
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), args[1]);
            var isFile = File.Exists(path);
            if (!isFile && !Directory.Exists(path))
            {
                return PrintError($"Couldn't find project using Playwright. Ensure a project or a solution exists in {path}, or provide another path using -p.");
            }

            if (isFile)
            {
                path = Path.Combine(path, "..");
            }

            var argsCloned = new string[args.Length - 2];
            Array.Copy(args, 2, argsCloned, 0, args.Length - 2);
            args = argsCloned;
        }

        // Locating project is important, otherwise we are at risk
        // of traversing entire fs from root.
        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
        {
            var solutions = Directory.GetFiles(path, "*.sln");
            var projects = Directory.GetFiles(path, "*.*proj");
            if (solutions.Length == 0 && projects.Length == 0)
            {
                return PrintError($"Couldn't find project using Playwright. Ensure a project or a solution exists in {path}, or provide another path using -p.");
            }
        }

        var file = Traverse(new(path));

        if (string.IsNullOrEmpty(file))
        {
            return PrintError(@"Please make sure Playwright is installed and built prior to using Playwright tool:
   dotnet add package Microsoft.Playwright
   dotnet build");
        }

        // Only Microsoft.Playwright.Program::Run knows how to run the driver.
        // Each version of Microsoft.Playwright has its own way and we must
        // delegate to it.
        var dll = Assembly.LoadFile(file);
        dynamic c = dll.CreateInstance("Microsoft.Playwright.Program");

        return c.Run(args);
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

    private static int PrintError(string error)
    {
        Console.Error.WriteLine("\x1b[91m" + error + "\x1b[0m");
        return 1;
    }
}
