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
public class PageKeyboardTests : UpstreamPageTest
{
    [SetUp]
    public void Skip() => Info.Skip(IsAndroid);

    [UpstreamTest("KB-001 should type into a textarea @smoke")]
    public async Task ShouldTypeIntoATextarea()
    {
        await Page.EvaluateAsync(@"() => {
    const textarea = document.createElement('textarea');
    document.body.appendChild(textarea);
    textarea.focus();
  }");
        var text = "Hello world. I am the text that was typed!";
        await Page.Keyboard.TypeAsync(text);
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo(text));
    }

    [UpstreamTest("KB-002 should move with the arrow keys")]
    public async Task ShouldMoveWithTheArrowKeys()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.TypeAsync("textarea", "Hello World!");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("Hello World!"));
        for (var i = 0; i < "World!".Length; i++)
        {
            await Page.Keyboard.PressAsync("ArrowLeft");
        }
        await Page.Keyboard.TypeAsync("inserted ");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("Hello inserted World!"));
        await Page.Keyboard.DownAsync("Shift");
        for (var i = 0; i < "inserted ".Length; i++)
        {
            await Page.Keyboard.PressAsync("ArrowLeft");
        }
        await Page.Keyboard.UpAsync("Shift");
        await Page.Keyboard.PressAsync("Backspace");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("Hello World!"));
    }

    [UpstreamTest("KB-003 should send a character with ElementHandle.press")]
    public async Task ShouldSendACharacterWithElementHandlePress()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.PressAsync("a");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("a"));

        await Page.EvaluateAsync("() => window.addEventListener('keydown', e => e.preventDefault(), true)");

        await textarea.PressAsync("b");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("a"));
    }

    [UpstreamTest("KB-004 should send a character with insertText")]
    public async Task ShouldSendACharacterWithInsertText()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        await Page.Keyboard.InsertTextAsync("嗨");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("嗨"));
        await Page.EvaluateAsync("() => window.addEventListener('keydown', e => e.preventDefault(), true)");
        await Page.Keyboard.InsertTextAsync("a");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("嗨a"));
    }

    [UpstreamTest("KB-005 should not type text with Alt modifier")]
    public async Task ShouldNotTypeTextWithAltModifier()
    {
        await Page.SetContentAsync("<input><textarea></textarea><div contenteditable></div>");
        foreach (var locator in new[] { Page.Locator("input"), Page.Locator("textarea"), Page.Locator("div") })
        {
            await locator.PressAsync("Alt+KeyA");
            await locator.PressAsync("Alt+Digit1");
            await locator.PressAsync("Alt+Shift+KeyA");
            await Page.Keyboard.DownAsync("Alt");
            await Page.Keyboard.PressAsync("b");
            await Page.Keyboard.UpAsync("Alt");
            await locator.PressAsync("c");
        }
        await Expect(Page.Locator("input")).ToHaveValueAsync("c");
        await Expect(Page.Locator("textarea")).ToHaveValueAsync("c");
        await Expect(Page.Locator("div")).ToHaveTextAsync("c");
    }

    [UpstreamTest("KB-006 insertText should only emit input event")]
    public async Task InsertTextShouldOnlyEmitInputEvent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        // page.on('console', m => console.log(m.text()));
        var events = await Page.EvaluateHandleAsync(@"() => {
    const events = [];
    document.addEventListener('keydown', e => events.push(e.type));
    document.addEventListener('keyup', e => events.push(e.type));
    document.addEventListener('keypress', e => events.push(e.type));
    document.addEventListener('input', e => events.push(e.type));
    return events;
  }");
        await Page.Keyboard.InsertTextAsync("hello world");
        Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "input" }));
    }

    [UpstreamTest("KB-007 should emit keydown, keypress, textInput and input when typing a character")]
    public async Task ShouldEmitKeydownKeypressTextInputAndInputWhenTypingACharacter()
    {
        await Page.SetContentAsync("<input>");
        var events = await Page.EvaluateHandleAsync(@"() => {
    const events = [];
    for (const type of ['keydown', 'keypress', 'textInput', 'input', 'keyup'])
      document.querySelector('input').addEventListener(type, () => events.push(type));
    return events;
  }");
        await Page.FocusAsync("input");
        await Page.Keyboard.PressAsync("f");
        Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "keydown", "keypress", "textInput", "input", "keyup" }));
    }

    [UpstreamTest("KB-008 should dispatch key events in separate tasks")]
    public async Task ShouldDispatchKeyEventsInSeparateTasks()
    {
        Info.Skip(BrowserName == "firefox" && !IsBidi, "Firefox/Juggler dispatches keydown and keypress in the same task");
        await Page.SetContentAsync("<input>");
        var log = await Page.EvaluateHandleAsync(@"() => {
    const log = [];
    const input = document.querySelector('input');
    for (const type of ['keydown', 'keypress'])
      input.addEventListener(type, () => { log.push(type); queueMicrotask(() => log.push('microtask-' + type)); });
    input.focus();
    return log;
  }");
        await Page.Keyboard.PressAsync("a");
        // A microtask scheduled in the keydown handler must run before keypress,
        // proving each event is delivered in its own task rather than synchronously.
        Assert.That(await log.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "keydown", "microtask-keydown", "keypress", "microtask-keypress" }));
    }

    [UpstreamTest("KB-009 should report shiftKey")]
    public async Task ShouldReportShiftKey()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        var keyboard = Page.Keyboard;
        var codeForKey = new Dictionary<string, int> { ["Shift"] = 16, ["Alt"] = 18, ["Control"] = 17 };
        foreach (var modifierKey in codeForKey.Keys)
        {
            await keyboard.DownAsync(modifierKey);
            Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: " + modifierKey + " " + modifierKey + "Left LEFT [" + modifierKey + "]"));
            await keyboard.DownAsync("!");
            // Shift+! will generate a keypress
            if (modifierKey == "Shift")
            {
                Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: ! Digit1 STANDARD [" + modifierKey + "]\nKeypress: ! Digit1 STANDARD 33 [" + modifierKey + "]"));
            }
            else
            {
                Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: ! Digit1 STANDARD [" + modifierKey + "]"));
            }

            await keyboard.UpAsync("!");
            Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: ! Digit1 STANDARD [" + modifierKey + "]"));
            await keyboard.UpAsync(modifierKey);
            Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: " + modifierKey + " " + modifierKey + "Left LEFT []"));
        }
    }

    [UpstreamTest("KB-010 should report multiple modifiers")]
    public async Task ShouldReportMultipleModifiers()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        var keyboard = Page.Keyboard;
        await keyboard.DownAsync("Control");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: Control ControlLeft LEFT [Control]"));
        await keyboard.DownAsync("Alt");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: Alt AltLeft LEFT [Alt Control]"));
        await keyboard.DownAsync(";");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keydown: ; Semicolon STANDARD [Alt Control]"));
        await keyboard.UpAsync(";");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: ; Semicolon STANDARD [Alt Control]"));
        await keyboard.UpAsync("Control");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: Control ControlLeft LEFT [Alt]"));
        await keyboard.UpAsync("Alt");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo("Keyup: Alt AltLeft LEFT []"));
    }

    [UpstreamTest("KB-011 should send proper codes while typing")]
    public async Task ShouldSendProperCodesWhileTyping()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.TypeAsync("!");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: ! Digit1 STANDARD []",
                "Keypress: ! Digit1 STANDARD 33 []",
                "Keyup: ! Digit1 STANDARD []",
            })));
        await Page.Keyboard.TypeAsync("^");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: ^ Digit6 STANDARD []",
                "Keypress: ^ Digit6 STANDARD 94 []",
                "Keyup: ^ Digit6 STANDARD []",
            })));
    }

    [UpstreamTest("KB-012 should send proper codes while typing with shift")]
    public async Task ShouldSendProperCodesWhileTypingWithShift()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        var keyboard = Page.Keyboard;
        await keyboard.DownAsync("Shift");
        await Page.Keyboard.TypeAsync("~");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
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
    public async Task ShouldNotTypeCanceledEvents()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        await Page.EvaluateAsync(@"() => {
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
        Assert.That(await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("He Wrd!"));
    }

    [UpstreamTest("KB-014 should press plus")]
    public async Task ShouldPressPlus()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("+");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: + Equal STANDARD []",
                "Keypress: + Equal STANDARD 43 []",
                "Keyup: + Equal STANDARD []",
            })));
    }

    [UpstreamTest("KB-015 should press shift plus")]
    public async Task ShouldPressShiftPlus()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Shift++");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
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
    public async Task ShouldSupportPlusSeparatedModifiers()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Shift+~");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
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
    public async Task ShouldSupportMultiplePlusSeparatedModifiers()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Control+Shift+~");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
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
    public async Task ShouldShiftRawCodes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Shift+Digit3");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
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
    public async Task ShouldSpecifyRepeatProperty()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        var lastEvent = await CaptureLastKeydown(Page);
        await Page.Keyboard.DownAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(false));
        await Page.Keyboard.PressAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(true));

        await Page.Keyboard.DownAsync("b");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(false));
        await Page.Keyboard.DownAsync("b");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(true));

        await Page.Keyboard.UpAsync("a");
        await Page.Keyboard.DownAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<bool>("e => e.repeat"), Is.EqualTo(false));
    }

    [UpstreamTest("KB-020 should type all kinds of characters")]
    public async Task ShouldTypeAllKindsOfCharacters()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        var text = "This text goes onto two lines.\nThis character is 嗨.";
        await Page.Keyboard.TypeAsync(text);
        Assert.That(await Page.EvalOnSelectorAsync<string>("textarea", "t => t.value"), Is.EqualTo(text));
    }

    [UpstreamTest("KB-021 should specify location")]
    public async Task ShouldSpecifyLocation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var lastEvent = await CaptureLastKeydown(Page);
        var textarea = await Page.QuerySelectorAsync("textarea");

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
    public async Task ShouldSpecifyNumpadLocationOnKeyup()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("NumpadSubtract");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(
            string.Join("\n", new[]
            {
                "Keydown: - NumpadSubtract NUMPAD []",
                "Keypress: - NumpadSubtract NUMPAD 45 []",
                "Keyup: - NumpadSubtract NUMPAD []",
            })));
    }

    [UpstreamTest("KB-023 should press NumpadDecimal as Delete")]
    public async Task ShouldPressNumpadDecimalAsDelete()
    {
        await Page.SetContentAsync("<textarea></textarea>");
        var lastEvent = await CaptureLastKeydown(Page);
        var textarea = Page.Locator("textarea");

        await textarea.PressAsync("NumpadDecimal");
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.key"), Is.EqualTo("Delete"));
        Assert.That(await lastEvent.EvaluateAsync<string>("e => e.code"), Is.EqualTo("NumpadDecimal"));
        Assert.That(await lastEvent.EvaluateAsync<int>("e => e.location"), Is.EqualTo(3));
        await Expect(textarea).ToHaveValueAsync("");
    }

    [UpstreamTest("KB-024 should type numpad digits with Shift")]
    public async Task ShouldTypeNumpadDigitsWithShift()
    {
        await Page.SetContentAsync("<textarea></textarea>");
        var textarea = Page.Locator("textarea");
        await textarea.PressAsync("Shift+Numpad1");
        await textarea.PressAsync("Shift+NumpadDecimal");
        await textarea.PressAsync("Shift+Numpad9");
        await Expect(textarea).ToHaveValueAsync("1.9");
    }

    [UpstreamTest("KB-025 should press Enter")]
    public async Task ShouldPressEnter()
    {
        await Page.SetContentAsync("<textarea></textarea>");
        await Page.FocusAsync("textarea");
        var lastEventHandle = await CaptureLastKeydown(Page);
        await testEnterKey("Enter", "Enter", "Enter");
        await testEnterKey("NumpadEnter", "Enter", "NumpadEnter");
        await testEnterKey("\n", "Enter", "Enter");
        await testEnterKey("\r", "Enter", "Enter");

        async Task testEnterKey(string key, string expectedKey, string expectedCode)
        {
            await Page.Keyboard.PressAsync(key);
            var lastEvent = await lastEventHandle.JsonValueAsync<JsonElement>();
            Assert.That(lastEvent.GetProperty("key").GetString(), Is.EqualTo(expectedKey)); // had the wrong key
            Assert.That(lastEvent.GetProperty("code").GetString(), Is.EqualTo(expectedCode)); // had the wrong code
            var value = await Page.EvalOnSelectorAsync<string>("textarea", "t => t.value");
            Assert.That(value, Is.EqualTo("\n")); // failed to create a newline
            await Page.EvalOnSelectorAsync("textarea", "t => t.value = ''");
        }
    }

    [UpstreamTest("KB-026 should press audio and media control keys")]
    public async Task ShouldPressAudioAndMediaControlKeys()
    {
        await Page.SetContentAsync("<input autofocus>");
        await Page.FocusAsync("input");
        var lastEvent = await CaptureLastKeydown(Page);
        var mediaKeys = new[]
        {
            new { key = "AudioVolumeMute", code = BrowserName == "firefox" ? "VolumeMute" : "AudioVolumeMute" },
            new { key = "AudioVolumeDown", code = BrowserName == "firefox" ? "VolumeDown" : "AudioVolumeDown" },
            new { key = "AudioVolumeUp", code = BrowserName == "firefox" ? "VolumeUp" : "AudioVolumeUp" },
            new { key = "MediaTrackNext", code = "MediaTrackNext" },
            new { key = "MediaTrackPrevious", code = "MediaTrackPrevious" },
            new { key = "MediaPlayPause", code = "MediaPlayPause" },
        };

        foreach (var mediaKey in mediaKeys)
        {
            await Page.Keyboard.PressAsync(mediaKey.key);
            Assert.That(await lastEvent.EvaluateAsync<string>("e => e.key"), Is.EqualTo(mediaKey.key));
            Assert.That(await lastEvent.EvaluateAsync<string>("e => e.code"), Is.EqualTo(mediaKey.code));
            Assert.That(await lastEvent.EvaluateAsync<int>("e => e.location"), Is.EqualTo(0));
        }
    }

    [UpstreamTest("KB-027 should throw on unknown keys")]
    public async Task ShouldThrowOnUnknownKeys()
    {
        var error = await Catch(() => Page.Keyboard.PressAsync("NotARealKey"));
        Assert.That(error.Message, Does.Contain("Unknown key: \"NotARealKey\""));

        error = await Catch(() => Page.Keyboard.PressAsync("ё"));
        Assert.That(error?.Message, Does.Contain("Unknown key: \"ё\""));

        error = await Catch(() => Page.Keyboard.PressAsync("😊"));
        Assert.That(error?.Message, Does.Contain("Unknown key: \"😊\""));
    }

    [UpstreamTest("KB-028 should type emoji")]
    public async Task ShouldTypeEmoji()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.TypeAsync("textarea", "👹 Tokyo street Japan 🇯🇵");
        Assert.That(await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("👹 Tokyo street Japan 🇯🇵"));
    }

    [UpstreamTest("KB-029 should type emoji into an iframe")]
    public async Task ShouldTypeEmojiIntoAnIframe()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "emoji-test", Server.Prefix + "/input/textarea.html");
        var frame = Page.Frames[1];
        var textarea = await frame.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("👹 Tokyo street Japan 🇯🇵");
        Assert.That(await frame.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("👹 Tokyo street Japan 🇯🇵"));
    }

    [UpstreamTest("KB-030 should handle selectAll")]
    public async Task ShouldHandleSelectAll()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("some text");
        await Page.Keyboard.DownAsync("ControlOrMeta");
        await Page.Keyboard.PressAsync("a");
        await Page.Keyboard.UpAsync("ControlOrMeta");
        await Page.Keyboard.PressAsync("Backspace");
        Assert.That(await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo(""));
    }

    [UpstreamTest("KB-031 pressing Meta should not result in any text insertion on any platform")]
    public async Task PressingMetaShouldNotResultInAnyTextInsertionOnAnyPlatform()
    {
        await Page.SetContentAsync("<input type=\"text\" value=\"hello world\">");
        var input = Page.Locator("input");
        await Expect(input).ToHaveValueAsync("hello world");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Meta");
        await Expect(input).ToHaveValueAsync("hello world");
    }

    [UpstreamTest("KB-032 should be able to prevent selectAll")]
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
        await Page.Keyboard.DownAsync("ControlOrMeta");
        await Page.Keyboard.PressAsync("a");
        await Page.Keyboard.UpAsync("ControlOrMeta");
        await Page.Keyboard.PressAsync("Backspace");
        Assert.That(await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("some tex"));
    }

    [UpstreamTest("KB-033 should support MacOS shortcuts")]
    public async Task ShouldSupportMacOSShortcuts()
    {
        Info.Skip(Platform != "darwin");
        // @see https://github.com/microsoft/playwright/issues/5721
        Info.Fixme(BrowserName == "firefox" && Platform == "darwin");

        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.TypeAsync("some text");
        // select one word backwards
        await Page.Keyboard.PressAsync("Shift+Control+Alt+KeyB");
        await Page.Keyboard.PressAsync("Backspace");
        Assert.That(await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"), Is.EqualTo("some "));
    }

    [UpstreamTest("KB-034 should press the meta key")]
    public async Task ShouldPressTheMetaKey()
    {
        var lastEvent = await CaptureLastKeydown(Page);
        await Page.Keyboard.PressAsync("Meta");
        var value = await lastEvent.JsonValueAsync<JsonElement>();
        var key = value.GetProperty("key").GetString();
        var code = value.GetProperty("code").GetString();
        var metaKey = value.GetProperty("metaKey").GetBoolean();
        Assert.That(key, Is.EqualTo("Meta"));
        Assert.That(code, Is.EqualTo("MetaLeft"));
        Assert.That(metaKey, Is.EqualTo(true));
    }

    [UpstreamTest("KB-035 should work with keyboard events with empty.html")]
    public async Task ShouldWorkWithKeyboardEventsWithEmptyHtml()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var lastEvent = await CaptureLastKeydown(Page);
        await Page.Keyboard.PressAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<string>("l => l.key"), Is.EqualTo("a"));
    }

    [UpstreamTest("KB-036 should work after a cross origin navigation")]
    public async Task ShouldWorkAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        var lastEvent = await CaptureLastKeydown(Page);
        await Page.Keyboard.PressAsync("a");
        Assert.That(await lastEvent.EvaluateAsync<string>("l => l.key"), Is.EqualTo("a"));
    }

    [UpstreamTest("KB-037 should expose keyIdentifier in webkit")]
    public async Task ShouldExposeKeyIdentifierInWebkit()
    {
        Info.Skip(BrowserName != "webkit", "event.keyIdentifier has been removed from all browsers except WebKit");

        var lastEvent = await CaptureLastKeydown(Page);
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
            await Page.Keyboard.PressAsync(key);
            Assert.That(await lastEvent.EvaluateAsync<string>("e => e.keyIdentifier"), Is.EqualTo(keyIdentifier));
        }
    }

    [UpstreamTest("KB-038 should scroll with PageDown")]
    public async Task ShouldScrollWithPageDown()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        // A click is required for WebKit to send the event into the body.
        await Page.ClickAsync("body");
        await Page.Keyboard.PressAsync("PageDown");
        // We can't wait for the scroll to finish, so just wait for it to start.
        await Page.WaitForFunctionAsync("() => scrollY > 0");
    }

    [UpstreamTest("KB-039 should move around the selection in a contenteditable")]
    public async Task ShouldMoveAroundTheSelectionInAContenteditable()
    {
        await Page.SetContentAsync("<div contenteditable></div>");
        await Page.FocusAsync("div");
        var modifier = IsMac ? "Alt" : "Control";
        await Page.Keyboard.TypeAsync("Hello World");
        await Page.Keyboard.DownAsync(modifier);
        await Page.Keyboard.DownAsync("Shift");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.Keyboard.UpAsync("Shift");
        await Page.Keyboard.UpAsync(modifier);
        Assert.That(await Page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("World"));
    }

    [UpstreamTest("KB-040 should move to the start of the document")]
    public async Task ShouldMoveToTheStartOfTheDocument()
    {
        Info.Skip(!IsMac);
        await Page.SetContentAsync("<div contenteditable></div>");
        await Page.FocusAsync("div");
        await Page.Keyboard.TypeAsync("1\n2\n3\n");
        await Page.Keyboard.PressAsync("Shift+Meta+ArrowUp");
        Assert.That(await Page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("1\n2\n3\n"));
    }

    [UpstreamTest("KB-041 should dispatch a click event on a button when Space gets pressed")]
    public async Task ShouldDispatchAClickEventOnAButtonWhenSpaceGetsPressed()
    {
        await Page.SetContentAsync("<button type=\"button\">a11y</button>");
        var actual = await Page.EvaluateHandleAsync(@"() => {
    const actual = { clicked: false };
    document.querySelector('button').addEventListener('click', () => (actual.clicked = true));
    return actual;
  }");
        await Page.FocusAsync("button");
        await Page.Keyboard.PressAsync("Space");
        Assert.That((await actual.JsonValueAsync<JsonElement>()).GetProperty("clicked").GetBoolean(), Is.EqualTo(true));
    }

    [UpstreamTest("KB-042 should dispatch a click event on a button when Enter gets pressed")]
    public async Task ShouldDispatchAClickEventOnAButtonWhenEnterGetsPressed()
    {
        await Page.SetContentAsync("<button type=\"button\">a11y</button>");
        var actual = await Page.EvaluateHandleAsync(@"() => {
    const actual = { clicked: false };
    document.querySelector('button').addEventListener('click', () => (actual.clicked = true));
    return actual;
  }");
        await Page.FocusAsync("button");
        await Page.Keyboard.PressAsync("Enter");
        Assert.That((await actual.JsonValueAsync<JsonElement>()).GetProperty("clicked").GetBoolean(), Is.EqualTo(true));
    }

    [UpstreamTest("KB-043 should support simple copy-pasting")]
    public async Task ShouldSupportSimpleCopyPasting()
    {
        await Page.SetContentAsync("<div contenteditable>123</div>");
        await Page.FocusAsync("div");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyA");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyC");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyV");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyV");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"), Is.EqualTo("123123"));
    }

    [UpstreamTest("KB-044 should support simple cut-pasting")]
    public async Task ShouldSupportSimpleCutPasting()
    {
        await Page.SetContentAsync("<div contenteditable>123</div>");
        await Page.FocusAsync("div");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyA");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyX");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyV");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyV");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"), Is.EqualTo("123123"));
    }

    [UpstreamTest("KB-045 should support undo-redo")]
    public async Task ShouldSupportUndoRedo()
    {
        Info.Fixme((BrowserName == "webkit" && IsLinux) || Channel == "webkit-wsl", "https://github.com/microsoft/playwright/issues/12000");
        await Page.SetContentAsync("<div contenteditable></div>");
        var div = Page.Locator("div");
        await Expect(div).ToHaveTextAsync("");
        await div.TypeAsync("123");
        await Expect(div).ToHaveTextAsync("123");
        await Page.Keyboard.PressAsync("ControlOrMeta+KeyZ");
        await Expect(div).ToHaveTextAsync("");
        await Page.Keyboard.PressAsync("Shift+ControlOrMeta+KeyZ");
        await Expect(div).ToHaveTextAsync("123");
    }

    [UpstreamTest("KB-046 should type repeatedly in contenteditable in shadow dom")]
    public async Task ShouldTypeRepeatedlyInContenteditableInShadowDom()
    {
        await Page.SetContentAsync(@"
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

        var editor = Page.Locator("shadow-element > .editor").First;
        await editor.TypeAsync("This is the first box.");

        var sectionEditor = Page.Locator("section .editor");
        await sectionEditor.TypeAsync("This is the second box.");

        Assert.That(await editor.TextContentAsync(), Is.EqualTo("This is the first box."));
        Assert.That(await sectionEditor.TextContentAsync(), Is.EqualTo("This is the second box."));
    }

    [UpstreamTest("KB-047 should type repeatedly in contenteditable in shadow dom with nested elements")]
    public async Task ShouldTypeRepeatedlyInContenteditableInShadowDomWithNestedElements()
    {
        await Page.SetContentAsync(@"
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

        var editor = Page.Locator("shadow-element > .editor").First;
        await editor.TypeAsync("This is the first box: ");

        var sectionEditor = Page.Locator("section .editor");
        await sectionEditor.TypeAsync("This is the second box: ");

        Assert.That(await editor.TextContentAsync(), Is.EqualTo("This is the first box: hello"));
        Assert.That(await sectionEditor.TextContentAsync(), Is.EqualTo("This is the second box: world"));
    }

    [UpstreamTest("KB-048 should type repeatedly in input in shadow dom")]
    public async Task ShouldTypeRepeatedlyInInputInShadowDom()
    {
        await Page.SetContentAsync(@"
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

        var editor = Page.Locator("shadow-element > .editor").First;
        await editor.TypeAsync("This is the first box.");

        var sectionEditor = Page.Locator("section .editor");
        await sectionEditor.TypeAsync("This is the second box.");

        Assert.That(await editor.InputValueAsync(), Is.EqualTo("This is the first box."));
        Assert.That(await sectionEditor.InputValueAsync(), Is.EqualTo("This is the second box."));
    }

    [UpstreamTest("KB-049 type to non-focusable element should maintain old focus")]
    public async Task TypeToNonFocusableElementShouldMaintainOldFocus()
    {
        await Page.SetContentAsync(@"
    <div id=""focusable"" tabindex=""0"">focusable div</div>
    <div id=""non-focusable-and-non-editable"">non-editable, non-focusable</div>
  ");

        await Page.Locator("#focusable").FocusAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement?.id"), Is.EqualTo("focusable"));
        await Page.Locator("#non-focusable-and-non-editable").TypeAsync("foo");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement?.id"), Is.EqualTo("focusable"));
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
    public async Task ShouldDispatchInsertTextAfterContextMenuWasOpened()
    {
        Info.Skip(BrowserName == "chromium" && IsWindows, "context menu support is best-effort for Linux and MacOS");
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.EvaluateAsync(@"() => {
    window['contextMenuPromise'] = new Promise(x => {
      window.addEventListener('contextmenu', x, false);
    });
  }");

        var box = await Page.Locator("textarea").BoundingBoxAsync();
        var cx = box.X + (box.Width / 2);
        var cy = box.Y + (box.Height / 2);
        await Page.Mouse.ClickAsync(cx, cy, new() { Button = MouseButton.Right });
        await Page.EvaluateAsync("() => window['contextMenuPromise']");

        await Page.Keyboard.InsertTextAsync("嗨");
        await Expect(async () => Assert.That(await Page.Locator("textarea").InputValueAsync(), Is.EqualTo("嗨"))).ToPassAsync();
    }

    [UpstreamTest("KB-051 should type after context menu was opened")]
    public async Task ShouldTypeAfterContextMenuWasOpened()
    {
        Info.Skip(BrowserName == "chromium" && IsWindows, "context menu support is best-effort for Linux and MacOS");
        await Page.EvaluateAsync(@"() => {
    window['keys'] = [];
    window.addEventListener('keydown', event => window['keys'].push(event.key));
    window['contextMenuPromise'] = new Promise(x => {
      window.addEventListener('contextmenu', x, false);
    });
  }");

        await Page.Mouse.MoveAsync(100, 100);
        await Page.Mouse.DownAsync(new() { Button = MouseButton.Right });
        await Page.EvaluateAsync("() => window['contextMenuPromise']");

        await Page.Keyboard.DownAsync("ArrowDown");

        await Expect(async () => Assert.That(await Page.EvaluateAsync<string[]>("window.keys"), Is.EqualTo(new[] { "ArrowDown" }))).ToPassAsync();
    }

    [UpstreamTest("KB-052 should have correct Keydown/Keyup order when pressing Escape key")]
    public async Task ShouldHaveCorrectKeydownKeyupOrderWhenPressingEscapeKey()
    {
        await Page.GotoAsync(Server.Prefix + "/input/keyboard.html");
        await Page.Keyboard.PressAsync("Escape");
        Assert.That(await Page.EvaluateAsync<string>("getResult()"), Is.EqualTo(@"
Keydown: Escape Escape STANDARD []
Keyup: Escape Escape STANDARD []
".Trim()));
    }

    [UpstreamTest("KB-053 should close dialog on Escape key press in contenteditable")]
    public async Task ShouldCloseDialogOnEscapeKeyPressInContenteditable()
    {
        Info.Skip(IsFrozenWebkit);

        await Page.SetContentAsync(@"
    <dialog>
      <div contenteditable>Edit Me</div>
    </dialog>
  ");

        var dialog = Page.Locator("dialog");
        var widget = dialog.Locator("[contenteditable]");
        await dialog.EvaluateAsync("node => node.showModal()");
        await Expect(dialog).ToHaveJSPropertyAsync("open", true);
        await Expect(widget).ToBeVisibleAsync();

        await widget.PressAsync("Escape");
        await Expect(dialog).ToHaveJSPropertyAsync("open", false);
        await Expect(widget).Not.ToBeVisibleAsync();
    }
}
