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

namespace Microsoft.Playwright;

/// <summary>
/// Chromium launch options for the Clearcote browser fork.
/// </summary>
public class ClearcoteLaunchOptions : BrowserTypeLaunchOptions
{
    public ClearcoteLaunchOptions()
    {
    }

    public ClearcoteLaunchOptions(BrowserTypeLaunchOptions clone) : base(clone)
    {
        if (clone is ClearcoteLaunchOptions clearcote)
        {
            Fingerprint = clearcote.Fingerprint;
            ClearcotePlatform = clearcote.ClearcotePlatform;
            PlatformVersion = clearcote.PlatformVersion;
            Brand = clearcote.Brand;
            BrandVersion = clearcote.BrandVersion;
            GpuVendor = clearcote.GpuVendor;
            GpuRenderer = clearcote.GpuRenderer;
            HardwareConcurrency = clearcote.HardwareConcurrency;
            Location = clearcote.Location;
            Timezone = clearcote.Timezone;
            AcceptLanguage = clearcote.AcceptLanguage;
            WebrtcIp = clearcote.WebrtcIp;
            TlsProfile = clearcote.TlsProfile;
            DisableGpuFingerprint = clearcote.DisableGpuFingerprint;
            FingerprintNoise = clearcote.FingerprintNoise;
            FingerprintProfile = clearcote.FingerprintProfile;
            StorageQuota = clearcote.StorageQuota;
            CanvasBridge = clearcote.CanvasBridge;
            DisablePrivacySandbox = clearcote.DisablePrivacySandbox;
            Extensions = clearcote.Extensions;
            Geoip = clearcote.Geoip;
            CacheDir = clearcote.CacheDir;
            Quiet = clearcote.Quiet;
            AutoUpdate = clearcote.AutoUpdate;
            Profile = clearcote.Profile;
            Humanize = clearcote.Humanize;
            ShowCursor = clearcote.ShowCursor;
            AgentLlmUrl = clearcote.AgentLlmUrl;
            AgentLlmKey = clearcote.AgentLlmKey;
            AgentModel = clearcote.AgentModel;
            AgentToolMode = clearcote.AgentToolMode;
            AgentTyping = clearcote.AgentTyping;
        }
    }

    /// <summary><para>Master fingerprint seed. Same seed produces the same identity.</para></summary>
    public string? Fingerprint { get; set; }

    /// <summary><para>Persona platform: <c>windows</c>, <c>linux</c>, or <c>macos</c>.</para></summary>
    public string? ClearcotePlatform { get; set; }

    /// <summary><para>UA-CH platform version.</para></summary>
    public string? PlatformVersion { get; set; }

    /// <summary><para>Browser brand for UA and UA-CH. Defaults to Chrome.</para></summary>
    public string? Brand { get; set; }

    /// <summary><para>Browser brand version.</para></summary>
    public string? BrandVersion { get; set; }

    /// <summary><para>WebGL unmasked vendor string.</para></summary>
    public string? GpuVendor { get; set; }

    /// <summary><para>WebGL unmasked renderer string.</para></summary>
    public string? GpuRenderer { get; set; }

    /// <summary><para>Value returned by <c>navigator.hardwareConcurrency</c>.</para></summary>
    public int? HardwareConcurrency { get; set; }

    /// <summary><para>Geolocation value as <c>lat,lng</c>.</para></summary>
    public string? Location { get; set; }

    /// <summary><para>IANA timezone, for example <c>America/New_York</c>.</para></summary>
    public string? Timezone { get; set; }

    /// <summary><para>Accept-Language list, for example <c>en-US,en</c>.</para></summary>
    public string? AcceptLanguage { get; set; }

    /// <summary><para>WebRTC egress IP to report.</para></summary>
    public string? WebrtcIp { get; set; }

    /// <summary><para>
    /// TLS network persona — keep the TLS ClientHello coherent with the persona's claimed Chrome
    /// version, so the network layer follows the UA. "match-persona" (default) follows brandVersion;
    /// "native"/"off" leaves native TLS; "chrome-&lt;major&gt;" or a number pins the TLS shape.
    /// </para></summary>
    public string? TlsProfile { get; set; }

    /// <summary><para>Use the real host GPU instead of a spoofed GPU.</para></summary>
    public bool? DisableGpuFingerprint { get; set; }

    /// <summary><para>Set <c>false</c> to turn off per-site fingerprint noise.</para></summary>
    public bool? FingerprintNoise { get; set; }

    /// <summary><para>Path to, or raw JSON string for, a captured Clearcote fingerprint profile.</para></summary>
    public string? FingerprintProfile { get; set; }

    /// <summary><para>Storage quota in megabytes reported by <c>navigator.storage.estimate()</c>.</para></summary>
    public long? StorageQuota { get; set; }

    /// <summary><para>Canvas bridge configuration.</para></summary>
    public ClearcoteCanvasBridgeOptions? CanvasBridge { get; set; }

    /// <summary><para>Set <c>false</c> to keep Privacy Sandbox runtime features enabled.</para></summary>
    public bool? DisablePrivacySandbox { get; set; }

    /// <summary><para>Unpacked extension directory paths to load.</para></summary>
    public IEnumerable<string>? Extensions { get; set; }

    /// <summary><para>Resolve proxy egress geo and auto-fill unset timezone, accept language, location, and WebRTC IP.</para></summary>
    public bool? Geoip { get; set; }

    /// <summary><para>Override the Clearcote browser cache directory.</para></summary>
    public string? CacheDir { get; set; }

    /// <summary><para>Suppress Clearcote download progress messages.</para></summary>
    public bool? Quiet { get; set; }

    /// <summary><para>Resolve the latest compatible Clearcote GitHub release instead of the pinned release.</para></summary>
    public bool? AutoUpdate { get; set; }

    /// <summary><para>Saved Clearcote profile name or explicit JSON path. Explicit option values override profile values.</para></summary>
    public string? Profile { get; set; }

    /// <summary><para>Route Playwright input through humanized native mouse and keyboard timing.</para></summary>
    public bool? Humanize { get; set; }

    /// <summary><para>Inject a red cursor dot that follows real mousemove events.</para></summary>
    public bool? ShowCursor { get; set; }

    /// <summary><para>OpenAI-compatible chat-completions base URL for the in-browser AI agent.</para></summary>
    public string? AgentLlmUrl { get; set; }

    /// <summary><para>API key for the in-browser AI agent LLM endpoint.</para></summary>
    public string? AgentLlmKey { get; set; }

    /// <summary><para>Model slug for the in-browser AI agent.</para></summary>
    public string? AgentModel { get; set; }

    /// <summary><para>Agent tool mode, for example <c>tools</c> or <c>json</c>.</para></summary>
    public string? AgentToolMode { get; set; }

    /// <summary><para>Agent typing cadence: <c>human</c>, <c>fast</c>, or <c>instant</c>.</para></summary>
    public string? AgentTyping { get; set; }
}
