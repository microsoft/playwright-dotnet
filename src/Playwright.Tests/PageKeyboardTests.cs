using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageKeyboardTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageKeyboardTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should type into a textarea")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTypeIntoATextarea()
        {
            await Page.EvaluateAsync<string>(@"() => {
                var textarea = document.createElement('textarea');
                document.body.appendChild(textarea);
                textarea.focus();
            }");
            string text = "Hello world. I am the text that was typed!";
            await Page.Keyboard.TypeAsync(text);
            Assert.Equal(text, await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should press the metaKey")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPressTheMetaKey1()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/empty.html");
            await Page.EvaluateAsync<string>(@"() => {
                window.keyPromise = new Promise(resolve => document.addEventListener('keydown', event => resolve(event.key)));
            }");
            await Page.Keyboard.PressAsync("Meta");
            Assert.Equal(TestConstants.IsFirefox && !TestConstants.IsMacOSX ? "OS" : "Meta", await Page.EvaluateAsync<string>("keyPromise"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should move with the arrow keys")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldMoveWithTheArrowKeys()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.TypeAsync("textarea", "Hello World!");
            Assert.Equal("Hello World!", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
            for (int i = 0; i < "World!".Length; i++)
            {
                _ = Page.Keyboard.PressAsync("ArrowLeft");
            }
            await Page.Keyboard.TypeAsync("inserted ");
            Assert.Equal("Hello inserted World!", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
            _ = Page.Keyboard.DownAsync("Shift");
            for (int i = 0; i < "inserted ".Length; i++)
            {
                _ = Page.Keyboard.PressAsync("ArrowLeft");
            }
            _ = Page.Keyboard.UpAsync("Shift");
            await Page.Keyboard.PressAsync("Backspace");
            Assert.Equal("Hello World!", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should send a character with ElementHandle.press")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendACharacterWithElementHandlePress()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.PressAsync("a");
            Assert.Equal("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));

            await Page.EvaluateAsync<string>("() => window.addEventListener('keydown', e => e.preventDefault(), true)");

            await textarea.PressAsync("b");
            Assert.Equal("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should send a character with sendCharacter")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendACharacterWithSendCharacter()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FocusAsync("textarea");
            await Page.Keyboard.InsertTextAsync("嗨");
            Assert.Equal("嗨", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
            await Page.EvaluateAsync<string>("() => window.addEventListener('keydown', e => e.preventDefault(), true)");
            await Page.Keyboard.InsertTextAsync("a");
            Assert.Equal("嗨a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "insertText should only emit input event")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task InsertTextShouldOnlyEmitInputEvent()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
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
            Assert.Equal(new[] { "input" }, await events.JsonValueAsync<string[]>());
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should report shiftKey")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipOSX: true)]
        public async Task ShouldReportShiftKey()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            var keyboard = Page.Keyboard;
            var codeForKey = new Dictionary<string, int> { ["Shift"] = 16, ["Alt"] = 18, ["Control"] = 17 };
            foreach (string modifierKey in codeForKey.Keys)
            {
                int modifierValue = codeForKey[modifierKey];
                await keyboard.DownAsync(modifierKey);
                Assert.Equal($"Keydown: {modifierKey} {modifierKey}Left {modifierValue} [{modifierKey}]", await Page.EvaluateAsync<string>("() => getResult()"));
                await keyboard.DownAsync("!");
                // Shift+! will generate a keypress
                if (modifierKey == "Shift")
                {
                    Assert.Equal($"Keydown: ! Digit1 49 [{modifierKey}]\nKeypress: ! Digit1 33 33 [{modifierKey}]", await Page.EvaluateAsync<string>("() => getResult()"));
                }
                else
                {
                    Assert.Equal($"Keydown: ! Digit1 49 [{modifierKey}]", await Page.EvaluateAsync<string>("() => getResult()"));
                }

                await keyboard.UpAsync("!");
                Assert.Equal($"Keyup: ! Digit1 49 [{modifierKey}]", await Page.EvaluateAsync<string>("() => getResult()"));
                await keyboard.UpAsync(modifierKey);
                Assert.Equal($"Keyup: {modifierKey} {modifierKey}Left {modifierValue} []", await Page.EvaluateAsync<string>("() => getResult()"));
            }
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should report multiple modifiers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportMultipleModifiers()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            var keyboard = Page.Keyboard;
            await keyboard.DownAsync("Control");
            Assert.Equal("Keydown: Control ControlLeft 17 [Control]", await Page.EvaluateAsync<string>("() => getResult()"));
            await keyboard.DownAsync("Alt");
            Assert.Equal("Keydown: Alt AltLeft 18 [Alt Control]", await Page.EvaluateAsync<string>("() => getResult()"));
            await keyboard.DownAsync(";");
            Assert.Equal("Keydown: ; Semicolon 186 [Alt Control]", await Page.EvaluateAsync<string>("() => getResult()"));
            await keyboard.UpAsync(";");
            Assert.Equal("Keyup: ; Semicolon 186 [Alt Control]", await Page.EvaluateAsync<string>("() => getResult()"));
            await keyboard.UpAsync("Control");
            Assert.Equal("Keyup: Control ControlLeft 17 [Alt]", await Page.EvaluateAsync<string>("() => getResult()"));
            await keyboard.UpAsync("Alt");
            Assert.Equal("Keyup: Alt AltLeft 18 []", await Page.EvaluateAsync<string>("() => getResult()"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should send proper codes while typing")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendProperCodesWhileTyping()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            await Page.Keyboard.TypeAsync("!");
            Assert.Equal(string.Join("\n",
                "Keydown: ! Digit1 49 []",
                "Keypress: ! Digit1 33 33 []",
                "Keyup: ! Digit1 49 []"), await Page.EvaluateAsync<string>("() => getResult()"));
            await Page.Keyboard.TypeAsync("^");
            Assert.Equal(string.Join("\n",
                "Keydown: ^ Digit6 54 []",
                "Keypress: ^ Digit6 94 94 []",
                "Keyup: ^ Digit6 54 []"), await Page.EvaluateAsync<string>("() => getResult()"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should send proper codes while typing with shift")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendProperCodesWhileTypingWithShift()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            var keyboard = Page.Keyboard;
            await keyboard.DownAsync("Shift");
            await Page.Keyboard.TypeAsync("~");
            Assert.Equal(string.Join("\n",
                "Keydown: Shift ShiftLeft 16 [Shift]",
                "Keydown: ~ Backquote 192 [Shift]", // 192 is ` keyCode
                "Keypress: ~ Backquote 126 126 [Shift]", // 126 is ~ charCode
                "Keyup: ~ Backquote 192 [Shift]"), await Page.EvaluateAsync<string>("() => getResult()"));
            await keyboard.UpAsync("Shift");
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should not type canceled events")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotTypeCanceledEvents()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
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
            Assert.Equal("He Wrd!", await Page.EvaluateAsync<string>("() => textarea.value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should press plus")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPressPlus()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            await Page.Keyboard.PressAsync("+");
            Assert.Equal(
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPressShiftPlus()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            await Page.Keyboard.PressAsync("Shift++");
            Assert.Equal(
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportPlusSeparatedModifiers()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            await Page.Keyboard.PressAsync("Shift+~");
            Assert.Equal(
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportMultiplePlusSeparatedModifiers()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            await Page.Keyboard.PressAsync("Control+Shift+~");
            Assert.Equal(
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldShiftRawCodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/keyboard.html");
            await Page.Keyboard.PressAsync("Shift+Digit3");
            Assert.Equal(
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSpecifyRepeatProperty()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTypeAllKindsOfCharacters()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FocusAsync("textarea");
            string text = "This text goes onto two lines.\nThis character is 嗨.";
            await Page.Keyboard.TypeAsync(text);
            Assert.Equal(text, await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should specify location")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSpecifyLocation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var lastEventHandle = await CaptureLastKeydownAsync(Page);
            var textarea = await Page.QuerySelectorAsync("textarea");

            await textarea.PressAsync("Digit5");
            Assert.Equal(0, await lastEventHandle.EvaluateAsync<int>("e => e.location"));

            await textarea.PressAsync("ControlLeft");
            Assert.Equal(1, await lastEventHandle.EvaluateAsync<int>("e => e.location"));

            await textarea.PressAsync("ControlRight");
            Assert.Equal(2, await lastEventHandle.EvaluateAsync<int>("e => e.location"));

            await textarea.PressAsync("NumpadSubtract");
            Assert.Equal(3, await lastEventHandle.EvaluateAsync<int>("e => e.location"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should press Enter")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
                Assert.Equal(expectedKey, lastEvent.key);
                Assert.Equal(expectedCode, lastEvent.code);

                string value = await Page.EvalOnSelectorAsync<string>("textarea", "t => t.value");
                Assert.Equal("\n", value);
                await Page.EvalOnSelectorAsync("textarea", "t => t.value = ''");
            }
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should throw on unknown keys")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowOnUnknownKeys()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => Page.Keyboard.PressAsync("NotARealKey"));
            Assert.Equal("Unknown key: \"NotARealKey\"", exception.Message);

            exception = await Assert.ThrowsAsync<PlaywrightException>(() => Page.Keyboard.PressAsync("ё"));
            Assert.Equal("Unknown key: \"ё\"", exception.Message);

            exception = await Assert.ThrowsAsync<PlaywrightException>(() => Page.Keyboard.PressAsync("😊"));
            Assert.Equal("Unknown key: \"😊\"", exception.Message);
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should type emoji")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTypeEmoji()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.TypeAsync("textarea", "👹 Tokyo street Japan 🇯🇵");
            Assert.Equal("👹 Tokyo street Japan 🇯🇵", await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should type emoji into an iframe")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTypeEmojiIntoAnIframe()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "emoji-test", TestConstants.ServerUrl + "/input/textarea.html");
            var frame = Page.Frames.ElementAt(1);
            var textarea = await frame.QuerySelectorAsync("textarea");
            await textarea.TypeAsync("👹 Tokyo street Japan 🇯🇵");
            Assert.Equal("👹 Tokyo street Japan 🇯🇵", await frame.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should handle selectAll")]
        [SkipBrowserAndPlatformFact(skipOSX: true, skipChromium: true)]
        public async Task ShouldHandleSelectAll()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.TypeAsync("some text");
            string modifier = TestConstants.IsMacOSX ? "Meta" : "Control";
            await Page.Keyboard.DownAsync(modifier);
            await Page.Keyboard.PressAsync("a");
            await Page.Keyboard.UpAsync(modifier);
            await Page.Keyboard.PressAsync("Backspace");
            Assert.Empty(await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should be able to prevent selectAll")]
        [SkipBrowserAndPlatformFact(skipOSX: true, skipChromium: true)]
        public async Task ShouldBeAbleToPreventSelectAll()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
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
            Assert.Equal("some tex", await Page.EvalOnSelectorAsync<string>("textarea", "textarea => textarea.value"));
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should press the meta key")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
                Assert.Equal("OS", key);
            }
            else
            {
                Assert.Equal("Meta", key);
            }

            if (TestConstants.IsFirefox)
            {
                Assert.Equal("OSLeft", code);
            }
            else
            {
                Assert.Equal("MetaLeft", code);
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/empty.html");
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            var lastEventHandle = await CaptureLastKeydownAsync(Page);
            await Page.Keyboard.PressAsync("a");
            dynamic result = await lastEventHandle.JsonValueAsync<ExpandoObject>();
            Assert.Equal("a", result.key);
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should expose keyIdentifier in webkit")]
        [SkipBrowserAndPlatformFact(skipChromium: true, skipFirefox: true)]
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
                Assert.Equal(kv.Value, await lastEventHandle.EvaluateAsync<string>("e => e.keyIdentifier"));
            }
        }

        [PlaywrightTest("page-keyboard.spec.ts", "should scroll with PageDown")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldScrollWithPageDown()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/scrollable.html");
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
}
