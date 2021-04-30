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
namespace Microsoft.Playwright.Contracts.Constants
{
    /// <summary>
    /// Contains the constants of resource types as returned by Playwright.
    /// </summary>
#pragma warning disable CS1591, SA1600 // Missing XML comment for publicly visible type or member
    public static class ResourceTypes
    {
        public static string Document => "document";

        public static string Stylesheet => "stylesheet";

        public static string Image => "image";

        public static string Media => "media";

        public static string Font => "font";

        public static string Script => "script";

        public static string TextTrack => "texttrack";

        public static string XHR => "xhr";

        public static string Fetch => "fetch";

        public static string EventSource => "eventsource";

        public static string WebSocket => "websocket";

        public static string Manifest => "manifest";

        public static string Other => "other";
    }
#pragma warning restore CS1591, SA1600 // Missing XML comment for publicly visible type or member
}
