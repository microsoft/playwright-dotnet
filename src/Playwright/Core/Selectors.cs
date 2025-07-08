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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class Selectors : ISelectors
{
    internal readonly List<BrowserContext> _contextsForSelectors = new();
    internal readonly List<Dictionary<string, object?>> _selectorEngines = new();
    internal string? _testIdAttributeName;

    public async Task RegisterAsync(string name, SelectorsRegisterOptions? options = default)
    {
        if (_selectorEngines.Where(engine => engine["name"]?.ToString() == name).Any())
        {
            throw new PlaywrightException($"\"{name}\" selector engine has been already registered");
        }

        options ??= new SelectorsRegisterOptions();
        var source = ScriptsHelper.EvaluationScript(options.Script, options.Path, false);
        var engine = new Dictionary<string, object?>()
        {
            ["name"] = name,
            ["source"] = source,
        };
        if (options.ContentScript != null)
        {
            engine["contentScript"] = options.ContentScript;
        }
        foreach (var context in _contextsForSelectors)
        {
            await context.SendMessageToServerAsync("registerSelectorEngine", new Dictionary<string, object?>
            {
                ["selectorEngine"] = engine,
            }).ConfigureAwait(false);
        }
        _selectorEngines.Add(engine);
    }

    public void SetTestIdAttribute(string attributeName)
    {
        Locator.SetTestIdAttribute(attributeName);
        _testIdAttributeName = attributeName;
        foreach (var context in _contextsForSelectors)
        {
            context.SendMessageToServerAsync(
            "setTestIdAttributeName",
            new Dictionary<string, object?>
            {
                ["testIdAttributeName"] = attributeName,
            }).IgnoreException();
        }
    }
}
