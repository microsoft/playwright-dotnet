using System.Text.Json;
using System.Xml;
using Microsoft.Playwright.TestAdapter;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class PlaywrightSettingsXmlTests
{
    [Test]
    public void ShouldParseRunSettings()
    {
        var settings = Parse("""
            <Playwright>
              <BrowserName>firefox</BrowserName>
              <Headless>false</Headless>
              <ExpectTimeout>1234.5</ExpectTimeout>
              <Retries>2</Retries>
              <LaunchOptions>
                <Args>['--one','--two=2']</Args>
                <ArtifactsDir>artifacts</ArtifactsDir>
                <Channel>chrome</Channel>
                <ChromiumSandbox>true</ChromiumSandbox>
                <DownloadsPath>downloads</DownloadsPath>
                <Env>{'KEY':'VALUE'}</Env>
                <ExecutablePath>/tmp/browser</ExecutablePath>
                <FirefoxUserPrefs>{'dom.webdriver.enabled':false,'layout.css.devPixelsPerPx':1.25}</FirefoxUserPrefs>
                <HandleSIGHUP>false</HandleSIGHUP>
                <HandleSIGINT>false</HandleSIGINT>
                <HandleSIGTERM>false</HandleSIGTERM>
                <Headless>true</Headless>
                <IgnoreAllDefaultArgs>true</IgnoreAllDefaultArgs>
                <IgnoreDefaultArgs>['--mute-audio']</IgnoreDefaultArgs>
                <Proxy>
                  <Server>http://proxy.test:3128</Server>
                  <Bypass>.example.com</Bypass>
                  <Username>user</Username>
                  <Password>pass</Password>
                </Proxy>
                <SlowMo>10.5</SlowMo>
                <Timeout>30000</Timeout>
                <TracesDir>traces</TracesDir>
              </LaunchOptions>
            </Playwright>
            """);

        Assert.AreEqual("firefox", settings.BrowserName);
        Assert.False(settings.Headless);
        Assert.AreEqual(1234.5f, settings.ExpectTimeout);
        Assert.AreEqual(2, settings.Retries);

        var options = settings.LaunchOptions!;
        CollectionAssert.AreEqual(new[] { "--one", "--two=2" }, options.Args!.ToArray());
        Assert.AreEqual("artifacts", options.ArtifactsDir);
        Assert.AreEqual("chrome", options.Channel);
        Assert.True(options.ChromiumSandbox);
        Assert.AreEqual("downloads", options.DownloadsPath);
        Assert.AreEqual("/tmp/browser", options.ExecutablePath);
        Assert.False(options.HandleSIGHUP);
        Assert.False(options.HandleSIGINT);
        Assert.False(options.HandleSIGTERM);
        Assert.True(options.Headless);
        Assert.True(options.IgnoreAllDefaultArgs);
        CollectionAssert.AreEqual(new[] { "--mute-audio" }, options.IgnoreDefaultArgs!.ToArray());
        Assert.AreEqual(10.5f, options.SlowMo);
        Assert.AreEqual(30000, options.Timeout);
        Assert.AreEqual("traces", options.TracesDir);

        var env = options.Env!.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.AreEqual("VALUE", env["KEY"]);

        var prefs = options.FirefoxUserPrefs!.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.False(((JsonElement)prefs["dom.webdriver.enabled"]).GetBoolean());
        Assert.AreEqual(1.25, ((JsonElement)prefs["layout.css.devPixelsPerPx"]).GetDouble());

        Assert.AreEqual("http://proxy.test:3128", options.Proxy!.Server);
        Assert.AreEqual(".example.com", options.Proxy.Bypass);
        Assert.AreEqual("user", options.Proxy.Username);
        Assert.AreEqual("pass", options.Proxy.Password);
    }

    [Test]
    public void ShouldContinueAfterUnsupportedElements()
    {
        var settings = Parse("""
            <Playwright>
              <NotImplemented>
                <Nested>ignored</Nested>
              </NotImplemented>
              <ExpectTimeout>250</ExpectTimeout>
              <LaunchOptions>
                <UnsupportedLaunchOption>
                  <Nested>ignored</Nested>
                </UnsupportedLaunchOption>
                <Headless>false</Headless>
              </LaunchOptions>
            </Playwright>
            """);

        Assert.AreEqual(250, settings.ExpectTimeout);
        Assert.False(settings.LaunchOptions!.Headless);
    }

    private static PlaywrightSettingsXml Parse(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { IgnoreWhitespace = true });
        return new PlaywrightSettingsXml(reader);
    }
}
