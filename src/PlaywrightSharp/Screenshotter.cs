using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp
{
    internal class Screenshotter : IDisposable
    {
        private const string ScreenshotDuringNavigationError = "Cannot take a screenshot while page is navigating";

        private readonly Page _page;
        private readonly TaskQueue _screenshotTaskQueue = new TaskQueue();

        public Screenshotter(Page page)
        {
            _page = page;
        }

        public Task<byte[]> ScreenshotPageAsync(ScreenshotOptions options)
            => _screenshotTaskQueue.Enqueue(() => PerformScreenshot(ValidateScreenshotOptions(options), options));

        public void Dispose() => _screenshotTaskQueue?.Dispose();

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
            Size? viewportSize = null;

            if (viewport == null)
            {
                viewportSize = await _page.EvaluateAsync<Size?>(@"() => {
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
                var fullPageRect = await _page.EvaluateAsync<Size?>(@"() => {
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

                if (viewport != null)
                {
                    overridenViewport = viewport.Clone();
                }

                overridenViewport.Height = fullPageRect.Value.Height;
                overridenViewport.Width = fullPageRect.Value.Width;

                await _page.SetViewportAsync(overridenViewport).ConfigureAwait(false);
            }
            else if (options.Clip != null)
            {
                options.Clip = TrimClipToViewport(viewport, options.Clip);
            }

            var result = await ScreenshotAsync(format, options, overridenViewport ?? viewport).ConfigureAwait(false);

            if (overridenViewport != null)
            {
                if (viewport != null)
                {
                    await _page.SetViewportAsync(viewport).ConfigureAwait(false);
                }
                else
                {
                    await _page.Delegate.ResetViewportAsync(viewportSize.Value).ConfigureAwait(false);
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

        private Clip TrimClipToViewport(Viewport viewport, Clip clip)
        {
            throw new NotImplementedException();
        }
    }
}
