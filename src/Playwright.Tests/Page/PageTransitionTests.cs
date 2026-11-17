/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-transition.spec.ts.</summary>
public class PageTransitionTests : UpstreamPageTest
{
    [UpstreamTest("PA-038 should not crash when filter transition completes")]
    public async Task ShouldNotCrashWhenFilterTransitionCompletes()
    {
        Info.Fixme(BrowserName == "webkit", "Web process crashes on the compositor thread when a filter transitions to none");

        await Page.SetContentAsync($@"
    <style>
      div {{ transition: filter 0.1s; }}
      .dim {{ filter: brightness(0.95); }}
    </style>
    {string.Concat(Enumerable.Repeat("<div>hello</div>", 10))}
  ");
        var crashed = false;
        Page.Crash += (_, _) => crashed = true;
        for (var i = 0; i < 10 && !crashed; i++)
        {
            await Catch(() => Page.EvaluateAsync(@"async () => {
      const divs = [...document.querySelectorAll('div')];
      const settled = () => Promise.all(divs.map(div => new Promise(f => div.addEventListener('transitionend', f, { once: true }))));
      let promise = settled();
      divs.forEach(div => div.classList.add('dim'));
      await promise;
      promise = settled();
      divs.forEach(div => div.classList.remove('dim'));
      await promise;
    }"));
        }
        Assert.That(crashed, Is.EqualTo(false));
    }
}
