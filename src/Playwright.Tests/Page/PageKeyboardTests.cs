/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-keyboard.spec.ts.</summary>
public class PageKeyboardTests
{
    [BeforeEach]
    public static void Skip(bool isAndroid) => It.Skip(isAndroid);

    [UpstreamTest("KB-001 should type into a textarea @smoke")]
    public async Task ShouldTypeIntoATextarea(IPage page)
    {
        await page.EvaluateAsync(@"() => {
    const textarea = document.createElement('textarea');
    document.body.appendChild(textarea);
    textarea.focus();
  }");
        var text = "Hello world. I am the text that was typed!";
        await page.Keyboard.TypeAsync(text);
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo(text));
    }

    [UpstreamTest("KB-002 should move with the arrow keys")]
    public async Task ShouldMoveWithTheArrowKeys(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.TypeAsync("textarea", "Hello World!");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("Hello World!"));
        for (var i = 0; i < "World!".Length; i++)
        {
            await page.Keyboard.PressAsync("ArrowLeft");
        }
        await page.Keyboard.TypeAsync("inserted ");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("Hello inserted World!"));
        await page.Keyboard.DownAsync("Shift");
        for (var i = 0; i < "inserted ".Length; i++)
        {
            await page.Keyboard.PressAsync("ArrowLeft");
        }
        await page.Keyboard.UpAsync("Shift");
        await page.Keyboard.PressAsync("Backspace");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("Hello World!"));
    }

    [UpstreamTest("KB-003 should send a character with ElementHandle.press")]
    public async Task ShouldSendACharacterWithElementHandlePress(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var textarea = await page.QuerySelectorAsync("textarea");
        await textarea.PressAsync("a");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("a"));

        await page.EvaluateAsync("() => window.addEventListener('keydown', e => e.preventDefault(), true)");

        await textarea.PressAsync("b");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("a"));
    }

    [UpstreamTest("KB-004 should send a character with insertText")]
    public async Task ShouldSendACharacterWithInsertText(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FocusAsync("textarea");
        await page.Keyboard.InsertTextAsync("嗨");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("嗨"));
        await page.EvaluateAsync("() => window.addEventListener('keydown', e => e.preventDefault(), true)");
        await page.Keyboard.InsertTextAsync("a");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("嗨a"));
    }

    [UpstreamTest("KB-005 should not type text with Alt modifier")]
    public async Task ShouldNotTypeTextWithAltModifier(IPage page)
    {
        await page.SetContentAsync("<input><textarea></textarea><div contenteditable></div>");
        foreach (var locator in new[] { page.Locator("input"), page.Locator("textarea"), page.Locator("div") })
        {
            await locator.PressAsync("Alt+KeyA");
            await locator.PressAsync("Alt+Digit1");
            await locator.PressAsync("Alt+Shift+KeyA");
            await page.Keyboard.DownAsync("Alt");
            await page.Keyboard.PressAsync("b");
            await page.Keyboard.UpAsync("Alt");
            await locator.PressAsync("c");
        }
        await Expect(page.Locator("input")).ToHaveValueAsync("c");
        await Expect(page.Locator("textarea")).ToHaveValueAsync("c");
        await Expect(page.Locator("div")).ToHaveTextAsync("c");
    }

    [UpstreamTest("KB-006 insertText should only emit input event")]
    public async Task InsertTextShouldOnlyEmitInputEvent(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FocusAsync("textarea");
        // page.on('console', m => console.log(m.text()));
        var events = await page.EvaluateHandleAsync(@"() => {
    const events = [];
    document.addEventListener('keydown', e => events.push(e.type));
    document.addEventListener('keyup', e => events.push(e.type));
    document.addEventListener('keypress', e => events.push(e.type));
    document.addEventListener('input', e => events.push(e.type));
    return events;
  }");
        await page.Keyboard.InsertTextAsync("hello world");
        Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "input" }));
    }

    [UpstreamTest("KB-007 should emit keydown, keypress, textInput and input when typing a character")]
    public async Task ShouldEmitKeydownKeypressTextInputAndInputWhenTypingACharacter(IPage page)
    {
        await page.SetContentAsync("<input>");
        var events = await page.EvaluateHandleAsync(@"() => {
    const events = [];
    for (const type of ['keydown', 'keypress', 'textInput', 'input', 'keyup'])
      document.querySelector('input').addEventListener(type, () => events.push(type));
    return events;
  }");
        await page.FocusAsync("input");
        await page.Keyboard.PressAsync("f");
        Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "keydown", "keypress", "textInput", "input", "keyup" }));
    }

    [UpstreamTest("KB-008 should dispatch key events in separate tasks")]
    public async Task ShouldDispatchKeyEventsInSeparateTasks(IPage page, string browserName, bool isBidi)
    {
        It.Skip(browserName == "firefox" && !isBidi, "Firefox/Juggler dispatches keydown and keypress in the same task");
        await page.SetContentAsync("<input>");
        var log = await page.EvaluateHandleAsync(@"() => {
    const log = [];
    const input = document.querySelector('input');
    for (const type of ['keydown', 'keypress'])
      input.addEventListener(type, () => { log.push(type); queueMicrotask(() => log.push('microtask-' + type)); });
    input.focus();
    return log;
  }");
        await page.Keyboard.PressAsync("a");
        // A microtask scheduled in the keydown handler must run before keypress,
        // proving each event is delivered in its own task rather than synchronously.
        Assert.That(await log.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "keydown", "microtask-keydown", "keypress", "microtask-keypress" }));
    }

    [UpstreamTest("KB-009 should report shiftKey")]
    public async Task ShouldReportShiftKey(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        var keyboard = page.Keyboard;
        var codeForKey = new Dictionary<string, int> { ["Shift"] = 16, ["Alt"] = 18, ["Control"] = 17 };
        foreach (var modifierKey in codeForKey.Keys)
        {
            await keyboard.DownAsync(modifierKey);
            Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: " + modifierKey + " " + modifierKey + "Left LEFT [" + modifierKey + "]"));
            await keyboard.DownAsync("!");
            // Shift+! will generate a keypress
            if (modifierKey == "Shift")
            {
                Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: ! Digit1 STANDARD [" + modifierKey + "]\nKeypress: ! Digit1 STANDARD 33 [" + modifierKey + "]"));
            }
            else
            {
                Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: ! Digit1 STANDARD [" + modifierKey + "]"));
            }

            await keyboard.UpAsync("!");
            Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: ! Digit1 STANDARD [" + modifierKey + "]"));
            await keyboard.UpAsync(modifierKey);
            Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: " + modifierKey + " " + modifierKey + "Left LEFT []"));
        }
    }

    [UpstreamTest("KB-010 should report multiple modifiers")]
    public async Task ShouldReportMultipleModifiers(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        var keyboard = page.Keyboard;
        await keyboard.DownAsync("Control");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: Control ControlLeft LEFT [Control]"));
        await keyboard.DownAsync("Alt");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: Alt AltLeft LEFT [Alt Control]"));
        await keyboard.DownAsync(";");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: ; Semicolon STANDARD [Alt Control]"));
        await keyboard.UpAsync(";");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: ; Semicolon STANDARD [Alt Control]"));
        await keyboard.UpAsync("Control");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: Control ControlLeft LEFT [Alt]"));
        await keyboard.UpAsync("Alt");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: Alt AltLeft LEFT []"));
    }

    [UpstreamTest("KB-011 should send proper codes while typing")]
    public async Task ShouldSendProperCodesWhileTyping(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        await page.Keyboard.TypeAsync("!");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: ! Digit1 STANDARD []",
                "Keypress: ! Digit1 STANDARD 33 []",
                "Keyup: ! Digit1 STANDARD []",
            })));
        await page.Keyboard.TypeAsync("^");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: ^ Digit6 STANDARD []",
                "Keypress: ^ Digit6 STANDARD 94 []",
                "Keyup: ^ Digit6 STANDARD []",
            })));
    }

    [UpstreamTest("KB-012 should send proper codes while typing with shift")]
    public async Task ShouldSendProperCodesWhileTypingWithShift(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        var keyboard = page.Keyboard;
        await keyboard.DownAsync("Shift");
        await page.Keyboard.TypeAsync("~");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: Shift ShiftLeft LEFT [Shift]",
                "Keydown: ~ Backquote STANDARD [Shift]",
                "Keypress: ~ Backquote STANDARD 126 [Shift]",
                "Keyup: ~ Backquote STANDARD [Shift]",
            })));
        await keyboard.UpAsync("Shift");
    }

    [UpstreamTest("KB-013 should not type canceled events")]
    public async Task ShouldNotTypeCanceledEvents(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FocusAsync("textarea");
        await page.EvaluateAsync(@"() => {
    window.addEventListener('keydown', event => {
      event.stopPropagation();
      event.stopImmediatePropagation();
      if (event.key === 'l')
        event.preventDefault();
      if (event.key === 'o')
        event.preventDefault();
    }, false);
  }");
        await page.Keyboard.TypeAsync("Hello World!");
        Assert.That(await page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("He Wrd!"));
    }

    [UpstreamTest("KB-014 should press plus")]
    public async Task ShouldPressPlus(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        await page.Keyboard.PressAsync("+");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: + Equal STANDARD []",
                "Keypress: + Equal STANDARD 43 []",
                "Keyup: + Equal STANDARD []",
            })));
    }

    [UpstreamTest("KB-015 should press shift plus")]
    public async Task ShouldPressShiftPlus(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        await page.Keyboard.PressAsync("Shift++");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: Shift ShiftLeft LEFT [Shift]",
                "Keydown: + Equal STANDARD [Shift]",
                "Keypress: + Equal STANDARD 43 [Shift]",
                "Keyup: + Equal STANDARD [Shift]",
                "Keyup: Shift ShiftLeft LEFT []",
            })));
    }

    [UpstreamTest("KB-016 should support plus-separated modifiers")]
    public async Task ShouldSupportPlusSeparatedModifiers(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        await page.Keyboard.PressAsync("Shift+~");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: Shift ShiftLeft LEFT [Shift]",
                "Keydown: ~ Backquote STANDARD [Shift]",
                "Keypress: ~ Backquote STANDARD 126 [Shift]",
                "Keyup: ~ Backquote STANDARD [Shift]",
                "Keyup: Shift ShiftLeft LEFT []",
            })));
    }

    [UpstreamTest("KB-017 should support multiple plus-separated modifiers")]
    public async Task ShouldSupportMultiplePlusSeparatedModifiers(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        await page.Keyboard.PressAsync("Control+Shift+~");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: Control ControlLeft LEFT [Control]",
                "Keydown: Shift ShiftLeft LEFT [Control Shift]",
                "Keydown: ~ Backquote STANDARD [Control Shift]",
                "Keyup: ~ Backquote STANDARD [Control Shift]",
                "Keyup: Shift ShiftLeft LEFT [Control]",
                "Keyup: Control ControlLeft LEFT []",
            })));
    }

    [UpstreamTest("KB-018 should shift raw codes")]
    public async Task ShouldShiftRawCodes(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        await page.Keyboard.PressAsync("Shift+Digit3");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: Shift ShiftLeft LEFT [Shift]",
                "Keydown: # Digit3 STANDARD [Shift]",
                "Keypress: # Digit3 STANDARD 35 [Shift]",
                "Keyup: # Digit3 STANDARD [Shift]",
                "Keyup: Shift ShiftLeft LEFT []",
            })));
    }

    [UpstreamTest("KB-019 should specify repeat property")]
    public async Task ShouldSpecifyRepeatProperty(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FocusAsync("textarea");
        var lastEvent = await CaptureLastKeydown(page);
        await page.Keyboard.DownAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(false));
        await page.Keyboard.PressAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(true));

        await page.Keyboard.DownAsync("b");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(false));
        await page.Keyboard.DownAsync("b");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(true));

        await page.Keyboard.UpAsync("a");
        await page.Keyboard.DownAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(false));
    }

    [UpstreamTest("KB-020 should type all kinds of characters")]
    public async Task ShouldTypeAllKindsOfCharacters(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FocusAsync("textarea");
        var text = "This text goes onto two lines.\nThis character is 嗨.";
        await page.Keyboard.TypeAsync(text);
        Assert.That(await page.EvalOnSelectorAsync<string>("textarea", "t => t.value"), Is.EqualTo(text));
    }

    [UpstreamTest("KB-021 should specify location")]
    public async Task ShouldSpecifyLocation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var lastEvent = await CaptureLastKeydown(page);
        var textarea = await page.QuerySelectorAsync("textarea");

        await textarea.PressAsync("Digit5");
        Assert.That(await lastEvent.EvaluateAsync<int>("e => e.location"), Is.EqualTo(0));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.key"), Is.EqualTo("5"));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.code"), Is.EqualTo("Digit5"));

        await textarea.PressAsync("ControlLeft");
        Assert.That(await lastEvent.EvaluateAsync<int>("e => e.location"), Is.EqualTo(1));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.key"), Is.EqualTo("Control"));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.code"), Is.EqualTo("ControlLeft"));

        await textarea.PressAsync("ControlRight");
        Assert.That(await lastEvent.EvaluateAsync<int>("e => e.location"), Is.EqualTo(2));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.key"), Is.EqualTo("Control"));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.code"), Is.EqualTo("ControlRight"));

        await textarea.PressAsync("NumpadSubtract");
        Assert.That(await lastEvent.EvaluateAsync<int>("e => e.location"), Is.EqualTo(3));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.key"), Is.EqualTo("-"));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.code"), Is.EqualTo("NumpadSubtract"));
    }

    [UpstreamTest("KB-022 should specify numpad location on keyup")]
    public async Task ShouldSpecifyNumpadLocationOnKeyup(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        await page.Keyboard.PressAsync("NumpadSubtract");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: - NumpadSubtract NUMPAD []",
                "Keypress: - NumpadSubtract NUMPAD 45 []",
                "Keyup: - NumpadSubtract NUMPAD []",
            })));
    }

    [UpstreamTest("KB-023 should press NumpadDecimal as Delete")]
    public async Task ShouldPressNumpadDecimalAsDelete(IPage page)
    {
        await page.SetContentAsync("<textarea></textarea>");
        var lastEvent = await CaptureLastKeydown(page);
        var textarea = page.Locator("textarea");

        await textarea.PressAsync("NumpadDecimal");
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.key"), Is.EqualTo("Delete"));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.code"), Is.EqualTo("NumpadDecimal"));
        Assert.That(await lastEvent.EvaluateAsync<int>("e => e.location"), Is.EqualTo(3));
        await Expect(textarea).ToHaveValueAsync("");
    }

    [UpstreamTest("KB-024 should type numpad digits with Shift")]
    public async Task ShouldTypeNumpadDigitsWithShift(IPage page)
    {
        await page.SetContentAsync("<textarea></textarea>");
        var textarea = page.Locator("textarea");
        await textarea.PressAsync("Shift+Numpad1");
        await textarea.PressAsync("Shift+NumpadDecimal");
        await textarea.PressAsync("Shift+Numpad9");
        await Expect(textarea).ToHaveValueAsync("1.9");
    }

    [UpstreamTest("KB-025 should press Enter")]
    public async Task ShouldPressEnter(IPage page, Server server)
    {
        await page.SetContentAsync("<textarea></textarea>");
        await page.FocusAsync("textarea");
        var lastEventHandle = await CaptureLastKeydown(page);
        await testEnterKey("Enter", "Enter", "Enter");
        await testEnterKey("NumpadEnter", "Enter", "NumpadEnter");
        await testEnterKey("\n", "Enter", "Enter");
        await testEnterKey("\r", "Enter", "Enter");

        async Task testEnterKey(string key, string expectedKey, string expectedCode)
        {
            await page.Keyboard.PressAsync(key);
            var lastEvent = await lastEventHandle.JsonValueAsync<JsonElement>();
            Assert.That(lastEvent.GetProperty("key").GetString(), Is.EqualTo(expectedKey)); // had the wrong key
            Assert.That(lastEvent.GetProperty("code").GetString(), Is.EqualTo(expectedCode)); // had the wrong code
            var value = await page.EvalOnSelectorAsync<string>("textarea", "t => t.value");
            Assert.That(value, Is.EqualTo("\n")); // failed to create a newline
            await page.EvalOnSelectorAsync("textarea", "t => t.value = ''");
        }
    }

    [UpstreamTest("KB-026 should press audio and media control keys")]
    public async Task ShouldPressAudioAndMediaControlKeys(IPage page, string browserName)
    {
        await page.SetContentAsync("<input autofocus>");
        await page.FocusAsync("input");
        var lastEvent = await CaptureLastKeydown(page);
        var mediaKeys = new[]
        {
            new { key = "AudioVolumeMute", code = browserName == "firefox" ? "VolumeMute" : "AudioVolumeMute" },
            new { key = "AudioVolumeDown", code = browserName == "firefox" ? "VolumeDown" : "AudioVolumeDown" },
            new { key = "AudioVolumeUp", code = browserName == "firefox" ? "VolumeUp" : "AudioVolumeUp" },
            new { key = "MediaTrackNext", code = "MediaTrackNext" },
            new { key = "MediaTrackPrevious", code = "MediaTrackPrevious" },
            new { key = "MediaPlayPause", code = "MediaPlayPause" },
        };

        foreach (var mediaKey in mediaKeys)
        {
            await page.Keyboard.PressAsync(mediaKey.key);
            Assert.That(await lastEvent.EvaluateAsync<string>("e => e.key"), Is.EqualTo(mediaKey.key));
            Assert.That(await lastEvent.EvaluateAsync<string>("e => e.code"), Is.EqualTo(mediaKey.code));
            Assert.That(await lastEvent.EvaluateAsync<int>("e => e.location"), Is.EqualTo(0));
        }
    }

    [UpstreamTest("KB-027 should throw on unknown keys")]
    public async Task ShouldThrowOnUnknownKeys(IPage page, Server server)
    {
        var error = await Catch(() => page.Keyboard.PressAsync("NotARealKey"));
        Assert.That(error.Message, Does.Contain("Unknown key: \"NotARealKey\""));

        error = await Catch(() => page.Keyboard.PressAsync("ё"));
        Assert.That(error?.Message, Does.Contain("Unknown key: \"ё\""));

        error = await Catch(() => page.Keyboard.PressAsync("😊"));
        Assert.That(error?.Message, Does.Contain("Unknown key: \"😊\""));
    }

    [UpstreamTest("KB-028 should type emoji")]
    public async Task ShouldTypeEmoji(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.TypeAsync("textarea", "👹 Tokyo street Japan 🇯🇵");
        Assert.That(await page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("👹 Tokyo street Japan 🇯🇵"));
    }

    [UpstreamTest("KB-029 should type emoji into an iframe")]
    public async Task ShouldTypeEmojiIntoAnIframe(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "emoji-test", server.Prefix + "/input/textarea.html");
        var frame = page.Frames[1];
        var textarea = await frame.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("👹 Tokyo street Japan 🇯🇵");
        Assert.That(await frame.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("👹 Tokyo street Japan 🇯🇵"));
    }

    [UpstreamTest("KB-030 should handle selectAll")]
    public async Task ShouldHandleSelectAll(IPage page, Server server, bool isMac)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var textarea = await page.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("some text");
        await page.Keyboard.DownAsync("ControlOrMeta");
        await page.Keyboard.PressAsync("a");
        await page.Keyboard.UpAsync("ControlOrMeta");
        await page.Keyboard.PressAsync("Backspace");
        Assert.That(await page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo(""));
    }

    [UpstreamTest("KB-031 pressing Meta should not result in any text insertion on any platform")]
    public async Task PressingMetaShouldNotResultInAnyTextInsertionOnAnyPlatform(IPage page, Server server, bool isMac)
    {
        await page.SetContentAsync("<input type=\"text\" value=\"hello world\">");
        var input = page.Locator("input");
        await Expect(input).ToHaveValueAsync("hello world");
        await input.FocusAsync();
        await page.Keyboard.PressAsync("Meta");
        await Expect(input).ToHaveValueAsync("hello world");
    }

    [UpstreamTest("KB-032 should be able to prevent selectAll")]
    public async Task ShouldBeAbleToPreventSelectAll(IPage page, Server server, bool isMac)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var textarea = await page.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("some text");
        await page.EvalOnSelectorAsync("textarea", @"textarea => {
    textarea.addEventListener('keydown', event => {
      if (event.key === 'a' && (event.metaKey || event.ctrlKey))
        event.preventDefault();
    }, false);
  }");
        await page.Keyboard.DownAsync("ControlOrMeta");
        await page.Keyboard.PressAsync("a");
        await page.Keyboard.UpAsync("ControlOrMeta");
        await page.Keyboard.PressAsync("Backspace");
        Assert.That(await page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("some tex"));
    }

    [UpstreamTest("KB-033 should support MacOS shortcuts")]
    public async Task ShouldSupportMacOSShortcuts(IPage page, Server server, string platform, string browserName)
    {
        It.Skip(platform != "darwin");
        // @see https://github.com/microsoft/playwright/issues/5721
        It.Fixme(browserName == "firefox" && platform == "darwin");

        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var textarea = await page.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("some text");
        // select one word backwards
        await page.Keyboard.PressAsync("Shift+Control+Alt+KeyB");
        await page.Keyboard.PressAsync("Backspace");
        Assert.That(await page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("some "));
    }

    [UpstreamTest("KB-034 should press the meta key")]
    public async Task ShouldPressTheMetaKey(IPage page)
    {
        var lastEvent = await CaptureLastKeydown(page);
        await page.Keyboard.PressAsync("Meta");
        var value = await lastEvent.JsonValueAsync<JsonElement>();
        var key = value.GetProperty("key").GetString();
        var code = value.GetProperty("code").GetString();
        var metaKey = value.GetProperty("metaKey").GetBoolean();
        Assert.That(key, Is.EqualTo("Meta"));
        Assert.That(code, Is.EqualTo("MetaLeft"));
        Assert.That(metaKey, Is.EqualTo(true));
    }

    [UpstreamTest("KB-035 should work with keyboard events with empty.html")]
    public async Task ShouldWorkWithKeyboardEventsWithEmptyHtml(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/empty.html");
        var lastEvent = await CaptureLastKeydown(page);
        await page.Keyboard.PressAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<string>("l => l.key"), Is.EqualTo("a"));
    }

    [UpstreamTest("KB-036 should work after a cross origin navigation")]
    public async Task ShouldWorkAfterACrossOriginNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/empty.html");
        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        var lastEvent = await CaptureLastKeydown(page);
        await page.Keyboard.PressAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<string>("l => l.key"), Is.EqualTo("a"));
    }

    [UpstreamTest("KB-037 should expose keyIdentifier in webkit")]
    public async Task ShouldExposeKeyIdentifierInWebkit(IPage page, string browserName)
    {
        It.Skip(browserName != "webkit", "event.keyIdentifier has been removed from all browsers except WebKit");

        var lastEvent = await CaptureLastKeydown(page);
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
        foreach (var (key, keyIdentifier) in keyMap)
        {
            await page.Keyboard.PressAsync(key);
            Assert.That(await lastEvent.EvaluateAsync<string>("e => e.keyIdentifier"), Is.EqualTo(keyIdentifier));
        }
    }

    [UpstreamTest("KB-038 should scroll with PageDown")]
    public async Task ShouldScrollWithPageDown(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        // A click is required for WebKit to send the event into the body.
        await page.ClickAsync("body");
        await page.Keyboard.PressAsync("PageDown");
        // We can't wait for the scroll to finish, so just wait for it to start.
        await page.WaitForFunctionAsync("() => scrollY > 0");
    }

    [UpstreamTest("KB-039 should move around the selection in a contenteditable")]
    public async Task ShouldMoveAroundTheSelectionInAContenteditable(IPage page, bool isMac)
    {
        await page.SetContentAsync("<div contenteditable></div>");
        await page.FocusAsync("div");
        var modifier = isMac ? "Alt" : "Control";
        await page.Keyboard.TypeAsync("Hello World");
        await page.Keyboard.DownAsync(modifier);
        await page.Keyboard.DownAsync("Shift");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.UpAsync("Shift");
        await page.Keyboard.UpAsync(modifier);
        Assert.That(await page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("World"));
    }

    [UpstreamTest("KB-040 should move to the start of the document")]
    public async Task ShouldMoveToTheStartOfTheDocument(IPage page, bool isMac)
    {
        It.Skip(!isMac);
        await page.SetContentAsync("<div contenteditable></div>");
        await page.FocusAsync("div");
        await page.Keyboard.TypeAsync("1\n2\n3\n");
        await page.Keyboard.PressAsync("Shift+Meta+ArrowUp");
        Assert.That(await page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("1\n2\n3\n"));
    }

    [UpstreamTest("KB-041 should dispatch a click event on a button when Space gets pressed")]
    public async Task ShouldDispatchAClickEventOnAButtonWhenSpaceGetsPressed(IPage page)
    {
        await page.SetContentAsync("<button type=\"button\">a11y</button>");
        var actual = await page.EvaluateHandleAsync(@"() => {
    const actual = { clicked: false };
    document.querySelector('button').addEventListener('click', () => (actual.clicked = true));
    return actual;
  }");
        await page.FocusAsync("button");
        await page.Keyboard.PressAsync("Space");
        Assert.That((await actual.JsonValueAsync<JsonElement>()).GetProperty("clicked").GetBoolean(), Is.EqualTo(true));
    }

    [UpstreamTest("KB-042 should dispatch a click event on a button when Enter gets pressed")]
    public async Task ShouldDispatchAClickEventOnAButtonWhenEnterGetsPressed(IPage page)
    {
        await page.SetContentAsync("<button type=\"button\">a11y</button>");
        var actual = await page.EvaluateHandleAsync(@"() => {
    const actual = { clicked: false };
    document.querySelector('button').addEventListener('click', () => (actual.clicked = true));
    return actual;
  }");
        await page.FocusAsync("button");
        await page.Keyboard.PressAsync("Enter");
        Assert.That((await actual.JsonValueAsync<JsonElement>()).GetProperty("clicked").GetBoolean(), Is.EqualTo(true));
    }

    [UpstreamTest("KB-043 should support simple copy-pasting")]
    public async Task ShouldSupportSimpleCopyPasting(IPage page)
    {
        await page.SetContentAsync("<div contenteditable>123</div>");
        await page.FocusAsync("div");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyA");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyC");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyV");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyV");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('div').textContent"), Is.EqualTo("123123"));
    }

    [UpstreamTest("KB-044 should support simple cut-pasting")]
    public async Task ShouldSupportSimpleCutPasting(IPage page)
    {
        await page.SetContentAsync("<div contenteditable>123</div>");
        await page.FocusAsync("div");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyA");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyX");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyV");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyV");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('div').textContent"), Is.EqualTo("123123"));
    }

    [UpstreamTest("KB-045 should support undo-redo")]
    public async Task ShouldSupportUndoRedo(IPage page, string browserName, bool isLinux, string channel)
    {
        It.Fixme((browserName == "webkit" && isLinux) || channel == "webkit-wsl", "https://github.com/microsoft/playwright/issues/12000");
        await page.SetContentAsync("<div contenteditable></div>");
        var div = page.Locator("div");
        await Expect(div).ToHaveTextAsync("");
        await div.TypeAsync("123");
        await Expect(div).ToHaveTextAsync("123");
        await page.Keyboard.PressAsync("ControlOrMeta+KeyZ");
        await Expect(div).ToHaveTextAsync("");
        await page.Keyboard.PressAsync("Shift+ControlOrMeta+KeyZ");
        await Expect(div).ToHaveTextAsync("123");
    }

    [UpstreamTest("KB-046 should type repeatedly in contenteditable in shadow dom")]
    public async Task ShouldTypeRepeatedlyInContenteditableInShadowDom(IPage page)
    {
        await page.SetContentAsync(@"
    <html>
      <body>
        <shadow-element></shadow-element>
        <script>
          customElements.define('shadow-element', class extends HTMLElement {
            constructor() {
              super();
              this.attachShadow({ mode: 'open' });
            }

            connectedCallback() {
              this.shadowRoot.innerHTML = `
                <style>
                  .editor { padding: 1rem; margin: 1rem; border: 1px solid #ccc; }
                </style>
                <div class=editor contenteditable id=foo></div>
                <hr>
                <section>
                  <div class=editor contenteditable id=bar></div>
                </section>
              `;
            }
          });
        </script>
      </body>
    </html>
  ");

        var editor = page.Locator("shadow-element > .editor").First;
        await editor.TypeAsync("This is the first box.");

        var sectionEditor = page.Locator("section .editor");
        await sectionEditor.TypeAsync("This is the second box.");

        Assert.That(await editor.TextContentAsync(), Is.EqualTo("This is the first box."));
        Assert.That(await sectionEditor.TextContentAsync(), Is.EqualTo("This is the second box."));
    }

    [UpstreamTest("KB-047 should type repeatedly in contenteditable in shadow dom with nested elements")]
    public async Task ShouldTypeRepeatedlyInContenteditableInShadowDomWithNestedElements(IPage page)
    {
        await page.SetContentAsync(@"
    <html>
      <body>
        <shadow-element></shadow-element>
        <script>
          customElements.define('shadow-element', class extends HTMLElement {
            constructor() {
              super();
              this.attachShadow({ mode: 'open' });
            }

            connectedCallback() {
              this.shadowRoot.innerHTML = `
                <style>
                  .editor { padding: 1rem; margin: 1rem; border: 1px solid #ccc; }
                </style>
                <div class=editor contenteditable id=foo><p>hello</p></div>
                <hr>
                <section>
                  <div class=editor contenteditable id=bar><p>world</p></div>
                </section>
              `;
            }
          });
        </script>
      </body>
    </html>
  ");

        var editor = page.Locator("shadow-element > .editor").First;
        await editor.TypeAsync("This is the first box: ");

        var sectionEditor = page.Locator("section .editor");
        await sectionEditor.TypeAsync("This is the second box: ");

        Assert.That(await editor.TextContentAsync(), Is.EqualTo("This is the first box: hello"));
        Assert.That(await sectionEditor.TextContentAsync(), Is.EqualTo("This is the second box: world"));
    }

    [UpstreamTest("KB-048 should type repeatedly in input in shadow dom")]
    public async Task ShouldTypeRepeatedlyInInputInShadowDom(IPage page)
    {
        await page.SetContentAsync(@"
    <html>
      <body>
        <shadow-element></shadow-element>
        <script>
          customElements.define('shadow-element', class extends HTMLElement {
            constructor() {
              super();
              this.attachShadow({ mode: 'open' });
            }

            connectedCallback() {
              this.shadowRoot.innerHTML = `
                <style>
                  .editor { padding: 1rem; margin: 1rem; border: 1px solid #ccc; }
                </style>
                <input class=editor id=foo>
                <hr>
                <section>
                  <input class=editor id=bar>
                </section>
              `;
            }
          });
        </script>
      </body>
    </html>
  ");

        var editor = page.Locator("shadow-element > .editor").First;
        await editor.TypeAsync("This is the first box.");

        var sectionEditor = page.Locator("section .editor");
        await sectionEditor.TypeAsync("This is the second box.");

        Assert.That(await editor.InputValueAsync(), Is.EqualTo("This is the first box."));
        Assert.That(await sectionEditor.InputValueAsync(), Is.EqualTo("This is the second box."));
    }

    [UpstreamTest("KB-049 type to non-focusable element should maintain old focus")]
    public async Task TypeToNonFocusableElementShouldMaintainOldFocus(IPage page)
    {
        await page.SetContentAsync(@"
    <div id=""focusable"" tabindex=""0"">focusable div</div>
    <div id=""non-focusable-and-non-editable"">non-editable, non-focusable</div>
  ");

        await page.Locator("#focusable").FocusAsync();
        Assert.That(await page.EvaluateAsync<string>("() => document.activeElement?.id"), Is.EqualTo("focusable"));
        await page.Locator("#non-focusable-and-non-editable").TypeAsync("foo");
        Assert.That(await page.EvaluateAsync<string>("() => document.activeElement?.id"), Is.EqualTo("focusable"));
    }

    private static async Task<IJSHandle> CaptureLastKeydown(IPage page)
    {
        var lastEvent = await page.EvaluateHandleAsync(@"() => {
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
      lastEvent.keyIdentifier = 'keyIdentifier' in e && typeof e['keyIdentifier'] === 'string' && e['keyIdentifier'];
    }, true);
    return lastEvent;
  }");
        return lastEvent;
    }

    [UpstreamTest("KB-050 should dispatch insertText after context menu was opened")]
    public async Task ShouldDispatchInsertTextAfterContextMenuWasOpened(Server server, IPage page, string browserName, bool isWindows)
    {
        It.Skip(browserName == "chromium" && isWindows, "context menu support is best-effort for Linux and MacOS");
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.EvaluateAsync(@"() => {
    window['contextMenuPromise'] = new Promise(x => {
      window.addEventListener('contextmenu', x, false);
    });
  }");

        var box = await page.Locator("textarea").BoundingBoxAsync();
        var cx = box.X + (box.Width / 2);
        var cy = box.Y + (box.Height / 2);
        await page.Mouse.ClickAsync(cx, cy, new() { Button = MouseButton.Right });
        await page.EvaluateAsync("() => window['contextMenuPromise']");

        await page.Keyboard.InsertTextAsync("嗨");
        await Expect(async () => Assert.That(await page.Locator("textarea").InputValueAsync(), Is.EqualTo("嗨"))).ToPassAsync();
    }

    [UpstreamTest("KB-051 should type after context menu was opened")]
    public async Task ShouldTypeAfterContextMenuWasOpened(Server server, IPage page, string browserName, bool isWindows)
    {
        It.Skip(browserName == "chromium" && isWindows, "context menu support is best-effort for Linux and MacOS");
        await page.EvaluateAsync(@"() => {
    window['keys'] = [];
    window.addEventListener('keydown', event => window['keys'].push(event.key));
    window['contextMenuPromise'] = new Promise(x => {
      window.addEventListener('contextmenu', x, false);
    });
  }");

        await page.Mouse.MoveAsync(100, 100);
        await page.Mouse.DownAsync(new() { Button = MouseButton.Right });
        await page.EvaluateAsync("() => window['contextMenuPromise']");

        await page.Keyboard.DownAsync("ArrowDown");

        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("window.keys"), Is.EqualTo(new[] { "ArrowDown" }))).ToPassAsync();
    }

    [UpstreamTest("KB-052 should have correct Keydown/Keyup order when pressing Escape key")]
    public async Task ShouldHaveCorrectKeydownKeyupOrderWhenPressingEscapeKey(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.Prefix + "/input/keyboard.html");
        await page.Keyboard.PressAsync("Escape");
        Assert.That(await page.EvaluateAsync<string>("getResult()"), Is.EqualTo(@"
Keydown: Escape Escape STANDARD []
Keyup: Escape Escape STANDARD []
".Trim()));
    }

    [UpstreamTest("KB-053 should close dialog on Escape key press in contenteditable")]
    public async Task ShouldCloseDialogOnEscapeKeyPressInContenteditable(IPage page, bool isFrozenWebkit)
    {
        It.Skip(isFrozenWebkit);

        await page.SetContentAsync(@"
    <dialog>
      <div contenteditable>Edit Me</div>
    </dialog>
  ");

        var dialog = page.Locator("dialog");
        var widget = dialog.Locator("[contenteditable]");
        await dialog.EvaluateAsync("node => node.showModal()");
        await Expect(dialog).ToHaveJSPropertyAsync("open", true);
        await Expect(widget).ToBeVisibleAsync();

        await widget.PressAsync("Escape");
        await Expect(dialog).ToHaveJSPropertyAsync("open", false);
        await Expect(widget).Not.ToBeVisibleAsync();
    }
}
