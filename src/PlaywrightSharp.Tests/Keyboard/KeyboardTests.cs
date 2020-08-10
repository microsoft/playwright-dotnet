using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Keyboard
{
    ///<playwright-file>keyboard.spec.js</playwright-file>
    ///<playwright-describe>Keyboard</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class KeyboardTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public KeyboardTests(ITestOutputHelper output) : base(output)
        {
        }
        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should type into a textarea</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should press the metaKey</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldPressTheMetaKey1()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/empty.html");
            await Page.EvaluateAsync<string>(@"() => {
                window.keyPromise = new Promise(resolve => document.addEventListener('keydown', event => resolve(event.key)));
            }");
            await Page.Keyboard.PressAsync("Meta");
            Assert.Equal(TestConstants.IsFirefox && !TestConstants.IsMacOSX ? "OS" : "Meta", await Page.EvaluateAsync<string>("keyPromise"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should move with the arrow keys</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should send a character with ElementHandle.press</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>ElementHandle.press should support |text| option</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ElementHandlePressShouldSupportTextOption()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.PressAsync("a", new PressOptions { Text = "ё" });
            Assert.Equal("ё", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should send a character with sendCharacter</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSendACharacterWithSendCharacter()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FocusAsync("textarea");
            await Page.Keyboard.SendCharactersAsync("嗨");
            Assert.Equal("嗨", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
            await Page.EvaluateAsync<string>("() => window.addEventListener('keydown', e => e.preventDefault(), true)");
            await Page.Keyboard.SendCharactersAsync("a");
            Assert.Equal("嗨a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should report shiftKey</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should report multiple modifiers</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should send proper codes while typing</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should send proper codes while typing with shift</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should not type canceled events</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should specify repeat property</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSpecifyRepeatProperty()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FocusAsync("textarea");
            await Page.EvaluateAsync("() => document.querySelector('textarea').addEventListener('keydown', e => window.lastEvent = e, true)");
            await Page.Keyboard.DownAsync("a");
            Assert.False(await Page.EvaluateAsync<bool>("() => window.lastEvent.repeat"));
            await Page.Keyboard.PressAsync("a");
            Assert.True(await Page.EvaluateAsync<bool>("() => window.lastEvent.repeat"));

            await Page.Keyboard.DownAsync("b");
            Assert.False(await Page.EvaluateAsync<bool>("() => window.lastEvent.repeat"));
            await Page.Keyboard.DownAsync("b");
            Assert.True(await Page.EvaluateAsync<bool>("() => window.lastEvent.repeat"));

            await Page.Keyboard.UpAsync("a");
            await Page.Keyboard.DownAsync("a");
            Assert.False(await Page.EvaluateAsync<bool>("() => window.lastEvent.repeat"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should type all kinds of characters</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTypeAllKindsOfCharacters()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FocusAsync("textarea");
            string text = "This text goes onto two lines.\nThis character is 嗨.";
            await Page.Keyboard.TypeAsync(text);
            Assert.Equal(text, await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should specify location</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSpecifyLocation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.EvaluateAsync<string>(@"() => {
                window.addEventListener('keydown', event => window.keyLocation = event.location, true);
            }");
            var textarea = await Page.QuerySelectorAsync("textarea");

            await textarea.PressAsync("Digit5");
            Assert.Equal(0, await Page.EvaluateAsync<int>("keyLocation"));

            await textarea.PressAsync("ControlLeft");
            Assert.Equal(1, await Page.EvaluateAsync<int>("keyLocation"));

            await textarea.PressAsync("ControlRight");
            Assert.Equal(2, await Page.EvaluateAsync<int>("keyLocation"));

            await textarea.PressAsync("NumpadSubtract");
            Assert.Equal(3, await Page.EvaluateAsync<int>("keyLocation"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should throw on unknown keys</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowOnUnknownKeys()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.Keyboard.PressAsync("NotARealKey"));
            Assert.Equal("Unknown key: \"NotARealKey\"", exception.Message);

            exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.Keyboard.PressAsync("ё"));
            Assert.Equal("Unknown key: \"ё\"", exception.Message);

            exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.Keyboard.PressAsync("😊"));
            Assert.Equal("Unknown key: \"😊\"", exception.Message);
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should type emoji</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTypeEmoji()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.TypeAsync("textarea", "👹 Tokyo street Japan 🇯🇵");
            Assert.Equal("👹 Tokyo street Japan 🇯🇵", await Page.QuerySelectorEvaluateAsync<string>("textarea", "textarea => textarea.value"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should type emoji into an iframe</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTypeEmojiIntoAnIframe()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "emoji-test", TestConstants.ServerUrl + "/input/textarea.html");
            var frame = Page.Frames[1];
            var textarea = await frame.QuerySelectorAsync("textarea");
            await textarea.TypeAsync("👹 Tokyo street Japan 🇯🇵");
            Assert.Equal("👹 Tokyo street Japan 🇯🇵", await frame.QuerySelectorEvaluateAsync<string>("textarea", "textarea => textarea.value"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should handle selectAll</playwright-it>
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
            Assert.Empty(await Page.QuerySelectorEvaluateAsync<string>("textarea", "textarea => textarea.value"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should be able to prevent selectAll</playwright-it>
        [SkipBrowserAndPlatformFact(skipOSX: true, skipChromium: true)]
        public async Task ShouldBeAbleToPreventSelectAll()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.TypeAsync("some text");
            await Page.QuerySelectorEvaluateAsync("textarea", @"textarea => {
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
            Assert.Equal("some tex", await Page.QuerySelectorEvaluateAsync<string>("textarea", "textarea => textarea.value"));
        }

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should press the meta key</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldPressTheMetaKey2()
        {
            await Page.EvaluateAsync<string>(@"() => {
                window.result = null;
                document.addEventListener('keydown', event => {
                    window.result = [event.key, event.code, event.metaKey];
                });
            }");
            await Page.Keyboard.PressAsync("Meta");
            object[] result = await Page.EvaluateAsync<object[]>("result");
            string key = result[0].ToString();
            string code = result[1].ToString();
            bool metaKey = ((JsonElement)result[2]).GetBoolean();

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

        ///<playwright-file>keyboard.spec.js</playwright-file>
        ///<playwright-describe>Keyboard</playwright-describe>
        ///<playwright-it>should work after a cross origin navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/empty.html");
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            await Page.EvaluateAsync<string>(@"() => {
                document.addEventListener('keydown', event => window.lastKey = event);
            }");
            await Page.Keyboard.PressAsync("a");
            Assert.Equal("a", await Page.EvaluateAsync<string>("lastKey.key"));
        }
    }
}
