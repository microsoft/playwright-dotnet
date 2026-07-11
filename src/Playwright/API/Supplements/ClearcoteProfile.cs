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

using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable SA1204
#pragma warning disable SA1611
#pragma warning disable SA1615

namespace Microsoft.Playwright;

/// <summary>
/// A saved Clearcote persona profile.
/// </summary>
public sealed class ClearcoteProfile
{
    public ClearcoteProfile(string name, ClearcoteLaunchPersistentContextOptions? options = null)
    {
        Name = name;
        Options = options == null ? new() : new(options);
    }

    /// <summary><para>Profile name or explicit JSON path used to resolve this profile.</para></summary>
    public string Name { get; }

    /// <summary><para>Saved launch options for this persona.</para></summary>
    public ClearcoteLaunchPersistentContextOptions Options { get; }

    /// <summary><para>Directory where named profiles are stored.</para></summary>
    public static string ProfileDirectory => Helpers.Clearcote.ProfileDirectory;

    /// <summary><para>Resolved path for this profile JSON.</para></summary>
    public string Path => Helpers.Clearcote.ProfilePath(Name);

    /// <summary><para>Merge more options into this profile.</para></summary>
    public ClearcoteProfile Set(ClearcoteLaunchPersistentContextOptions options)
    {
        Helpers.Clearcote.MergeInto(Options, options, overrideExisting: true);
        return this;
    }

    /// <summary><para>Persist this profile as JSON and return the written path.</para></summary>
    public string Save(string? path = null)
        => Helpers.Clearcote.SaveProfile(this, path);

    /// <summary><para>Load a saved profile by name or explicit path.</para></summary>
    public static ClearcoteProfile Load(string nameOrPath)
        => Helpers.Clearcote.LoadProfile(nameOrPath);

    /// <summary><para>List saved profile names under <see cref="ProfileDirectory"/>.</para></summary>
    public static IReadOnlyList<string> ListProfiles()
        => Helpers.Clearcote.ListProfiles();

    /// <summary><para>Launch this saved persona; explicit overrides win over saved options.</para></summary>
    public Task<IBrowser> LaunchAsync(IBrowserType chromium, ClearcoteLaunchOptions? overrides = null)
        => Helpers.Clearcote.LaunchProfileAsync(chromium, this, overrides);

    /// <summary><para>Launch this saved persona with a persistent profile directory.</para></summary>
    public Task<IBrowserContext> LaunchPersistentContextAsync(
        IBrowserType chromium,
        string userDataDir,
        ClearcoteLaunchPersistentContextOptions? overrides = null)
        => Helpers.Clearcote.LaunchPersistentProfileAsync(chromium, userDataDir, this, overrides);
}
