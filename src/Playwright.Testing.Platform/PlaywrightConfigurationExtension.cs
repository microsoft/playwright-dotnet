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
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.Playwright.TestAdapter;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;

namespace Microsoft.Playwright.Testing.Platform;

internal sealed class PlaywrightConfigurationExtension(IConfiguration configuration) : ITestSessionLifetimeHandler
{
    private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();
    private string? _originalAdapterSettings;
    private bool _settingsForwarded;

    public string Uid => "Microsoft.Playwright.Testing.Platform";
    public string Version => typeof(PlaywrightConfigurationExtension).Assembly.GetName().Version!.ToString();
    public string DisplayName => "Playwright configuration";
    public string Description => "Configures Playwright test fixtures using the playwright section of testconfig.json.";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task OnTestSessionStartingAsync(ITestSessionContext testSessionContext)
    {
        testSessionContext.CancellationToken.ThrowIfCancellationRequested();
        var json = configuration["playwright"];
        var settings = json == null ? new PlaywrightConfiguration() :
            JsonSerializer.Deserialize<PlaywrightConfiguration>(json, _serializerOptions) ??
                throw new ArgumentException("The 'playwright' configuration section must be an object.");

        if (settings.ExpectTimeout is float timeout && (timeout < 0 || float.IsNaN(timeout) || float.IsInfinity(timeout)))
        {
            throw new ArgumentException("'playwright:expectTimeout' must be a finite, non-negative number.");
        }
        PlaywrightSettingsProvider.SetSettings(new PlaywrightSettingsXml
        {
            BrowserName = settings.BrowserName,
            ExpectTimeout = settings.ExpectTimeout,
            TestIdAttribute = settings.TestIdAttribute,
            LaunchOptions = settings.LaunchOptions,
            ContextOptions = settings.ContextOptions,
        });

        // Validate the effective browser before any test fixture starts its driver.
        _ = PlaywrightSettingsProvider.BrowserName;
        // NUnit runs fixtures in a separate load context, so forward the same settings there.
        _originalAdapterSettings = PlaywrightSettingsProvider.ForwardSettings();
        _settingsForwarded = true;
        return Task.CompletedTask;
    }

    public Task OnTestSessionFinishingAsync(ITestSessionContext testSessionContext)
    {
        if (_settingsForwarded)
        {
            PlaywrightSettingsProvider.RestoreForwardedSettings(_originalAdapterSettings);
            _settingsForwarded = false;
        }
        return Task.CompletedTask;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            foreach (var property in typeInfo.Properties)
            {
                if (property.AttributeProvider is PropertyInfo member)
                {
                    // Configuration follows .NET option names, not driver protocol aliases such as "viewport".
                    property.Name = JsonNamingPolicy.CamelCase.ConvertName(member.Name);
                }
            }
        });
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            TypeInfoResolver = resolver,
            Converters = { new JsonStringEnumMemberConverter(), new DictionaryConverter() },
        };
    }
}

internal sealed class PlaywrightConfiguration
{
    public string? BrowserName { get; set; }
    public float? ExpectTimeout { get; set; }
    public string? TestIdAttribute { get; set; }
    public BrowserTypeLaunchOptions? LaunchOptions { get; set; }
    public BrowserNewContextOptions? ContextOptions { get; set; }
}
