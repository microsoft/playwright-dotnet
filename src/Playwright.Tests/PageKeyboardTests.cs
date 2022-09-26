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

using System.Dynamic;

namespace Microsoft.Playwright.Tests;

public class PageKeyboardTests : PageTestEx
{
    [PlaywrightTest("page-keyboard.spec.ts", "should type into a textarea")]
    public async Task ShouldTypeIntoATextarea()
    {
        await Page.EvaluateAsync<string>(@"() => {
                var textarea = document.createElement('textarea');
                document.body.appendChild(textarea);
                textarea.focus();
            }");
        string text = "Hello world. I am the text that was typed!";
        await Page.Keyboard.TypeAsync(text);
        Assert.AreEqual(text, await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should press the metaKey")]
    public async Task ShouldPressTheMetaKey1()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        await Page.EvaluateAsync<string>(@"() => {
                window.keyPromise = new Promise(resolve => document.addEventListener('keydown', event => resolve(event.key)));
            }");
        await Page.Keyboard.PressAsync("Meta");
        Assert.AreEqual(TestConstants.IsFirefox && !TestConstants.IsMacOSX ? "OS" : "Meta", await Page.EvaluateAsync<string>("keyPromise"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should move with the arrow keys")]
    public async Task ShouldMoveWithTheArrowKeys()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.TypeAsync("textarea", "Hello World!");
        Assert.AreEqual("Hello World!", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        for (int i = 0; i < "World!".Length; i++)
        {
            _ = Page.Keyboard.PressAsync("ArrowLeft");
        }
        await Page.Keyboard.TypeAsync("inserted ");
        Assert.AreEqual("Hello inserted World!", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        _ = Page.Keyboard.DownAsync("Shift");
        for (int i = 0; i < "inserted ".Length; i++)
        {
            _ = Page.Keyboard.PressAsync("ArrowLeft");
        }
        _ = Page.Keyboard.UpAsync("Shift");
        await Page.Keyboard.PressAsync("Backspace");
        Assert.AreEqual("Hello World!", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should send a character with ElementHandle.press")]
    public async Task ShouldSendACharacterWithElementHandlePress()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.PressAsync("a");
        Assert.AreEqual("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));

        await Page.EvaluateAsync<string>("() => window.addEventListener('keydown', e => e.preventDefault(), true)");

        await textarea.PressAsync("b");
        Assert.AreEqual("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should send a character with sendCharacter")]
    public async Task ShouldSendACharacterWithSendCharacter()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        await Page.Keyboard.InsertTextAsync("嗨");
        Assert.AreEqual("嗨", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        await Page.EvaluateAsync<string>("() => window.addEventListener('keydown', e => e.preventDefault(), true)");
        await Page.Keyboard.InsertTextAsync("a");
        Assert.AreEqual("嗨a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "insertText should only emit input event")]
    public async Task InsertTextShouldOnlyEmitInputEvent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");

        var events = await Page.EvaluateHandleAsync(@"() => {
                const events = [];
                document.addEventListener('keydown', e => events.push(e.type));
                document.addEventListener('keyup', e => events.push(e.type));
                document.addEventListener('keypress', e => events.push(e.type));
                document.addEventListener('input', e => events.push(e.type));
                return events;
            }");

        await Page.Keyboard.InsertTextAsync("hello world");
        Assert.AreEqual(new[] { "input" }, await events.JsonValueAsync<string[]>());
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should report shiftKey")]
    [Skip(SkipAttribute.Targets.Firefox | SkipAttribute.Targets.OSX)]
    public async Task ShouldReportShiftKey()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        var keyboard = Page.Keyboard;
        var codeForKey = new Dictionary<string, int> { ["Shift"] = 16, ["Alt"] = 18, ["Control"] = 17 };
        foreach (string modifierKey in codeForKey.Keys)
        {
            int modifierValue = codeForKey[modifierKey];
            await keyboard.DownAsync(modifierKey);
            Assert.AreEqual($"Keydown: {modifierKey} {modifierKey}Left {modifierValue} [{modifierKey}]", await Page.EvaluateAsync<string>("() => getResult()"));
            await keyboard.DownAsync("!");
            // Shift+! will generate a keypress
            if (modifierKey == "Shift")
            {
                Assert.AreEqual($"Keydown: ! Digit1 49 [{modifierKey}]\nKeypress: ! Digit1 33 33 [{modifierKey}]", await Page.EvaluateAsync<string>("() => getResult()"));
            }
            else
            {
                Assert.AreEqual($"Keydown: ! Digit1 49 [{modifierKey}]", await Page.EvaluateAsync<string>("() => getResult()"));
            }

            await keyboard.UpAsync("!");
            Assert.AreEqual($"Keyup: ! Digit1 49 [{modifierKey}]", await Page.EvaluateAsync<string>("() => getResult()"));
            await keyboard.UpAsync(modifierKey);
            Assert.AreEqual($"Keyup: {modifierKey} {modifierKey}Left {modifierValue} []", await Page.EvaluateAsync<string>("() => getResult()"));
        }
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should report multiple modifiers")]
    public async Task ShouldReportMultipleModifiers()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        var keyboard = Page.Keyboard;
        await keyboard.DownAsync("Control");
        Assert.AreEqual("Keydown: Control ControlLeft 17 [Control]", await Page.EvaluateAsync<string>("() => getResult()"));
        await keyboard.DownAsync("Alt");
        Assert.AreEqual("Keydown: Alt AltLeft 18 [Alt Control]", await Page.EvaluateAsync<string>("() => getResult()"));
        await keyboard.DownAsync(";");
        Assert.AreEqual("Keydown: ; Semicolon 186 [Alt Control]", await Page.EvaluateAsync<string>("() => getResult()"));
        await keyboard.UpAsync(";");
        Assert.AreEqual("Keyup: ; Semicolon 186 [Alt Control]", await Page.EvaluateAsync<string>("() => getResult()"));
        await keyboard.UpAsync("Control");
        Assert.AreEqual("Keyup: Control ControlLeft 17 [Alt]", await Page.EvaluateAsync<string>("() => getResult()"));
        await keyboard.UpAsync("Alt");
        Assert.AreEqual("Keyup: Alt AltLeft 18 []", await Page.EvaluateAsync<string>("() => getResult()"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should send proper codes while typing")]
    public async Task ShouldSendProperCodesWhileTyping()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.TypeAsync("!");
        Assert.AreEqual(string.Join("\n",
            "Keydown: ! Digit1 49 []",
            "Keypress: ! Digit1 33 33 []",
            "Keyup: ! Digit1 49 []"), await Page.EvaluateAsync<string>("() => getResult()"));
        await Page.Keyboard.TypeAsync("^");
        Assert.AreEqual(string.Join("\n",
            "Keydown: ^ Digit6 54 []",
            "Keypress: ^ Digit6 94 94 []",
            "Keyup: ^ Digit6 54 []"), await Page.EvaluateAsync<string>("() => getResult()"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should send proper codes while typing with shift")]
    public async Task ShouldSendProperCodesWhileTypingWithShift()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        var keyboard = Page.Keyboard;
        await keyboard.DownAsync("Shift");
        await Page.Keyboard.TypeAsync("~");
        Assert.AreEqual(string.Join("\n",
            "Keydown: Shift ShiftLeft 16 [Shift]",
            "Keydown: ~ Backquote 192 [Shift]", // 192 is ` keyCode
            "Keypress: ~ Backquote 126 126 [Shift]", // 126 is ~ charCode
            "Keyup: ~ Backquote 192 [Shift]"), await Page.EvaluateAsync<string>("() => getResult()"));
        await keyboard.UpAsync("Shift");
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should not type canceled events")]
    public async Task ShouldNotTypeCanceledEvents()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        await Page.EvaluateAsync<string>(@"() =>{
                window.addEventListener('keydown', event => {
                    event.stopPropagation();
                    event.stopImmediatePropagation();
                    if (event.key === 'l')
                        event.preventDefault();
                    if (event.key === 'o')
                        event.preventDefault();
                }, false);
            }");
        await Page.Keyboard.TypeAsync("Hello World!");
        Assert.AreEqual("He Wrd!", await Page.EvaluateAsync<string>("() => textarea.value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should press plus")]
    public async Task ShouldPressPlus()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("+");
        Assert.AreEqual(
            string.Join(
                "\n",
                new[]
                {
                        "Keydown: + Equal 187 []", // 192 is ` keyCode
                        "Keypress: + Equal 43 43 []", // 126 is ~ charCode
                        "Keyup: + Equal 187 []"
                }),
            await Page.EvaluateAsync<string>("() => getResult()"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should press shift plus")]
    public async Task ShouldPressShiftPlus()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Shift++");
        Assert.AreEqual(
            string.Join(
                "\n",
                new[]
                {
                        "Keydown: Shift ShiftLeft 16 [Shift]",
                        "Keydown: + Equal 187 [Shift]", // 192 is ` keyCode
                        "Keypress: + Equal 43 43 [Shift]", // 126 is ~ charCode
                        "Keyup: + Equal 187 [Shift]",
                        "Keyup: Shift ShiftLeft 16 []"
                }),
            await Page.EvaluateAsync<string>("() => getResult()"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should support plus-separated modifiers")]
    public async Task ShouldSupportPlusSeparatedModifiers()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Shift+~");
        Assert.AreEqual(
            string.Join(
                "\n",
                new[]
                {
                        "Keydown: Shift ShiftLeft 16 [Shift]",
                        "Keydown: ~ Backquote 192 [Shift]", // 192 is ` keyCode
                        "Keypress: ~ Backquote 126 126 [Shift]", // 126 is ~ charCode
                        "Keyup: ~ Backquote 192 [Shift]",
                        "Keyup: Shift ShiftLeft 16 []"
                }),
            await Page.EvaluateAsync<string>("() => getResult()"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should support multiple plus-separated modifiers")]
    public async Task ShouldSupportMultiplePlusSeparatedModifiers()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Control+Shift+~");
        Assert.AreEqual(
            string.Join(
                "\n",
                new[]
                {
                        "Keydown: Control ControlLeft 17 [Control]",
                        "Keydown: Shift ShiftLeft 16 [Control Shift]",
                        "Keydown: ~ Backquote 192 [Control Shift]", // 192 is ` keyCode
                        "Keyup: ~ Backquote 192 [Control Shift]",
                        "Keyup: Shift ShiftLeft 16 [Control]",
                        "Keyup: Control ControlLeft 17 []"
                }),
            await Page.EvaluateAsync<string>("() => getResult()"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should shift raw codes")]
    public async Task ShouldShiftRawCodes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Shift+Digit3");
        Assert.AreEqual(
            string.Join(
                "\n",
                new[]
                {
                        "Keydown: Shift ShiftLeft 16 [Shift]",
                        "Keydown: # Digit3 51 [Shift]", // 51 is # keyCode
                        "Keypress: # Digit3 35 35 [Shift]", // 35 is # charCode
                        "Keyup: # Digit3 51 [Shift]",
                        "Keyup: Shift ShiftLeft 16 []"
                }),
            await Page.EvaluateAsync<string>("() => getResult()"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should specify repeat property")]
    public async Task ShouldSpecifyRepeatProperty()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        var lastEvent = await CaptureLastKeydownAsync(Page);
        await Page.EvaluateAsync("() => document.querySelector('textarea').addEventListener('keydown', e => window.lastEvent = e, true)");
        await Page.Keyboard.DownAsync("a");
        Assert.False(await lastEvent.EvaluateAsync<bool>("e => e.repeat"));
        await Page.Keyboard.PressAsync("a");
        Assert.True(await lastEvent.EvaluateAsync<bool>("e => e.repeat"));

        await Page.Keyboard.DownAsync("b");
        Assert.False(await lastEvent.EvaluateAsync<bool>("e => e.repeat"));
        await Page.Keyboard.DownAsync("b");
        Assert.True(await lastEvent.EvaluateAsync<bool>("e => e.repeat"));

        await Page.Keyboard.UpAsync("a");
        await Page.Keyboard.DownAsync("a");
        Assert.False(await lastEvent.EvaluateAsync<bool>("e => e.repeat"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should type all kinds of characters")]
    public async Task ShouldTypeAllKindsOfCharacters()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        string text = "This text goes onto two lines.\nThis character is 嗨.";
        await Page.Keyboard.TypeAsync(text);
        Assert.AreEqual(text, await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should specify location")]
    public async Task ShouldSpecifyLocation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var lastEventHandle = await CaptureLastKeydownAsync(Page);
        var textarea = await Page.QuerySelectorAsync("textarea");

        await textarea.PressAsync("Digit5");
        Assert.AreEqual(0, await lastEventHandle.EvaluateAsync<int>("e => e.location"));

        await textarea.PressAsync("ControlLeft");
        Assert.AreEqual(1, await lastEventHandle.EvaluateAsync<int>("e => e.location"));

        await textarea.PressAsync("ControlRight");
        Assert.AreEqual(2, await lastEventHandle.EvaluateAsync<int>("e => e.location"));

        await textarea.PressAsync("NumpadSubtract");
        Assert.AreEqual(3, await lastEventHandle.EvaluateAsync<int>("e => e.location"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should press Enter")]
    public async Task ShouldPressEnter()
    {
        await Page.SetContentAsync("<textarea></textarea>");
        await Page.FocusAsync("textarea");
        var lastEventHandle = await CaptureLastKeydownAsync(Page);

        await TestEnterKeyAsync("Enter", "Enter", "Enter");
        await TestEnterKeyAsync("NumpadEnter", "Enter", "NumpadEnter");
        await TestEnterKeyAsync("\n", "Enter", "Enter");
        await TestEnterKeyAsync("\r", "Enter", "Enter");

        async Task TestEnterKeyAsync(string key, string expectedKey, string expectedCode)
        {
            await Page.Keyboard.PressAsync(key);
            dynamic lastEvent = await lastEventHandle.JsonValueAsync<ExpandoObject>();
            Assert.AreEqual(expectedKey, lastEvent.key);
            Assert.AreEqual(expectedCode, lastEvent.code);

            string value = await Page.EvalOnSelectorAsync<string>("textarea", "t => t.value");
            Assert.AreEqual("\n", value);
            await Page.EvalOnSelectorAsync("textarea", "t => t.value = ''");
        }
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should throw on unknown keys")]
    public async Task ShouldThrowOnUnknownKeys()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Keyboard.PressAsync("NotARealKey"));
        Assert.AreEqual("Unknown key: \"NotARealKey\"", exception.Message);

        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Keyboard.PressAsync("ё"));
        Assert.AreEqual("Unknown key: \"ё\"", exception.Message);

        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Keyboard.PressAsync("😊"));
        Assert.AreEqual("Unknown key: \"😊\"", exception.Message);
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should type emoji")]
    public async Task ShouldTypeEmoji()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.TypeAsync("textarea", "👹 Tokyo street Japan 🇯🇵");
        Assert.AreEqual("👹 Tokyo street Japan 🇯🇵", await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should type emoji into an iframe")]
    public async Task ShouldTypeEmojiIntoAnIframe()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "emoji-test", Server.Prefix + "/input/textarea.html");
        var frame = Page.Frames.ElementAt(1);
        var textarea = await frame.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("👹 Tokyo street Japan 🇯🇵");
        Assert.AreEqual("👹 Tokyo street Japan 🇯🇵", await frame.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should handle selectAll")]
    [Skip(SkipAttribute.Targets.Chromium | SkipAttribute.Targets.OSX)]
    public async Task ShouldHandleSelectAll()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("some text");
        string modifier = TestConstants.IsMacOSX ? "Meta" : "Control";
        await Page.Keyboard.DownAsync(modifier);
        await Page.Keyboard.PressAsync("a");
        await Page.Keyboard.UpAsync(modifier);
        await Page.Keyboard.PressAsync("Backspace");
        Assert.IsEmpty(await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should be able to prevent selectAll")]
    [Skip(SkipAttribute.Targets.Chromium | SkipAttribute.Targets.OSX)]
    public async Task ShouldBeAbleToPreventSelectAll()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("some text");
        await Page.EvalOnSelectorAsync("textarea", @"textarea => {
                textarea.addEventListener('keydown', event => {
                    if (event.key === 'a' && (event.metaKey || event.ctrlKey))
                    event.preventDefault();
                }, false);
            }");
        string modifier = TestConstants.IsMacOSX ? "Meta" : "Control";
        await Page.Keyboard.DownAsync(modifier);
        await Page.Keyboard.PressAsync("a");
        await Page.Keyboard.UpAsync(modifier);
        await Page.Keyboard.PressAsync("Backspace");
        Assert.AreEqual("some tex", await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"));
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should press the meta key")]
    public async Task ShouldPressTheMetaKey2()
    {
        var lastEventHandle = await CaptureLastKeydownAsync(Page);
        await Page.Keyboard.PressAsync("Meta");
        dynamic result = await lastEventHandle.JsonValueAsync<ExpandoObject>();
        string key = result.key;
        string code = result.code;
        bool metaKey = result.metaKey;

        if (TestConstants.IsFirefox && !TestConstants.IsMacOSX)
        {
            Assert.AreEqual("OS", key);
        }
        else
        {
            Assert.AreEqual("Meta", key);
        }

        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual("OSLeft", code);
        }
        else
        {
            Assert.AreEqual("MetaLeft", code);
        }

        if (TestConstants.IsFirefox && !TestConstants.IsMacOSX)
        {
            Assert.False(metaKey);
        }
        else
        {
            Assert.True(metaKey);
        }
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should work after a cross origin navigation")]
    public async Task ShouldWorkAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        var lastEventHandle = await CaptureLastKeydownAsync(Page);
        await Page.Keyboard.PressAsync("a");
        dynamic result = await lastEventHandle.JsonValueAsync<ExpandoObject>();
        Assert.AreEqual("a", result.key);
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should expose keyIdentifier in webkit")]
    [Skip(SkipAttribute.Targets.Chromium, SkipAttribute.Targets.Firefox)]
    public async Task ShouldExposeKeyIdentifierInWebkit()
    {
        var lastEventHandle = await CaptureLastKeydownAsync(Page);
        var keyMap = new Dictionary<string, string>
        {
            ["ArrowUp"] = "Up",
            ["ArrowDown"] = "Down",
            ["ArrowLeft"] = "Left",
            ["ArrowRight"] = "Right",
            ["Backspace"] = "U+0008",
            ["Tab"] = "U+0009",
            ["Delete"] = "U+007F",
            ["a"] = "U+0041",
            ["b"] = "U+0042",
            ["F12"] = "F12",
        };

        foreach (var kv in keyMap)
        {
            await Page.Keyboard.PressAsync(kv.Key);
            Assert.AreEqual(kv.Value, await lastEventHandle.EvaluateAsync<string>("e => e.keyIdentifier"));
        }
    }

    [PlaywrightTest("page-keyboard.spec.ts", "should scroll with PageDown")]
    public async Task ShouldScrollWithPageDown()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.ClickAsync("body");
        await Page.Keyboard.PressAsync("PageDown");
        await Page.WaitForFunctionAsync("() => scrollY > 0");
    }

    private Task<IJSHandle> CaptureLastKeydownAsync(IPage page)
        => page.EvaluateHandleAsync(@"() => {
                const lastEvent = {
                  repeat: false,
                  location: -1,
                  code: '',
                  key: '',
                  metaKey: false,
                  keyIdentifier: 'unsupported'
                };
                document.addEventListener('keydown', e => {
                  lastEvent.repeat = e.repeat;
                  lastEvent.location = e.location;
                  lastEvent.key = e.key;
                  lastEvent.code = e.code;
                  lastEvent.metaKey = e.metaKey;
                  lastEvent.keyIdentifier = 'keyIdentifier' in e && e.keyIdentifier;
                }, true);
                return lastEvent;
            }");
}
