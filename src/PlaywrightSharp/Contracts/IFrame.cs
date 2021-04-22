using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IFrame
    {
        /// <inheritdoc cref="EvaluateAsync{T}(string, object)"/>
        public Task<JsonElement?> EvaluateAsync(string expression, object arg = default);

        /// <inheritdoc cref="EvalOnSelectorAsync{T}(string, string, object)"/>
        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg);

        /// <inheritdoc cref="EvalOnSelectorAllAsync{T}(string, string, object)"/>
        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = default);

        /// <inheritdoc cref="WaitForNavigationAsync(string, Regex, Func{string, bool}, WaitUntilState, float?)"/>
        Task<IResponse> WaitForNavigationAsync(WaitUntilState waitUntil, float? timeout = default);

        /// <inheritdoc cref="WaitForNavigationAsync(string, Regex, Func{string, bool}, WaitUntilState, float?)"/>
        Task<IResponse> WaitForNavigationAsync(Regex urlRegex, WaitUntilState waitUntil = default, float? timeout = default);

        /// <inheritdoc cref="WaitForNavigationAsync(string, Regex, Func{string, bool}, WaitUntilState, float?)"/>
        Task<IResponse> WaitForNavigationAsync(Func<string, bool> urlFunc, WaitUntilState waitUntil = default, float? timeout = default);

        /// <inheritdoc cref="SetInputFilesAsync(string, IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(string selector, string files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SetInputFilesAsync(string, IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(string selector, IEnumerable<string> files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SetInputFilesAsync(string, IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(string selector, FilePayload files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, string values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<string> values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)"/>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IElementHandle values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, SelectOptionValue values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, params string[] values);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, params SelectOptionValue[] values);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, params IElementHandle[] values);
    }
}
