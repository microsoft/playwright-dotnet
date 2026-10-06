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

using System.Diagnostics;
using System.Text;

namespace Microsoft.Playwright.Tests;

[Platform("Win")]
public class StdIOTransportTests
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task ShouldPreserveFrameworkDriverInputEncoding(bool emitBom, bool detachConsole)
    {
        // Windows PowerShell runs on .NET Framework. Keep console changes in a child process.
        var assemblyPath = typeof(Playwright).Assembly.Location.Replace("'", "''");
        var reader = Convert.ToBase64String(Encoding.Unicode.GetBytes("$stream = [Console]::OpenStandardInput(); $bytes = New-Object byte[] 4; $read = 0; while ($read -lt 4) { $count = $stream.Read($bytes, $read, 4 - $read); if (!$count) { exit 1 }; $read += $count }; [BitConverter]::ToString($bytes)"));
        var script = $$"""
            $ErrorActionPreference = 'Stop'
            Add-Type 'using System.Runtime.InteropServices; public static class NativeConsole { [DllImport("kernel32.dll")] public static extern bool FreeConsole(); }'
            $encoding = New-Object System.Text.UTF8Encoding(${{emitBom}})
            [Console]::InputEncoding = $encoding
            [Console]::OutputEncoding = $encoding
            if (${{detachConsole}}) { [void][NativeConsole]::FreeConsole() }
            $originalInput = [Console]::InputEncoding.GetPreamble().Length
            $originalOutput = [Console]::OutputEncoding.GetPreamble().Length
            $assembly = [Reflection.Assembly]::LoadFrom('{{assemblyPath}}')
            $start = $assembly.GetType('Microsoft.Playwright.Transport.StdIOTransport').GetMethod('StartProcessWithUTF8IOEncoding', [Reflection.BindingFlags]'NonPublic,Static')
            $driver = New-Object Diagnostics.Process
            $driver.StartInfo.FileName = (Get-Process -Id $PID).Path
            $driver.StartInfo.Arguments = '-NoLogo -NoProfile -NonInteractive -EncodedCommand {{reader}}'
            $driver.StartInfo.UseShellExecute = $false
            $driver.StartInfo.CreateNoWindow = $true
            $driver.StartInfo.RedirectStandardInput = $true
            $driver.StartInfo.RedirectStandardOutput = $true
            $driver.StartInfo.RedirectStandardError = $true
            $started = $false
            try {
                $arguments = [Array]::CreateInstance([object], 1)
                $arguments.SetValue($driver, 0)
                [void]$start.Invoke($null, $arguments)
                $started = $true
                if (${{emitBom}} -and ${{detachConsole}}) { throw 'Expected a console encoding error' }
                $driver.StandardInput.BaseStream.Write([byte[]](0, 0, 0, 0), 0, 4)
                $driver.StandardInput.BaseStream.Flush()
                $driver.StandardInput.Close()
                if (!$driver.WaitForExit(5000)) { throw 'Driver input timed out' }
                if ($driver.StandardOutput.ReadToEnd().Trim() -ne '00-00-00-00') { throw 'Driver input contains a byte order mark' }
            } catch {
                if (!(${{emitBom}} -and ${{detachConsole}}) -or
                    $_.Exception.ToString() -notlike '*Set Console.InputEncoding to new UTF8Encoding(false)*') { throw }
            } finally {
                if ($started -and !$driver.HasExited) { $driver.Kill(); $driver.WaitForExit() }
                $driver.Dispose()
            }
            if ([Console]::InputEncoding.GetPreamble().Length -ne $originalInput -or
                [Console]::OutputEncoding.GetPreamble().Length -ne $originalOutput) { throw 'Console encoding changed' }
            'Driver startup verified'
            """;
        var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
        using var process = new Process
        {
            StartInfo = new(powershell, "-NoLogo -NoProfile -NonInteractive -EncodedCommand " +
                Convert.ToBase64String(Encoding.Unicode.GetBytes(script)))
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            },
        };
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            Assert.Fail("Windows PowerShell did not finish driver startup.");
        }
        Assert.AreEqual(0, process.ExitCode, await error);
        StringAssert.Contains("Driver startup verified", await output);
    }
}
