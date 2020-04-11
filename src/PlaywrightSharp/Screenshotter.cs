using System;
using System.Drawing;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp
{
    internal class Screenshotter : IDisposable
    {
        private const string ScreenshotDuringNavigationError = "Cannot take a screenshot while page is navigating";

        private readonly Page _page;
        private readonly TaskQueue _queue = new TaskQueue();

        public Screenshotter(Page page)
        {
            _page = page;
        }

        public Task<byte[]> ScreenshotPageAsync(ScreenshotOptions options)
        {
            options ??= new ScreenshotOptions();
            return _queue.Enqueue(() => PerformScreenshot(ValidateScreenshotOptions(options), options));
        }

        public void Dispose() => _queue?.Dispose();

        internal Task<byte[]> ScreenshotElementAsync(ElementHandle handle, ScreenshotOptions options)
        {
            var format = ValidateScreenshotOptions(options);
            var rewrittenOptions = options.Clone();

            return _queue.Enqueue(() => PerformScreenshotElementAsync(handle, format, rewrittenOptions));
        }

        private ScreenshotFormat ValidateScreenshotOptions(ScreenshotOptions options)
        {
            var screenshotFormat = options.Type ?? ScreenshotFormat.Png;

            if (options.Quality.HasValue)
            {
                if (screenshotFormat != ScreenshotFormat.Jpeg)
                {
                    throw new ArgumentException($"options.Quality is unsupported for the {screenshotFormat} screenshots");
                }

                if (options.Quality < 0 || options.Quality > 100)
                {
                    throw new ArgumentException($"Expected options.quality to be between 0 and 100 (inclusive), got {options.Quality}");
                }
            }

            if (options?.Clip?.Width == 0)
            {
                throw new PlaywrightSharpException("Expected options.Clip.Width not to be 0.");
            }

            if (options?.Clip?.Height == 0)
            {
                throw new PlaywrightSharpException("Expected options.Clip.Height not to be 0.");
            }

            if (options.Clip != null && options.FullPage)
            {
                throw new ArgumentException("options.clip and options.fullPage are exclusive");
            }

            return screenshotFormat;
        }

        private async Task<byte[]> PerformScreenshot(ScreenshotFormat format, ScreenshotOptions options)
        {
            Viewport overridenViewport = null;
            var viewport = _page.Viewport;
            Viewport viewportSize = null;

            if (viewport == null)
            {
                viewportSize = await _page.EvaluateAsync<Viewport>(@"() => {
                  if (!document.body || !document.documentElement)
                    return;
                  return {
                    width: Math.max(document.body.offsetWidth, document.documentElement.offsetWidth),
                    height: Math.max(document.body.offsetHeight, document.documentElement.offsetHeight),
                  };
                }").ConfigureAwait(false);

                if (viewportSize == null)
                {
                    throw new PlaywrightSharpException(ScreenshotDuringNavigationError);
                }
            }

            if (options.FullPage && !_page.Delegate.CanScreenshotOutsideViewport())
            {
                var fullPageRect = await _page.EvaluateAsync<Size>(@"() => {
                  if (!document.body || !document.documentElement)
                    return null;
                  return {
                    width: Math.max(
                        document.body.scrollWidth, document.documentElement.scrollWidth,
                        document.body.offsetWidth, document.documentElement.offsetWidth,
                        document.body.clientWidth, document.documentElement.clientWidth
                    ),
                    height: Math.max(
                        document.body.scrollHeight, document.documentElement.scrollHeight,
                        document.body.offsetHeight, document.documentElement.offsetHeight,
                        document.body.clientHeight, document.documentElement.clientHeight
                    ),
                  };
                }").ConfigureAwait(false);

                if (fullPageRect == null)
                {
                    throw new PlaywrightSharpException(ScreenshotDuringNavigationError);
                }

                overridenViewport = viewport != null ? viewport.Clone() : new Viewport();
                overridenViewport.Height = fullPageRect.Height;
                overridenViewport.Width = fullPageRect.Width;

                await _page.SetViewportAsync(overridenViewport).ConfigureAwait(false);
            }
            else if (options.Clip != null)
            {
                options.Clip = TrimClipToViewport(viewport, options.Clip);
            }

            byte[] result = await ScreenshotAsync(format, options, overridenViewport ?? viewport).ConfigureAwait(false);

            if (overridenViewport != null)
            {
                if (viewport != null)
                {
                    await _page.SetViewportAsync(viewport).ConfigureAwait(false);
                }
                else
                {
                    await _page.Delegate.ResetViewportAsync(viewportSize).ConfigureAwait(false);
                }
            }

            return result;
        }

        private async Task<byte[]> PerformScreenshotElementAsync(ElementHandle handle, ScreenshotFormat format, ScreenshotOptions options)
        {
            Viewport overridenViewport = null;
            Viewport viewportSize = null;

            var maybeBoundingBox = await _page.Delegate.GetBoundingBoxForScreenshotAsync(handle).ConfigureAwait(false);

            if (maybeBoundingBox == null)
            {
                throw new PlaywrightSharpException("Node is either not visible or not an HTMLElement");
            }

            var boundingBox = maybeBoundingBox;

            if (boundingBox.Width == 0)
            {
                throw new PlaywrightSharpException("Node has 0 width");
            }

            if (boundingBox.Height == 0)
            {
                throw new PlaywrightSharpException("Node has 0 height");
            }

            boundingBox = EnclosingIntRect(boundingBox);

            var viewport = _page.Viewport;

            if (!_page.Delegate.CanScreenshotOutsideViewport())
            {
                if (viewport == null)
                {
                    var maybeViewportSize = await _page.EvaluateAsync<Viewport>(@"() => {
                        if (!document.body || !document.documentElement)
                            return;
                        return {
                            width: Math.max(document.body.offsetWidth, document.documentElement.offsetWidth),
                            height: Math.max(document.body.offsetHeight, document.documentElement.offsetHeight),
                        };
                    }").ConfigureAwait(false);

                    if (maybeViewportSize == null)
                    {
                        throw new PlaywrightSharpException(ScreenshotDuringNavigationError);
                    }

                    viewportSize = maybeViewportSize;
                }
                else
                {
                    viewportSize = viewport;
                }

                if (boundingBox.Width > viewportSize.Width || boundingBox.Height > viewportSize.Height)
                {
                    overridenViewport = (viewport ?? viewportSize).Clone();
                    overridenViewport.Width = Convert.ToInt32(Math.Max(viewportSize.Width, boundingBox.Width));
                    overridenViewport.Height = Convert.ToInt32(Math.Max(viewportSize.Height, boundingBox.Height));
                    await _page.SetViewportAsync(overridenViewport).ConfigureAwait(false);
                }

                await handle.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
                maybeBoundingBox = await _page.Delegate.GetBoundingBoxForScreenshotAsync(handle).ConfigureAwait(false);

                if (maybeBoundingBox == null)
                {
                    throw new PlaywrightSharpException("Node is either not visible or not an HTMLElement");
                }

                boundingBox = EnclosingIntRect(maybeBoundingBox!);
            }

            if (overridenViewport == null)
            {
                options.Clip = boundingBox;
            }

            byte[] result = await ScreenshotAsync(format, options, overridenViewport ?? viewport).ConfigureAwait(false);

            if (overridenViewport != null)
            {
                if (viewport != null)
                {
                    await _page.SetViewportAsync(viewport).ConfigureAwait(false);
                }
                else
                {
                    await _page.Delegate.ResetViewportAsync(viewportSize).ConfigureAwait(false);
                }
            }

            return result;
        }

        private async Task<byte[]> ScreenshotAsync(ScreenshotFormat format, ScreenshotOptions options, Viewport viewport)
        {
            bool shouldSetDefaultBackground = options.OmitBackground && format == ScreenshotFormat.Png;

            if (shouldSetDefaultBackground)
            {
                await _page.Delegate.SetBackgroundColorAsync(Color.FromArgb(0, 0, 0, 0)).ConfigureAwait(false);
            }

            byte[] result = await _page.Delegate.TakeScreenshotAsync(format, options, viewport).ConfigureAwait(false);

            if (shouldSetDefaultBackground)
            {
                await _page.Delegate.SetBackgroundColorAsync().ConfigureAwait(false);
            }

            return result;
        }

        private Rect TrimClipToViewport(Viewport viewport, Rect clip)
        {
            if (clip == null || viewport == null)
            {
                return clip;
            }

            var p1 = new Point
            {
                X = (int)Math.Min(clip.X, viewport.Width),
                Y = (int)Math.Min(clip.Y, viewport.Height),
            };
            var p2 = new Point
            {
                X = (int)Math.Min(clip.X + clip.Width, viewport.Width),
                Y = (int)Math.Min(clip.Y + clip.Height, viewport.Height),
            };

            var result = new Rect
            {
                X = p1.X,
                Y = p1.Y,
                Width = p2.X - p1.X,
                Height = p2.Y - p1.Y,
            };

            if (result.Width <= 0 || result.Height <= 0)
            {
                throw new PlaywrightSharpException("Clipped area is either empty or outside the viewport");
            }

            return result;
        }

        private Rect EnclosingIntRect(Rect rect)
        {
            double x = Math.Floor(rect.X + 1e-3);
            double y = Math.Floor(rect.Y + 1e-3);
            double x2 = Math.Ceiling(rect.X + rect.Width - 1e-3);
            double y2 = Math.Ceiling(rect.Y + rect.Height - 1e-3);

            return new Rect
            {
                X = x,
                Y = y,
                Width = x2 - x,
                Height = y2 - y,
            };
        }
    }
}
