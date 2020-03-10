using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IElementHandle"/>
    public class ElementHandle : JSHandle, IElementHandle
    {
        /// <inheritdoc cref="IElementHandle"/>
        public Task ClickAsync(ClickOptions options = null)
        {
            throw new NotImplementedException();
        }

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
        public Task<BoundingBox> GetBoundingBoxAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.GetContentFrameAsync"/>
        public Task<IFrame> GetContentFrameAsync()
        {
            throw new NotImplementedException();
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
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IElementHandle.ScrollIntoViewIfNeededAsync"/>
        public Task ScrollIntoViewIfNeededAsync()
        {
            throw new NotImplementedException();
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
    }
}
