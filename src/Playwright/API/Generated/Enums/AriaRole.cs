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
 */

using System.Runtime.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public enum AriaRole
{
    [EnumMember(Value = "alert")]
    Alert,
    [EnumMember(Value = "alertdialog")]
    Alertdialog,
    [EnumMember(Value = "application")]
    Application,
    [EnumMember(Value = "article")]
    Article,
    [EnumMember(Value = "banner")]
    Banner,
    [EnumMember(Value = "blockquote")]
    Blockquote,
    [EnumMember(Value = "button")]
    Button,
    [EnumMember(Value = "caption")]
    Caption,
    [EnumMember(Value = "cell")]
    Cell,
    [EnumMember(Value = "checkbox")]
    Checkbox,
    [EnumMember(Value = "code")]
    Code,
    [EnumMember(Value = "columnheader")]
    Columnheader,
    [EnumMember(Value = "combobox")]
    Combobox,
    [EnumMember(Value = "complementary")]
    Complementary,
    [EnumMember(Value = "contentinfo")]
    Contentinfo,
    [EnumMember(Value = "definition")]
    Definition,
    [EnumMember(Value = "deletion")]
    Deletion,
    [EnumMember(Value = "dialog")]
    Dialog,
    [EnumMember(Value = "directory")]
    Directory,
    [EnumMember(Value = "document")]
    Document,
    [EnumMember(Value = "emphasis")]
    Emphasis,
    [EnumMember(Value = "feed")]
    Feed,
    [EnumMember(Value = "figure")]
    Figure,
    [EnumMember(Value = "form")]
    Form,
    [EnumMember(Value = "generic")]
    Generic,
    [EnumMember(Value = "grid")]
    Grid,
    [EnumMember(Value = "gridcell")]
    Gridcell,
    [EnumMember(Value = "group")]
    Group,
    [EnumMember(Value = "heading")]
    Heading,
    [EnumMember(Value = "img")]
    Img,
    [EnumMember(Value = "insertion")]
    Insertion,
    [EnumMember(Value = "link")]
    Link,
    [EnumMember(Value = "list")]
    List,
    [EnumMember(Value = "listbox")]
    Listbox,
    [EnumMember(Value = "listitem")]
    Listitem,
    [EnumMember(Value = "log")]
    Log,
    [EnumMember(Value = "main")]
    Main,
    [EnumMember(Value = "marquee")]
    Marquee,
    [EnumMember(Value = "math")]
    Math,
    [EnumMember(Value = "meter")]
    Meter,
    [EnumMember(Value = "menu")]
    Menu,
    [EnumMember(Value = "menubar")]
    Menubar,
    [EnumMember(Value = "menuitem")]
    Menuitem,
    [EnumMember(Value = "menuitemcheckbox")]
    Menuitemcheckbox,
    [EnumMember(Value = "menuitemradio")]
    Menuitemradio,
    [EnumMember(Value = "navigation")]
    Navigation,
    [EnumMember(Value = "none")]
    None,
    [EnumMember(Value = "note")]
    Note,
    [EnumMember(Value = "option")]
    Option,
    [EnumMember(Value = "paragraph")]
    Paragraph,
    [EnumMember(Value = "presentation")]
    Presentation,
    [EnumMember(Value = "progressbar")]
    Progressbar,
    [EnumMember(Value = "radio")]
    Radio,
    [EnumMember(Value = "radiogroup")]
    Radiogroup,
    [EnumMember(Value = "region")]
    Region,
    [EnumMember(Value = "row")]
    Row,
    [EnumMember(Value = "rowgroup")]
    Rowgroup,
    [EnumMember(Value = "rowheader")]
    Rowheader,
    [EnumMember(Value = "scrollbar")]
    Scrollbar,
    [EnumMember(Value = "search")]
    Search,
    [EnumMember(Value = "searchbox")]
    Searchbox,
    [EnumMember(Value = "separator")]
    Separator,
    [EnumMember(Value = "slider")]
    Slider,
    [EnumMember(Value = "spinbutton")]
    Spinbutton,
    [EnumMember(Value = "status")]
    Status,
    [EnumMember(Value = "strong")]
    Strong,
    [EnumMember(Value = "subscript")]
    Subscript,
    [EnumMember(Value = "superscript")]
    Superscript,
    [EnumMember(Value = "switch")]
    Switch,
    [EnumMember(Value = "tab")]
    Tab,
    [EnumMember(Value = "table")]
    Table,
    [EnumMember(Value = "tablist")]
    Tablist,
    [EnumMember(Value = "tabpanel")]
    Tabpanel,
    [EnumMember(Value = "term")]
    Term,
    [EnumMember(Value = "textbox")]
    Textbox,
    [EnumMember(Value = "time")]
    Time,
    [EnumMember(Value = "timer")]
    Timer,
    [EnumMember(Value = "toolbar")]
    Toolbar,
    [EnumMember(Value = "tooltip")]
    Tooltip,
    [EnumMember(Value = "tree")]
    Tree,
    [EnumMember(Value = "treegrid")]
    Treegrid,
    [EnumMember(Value = "treeite")]
    Treeite,
}

#nullable disable
