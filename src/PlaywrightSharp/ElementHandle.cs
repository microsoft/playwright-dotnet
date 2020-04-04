using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Esprima.Ast;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IElementHandle"/>
    public class ElementHandle : JSHandle, IElementHandle
    {
        private readonly Page _page;

        internal ElementHandle(FrameExecutionContext context, IRemoteObject remoteObject) : base(context, remoteObject)
        {
            _page = context.Frame.Page;
            FrameExecutionContext = context;
        }

        internal FrameExecutionContext FrameExecutionContext { get; set; }

        /// <inheritdoc cref="IElementHandle.ClickAsync(ClickOptions)"/>
        public Task ClickAsync(ClickOptions options = null) => PerformPointerActionAsync(point => _page.Mouse.ClickAsync(point.X, point.Y, options), options);

        /// <inheritdoc cref="IElementHandle.EvaluateHandleAsync"/>
        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args)
            => FrameExecutionContext.EvaluateHandleAsync(script, args.InsertAt(0, this));

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

            await _page.Keyboard.SendCharactersAsync(text).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IElementHandle.GetBoundingBoxAsync"/>
        public Task<Rect> GetBoundingBoxAsync() => _page.Delegate.GetBoundingBoxAsync(this);

        /// <inheritdoc cref="IElementHandle.GetContentFrameAsync"/>
        public async Task<IFrame> GetContentFrameAsync()
        {
            bool isFrameElement = await EvaluateInUtilityAsync<bool>("node => node && (node.nodeName === 'IFRAME' || node.nodeName === 'FRAME')").ConfigureAwait(false);
            if (!isFrameElement)
            {
                return null;
            }

            return await _page.Delegate.GetContentFrameAsync(this).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IElementHandle.GetOwnerFrameAsync"/>
        public async Task<IFrame> GetOwnerFrameAsync()
        {
            string frameId = await _page.Delegate.GetOwnerFrameAsync(this).ConfigureAwait(false);
            if (string.IsNullOrEmpty(frameId))
            {
                return null;
            }

            var pages = _page.BrowserContext.GetExistingPages();
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
            => EvaluateInUtility<double>(@"async (node) => {
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
        public Task HoverAsync(PointerActionOptions options = null) => PerformPointerActionAsync(point => _page.Mouse.MoveAsync(point.X, point.Y), options);

        /// <inheritdoc cref="IElementHandle.PressAsync"/>
        public Task PressAsync(string key, PressOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.QuerySelectorAllAsync"/>
        public Task<IElementHandle[]> QuerySelectorAllAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.QuerySelectorAllEvaluateAsync"/>
        public Task QuerySelectorAllEvaluateAsync(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.QuerySelectorAllEvaluateAsync{T}"/>
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.QuerySelectorAsync"/>
        public Task<IElementHandle> QuerySelectorAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.QuerySelectorEvaluateAsync"/>
        public Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.QuerySelectorEvaluateAsync{T}"/>
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.ScreenshotAsync"/>
        public Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null)
            => _page.Screenshotter.ScreenshotElementAsync(this, options ?? new ScreenshotOptions());

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
                this,
                _page.BrowserContext.Options.JavaScriptEnabled).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(error))
            {
                throw new PlaywrightSharpException(error);
            }
        }

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync"/>
        public Task SetInputFilesAsync(params string[] filePath)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.SetInputFilesAsync"/>
        public Task TypeAsync(string text, TypeOptions options = null)
        {
            throw new NotImplementedException();
        }

        internal Task DisposeAsync() => Task.CompletedTask;

        private async Task<T> EvaluateInUtilityAsync<T>(string pageFunction, params object[] args)
        {
            var utility = await FrameExecutionContext.Frame.GetUtilityContextAsync().ConfigureAwait(false);
            return await utility.EvaluateAsync<T>(pageFunction, args.InsertAt(0, this)).ConfigureAwait(false);
        }

        private async Task PerformPointerActionAsync(Func<Point, Task> action, IPointerActionOptions options)
        {
            var point = await EnsurePointerActionPointAsync(options?.RelativePoint).ConfigureAwait(false);
            Modifier[] restoreModifiers = null;

            if (options?.Modifiers != null)
            {
                restoreModifiers = await _page.Keyboard.EnsureModifiersAsync(options.Modifiers).ConfigureAwait(false);
            }

            await action(point).ConfigureAwait(false);

            if (restoreModifiers != null)
            {
                await _page.Keyboard.EnsureModifiersAsync(restoreModifiers).ConfigureAwait(false);
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

            if (r.ScrollX != null || r.ScrollY != null)
            {
                string error = await EvaluateInUtility<string>(
                    @"(element, scrollX, scrollY) =>
                    {
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

                if (r.ScrollX != null || r.ScrollY != null)
                {
                    throw new PlaywrightSharpException("Failed to scroll relative point into viewport");
                }
            }

            return r.Point;
        }

        private async Task<T> EvaluateInUtility<T>(string script, params object[] args)
        {
            var utility = await FrameExecutionContext.Frame.GetUtilityContextAsync().ConfigureAwait(false);
            return await utility.EvaluateAsync<T>(script, this, args).ConfigureAwait(false);
        }

        private async Task<PointAndScroll> ViewportPointAndScrollAsync(Point relativePoint)
        {
            var boxTask = GetBoundingBoxAsync();
            var borderTask = EvaluateInUtility<Point>(@"(node) => {
                if (node.nodeType !== Node.ELEMENT_NODE || !node.ownerDocument || !node.ownerDocument.defaultView)
                    return { x: 0, y: 0 };
                const style = node.ownerDocument.defaultView.getComputedStyle(node);
                return { x: parseInt(style.borderLeftWidth || '', 10), y: parseInt(style.borderTopWidth || '', 10) };
            }");

            await Task.WhenAll(
                boxTask,
                borderTask.ContinueWith(
                    t => System.Diagnostics.Debug.WriteLine(t.Exception?.Message),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default)).ConfigureAwait(false);

            Rect box = boxTask.Result;
            Point? border = borderTask.IsFaulted ? (Point?)null : borderTask.Result;

            var point = new Point
            {
                X = relativePoint.X,
                Y = relativePoint.Y,
            };

            if (box != null)
            {
                point.X += Convert.ToInt32(box.X);
                point.Y += Convert.ToInt32(box.Y);
            }

            if (border != null)
            {
                // Make point relative to the padding box to align with offsetX/offsetY.
                point.X += border.Value.X;
                point.Y += border.Value.Y;
            }

            var metrics = await _page.Delegate.GetLayoutViewportAsync().ConfigureAwait(false);

            // Give 20 extra pixels to avoid any issues on viewport edge.
            double scrollX = 0;

            if (point.X < 20)
            {
                scrollX = point.X - 20;
            }

            if (point.X > metrics.Width - 20)
            {
                scrollX = point.X - metrics.Width + 20;
            }

            double scrollY = 0;
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

            var quadsTask = _page.Delegate.GetContentQuadsAsync(this);
            var metricsTask = _page.Delegate.GetLayoutViewportAsync();

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
    }
}
