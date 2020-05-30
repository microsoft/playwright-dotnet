using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IElementHandle"/>
    public class ElementHandle : JSHandle, IElementHandle
    {
        internal ElementHandle(FrameExecutionContext context, IRemoteObject remoteObject) : base(context, remoteObject)
        {
            Page = context.Frame.Page;
            Context = context;
        }

        internal new FrameExecutionContext Context { get; }

        internal Page Page { get; }

        /// <inheritdoc cref="IElementHandle.ClickAsync(ClickOptions)"/>
        public Task ClickAsync(ClickOptions options = null) => PerformPointerActionAsync(point => Page.Mouse.ClickAsync(point.X, point.Y, options), options);

        /// <inheritdoc cref="IElementHandle.DoubleClickAsync(ClickOptions)"/>
        public Task DoubleClickAsync(ClickOptions options = null) => PerformPointerActionAsync(point => Page.Mouse.DoubleClickAsync(point.X, point.Y, options), options);

        /// <inheritdoc cref="IElementHandle.TripleClickAsync(ClickOptions)"/>
        public Task TripleClickAsync(ClickOptions options = null) => PerformPointerActionAsync(point => Page.Mouse.TripleClickAsync(point.X, point.Y, options), options);

        /// <inheritdoc cref="IElementHandle.EvaluateHandleAsync"/>
        public async Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args)
            => await Context.EvaluateHandleAsync(script, args.Prepend(this)).ConfigureAwait(false);

        /// <inheritdoc cref="IElementHandle.FillAsync(string)"/>
        public async Task FillAsync(string text)
        {
            string error = await EvaluateInUtilityAsync<string>(@"(node) => {
                if (node.nodeType !== Node.ELEMENT_NODE)
                    return 'Node is not of type HTMLElement';
                const element = node;
                if (!element.isConnected)
                    return 'Element is not attached to the DOM';
                if (!element.ownerDocument || !element.ownerDocument.defaultView)
                    return 'Element does not belong to a window';
                const style = element.ownerDocument.defaultView.getComputedStyle(element);
                if (!style || style.visibility === 'hidden')
                    return 'Element is hidden';
                if (!element.offsetParent && element.tagName !== 'BODY')
                    return 'Element is not visible';
                if (element.nodeName.toLowerCase() === 'input') {
                    const input = element;
                    const type = input.getAttribute('type') || '';
                    const kTextInputTypes = new Set(['', 'email', 'password', 'search', 'tel', 'text', 'url']);
                    if (!kTextInputTypes.has(type.toLowerCase()))
                        return 'Cannot fill input of type ""' + type + '"".';
                    if (input.disabled)
                    return 'Cannot fill a disabled input.';
                if (input.readOnly)
                    return 'Cannot fill a readonly input.';
                input.select();
                input.focus();
                }
                else if (element.nodeName.toLowerCase() === 'textarea') {
                    const textarea = element;
                    if (textarea.disabled)
                        return 'Cannot fill a disabled textarea.';
                    if (textarea.readOnly)
                        return 'Cannot fill a readonly textarea.';
                    textarea.selectionStart = 0;
                    textarea.selectionEnd = textarea.value.length;
                    textarea.focus();
                }
                else if (element.isContentEditable) {
                    const range = element.ownerDocument.createRange();
                    range.selectNodeContents(element);
                    const selection = element.ownerDocument.defaultView.getSelection();
                    if (!selection)
                        return 'Element belongs to invisible iframe.';
                    selection.removeAllRanges();
                    selection.addRange(range);
                    element.focus();
                }
                else {
                    return 'Element is not an <input>, <textarea> or [contenteditable] element.';
                }
                return '';
            }").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(error))
            {
                throw new PlaywrightSharpException(error);
            }

            await Page.Keyboard.SendCharactersAsync(text).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IElementHandle.GetBoundingBoxAsync"/>
        public Task<Rect> GetBoundingBoxAsync() => Page.Delegate.GetBoundingBoxAsync(this);

        /// <inheritdoc cref="IElementHandle.GetContentFrameAsync"/>
        public async Task<IFrame> GetContentFrameAsync()
        {
            bool isFrameElement = await EvaluateInUtilityAsync<bool>("node => node && (node.nodeName === 'IFRAME' || node.nodeName === 'FRAME')").ConfigureAwait(false);
            if (!isFrameElement)
            {
                return null;
            }

            return await Page.Delegate.GetContentFrameAsync(this).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IElementHandle.GetOwnerFrameAsync"/>
        public async Task<IFrame> GetOwnerFrameAsync()
        {
            string frameId = await Page.Delegate.GetOwnerFrameAsync(this).ConfigureAwait(false);
            if (string.IsNullOrEmpty(frameId))
            {
                return null;
            }

            var pages = Page.BrowserContext.GetExistingPages();
            foreach (var page in pages)
            {
                if (((Page)page).FrameManager.Frames.TryGetValue(frameId, out var frame))
                {
                    return frame;
                }
            }

            return null;
        }

        /// <inheritdoc cref="IElementHandle.GetVisibleRatioAsync"/>
        public Task<double> GetVisibleRatioAsync()
            => EvaluateInUtilityAsync<double>(@"async (node) => {
                if (node.nodeType !== Node.ELEMENT_NODE)
                    throw new Error('Node is not of type HTMLElement');
                const element = node;
                const visibleRatio = await new Promise(resolve => {
                    const observer = new IntersectionObserver(entries => {
                        resolve(entries[0].intersectionRatio);
                        observer.disconnect();
                    });
                    observer.observe(element);
                    // Firefox doesn't call IntersectionObserver callback unless
                    // there are rafs.
                    requestAnimationFrame(() => { });
                });
                return visibleRatio;
            }");

        /// <inheritdoc cref="IElementHandle.HoverAsync"/>
        public Task HoverAsync(PointerActionOptions options = null) => PerformPointerActionAsync(point => Page.Mouse.MoveAsync(point.X, point.Y), options);

        /// <inheritdoc cref="IElementHandle.PressAsync"/>
        public async Task PressAsync(string key, PressOptions options = null)
        {
            await FocusAsync().ConfigureAwait(false);
            await Page.Keyboard.PressAsync(key, options).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IElementHandle.QuerySelectorAllAsync"/>
        public Task<IElementHandle[]> QuerySelectorAllAsync(string selector)
            => Context.QuerySelectorAllAsync(selector, this);

        /// <inheritdoc cref="IElementHandle.QuerySelectorAllEvaluateAsync"/>
        public Task QuerySelectorAllEvaluateAsync(string selector, string pageFunction, params object[] args)
            => QuerySelectorAllEvaluateAsync<object>(selector, pageFunction, args);

        /// <inheritdoc cref="IElementHandle.QuerySelectorAllEvaluateAsync{T}"/>
        public async Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string pageFunction, params object[] args)
        {
            var arrayHandle = await Context.QuerySelectorArrayAsync(selector, this).ConfigureAwait(false);
            var result = await arrayHandle.EvaluateAsync<T>(pageFunction, args).ConfigureAwait(false);
            await arrayHandle.DisposeAsync().ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc cref="IElementHandle.FocusAsync"/>
        public async Task FocusAsync()
        {
            string errorMessage = await EvaluateInUtilityAsync<string>(@"(element) => {
                if (!element['focus'])
                    return 'Node is not an HTML or SVG element.';
                element.focus();
                return '';
            }").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new PlaywrightSharpException(errorMessage);
            }
        }

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string[] values)
            => SelectInternalAsync(values.Select(v => new SelectOption { Value = v }).ToArray());

        /// <inheritdoc />
        public Task<string[]> SelectAsync(IElementHandle[] values) => SelectInternalAsync(values);

        /// <inheritdoc />
        public Task<string[]> SelectAsync(SelectOption[] values) => SelectInternalAsync(values);

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string value) => SelectInternalAsync(new[] { value });

        /// <inheritdoc />
        public Task<string[]> SelectAsync(IElementHandle value) => SelectInternalAsync(new[] { value });

        /// <inheritdoc />
        public Task<string[]> SelectAsync(SelectOption value) => SelectInternalAsync(new[] { value });

        /// <inheritdoc cref="IElementHandle.QuerySelectorAsync"/>
        public Task<IElementHandle> QuerySelectorAsync(string selector)
            => Context.QuerySelectorAsync(selector, this);

        /// <inheritdoc cref="IElementHandle.QuerySelectorEvaluateAsync"/>
        public Task QuerySelectorEvaluateAsync(string selector, string pageFunction, params object[] args)
            => QuerySelectorEvaluateAsync<object>(selector, pageFunction, args);

        /// <inheritdoc cref="IElementHandle.QuerySelectorEvaluateAsync{T}"/>
        public async Task<T> QuerySelectorEvaluateAsync<T>(string selector, string pageFunction, params object[] args)
        {
            var handle = await Context.QuerySelectorAsync(selector, this).ConfigureAwait(false);
            if (handle == null)
            {
                throw new SelectorException("Failed to find element matching selector", selector);
            }

            var result = await handle.EvaluateAsync<T>(pageFunction, args).ConfigureAwait(false);
            await handle.DisposeAsync().ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc cref="IElementHandle.ScreenshotAsync"/>
        public Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null)
            => Page.Screenshotter.ScreenshotElementAsync(this, options ?? new ScreenshotOptions());

        /// <inheritdoc cref="IElementHandle.ScrollIntoViewIfNeededAsync"/>
        public async Task ScrollIntoViewIfNeededAsync()
        {
            string error = await EvaluateInUtilityAsync<string>(
                @"async (node, pageJavascriptEnabled) => {
                    if (!node.isConnected)
                        return 'Node is detached from document';
                    if (node.nodeType !== Node.ELEMENT_NODE)
                        return 'Node is not of type HTMLElement';
                    const element = node;
                    // force-scroll if page's javascript is disabled.
                    if (!pageJavascriptEnabled) {
                        // @ts-ignore because only Chromium still supports 'instant'
                        element.scrollIntoView({ block: 'center', inline: 'center', behavior: 'instant' });
                        return '';
                    }
                    const visibleRatio = await new Promise(resolve => {
                        const observer = new IntersectionObserver(entries => {
                            resolve(entries[0].intersectionRatio);
                            observer.disconnect();
                        });
                        observer.observe(element);
                        // Firefox doesn't call IntersectionObserver callback unless
                        // there are rafs.
                        requestAnimationFrame(() => { });
                    });
                    if (visibleRatio !== 1.0) {
                        // @ts-ignore because only Chromium still supports 'instant'
                        element.scrollIntoView({ block: 'center', inline: 'center', behavior: 'instant' });
                    }
                    return '';
                }",
                Page.BrowserContext.Options.JavaScriptEnabled).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(error))
            {
                throw new PlaywrightSharpException(error);
            }
        }

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync"/>
        public async Task SetInputFilesAsync(params string[] files)
        {
            bool multiple = await EvaluateInUtilityAsync<bool>(@"(node) => {
                if (node.nodeType !== Node.ELEMENT_NODE || node.tagName !== 'INPUT')
                    throw new Error('Node is not an HTMLInputElement');
                const input = node;
                return input.multiple;
            }").ConfigureAwait(false);

            if (!multiple && files.Length > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(files), "Non-multiple file input can only accept single file!");
            }

            var filePayloads = files.Select(item =>
                new FilePayload
                {
                    Name = new FileInfo(item).Name,
                    Type = "application/octet-stream",
                    Data = Convert.ToBase64String(File.ReadAllBytes(item)),
                });

            await Page.Delegate.SetInputFilesAsync(this, filePayloads).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IElementHandle.TypeAsync"/>
        public async Task TypeAsync(string text, int delay = 0)
        {
            await FocusAsync().ConfigureAwait(false);
            await Page.Keyboard.TypeAsync(text, delay).ConfigureAwait(false);
        }

        internal Task<string[]> SelectInternalAsync(IEnumerable<object> values)
            => EvaluateInUtilityAsync<string[]>(
                @"(node, ...optionsToSelect) => {
                    if (node.nodeName.toLowerCase() !== 'select')
                        throw new Error('Element is not a <select> element.');
                    const element = node;
                    const options = Array.from(element.options);
                    element.value = undefined;
                    for (let index = 0; index < options.length; index++) {
                        const option = options[index];
                        option.selected = optionsToSelect.some(optionToSelect => {
                            if (!optionToSelect)
                                return false;
                            if (optionToSelect instanceof Node)
                                return option === optionToSelect;
                            let matches = true;
                            if (optionToSelect.value !== undefined)
                                matches = matches && optionToSelect.value === option.value;
                            if (optionToSelect.label !== undefined)
                                matches = matches && optionToSelect.label === option.label;
                            if (optionToSelect.index !== undefined)
                                matches = matches && optionToSelect.index === index;
                            return matches;
                        });
                        if (option.selected && !element.multiple)
                            break;
                    }
                    element.dispatchEvent(new Event('input', { 'bubbles': true }));
                    element.dispatchEvent(new Event('change', { 'bubbles': true }));
                    return options.filter(option => option.selected).map(option => option.value);
                }",
                (values ?? Array.Empty<object>()).ToArray());

        private async Task PerformPointerActionAsync(Func<Point, Task> action, IPointerActionOptions options)
        {
            var point = await EnsurePointerActionPointAsync(options?.RelativePoint).ConfigureAwait(false);
            Modifier[] restoreModifiers = null;

            if (options?.Modifiers != null)
            {
                restoreModifiers = await Page.Keyboard.EnsureModifiersAsync(options.Modifiers).ConfigureAwait(false);
            }

            await action(point).ConfigureAwait(false);

            if (restoreModifiers != null)
            {
                await Page.Keyboard.EnsureModifiersAsync(restoreModifiers).ConfigureAwait(false);
            }
        }

        private async Task<Point> EnsurePointerActionPointAsync(Point? relativePoint)
        {
            await ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
            if (relativePoint == null)
            {
                return await ClickablePointAsync().ConfigureAwait(false);
            }

            var r = await ViewportPointAndScrollAsync(relativePoint.Value).ConfigureAwait(false);

            if (r.ScrollX != 0 || r.ScrollY != 0)
            {
                string error = await EvaluateInUtilityAsync<string>(
                    @"(element, scrollX, scrollY) => {
                        if (!element.ownerDocument || !element.ownerDocument.defaultView)
                            return 'Node does not have a containing window';
                        element.ownerDocument.defaultView.scrollBy(scrollX, scrollY);
                        return '';
                    }",
                    r.ScrollX,
                    r.ScrollY).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(error))
                {
                    throw new PlaywrightSharpException(error);
                }

                r = await ViewportPointAndScrollAsync(relativePoint.Value).ConfigureAwait(false);

                if (r.ScrollX != 0 || r.ScrollY != 0)
                {
                    throw new PlaywrightSharpException("Failed to scroll relative point into viewport");
                }
            }

            return r.Point;
        }

        private async Task<PointAndScroll> ViewportPointAndScrollAsync(Point relativePoint)
        {
            // TODO: debug log
            var boxTask = GetBoundingBoxAsync();
            var borderTask = EvaluateInUtilityAsync<Point>(@"node => {
                    if (node.nodeType !== Node.ELEMENT_NODE || !node.ownerDocument || !node.ownerDocument.defaultView)
                        return { x: 0, y: 0 };
                    const style = node.ownerDocument.defaultView.getComputedStyle(node);
                    return { x: parseInt(style.borderLeftWidth || '', 10), y: parseInt(style.borderTopWidth || '', 10) };
                }");
            await Task.WhenAll(boxTask, borderTask).ConfigureAwait(false);
            var box = boxTask.Result;
            var border = borderTask.Result;
            var point = new Point { X = relativePoint.X, Y = relativePoint.Y };
            if (box != null)
            {
                point = new Point { X = point.X + (int)box.X, Y = point.Y + (int)box.Y };
            }

            // Make point relative to the padding box to align with offsetX/offsetY.
            point = new Point { X = point.X + border.X, Y = point.Y + border.Y };

            var metrics = await Page.Delegate.GetLayoutViewportAsync().ConfigureAwait(false);
            int scrollX = 0;
            if (point.X < 20)
            {
                scrollX = point.X - 20;
            }

            if (point.X > metrics.Width - 20)
            {
                scrollX = point.X - metrics.Width + 20;
            }

            int scrollY = 0;
            if (point.Y < 20)
            {
                scrollY = point.Y - 20;
            }

            if (point.Y > metrics.Height - 20)
            {
                scrollY = point.Y - metrics.Height + 20;
            }

            return new PointAndScroll
            {
                Point = point,
                ScrollX = scrollX,
                ScrollY = scrollY,
            };
        }

        private async Task<Point> ClickablePointAsync()
        {
            static int ComputeQuadArea(Point[] quad)
            {
                // Compute sum of all directed areas of adjacent triangles
                // https://en.wikipedia.org/wiki/Polygon#Simple_polygons
                int area = 0;
                for (int i = 0; i < quad.Length; ++i)
                {
                    var p1 = quad[i];
                    var p2 = quad[(i + 1) % quad.Length];
                    area += Convert.ToInt32(((p1.X * p2.Y) - (p2.X * p1.Y)) / 2);
                }

                return Math.Abs(area);
            }

            var quadsTask = Page.Delegate.GetContentQuadsAsync(this);
            var metricsTask = Page.Delegate.GetLayoutViewportAsync();

            await Task.WhenAll(quadsTask, metricsTask).ConfigureAwait(false);
            var quads = quadsTask.Result;
            var metrics = metricsTask.Result;

            Point[] IntersectQuadWithViewport(Quad[] quad)
            {
                return quad.Select(point => new Point
                {
                    X = Convert.ToInt32(Math.Min(Math.Max(point.X, 0), metrics.Width)),
                    Y = Convert.ToInt32(Math.Min(Math.Max(point.Y, 0), metrics.Height)),
                }).ToArray();
            }

            if (quads == null || quads.Length == 0)
            {
                throw new PlaywrightSharpException("Node is either not visible or not an HTMLElement");
            }

            var filtered = quads.Select(IntersectQuadWithViewport).Where(quad => ComputeQuadArea(quad) > 1).ToArray();
            if (filtered.Length == 0)
            {
                throw new PlaywrightSharpException("Node is either not visible or not an HTMLElement");
            }

            // Return the middle point of the first quad.
            var result = new Point { X = 0, Y = 0 };
            foreach (var point in filtered.First())
            {
                result.X += point.X / 4;
                result.Y += point.Y / 4;
            }

            return result;
        }

        private async Task<T> EvaluateInUtilityAsync<T>(string pageFunction, params object[] args)
        {
            var utility = await Context.Frame.GetUtilityContextAsync().ConfigureAwait(false);
            return await utility.EvaluateAsync<T>(pageFunction, args.Prepend(this)).ConfigureAwait(false);
        }
    }
}
