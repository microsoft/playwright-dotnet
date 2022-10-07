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

using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public class PagePdfOptions
{
    public PagePdfOptions() { }

    public PagePdfOptions(PagePdfOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        DisplayHeaderFooter = clone.DisplayHeaderFooter;
        FooterTemplate = clone.FooterTemplate;
        Format = clone.Format;
        HeaderTemplate = clone.HeaderTemplate;
        Height = clone.Height;
        Landscape = clone.Landscape;
        Margin = clone.Margin;
        PageRanges = clone.PageRanges;
        Path = clone.Path;
        PreferCSSPageSize = clone.PreferCSSPageSize;
        PrintBackground = clone.PrintBackground;
        Scale = clone.Scale;
        Width = clone.Width;
    }

    /// <summary><para>Display header and footer. Defaults to <c>false</c>.</para></summary>
    [JsonPropertyName("displayHeaderFooter")]
    public bool? DisplayHeaderFooter { get; set; }

    /// <summary>
    /// <para>
    /// HTML template for the print footer. Should use the same format as the <paramref
    /// name="headerTemplate"/>.
    /// </para>
    /// </summary>
    [JsonPropertyName("footerTemplate")]
    public string? FooterTemplate { get; set; }

    /// <summary>
    /// <para>
    /// Paper format. If set, takes priority over <paramref name="width"/> or <paramref
    /// name="height"/> options. Defaults to 'Letter'.
    /// </para>
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// <para>
    /// HTML template for the print header. Should be valid HTML markup with following classes
    /// used to inject printing values into them:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>'date'</c> formatted print date</description></item>
    /// <item><description><c>'title'</c> document title</description></item>
    /// <item><description><c>'url'</c> document location</description></item>
    /// <item><description><c>'pageNumber'</c> current page number</description></item>
    /// <item><description><c>'totalPages'</c> total pages in the document</description></item>
    /// </list>
    /// </summary>
    [JsonPropertyName("headerTemplate")]
    public string? HeaderTemplate { get; set; }

    /// <summary><para>Paper height, accepts values labeled with units.</para></summary>
    [JsonPropertyName("height")]
    public string? Height { get; set; }

    /// <summary><para>Paper orientation. Defaults to <c>false</c>.</para></summary>
    [JsonPropertyName("landscape")]
    public bool? Landscape { get; set; }

    /// <summary><para>Paper margins, defaults to none.</para></summary>
    [JsonPropertyName("margin")]
    public Margin? Margin { get; set; }

    /// <summary>
    /// <para>
    /// Paper ranges to print, e.g., '1-5, 8, 11-13'. Defaults to the empty string, which
    /// means print all pages.
    /// </para>
    /// </summary>
    [JsonPropertyName("pageRanges")]
    public string? PageRanges { get; set; }

    /// <summary>
    /// <para>
    /// The file path to save the PDF to. If <paramref name="path"/> is a relative path,
    /// then it is resolved relative to the current working directory. If no path is provided,
    /// the PDF won't be saved to the disk.
    /// </para>
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// <para>
    /// Give any CSS <c>@page</c> size declared in the page priority over what is declared
    /// in <paramref name="width"/> and <paramref name="height"/> or <paramref name="format"/>
    /// options. Defaults to <c>false</c>, which will scale the content to fit the paper
    /// size.
    /// </para>
    /// </summary>
    [JsonPropertyName("preferCSSPageSize")]
    public bool? PreferCSSPageSize { get; set; }

    /// <summary><para>Print background graphics. Defaults to <c>false</c>.</para></summary>
    [JsonPropertyName("printBackground")]
    public bool? PrintBackground { get; set; }

    /// <summary>
    /// <para>
    /// Scale of the webpage rendering. Defaults to <c>1</c>. Scale amount must be between
    /// 0.1 and 2.
    /// </para>
    /// </summary>
    [JsonPropertyName("scale")]
    public float? Scale { get; set; }

    /// <summary><para>Paper width, accepts values labeled with units.</para></summary>
    [JsonPropertyName("width")]
    public string? Width { get; set; }
}

#nullable disable
