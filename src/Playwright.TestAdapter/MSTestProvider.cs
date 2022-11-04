/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Microsoft.Playwright.TestAdapter;

/// <summary>
/// MSTest does not provide a way to get the global timeout value during a test execution.
/// This class is a workaround to get the timeout value from the runsettings file / CLI options.
/// </summary>
[ExtensionUri("settings://playwright-mstest")]
[SettingsName("MSTest")]
public class MSTestProvider : ISettingsProvider
{
    public static int? TestTimeout = null;
    public void Load(XmlReader reader)
    {
        reader.Read();
        // Read the <MSTest> element
        if (reader.IsEmptyElement)
        {
            return;
        }
        while (reader.Read())
        // https://github.com/microsoft/testfx/blob/9d520357fde04064f9964fa42c9434b844587ce7/src/Adapter/MSTest.TestAdapter/MSTestSettings.cs#L429
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            switch (reader.Name.ToUpperInvariant())
            {
                case "TESTTIMEOUT":
                    {
                        if (int.TryParse(reader.ReadInnerXml(), out int testTimeout) && testTimeout > 0)
                        {
                            TestTimeout = testTimeout;
                        }
                        break;
                    }
            }
        }
    }
}
