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
 *
 *
 * ------------------------------------------------------------------------------ 
 * <auto-generated> 
 * This code was generated by a tool at:
 * /utils/doclint/generateDotnetApi.js
 * 
 * Changes to this file may cause incorrect behavior and will be lost if 
 * the code is regenerated. 
 * </auto-generated> 
 * ------------------------------------------------------------------------------
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
	/// Keyboard provides an api for managing a virtual keyboard. The high level api is <see cref="IKeyboard.TypeAsync"/>, which
	/// takes raw characters and generates proper keydown, keypress/input, and keyup events on your page.
	/// For finer control, you can use <see cref="IKeyboard.DownAsync"/>, <see cref="IKeyboard.UpAsync"/>, and 
	/// <see cref="IKeyboard.InsertTextAsync"/> to manually fire events as if they were generated from a real keyboard.
	/// An example of holding down `Shift` in order to select and delete some text:
	/// An example of pressing uppercase `A`
	/// An example to trigger select-all with the keyboard
	/// </summary>
	public partial interface IKeyboard
	{
		/// <summary>
		/// Dispatches a `keydown` event.
		/// {PARAM} can specify the intended <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key">keyboardEvent.key</a> value
		/// or a single character to generate the text for. A superset of the {PARAM} values can be found <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key/Key_Values">here</a>.
		/// Examples of the keys are:
		/// `F1` - `F12`, `Digit0`- `Digit9`, `KeyA`- `KeyZ`, `Backquote`, `Minus`, `Equal`, `Backslash`, `Backspace`, `Tab`, `Delete`,
		/// `Escape`, `ArrowDown`, `End`, `Enter`, `Home`, `Insert`, `PageDown`, `PageUp`, `ArrowRight`, `ArrowUp`, etc.
		/// Following modification shortcuts are also supported: `Shift`, `Control`, `Alt`, `Meta`, `ShiftLeft`.
		/// Holding down `Shift` will type the text that corresponds to the {PARAM} in the upper case.
		/// If {PARAM} is a single character, it is case-sensitive, so the values `a` and `A` will generate different respective texts.
		/// If {PARAM} is a modifier key, `Shift`, `Meta`, `Control`, or `Alt`, subsequent key presses will be sent with that modifier
		/// active. To release the modifier key, use <see cref="IKeyboard.UpAsync"/>.
		/// After the key is pressed once, subsequent calls to <see cref="IKeyboard.DownAsync"/> will have <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/repeat">repeat</a> set
		/// to true. To release the key, use <see cref="IKeyboard.UpAsync"/>.
		/// </summary>
		Task DownAsync(string key);
		/// <summary>
		/// Dispatches only `input` event, does not emit the `keydown`, `keyup` or `keypress` events.
		/// </summary>
		Task InsertTextAsync(string text);
		/// <summary>
		/// {PARAM} can specify the intended <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key">keyboardEvent.key</a> value
		/// or a single character to generate the text for. A superset of the {PARAM} values can be found <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key/Key_Values">here</a>.
		/// Examples of the keys are:
		/// `F1` - `F12`, `Digit0`- `Digit9`, `KeyA`- `KeyZ`, `Backquote`, `Minus`, `Equal`, `Backslash`, `Backspace`, `Tab`, `Delete`,
		/// `Escape`, `ArrowDown`, `End`, `Enter`, `Home`, `Insert`, `PageDown`, `PageUp`, `ArrowRight`, `ArrowUp`, etc.
		/// Following modification shortcuts are also supported: `Shift`, `Control`, `Alt`, `Meta`, `ShiftLeft`.
		/// Holding down `Shift` will type the text that corresponds to the {PARAM} in the upper case.
		/// If {PARAM} is a single character, it is case-sensitive, so the values `a` and `A` will generate different respective texts.
		/// Shortcuts such as `key: "Control+o"` or `key: "Control+Shift+T"` are supported as well. When speficied with the modifier,
		/// modifier is pressed and being held while the subsequent key is being pressed.
		/// Shortcut for <see cref="IKeyboard.DownAsync"/> and <see cref="IKeyboard.UpAsync"/>.
		/// </summary>
		Task PressAsync(string key, decimal delay);
		/// <summary>
		/// Sends a `keydown`, `keypress`/`input`, and `keyup` event for each character in the text.
		/// To press a special key, like `Control` or `ArrowDown`, use <see cref="IKeyboard.PressAsync"/>.
		/// </summary>
		Task TypeAsync(string text, decimal delay);
		/// <summary>
		/// Dispatches a `keyup` event.
		/// </summary>
		Task UpAsync(string key);
	}
}