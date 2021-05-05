using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IPage
    {
        /// <inheritdoc cref="EmulateMediaAsync(Media?, ColorScheme?)"/>
        Task EmulateMediaAsync(ColorScheme? colorScheme);

        /// <inheritdoc cref="EvaluateAsync{T}(string, object)"/>
        public Task<JsonElement?> EvaluateAsync(string expression, object arg = default);

        /// <inheritdoc cref="EvalOnSelectorAsync{T}(string, string, object)"/>
        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg);

        /// <inheritdoc cref="EvalOnSelectorAllAsync{T}(string, string, object)"/>
        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = default);

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

        /// <inheritdoc cref="WaitForEventAsync(string, float?)"/>
        Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> pageEvent, Func<T, bool> predicate = default, float? timeout = default);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, bool?)"/>
        Task ExposeBindingAsync(string name, Action<BindingSource> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, bool?)"/>
        Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, bool?)"/>
        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, bool?)"/>
        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, bool?)"/>
        Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, bool?)"/>
        Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, bool?)"/>
        Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, bool?)"/>
        Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T>(string name, Action<T> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<TResult>(string name, Func<TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> callback);
    }
}
