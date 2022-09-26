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

namespace Microsoft.Playwright.Tests;

public class SelectorMiscTests : PageTestEx
{
    [PlaywrightTest("selectors-misc.spec.ts", "should work for open shadow roots")]
    public async Task ShouldWorkForOpenShadowRoots()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("id=target", "e => e.textContent"));
        Assert.AreEqual("Hello from root1", await Page.EvalOnSelectorAsync<string>("data-testid=foo", "e => e.textContent"));
        Assert.AreEqual(3, await Page.EvalOnSelectorAllAsync<int>("data-testid=foo", "els => els.length"));
        Assert.Null(await Page.QuerySelectorAsync("id:light=target"));
        Assert.Null(await Page.QuerySelectorAsync("data-testid:light=foo"));
        Assert.IsEmpty(await Page.QuerySelectorAllAsync("data-testid:light=foo"));
    }

    [PlaywrightTest("selectors-misc.spec.ts", "should work with layout selectors")]
    public async Task ShouldWorkWithLayoutSelectors()
    {
        /*
            +--+  +--+
            | 1|  | 2|
            +--+  ++-++
            | 3|   | 4|
        +-------+  ++-++
        |   0   |  | 5|
        | +--+  +--+--+
        | | 6|  | 7|
        | +--+  +--+
        |       |
        O-------+
                +--+
                | 8|
                +--++--+
                    | 9|
                    +--+
        */
        await Page.SetContentAsync("<container style=\"width: 500px; height: 500px; position: relative;\"></container>");
        await Page.EvalOnSelectorAsync("container", @"container => {
                const boxes = [
                    // x, y, width, height
                    [0, 0, 150, 150],
                    [100, 200, 50, 50],
                    [200, 200, 50, 50],
                    [100, 150, 50, 50],
                    [201, 150, 50, 50],
                    [200, 100, 50, 50],
                    [50, 50, 50, 50],
                    [150, 50, 50, 50],
                    [150, -51, 50, 50],
                    [201, -101, 50, 50],
                ];
                for (let i = 0; i < boxes.length; i++) {
                    const div = document.createElement('div');
                    div.style.position = 'absolute';
                    div.style.overflow = 'hidden';
                    div.style.boxSizing = 'border-box';
                    div.style.border = '1px solid black';
                    div.id = 'id' + i;
                    div.textContent = 'id' + i;
                    const box = boxes[i];
                    div.style.left = box[0] + 'px';
                    // Note that top is a flipped y coordinate.
                    div.style.top = (250 - box[1] - box[3]) + 'px';
                    div.style.width = box[2] + 'px';
                    div.style.height = box[3] + 'px';
                    container.appendChild(div);
                    const span = document.createElement('span');
                    span.textContent = '' + i;
                    div.appendChild(span);
                }
            }");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:right-of(#id6)", "e => e.id"), "id7");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:right-of(#id1)", "e => e.id"), "id2");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:right-of(#id3)", "e => e.id"), "id4");
        Assert.AreEqual(await Page.QuerySelectorAsync("div:right-of(#id4)"), null);
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:right-of(#id0)", "e => e.id"), "id7");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:right-of(#id8)", "e => e.id"), "id9");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:right-of(#id3)", "els => els.map(e => e.id).join(',')"), "id4,id2,id5,id7,id8,id9");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:right-of(#id3, 50)", "els => els.map(e => e.id).join(',')"), "id2,id5,id7,id8");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:right-of(#id3, 49)", "els => els.map(e => e.id).join(',')"), "id7,id8");

        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:left-of(#id2)", "e => e.id"), "id1");
        Assert.AreEqual(await Page.QuerySelectorAsync("div:left-of(#id0)"), null);
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:left-of(#id5)", "e => e.id"), "id0");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:left-of(#id9)", "e => e.id"), "id8");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:left-of(#id4)", "e => e.id"), "id3");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:left-of(#id5)", "els => els.map(e => e.id).join(',')"), "id0,id7,id3,id1,id6,id8");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:left-of(#id5, 3)", "els => els.map(e => e.id).join(',')"), "id7,id8");

        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:above(#id0)", "e => e.id"), "id3");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:above(#id5)", "e => e.id"), "id4");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:above(#id7)", "e => e.id"), "id5");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:above(#id8)", "e => e.id"), "id0");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:above(#id9)", "e => e.id"), "id8");
        Assert.AreEqual(await Page.QuerySelectorAsync("div:above(#id2)"), null);
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:above(#id5)", "els => els.map(e => e.id).join(',')"), "id4,id2,id3,id1");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:above(#id5, 20)", "els => els.map(e => e.id).join(',')"), "id4,id3");

        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:below(#id4)", "e => e.id"), "id5");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:below(#id3)", "e => e.id"), "id0");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:below(#id2)", "e => e.id"), "id4");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:below(#id6)", "e => e.id"), "id8");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:below(#id7)", "e => e.id"), "id8");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:below(#id8)", "e => e.id"), "id9");
        Assert.AreEqual(await Page.QuerySelectorAsync("div:below(#id9)"), null);
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:below(#id3)", "els => els.map(e => e.id).join(',')"), "id0,id5,id6,id7,id8,id9");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:below(#id3, 105)", "els => els.map(e => e.id).join(',')"), "id0,id5,id6,id7");

        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:near(#id0)", "e => e.id"), "id3");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:near(#id7)", "els => els.map(e => e.id).join(',')"), "id0,id5,id3,id6");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:near(#id0)", "els => els.map(e => e.id).join(',')"), "id3,id6,id7,id8,id1,id5");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:near(#id6)", "els => els.map(e => e.id).join(',')"), "id0,id3,id7");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:near(#id6, 10)", "els => els.map(e => e.id).join(',')"), "id0");
        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:near(#id0, 100)", "els => els.map(e => e.id).join(',')"), "id3,id6,id7,id8,id1,id5,id4,id2");

        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:below(#id5):above(#id8)", "els => els.map(e => e.id).join(',')"), "id7,id6");
        Assert.AreEqual(await Page.EvalOnSelectorAsync<string>("div:below(#id5):above(#id8)", "e => e.id"), "id7");

        Assert.AreEqual(await Page.EvalOnSelectorAllAsync<string>("div:right-of(#id0) + div:above(#id8)", "els => els.map(e => e.id).join(',')"), "id5,id6,id3");

        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.QuerySelectorAsync(":near(50)"));
        StringAssert.Contains("\"near\" engine expects a selector list and optional maximum distance in pixels", error.Message);
        var error1 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.QuerySelectorAsync("div >> left-of=abc"));
        StringAssert.Contains("Malformed selector: left-of=abc", error1.Message);
        var error2 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.QuerySelectorAsync("left-of=\"div\""));
        StringAssert.Contains("\"left-of\" selector cannot be first", error2.Message);
        var error3 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.QuerySelectorAsync("div >> left-of=33"));
        StringAssert.Contains("Malformed selector: left-of=33", error3.Message);
        var error4 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.QuerySelectorAsync("div >> left-of=\"span\",\"foo\""));
        StringAssert.Contains("Malformed selector: left-of=\"span\",\"foo\"", error4.Message);
        var error5 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.QuerySelectorAsync("div >> left-of=\"span\",3,4"));
        StringAssert.Contains("Malformed selector: left-of=\"span\",3,4", error5.Message);
    }
}
