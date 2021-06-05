using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    [SuppressMessage("Microsoft.Design", "CA1724", Justification = "Playwright is the entrypoint for all languages.")]
    public static class Playwright
    {
        private static Connection _connection;
        private static Process _playwrightServerProcess;

        /// <summary>
        /// Launches Playwright.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the playwright driver is ready to be used.</returns>
        public static async Task<IPlaywright> CreateAsync()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter((f, _) => f == "PlaywrightSharp.Playwright");
            });
            _playwrightServerProcess = GetProcess();
            _playwrightServerProcess.StartInfo.Arguments = "run-driver";
            _playwrightServerProcess.Start();
            _playwrightServerProcess.Exited += (_, _) => Close("Process exited");

            var transport = new StdIOTransport(_playwrightServerProcess, loggerFactory);

            _connection = new Connection(transport, loggerFactory);
            var playwright = await _connection.WaitForObjectWithKnownNameAsync<PlaywrightImpl>("Playwright").ConfigureAwait(false);
            playwright.Connection = _connection;
            return playwright;
        }

        internal static async Task InstallAsync(string driverPath = null, string browsersPath = null)
        {
            if (!string.IsNullOrEmpty(browsersPath))
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.BrowsersPathEnvironmentVariable, Path.GetFullPath(browsersPath));
            }

            var tcs = new TaskCompletionSource<bool>();
            using var process = GetProcess(driverPath);
            process.StartInfo.Arguments = "install";
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardError = false;
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => tcs.TrySetResult(true);
            process.Start();

            await tcs.Task.ConfigureAwait(false);
        }

        private static Process GetProcess(string driverExecutablePath = null)
            => new()
            {
                StartInfo =
                {
                    FileName = string.IsNullOrEmpty(driverExecutablePath) ? GetExecutablePath() : driverExecutablePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

        private static string GetExecutablePath()
        {
            string driversPath;

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariables.DriverPathEnvironmentVariable)))
            {
                driversPath = Environment.GetEnvironmentVariable(EnvironmentVariables.DriverPathEnvironmentVariable);
            }
            else
            {
                var assembly = typeof(Playwright).Assembly;
                driversPath = new FileInfo(assembly.Location).Directory.FullName;
            }

            string executableFile = GetPath(driversPath);
            if (File.Exists(executableFile))
            {
                return executableFile;
            }

            string fallbackBinPath = Path.Combine(
                driversPath,
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "playwright.sh" : "playwright.cmd");

            if (File.Exists(fallbackBinPath))
            {
                return fallbackBinPath;
            }

            throw new PlaywrightException($@"Driver not found in any of the locations. Tried:
 * {executableFile}
 * {fallbackBinPath}");
        }

        private static string GetPath(string driversPath)
        {
            string platformId;
            string runnerName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformId = RuntimeInformation.OSArchitecture == Architecture.X64 ? "win-x64" : "win-x86";
                runnerName = "playwright.cmd";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                runnerName = "playwright.sh";
                platformId = "osx";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                runnerName = "playwright.sh";
                platformId = "unix";
            }
            else
            {
                throw new PlaywrightException("Unknown platform");
            }

            return Path.Combine(driversPath, ".playwright", platformId, "native", runnerName);
        }

        private static void Close(string message)
        {
            _connection.Close(message);
            try
            {
                _playwrightServerProcess?.Kill();
                _playwrightServerProcess?.Dispose();
            }
            catch
            {
            }
        }
    }
}
