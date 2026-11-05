using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.TestAdapter;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;

namespace Microsoft.Playwright.Testing.Platform;

internal sealed class PlaywrightConfigurationExtension(IConfiguration configuration) : ITestSessionLifetimeHandler
{
    private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();
    private string? _originalPwDebug;
    private bool _changedPwDebug;

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
        if (settings.PwDebug != null && settings.PwDebug != "0" && settings.PwDebug != "1" && settings.PwDebug != "console")
        {
            throw new ArgumentException("'playwright:pwDebug' must be '0', '1', or 'console'.");
        }

        PlaywrightSettingsProvider.SetSettings(new PlaywrightSettingsXml
        {
            BrowserName = settings.BrowserName,
            ExpectTimeout = settings.ExpectTimeout,
            TestIdAttribute = settings.TestIdAttribute,
            LaunchOptions = settings.LaunchOptions,
        }, settings.ContextOptions);

        // Validate the effective browser before any test fixture starts its driver.
        _ = PlaywrightSettingsProvider.BrowserName;
        _originalPwDebug = Environment.GetEnvironmentVariable("PWDEBUG");
        if (_originalPwDebug == null && settings.PwDebug != null)
        {
            Environment.SetEnvironmentVariable("PWDEBUG", settings.PwDebug);
            _changedPwDebug = true;
        }
        return Task.CompletedTask;
    }

    public Task OnTestSessionFinishingAsync(ITestSessionContext testSessionContext)
    {
        if (_changedPwDebug)
        {
            Environment.SetEnvironmentVariable("PWDEBUG", _originalPwDebug);
            _changedPwDebug = false;
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
        return new JsonSerializerOptions(JsonExtensions.DefaultJsonSerializerOptions)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            TypeInfoResolver = resolver,
            Converters = { new DictionaryConverter() },
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
    public string? PwDebug { get; set; }
}
