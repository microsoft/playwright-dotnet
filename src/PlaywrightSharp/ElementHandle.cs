using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

        /// <inheritdoc cref="IElementHandle"/>
        public Task ClickAsync(ClickOptions options = null) => PerformPointerActionAsync(point => _page.Mouse.ClickAsync(point.X, point.Y, options), options);

        /// <inheritdoc cref="IElementHandle.EvaluateAsync{T}(string, object[])"/>
        public Task<T> EvaluateAsync<T>(string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.EvaluateAsync(string, object[])"/>
        public Task<JsonElement?> EvaluateAsync(string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.FillAsync(string)"/>
        public Task FillAsync(string text)
        {
            throw new NotImplementedException();
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
        public Task<IFrame> GetOwnerFrameAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.GetVisibleRatioAsync"/>
        public Task<double> GetVisibleRatioAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.HoverAsync"/>
        public Task HoverAsync()
        {
            throw new NotImplementedException();
        }

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
            string error = await EvaluateInUtility<string>(
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
            var list = new List<object>(args);
            list.Insert(0, this);
            return await utility.EvaluateAsync<T>(pageFunction, list.ToArray()).ConfigureAwait(false);
        }

        private async Task PerformPointerActionAsync(Func<Point, Task> action, ClickOptions options)
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

        private Task<PointAndScroll> ViewportPointAndScrollAsync(Point value)
        {
            throw new NotImplementedException();
        }

        private async Task<Point> ClickablePointAsync()
        {
            Func<Point[], int> computeQuadArea = (quad) =>
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
            };

            var quadsTask = _page.Delegate.GetContentQuadsAsync(this);
            var metricsTask = _page.Delegate.GetLayoutViewportAsync();

            await Task.WhenAll(quadsTask, metricsTask).ConfigureAwait(false);
            var quads = quadsTask.Result;
            var metrics = metricsTask.Result;

            Func<Quad[], Point[]> intersectQuadWithViewport = (quad) =>
             {
                 return quad.Select(point => new Point
                 {
                     X = Convert.ToInt32(Math.Min(Math.Max(point.X, 0), metrics.Width)),
                     Y = Convert.ToInt32(Math.Min(Math.Max(point.Y, 0), metrics.Height)),
                 }).ToArray();
             };

            if (quads == null || quads.Length == 0)
            {
                throw new PlaywrightSharpException("Node is either not visible or not an HTMLElement");
            }

            var filtered = quads.Select(quad => intersectQuadWithViewport(quad)).Where(quad => computeQuadArea(quad) > 1);
            if (!filtered.Any())
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
