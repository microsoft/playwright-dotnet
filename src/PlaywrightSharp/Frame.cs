using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IFrame" />
    public class Frame : IChannelOwner<Frame>, IFrame
    {
        private readonly ConnectionScope _scope;
        private readonly FrameChannel _channel;

        internal Frame(ConnectionScope scope, string guid, FrameInitializer initializer)
        {
            _scope = scope;
            _channel = new FrameChannel(guid, scope, this);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Frame> IChannelOwner<Frame>.Channel => _channel;

        /// <inheritdoc />
        public IFrame[] ChildFrames { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string Url { get; }

        /// <inheritdoc />
        public IFrame ParentFrame { get; }

        /// <inheritdoc />
        public IPage Page { get; internal set; }

        /// <inheritdoc />
        public bool Detached { get; }

        /// <inheritdoc />
        public string Id { get; set; }

        /// <inheritdoc />
        public Task<string> GetTitleAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public async Task<IResponse> GoToAsync(string url, GoToOptions options = null)
            => (await _channel.GoToAsync(url, options).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, WaitUntilNavigation waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetContentAsync(string html, NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string> GetContentAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options) => AddScriptTagAsync(false, options);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script) => EvaluateAsync<T>(false, script);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script, object args) => EvaluateAsync<T>(false, script, args);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script) => EvaluateAsync(false, script);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script, object args) => EvaluateAsync(false, script, args);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string script) => EvaluateHandleAsync(false, script);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string script, object args) => EvaluateHandleAsync(false, script, args);

        /// <inheritdoc />
        public Task FillAsync(string selector, string text, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null)
            => WaitForSelectorAsync(false, selector, options);

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

        internal async Task<IElementHandle> AddScriptTagAsync(bool isPageCall, AddTagOptions options)
            => (await _channel.AddScriptTagAsync(
                options: options,
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal async Task<IElementHandle> WaitForSelectorAsync(bool isPageCall, string selector, WaitForSelectorOptions options)
            => (await _channel.WaitForSelector(
                selector: selector,
                options: options ?? new WaitForSelectorOptions(),
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal async Task<IJSHandle> EvaluateHandleAsync(bool isPageCall, string script)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal async Task<IJSHandle> EvaluateHandleAsync(bool isPageCall, string script, object args)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal async Task<T> EvaluateAsync<T>(bool isPageCall, string script)
            => ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> EvaluateAsync<T>(bool isPageCall, string script, object args)
            => ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> EvaluateAsync(bool isPageCall, string script)
            => ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> EvaluateAsync(bool isPageCall, string script, object args)
            => ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        private static bool IsPrimitiveValue(Type type)
            => type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(double) ||
            type == typeof(bool) ||
            type == typeof(decimal?) ||
            type == typeof(double?) ||
            type == typeof(bool?);

        private static T ParseEvaluateResult<T>(JsonElement? result)
        {
            if (!result.HasValue)
            {
                return default;
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("v", out var value) && value.ToString() == "undefined")
            {
                return default;
            }

            if (typeof(T) == typeof(JsonElement?))
            {
                return (T)(object)result;
            }

            return result.Value.ToObject<T>();
        }

        private EvaluateArgument SerializedArgument(object args)
        {
            var result = new EvaluateArgument();
            var guids = new List<EvaluateArgumentGuidElement>();

            int PushHandle(string guid)
            {
                guids.Add(new EvaluateArgumentGuidElement { Guid = guid });
                return guids.Count - 1;
            }

            object value = SerializeAsCallArgument(args, value =>
            {
                if (value is IChannelOwner channelOwner)
                {
                    return new EvaluateArgumentValueElement
                    {
                        H = PushHandle(channelOwner.Channel.Guid),
                    };
                }

                return new EvaluateArgumentValueElement
                {
                    FallThrough = value,
                };
            });

            return new EvaluateArgument
            {
                Value = value,
                Guids = guids,
            };
        }

        private object SerializeAsCallArgument(object value, Func<object, EvaluateArgumentValueElement> jsHandleSerializer)
            => Serialize(value, jsHandleSerializer, new List<object>());

        private object Serialize(object value, Func<object, EvaluateArgumentValueElement> jsHandleSerializer, List<object> visited)
        {
            // This will endupt being a converter when we need to fully implement this
            value = jsHandleSerializer(value);

            if (value is EvaluateArgumentValueElement valueElement && valueElement.FallbackSet)
            {
                value = valueElement.FallThrough;
            }
            else
            {
                return value;
            }

            if (visited.Contains(value))
            {
                throw new PlaywrightSharpException("Argument is a circular structure");
            }

            if (value == null)
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "null" };
            }

            if (value is double nan && double.IsNaN(nan))
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "NaN" };
            }

            if (value is double infinity && double.IsInfinity(infinity))
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "Infinity" };
            }

            if (value is double negativeInfinity && double.IsNegativeInfinity(negativeInfinity))
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "Infinity" };
            }

            if (value is double negativeZero && negativeZero == -0)
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "-0" };
            }

            if (IsPrimitiveValue(value.GetType()))
            {
                return value;
            }

            if (value is DateTime date)
            {
                return new EvaluateArgumentValueElement.Datetime { D = date };
            }

            if (value is IEnumerable enumerable)
            {
                var result = new List<object>();
                visited.Add(value);

                foreach (object item in enumerable)
                {
                    result.Add(Serialize(item, jsHandleSerializer, visited));
                }

                visited.Remove(value);

                return new EvaluateArgumentValueElement.Array { A = result.ToArray() };
            }

            return new EvaluateArgumentValueElement.Object { O = value };
        }
    }
}
