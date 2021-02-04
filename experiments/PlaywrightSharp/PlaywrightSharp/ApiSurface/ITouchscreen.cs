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
	/// The Touchscreen class operates in main-frame CSS pixels relative to the top-left corner of the viewport. Methods on the touchscreen
	/// can only be used in browser contexts that have been intialized with `hasTouch` set to true.
	/// </summary>
	public partial interface ITouchscreen
	{
		/// <summary>
		/// Dispatches a `touchstart` and `touchend` event with a single touch at the position ({PARAM},{PARAM}).
		/// </summary>
		Task TapAsync(decimal x, decimal y);
	}
}