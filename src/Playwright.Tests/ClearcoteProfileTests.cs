using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class ClearcoteProfileTests
{
    [Test]
    [NonParallelizable]
    public void SaveShouldAllowExplicitBareJsonFilename()
    {
        using var tmpDir = new TempDirectory();
        var previousCurrentDirectory = Environment.CurrentDirectory;
        try
        {
            Directory.SetCurrentDirectory(tmpDir.Path);

            var profile = new ClearcoteProfile("persona", new() { Timezone = "UTC" });
            var savedPath = profile.Save("persona.json");

            Assert.AreEqual(Path.Combine(tmpDir.Path, "persona.json"), savedPath);
            Assert.True(File.Exists(Path.Combine(tmpDir.Path, "persona.json")));
            var loaded = ClearcoteProfile.Load(Path.Combine(tmpDir.Path, "persona.json"));
            Assert.AreEqual("persona", loaded.Name);
            Assert.AreEqual("UTC", loaded.Options.Timezone);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCurrentDirectory);
        }
    }

    [Test]
    [NonParallelizable]
    public void ProfileDirectoryShouldRejectRelativeEnvironmentDirectory()
    {
        using var tmpDir = new TempDirectory();
        var previousCurrentDirectory = Environment.CurrentDirectory;
        var originalProfileDir = Environment.GetEnvironmentVariable("CLEARCOTE_PROFILE_DIR");
        try
        {
            Directory.SetCurrentDirectory(tmpDir.Path);
            Environment.SetEnvironmentVariable("CLEARCOTE_PROFILE_DIR", "profiles");

            var exception = Assert.Throws<PlaywrightException>(() => _ = Clearcote.ProfileDirectory);

            StringAssert.Contains("CLEARCOTE_PROFILE_DIR must be a fully-qualified path", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLEARCOTE_PROFILE_DIR", originalProfileDir);
            Directory.SetCurrentDirectory(previousCurrentDirectory);
        }
    }

    [Test]
    [NonParallelizable]
    public void ProfilePathShouldCanonicalizeExplicitBareJsonFilename()
    {
        using var tmpDir = new TempDirectory();
        var previousCurrentDirectory = Environment.CurrentDirectory;
        try
        {
            Directory.SetCurrentDirectory(tmpDir.Path);

            var profilePath = Clearcote.ProfilePath("persona.json");

            Assert.AreEqual(Path.Combine(tmpDir.Path, "persona.json"), profilePath);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCurrentDirectory);
        }
    }

    [Test]
    [NonParallelizable]
    public void SaveShouldRoundTripInheritedPersistentPersonaOptions()
    {
        using var tmpDir = new TempDirectory();
        var path = Path.Combine(tmpDir.Path, "persona.json");
        var profile = new ClearcoteProfile("persona", new()
        {
            AcceptDownloads = false,
            ArtifactsDir = "/tmp/artifacts",
            BaseURL = "https://example.test/app/",
            BypassCSP = true,
            ChromiumSandbox = true,
            ColorScheme = ColorScheme.Dark,
            Contrast = Contrast.More,
            DeviceScaleFactor = 2,
            DownloadsPath = "/tmp/downloads",
            Env = new Dictionary<string, string> { ["PROFILE_ENV"] = "1" },
            ExtraHTTPHeaders = new Dictionary<string, string> { ["x-profile"] = "yes" },
            FirefoxUserPrefs = new List<KeyValuePair<string, object>>
            {
                new("pref.bool", false),
                new("pref.int", 3),
                new("pref.string", "value"),
            },
            ForcedColors = ForcedColors.Active,
            Geolocation = new() { Latitude = 10.5f, Longitude = 20.25f, Accuracy = 4 },
            HandleSIGHUP = false,
            HandleSIGINT = false,
            HandleSIGTERM = false,
            HasTouch = true,
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = "https://example.test", Send = HttpCredentialsSend.Always },
            IgnoreAllDefaultArgs = true,
            IgnoreDefaultArgs = new[] { "--enable-automation" },
            IgnoreHTTPSErrors = true,
            IsMobile = true,
            JavaScriptEnabled = false,
            Locale = "en-US",
            Offline = true,
            Permissions = new[] { "geolocation", "notifications" },
            Proxy = new() { Server = "http://proxy.test:3128", Bypass = ".local", Username = "proxy-user", Password = "proxy-pass" },
            ReducedMotion = ReducedMotion.NoPreference,
            ScreenSize = new() { Width = 1440, Height = 900 },
            ServiceWorkers = ServiceWorkerPolicy.Block,
            SlowMo = 123,
            StrictSelectors = true,
            Timeout = 456,
            TimezoneId = "America/New_York",
            TracesDir = "/tmp/traces",
            UserAgent = "ClearcoteProfileTest/1.0",
            ViewportSize = new() { Width = 1280, Height = 720 },
        });

        profile.Save(path);

        var loaded = ClearcoteProfile.Load(path);
        Assert.AreEqual(false, loaded.Options.AcceptDownloads);
        Assert.AreEqual("/tmp/artifacts", loaded.Options.ArtifactsDir);
        Assert.AreEqual("https://example.test/app/", loaded.Options.BaseURL);
        Assert.AreEqual(true, loaded.Options.BypassCSP);
        Assert.AreEqual(true, loaded.Options.ChromiumSandbox);
        Assert.AreEqual(ColorScheme.Dark, loaded.Options.ColorScheme);
        Assert.AreEqual(Contrast.More, loaded.Options.Contrast);
        Assert.AreEqual(2, loaded.Options.DeviceScaleFactor);
        Assert.AreEqual("/tmp/downloads", loaded.Options.DownloadsPath);
        Assert.AreEqual("1", loaded.Options.Env!.Single(pair => pair.Key == "PROFILE_ENV").Value);
        Assert.AreEqual("yes", loaded.Options.ExtraHTTPHeaders!.Single(pair => pair.Key == "x-profile").Value);
        Assert.AreEqual(false, loaded.Options.FirefoxUserPrefs!.Single(pair => pair.Key == "pref.bool").Value);
        Assert.AreEqual(3, loaded.Options.FirefoxUserPrefs!.Single(pair => pair.Key == "pref.int").Value);
        Assert.AreEqual("value", loaded.Options.FirefoxUserPrefs!.Single(pair => pair.Key == "pref.string").Value);
        Assert.AreEqual(ForcedColors.Active, loaded.Options.ForcedColors);
        Assert.AreEqual(10.5f, loaded.Options.Geolocation!.Latitude);
        Assert.AreEqual(20.25f, loaded.Options.Geolocation.Longitude);
        Assert.AreEqual(4, loaded.Options.Geolocation.Accuracy);
        Assert.AreEqual(false, loaded.Options.HandleSIGHUP);
        Assert.AreEqual(false, loaded.Options.HandleSIGINT);
        Assert.AreEqual(false, loaded.Options.HandleSIGTERM);
        Assert.AreEqual(true, loaded.Options.HasTouch);
        Assert.AreEqual("user", loaded.Options.HttpCredentials!.Username);
        Assert.AreEqual("pass", loaded.Options.HttpCredentials.Password);
        Assert.AreEqual("https://example.test", loaded.Options.HttpCredentials.Origin);
        Assert.AreEqual(HttpCredentialsSend.Always, loaded.Options.HttpCredentials.Send);
        Assert.AreEqual(true, loaded.Options.IgnoreAllDefaultArgs);
        CollectionAssert.AreEqual(new[] { "--enable-automation" }, loaded.Options.IgnoreDefaultArgs);
        Assert.AreEqual(true, loaded.Options.IgnoreHTTPSErrors);
        Assert.AreEqual(true, loaded.Options.IsMobile);
        Assert.AreEqual(false, loaded.Options.JavaScriptEnabled);
        Assert.AreEqual("en-US", loaded.Options.Locale);
        Assert.AreEqual(true, loaded.Options.Offline);
        CollectionAssert.AreEqual(new[] { "geolocation", "notifications" }, loaded.Options.Permissions);
        Assert.AreEqual("http://proxy.test:3128", loaded.Options.Proxy!.Server);
        Assert.AreEqual(".local", loaded.Options.Proxy.Bypass);
        Assert.AreEqual("proxy-user", loaded.Options.Proxy.Username);
        Assert.AreEqual("proxy-pass", loaded.Options.Proxy.Password);
        Assert.AreEqual(ReducedMotion.NoPreference, loaded.Options.ReducedMotion);
        Assert.AreEqual(1440, loaded.Options.ScreenSize!.Width);
        Assert.AreEqual(900, loaded.Options.ScreenSize.Height);
        Assert.AreEqual(ServiceWorkerPolicy.Block, loaded.Options.ServiceWorkers);
        Assert.AreEqual(123, loaded.Options.SlowMo);
        Assert.AreEqual(true, loaded.Options.StrictSelectors);
        Assert.AreEqual(456, loaded.Options.Timeout);
        Assert.AreEqual("America/New_York", loaded.Options.TimezoneId);
        Assert.AreEqual("/tmp/traces", loaded.Options.TracesDir);
        Assert.AreEqual("ClearcoteProfileTest/1.0", loaded.Options.UserAgent);
        Assert.AreEqual(1280, loaded.Options.ViewportSize!.Width);
        Assert.AreEqual(720, loaded.Options.ViewportSize.Height);
    }

    [Test]
    public void MergeIntoShouldApplyInheritedPersistentPersonaOptions()
    {
        var target = new ClearcoteLaunchPersistentContextOptions();
        var source = new ClearcoteLaunchPersistentContextOptions
        {
            Locale = "en-US",
            UserAgent = "ClearcoteProfileTest/1.0",
            Proxy = new() { Server = "http://proxy.test:3128" },
            ViewportSize = new() { Width = 1280, Height = 720 },
            ColorScheme = ColorScheme.Dark,
        };

        Clearcote.MergeInto(target, source, overrideExisting: false);

        Assert.AreEqual("en-US", target.Locale);
        Assert.AreEqual("ClearcoteProfileTest/1.0", target.UserAgent);
        Assert.AreEqual("http://proxy.test:3128", target.Proxy!.Server);
        Assert.AreEqual(1280, target.ViewportSize!.Width);
        Assert.AreEqual(ColorScheme.Dark, target.ColorScheme);

        Clearcote.MergeInto(target, new() { Locale = "fr-FR" }, overrideExisting: false);
        Assert.AreEqual("en-US", target.Locale);

        Clearcote.MergeInto(target, new() { Locale = "fr-FR" }, overrideExisting: true);
        Assert.AreEqual("fr-FR", target.Locale);
    }

    [Test]
    public void MergeIntoShouldApplyLaunchBaseOptions()
    {
        var target = new ClearcoteLaunchOptions();
        var source = new ClearcoteLaunchOptions
        {
            Args = new[] { "--profile-arg" },
            ArtifactsDir = "/tmp/artifacts",
            Channel = "chrome",
            ChromiumSandbox = true,
            DownloadsPath = "/tmp/downloads",
            Env = new Dictionary<string, string> { ["PROFILE_ENV"] = "1" },
            ExecutablePath = "/tmp/chrome",
            FirefoxUserPrefs = new List<KeyValuePair<string, object>> { new("pref", false) },
            HandleSIGHUP = false,
            HandleSIGINT = false,
            HandleSIGTERM = false,
            Headless = false,
            IgnoreAllDefaultArgs = true,
            IgnoreDefaultArgs = new[] { "--enable-automation" },
            Proxy = new() { Server = "http://proxy.test:3128" },
            SlowMo = 123,
            Timeout = 456,
            TracesDir = "/tmp/traces",
        };

        Clearcote.MergeInto(target, source, overrideExisting: false);

        CollectionAssert.AreEqual(new[] { "--profile-arg" }, target.Args);
        Assert.AreEqual("/tmp/artifacts", target.ArtifactsDir);
        Assert.AreEqual("chrome", target.Channel);
        Assert.AreEqual(true, target.ChromiumSandbox);
        Assert.AreEqual("/tmp/downloads", target.DownloadsPath);
        Assert.AreEqual("1", target.Env!.Single(pair => pair.Key == "PROFILE_ENV").Value);
        Assert.AreEqual("/tmp/chrome", target.ExecutablePath);
        Assert.AreEqual(false, target.FirefoxUserPrefs!.Single(pair => pair.Key == "pref").Value);
        Assert.AreEqual(false, target.HandleSIGHUP);
        Assert.AreEqual(false, target.HandleSIGINT);
        Assert.AreEqual(false, target.HandleSIGTERM);
        Assert.AreEqual(false, target.Headless);
        Assert.AreEqual(true, target.IgnoreAllDefaultArgs);
        CollectionAssert.AreEqual(new[] { "--enable-automation" }, target.IgnoreDefaultArgs);
        Assert.AreEqual("http://proxy.test:3128", target.Proxy!.Server);
        Assert.AreEqual(123, target.SlowMo);
        Assert.AreEqual(456, target.Timeout);
        Assert.AreEqual("/tmp/traces", target.TracesDir);

        Clearcote.MergeInto(target, new() { Proxy = new() { Server = "http://override.test:3128" } }, overrideExisting: false);
        Assert.AreEqual("http://proxy.test:3128", target.Proxy.Server);

        Clearcote.MergeInto(target, new() { Proxy = new() { Server = "http://override.test:3128" } }, overrideExisting: true);
        Assert.AreEqual("http://override.test:3128", target.Proxy.Server);
    }

    [Test]
    public void ToLaunchOptionsShouldCarryLaunchCompatibleProfileOptions()
    {
        var source = new ClearcoteLaunchPersistentContextOptions
        {
            Args = new[] { "--profile-arg" },
            ArtifactsDir = "/tmp/artifacts",
            Channel = "chrome",
            ChromiumSandbox = true,
            DownloadsPath = "/tmp/downloads",
            Env = new Dictionary<string, string> { ["PROFILE_ENV"] = "1" },
            ExecutablePath = "/tmp/chrome",
            FirefoxUserPrefs = new List<KeyValuePair<string, object>> { new("pref", false) },
            HandleSIGHUP = false,
            HandleSIGINT = false,
            HandleSIGTERM = false,
            Headless = false,
            IgnoreAllDefaultArgs = true,
            IgnoreDefaultArgs = new[] { "--enable-automation" },
            Proxy = new() { Server = "http://proxy.test:3128" },
            SlowMo = 123,
            Timeout = 456,
            TracesDir = "/tmp/traces",
            Fingerprint = "seed",
        };

        var launch = Clearcote.ToLaunchOptions(source);

        CollectionAssert.AreEqual(new[] { "--profile-arg" }, launch.Args);
        Assert.AreEqual("/tmp/artifacts", launch.ArtifactsDir);
        Assert.AreEqual("chrome", launch.Channel);
        Assert.AreEqual(true, launch.ChromiumSandbox);
        Assert.AreEqual("/tmp/downloads", launch.DownloadsPath);
        Assert.AreEqual("1", launch.Env!.Single(pair => pair.Key == "PROFILE_ENV").Value);
        Assert.AreEqual("/tmp/chrome", launch.ExecutablePath);
        Assert.AreEqual(false, launch.FirefoxUserPrefs!.Single(pair => pair.Key == "pref").Value);
        Assert.AreEqual(false, launch.HandleSIGHUP);
        Assert.AreEqual(false, launch.HandleSIGINT);
        Assert.AreEqual(false, launch.HandleSIGTERM);
        Assert.AreEqual(false, launch.Headless);
        Assert.AreEqual(true, launch.IgnoreAllDefaultArgs);
        CollectionAssert.AreEqual(new[] { "--enable-automation" }, launch.IgnoreDefaultArgs);
        Assert.AreEqual("http://proxy.test:3128", launch.Proxy!.Server);
        Assert.AreEqual(123, launch.SlowMo);
        Assert.AreEqual(456, launch.Timeout);
        Assert.AreEqual("/tmp/traces", launch.TracesDir);
        Assert.AreEqual("seed", launch.Fingerprint);
    }
}
