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

#pragma warning disable SA1611
#pragma warning disable SA1615

namespace Microsoft.Playwright;

/// <summary>
/// Helpers for resolving the verified Clearcote browser binary.
/// </summary>
public static class ClearcoteBrowser
{
    /// <summary>
    /// Resolve the Clearcote browser executable path, honoring an explicit path and <c>CLEARCOTE_BINARY</c>.
    /// </summary>
    /// <param name="options">Download and path resolution options.</param>
    /// <returns>The resolved Clearcote browser executable path.</returns>
    public static Task<string> ExecutablePathAsync(ClearcoteDownloadOptions? options = default)
        => Helpers.Clearcote.ExecutablePathAsync(
            options?.ExecutablePath,
            options?.CacheDir,
            options?.Quiet == true,
            Helpers.Clearcote.AutoUpdateRequested(options?.AutoUpdate));

    /// <summary>
    /// Download, verify, extract, and return the Clearcote browser executable path.
    /// </summary>
    /// <param name="options">Download options.</param>
    /// <returns>The verified Clearcote browser executable path.</returns>
    public static Task<string> DownloadAsync(ClearcoteDownloadOptions? options = default)
        => Helpers.Clearcote.DownloadAsync(
            options?.Dest,
            options?.CacheDir,
            options?.Quiet == true,
            Helpers.Clearcote.AutoUpdateRequested(options?.AutoUpdate));

    /// <summary>
    /// Launch Clearcote through a Playwright Chromium browser type.
    /// </summary>
    public static Task<IBrowser> LaunchAsync(IBrowserType chromium, ClearcoteLaunchOptions? options = default)
        => chromium.LaunchAsync(options ?? new ClearcoteLaunchOptions());

    /// <summary>
    /// Launch Clearcote through a Playwright instance.
    /// </summary>
    public static Task<IBrowser> LaunchAsync(IPlaywright playwright, ClearcoteLaunchOptions? options = default)
    {
        if (playwright == null)
        {
            throw new ArgumentNullException(nameof(playwright));
        }

        return LaunchAsync(playwright.Chromium, options);
    }

    /// <summary>
    /// Launch Clearcote with a persistent context through a Playwright Chromium browser type.
    /// </summary>
    public static Task<IBrowserContext> LaunchPersistentContextAsync(
        IBrowserType chromium,
        string userDataDir,
        ClearcoteLaunchPersistentContextOptions? options = default)
        => chromium.LaunchPersistentContextAsync(userDataDir, options ?? new ClearcoteLaunchPersistentContextOptions());

    /// <summary>
    /// Launch Clearcote with a persistent context through a Playwright instance.
    /// </summary>
    public static Task<IBrowserContext> LaunchPersistentContextAsync(
        IPlaywright playwright,
        string userDataDir,
        ClearcoteLaunchPersistentContextOptions? options = default)
    {
        if (playwright == null)
        {
            throw new ArgumentNullException(nameof(playwright));
        }

        return LaunchPersistentContextAsync(playwright.Chromium, userDataDir, options);
    }

    /// <summary>
    /// Launch a persistent Clearcote context prepared for the in-browser AI agent.
    /// </summary>
    public static Task<IBrowserContext> LaunchAgentAsync(
        IBrowserType chromium,
        ClearcoteLaunchAgentOptions? options = default)
        => Helpers.Clearcote.LaunchAgentAsync(chromium, options);

    /// <summary>
    /// Launch a persistent Clearcote context prepared for the in-browser AI agent.
    /// </summary>
    public static Task<IBrowserContext> LaunchAgentAsync(
        IPlaywright playwright,
        ClearcoteLaunchAgentOptions? options = default)
    {
        if (playwright == null)
        {
            throw new ArgumentNullException(nameof(playwright));
        }

        return LaunchAgentAsync(playwright.Chromium, options);
    }

    /// <summary>
    /// Run an in-browser Clearcote AI agent task against a page.
    /// </summary>
    public static Task<ClearcoteAgentTaskResult> RunAgentTaskAsync(
        IPage page,
        string goal,
        ClearcoteAgentTaskOptions? options = default)
        => Helpers.Clearcote.RunAgentTaskAsync(page, goal, options);

    /// <summary>
    /// Probe a page for WebGL render-backend coherence.
    /// </summary>
    public static Task<ClearcoteRenderVerdict> CheckRenderCoherenceAsync(IPage page, string? claimedGpu = default)
        => Helpers.Clearcote.CheckRenderCoherenceAsync(page, claimedGpu);

    /// <summary>
    /// Launch Clearcote with a raw CDP endpoint — the drop-in-for-the-whole-ecosystem mode.
    /// Unlike <see cref="ClearcoteBrowser.LaunchAsync(IPlaywright, ClearcoteLaunchOptions)"/>
    /// (which spawns and owns a Playwright browser), <c>ServeAsync</c> leaves a standing
    /// browser any CDP client can attach to. The binary is launched directly (not through
    /// Playwright), so <c>--enable-automation</c> is never present and
    /// <c>navigator.webdriver</c> stays <c>false</c>.
    /// </summary>
    public static Task<ClearcoteServer> ServeAsync(ClearcoteServeOptions? options = default)
        => Helpers.Clearcote.ServeAsync(options ?? new ClearcoteServeOptions());

    /// <summary>
    /// Download and verify the opt-in Widevine CDM.
    /// </summary>
    public static Task<string> FetchWidevineAsync(ClearcoteWidevineOptions? options = default)
        => Helpers.Clearcote.FetchWidevineAsync(options);

    /// <summary>
    /// Seed the opt-in Widevine CDM into a persistent profile directory.
    /// </summary>
    public static Task<string> SeedWidevineAsync(string userDataDir, ClearcoteWidevineOptions? options = default)
        => Helpers.Clearcote.SeedWidevineAsync(userDataDir, options);
}
