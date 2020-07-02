using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IFrame" />
    public class Frame : IChannelOwner, IFrame
    {
        internal Frame(PlaywrightClient client, Channel channel, FrameInitializer initializer)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IFrame[] ChildFrames { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string Url { get; }

        /// <inheritdoc />
        public IFrame ParentFrame { get; }

        /// <inheritdoc />
        public bool Detached { get; }

        /// <inheritdoc />
        public string Id { get; set; }

        /// <inheritdoc />
        public Task<string> GetTitleAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, GoToOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, WaitUntilNavigation waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetContentAsync(string html, NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string> GetContentAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task FillAsync(string selector, string text, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> WaitForSelectorEvaluateAsync(
            string selector,
            string script,
            WaitForFunctionOptions options = null,
            params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string pageFunction, WaitForFunctionOptions options = null, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle[]> QuerySelectorAllAsync(string selector) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClickAsync(string selector, ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task DoubleClickAsync(string selector, ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task TripleClickAsync(string selector, ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null, CancellationToken token = default) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task FocusAsync(string selector, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task HoverAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, TypeOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task WaitForLoadStateAsync(NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> AddStyleTagAsync(AddTagOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, string value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, SelectOption value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, IElementHandle value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, string[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, SelectOption[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, IElementHandle[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params string[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params SelectOption[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params IElementHandle[] values) => throw new NotImplementedException();
    }
}
