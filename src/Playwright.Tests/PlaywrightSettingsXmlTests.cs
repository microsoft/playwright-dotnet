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

using System.Xml;
using Microsoft.Playwright.TestAdapter;

namespace Microsoft.Playwright.Tests;

/// <summary>
/// The parser of the Playwright section of a .runsettings file. These tests give it the XML
/// directly; the adapters are tested end to end in Playwright.TestingHarnessTest.
/// </summary>
// See https://github.com/microsoft/playwright-dotnet/issues/3431
public class PlaywrightSettingsXmlTests
{
    private static PlaywrightSettingsXml Parse(string playwrightSection)
    {
        using var reader = XmlReader.Create(new StringReader(playwrightSection));
        return new PlaywrightSettingsXml(reader);
    }

    [Test]
    public void ShouldParseTheShorthandWithSingleQuotes()
    {
        var settings = Parse(@"
            <Playwright>
              <LaunchOptions>
                <Args>['--first', '--second']</Args>
              </LaunchOptions>
              <ContextOptions>
                <ExtraHTTPHeaders>{'X-Owner': 'me'}</ExtraHTTPHeaders>
              </ContextOptions>
            </Playwright>");
        Assert.AreEqual(new[] { "--first", "--second" }, settings.LaunchOptions.Args);
        Assert.AreEqual("me", settings.ContextOptions.ExtraHTTPHeaders.Single(header => header.Key == "X-Owner").Value);
    }

    [Test]
    public void ShouldKeepApostrophesInJsonValues()
    {
        var settings = Parse(@"
            <Playwright>
              <LaunchOptions>
                <Args>[""--user-agent=Bob's bot""]</Args>
              </LaunchOptions>
              <ContextOptions>
                <ExtraHTTPHeaders>{""X-Owner"": ""O'Brien""}</ExtraHTTPHeaders>
              </ContextOptions>
            </Playwright>");
        Assert.AreEqual(new[] { "--user-agent=Bob's bot" }, settings.LaunchOptions.Args);
        Assert.AreEqual("O'Brien", settings.ContextOptions.ExtraHTTPHeaders.Single(header => header.Key == "X-Owner").Value);
    }

    [Test]
    public void ShouldAcceptEnumValuesByTheirCSharpName()
    {
        Assert.AreEqual(ColorScheme.Dark, Parse("<Playwright><ContextOptions><ColorScheme>dark</ColorScheme></ContextOptions></Playwright>").ContextOptions.ColorScheme);
        Assert.AreEqual(ColorScheme.Dark, Parse("<Playwright><ContextOptions><ColorScheme>Dark</ColorScheme></ContextOptions></Playwright>").ContextOptions.ColorScheme);
        Assert.AreEqual(ColorScheme.NoPreference, Parse("<Playwright><ContextOptions><ColorScheme>no-preference</ColorScheme></ContextOptions></Playwright>").ContextOptions.ColorScheme);
        Assert.AreEqual(ColorScheme.NoPreference, Parse("<Playwright><ContextOptions><ColorScheme>nopreference</ColorScheme></ContextOptions></Playwright>").ContextOptions.ColorScheme);
        Assert.Throws<ArgumentException>(() => Parse("<Playwright><ContextOptions><ColorScheme>purple</ColorScheme></ContextOptions></Playwright>"));
    }

    [Test]
    public void ShouldNotReadTheChildrenOfAnUnknownElementAsSettings()
    {
        // No whitespace between the elements, so that skipping one element too far would show.
        var settings = Parse(
            "<Playwright>" +
            "<Unknown><BrowserName>webkit</BrowserName><ExpectTimeout>1</ExpectTimeout></Unknown>" +
            "<BrowserName>firefox</BrowserName>" +
            "<LaunchOptions><Unknown><Channel>msedge</Channel></Unknown><Headless>false</Headless><Empty/><SlowMo>100</SlowMo></LaunchOptions>" +
            "<ExpectTimeout>2000</ExpectTimeout>" +
            "</Playwright>");
        Assert.AreEqual("firefox", settings.BrowserName);
        Assert.AreEqual(2000, settings.ExpectTimeout);
        Assert.Null(settings.LaunchOptions.Channel);
        Assert.AreEqual(false, settings.LaunchOptions.Headless);
        Assert.AreEqual(100, settings.LaunchOptions.SlowMo);
    }
}
