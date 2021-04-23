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
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
	/// <summary>
	/// Result of calling <see cref="IAccessibility.SnapshotAsync"/>.
	/// </summary>
	public partial class AccessibilitySnapshotResult
	{
		/// <summary><para>The <a href="https://www.w3.org/TR/wai-aria/#usage_intro">role</a>.</para></summary>
		[JsonPropertyName("role")]
		public string Role { get; set; }
	
		/// <summary><para>A human readable name for the node.</para></summary>
		[JsonPropertyName("name")]
		public string Name { get; set; }
	
		/// <summary><para>The current value of the node, if applicable.</para></summary>
		[JsonPropertyName("valueString")]
		public string Value { get; set; }
	
		/// <summary><para>An additional human readable description of the node, if applicable.</para></summary>
		[JsonPropertyName("description")]
		public string Description { get; set; }
	
		/// <summary><para>Keyboard shortcuts associated with this node, if applicable.</para></summary>
		[JsonPropertyName("keyshortcuts")]
		public string Keyshortcuts { get; set; }
	
		/// <summary><para>A human readable alternative to the role, if applicable.</para></summary>
		[JsonPropertyName("roledescription")]
		public string Roledescription { get; set; }
	
		/// <summary><para>A description of the current value, if applicable.</para></summary>
		[JsonPropertyName("valuetext")]
		public string Valuetext { get; set; }
	
		/// <summary><para>Whether the node is disabled, if applicable.</para></summary>
		[JsonPropertyName("disabled")]
		public bool? Disabled { get; set; }
	
		/// <summary><para>Whether the node is expanded or collapsed, if applicable.</para></summary>
		[JsonPropertyName("expanded")]
		public bool? Expanded { get; set; }
	
		/// <summary><para>Whether the node is focused, if applicable.</para></summary>
		[JsonPropertyName("focused")]
		public bool? Focused { get; set; }
	
		/// <summary>
		/// <para>
		/// Whether the node is <a href="https://en.wikipedia.org/wiki/Modal_window">modal</a>,
		/// if applicable.
		/// </para>
		/// </summary>
		[JsonPropertyName("modal")]
		public bool? Modal { get; set; }
	
		/// <summary><para>Whether the node text input supports multiline, if applicable.</para></summary>
		[JsonPropertyName("multiline")]
		public bool? Multiline { get; set; }
	
		/// <summary><para>Whether more than one child can be selected, if applicable.</para></summary>
		[JsonPropertyName("multiselectable")]
		public bool? Multiselectable { get; set; }
	
		/// <summary><para>Whether the node is read only, if applicable.</para></summary>
		[JsonPropertyName("readonly")]
		public bool? Readonly { get; set; }
	
		/// <summary><para>Whether the node is required, if applicable.</para></summary>
		[JsonPropertyName("required")]
		public bool? Required { get; set; }
	
		/// <summary><para>Whether the node is selected in its parent node, if applicable.</para></summary>
		[JsonPropertyName("selected")]
		public bool? Selected { get; set; }
	
		/// <summary><para>Whether the checkbox is checked, or "mixed", if applicable.</para></summary>
		[JsonPropertyName("checked")]
		public MixedState Checked { get; set; }
	
		/// <summary><para>Whether the toggle button is checked, or "mixed", if applicable.</para></summary>
		[JsonPropertyName("pressed")]
		public MixedState Pressed { get; set; }
	
		/// <summary><para>The level of a heading, if applicable.</para></summary>
		[JsonPropertyName("level")]
		public int? Level { get; set; }
	
		/// <summary><para>The minimum value in a node, if applicable.</para></summary>
		[JsonPropertyName("valuemin")]
		public float? Valuemin { get; set; }
	
		/// <summary><para>The maximum value in a node, if applicable.</para></summary>
		[JsonPropertyName("valuemax")]
		public float? Valuemax { get; set; }
	
		/// <summary><para>What kind of autocomplete is supported by a control, if applicable.</para></summary>
		[JsonPropertyName("autocomplete")]
		public string Autocomplete { get; set; }
	
		/// <summary><para>What kind of popup is currently being shown for a node, if applicable.</para></summary>
		[JsonPropertyName("haspopup")]
		public string Haspopup { get; set; }
	
		/// <summary><para>Whether and in what way this node's value is invalid, if applicable.</para></summary>
		[JsonPropertyName("invalid")]
		public string Invalid { get; set; }
	
		/// <summary><para>Whether the node is oriented horizontally or vertically, if applicable.</para></summary>
		[JsonPropertyName("orientation")]
		public string Orientation { get; set; }
	
		/// <summary><para>Child nodes, if any, if applicable.</para></summary>
		[JsonPropertyName("children")]
		public IEnumerable<AccessibilitySnapshotResult> Children { get; set; }
	}
}
