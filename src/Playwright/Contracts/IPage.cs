using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IPage
    {
        public Task<JsonElement?> EvaluateAsync(string expression, object arg = default);

        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg);

        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = default);

        Task SetInputFilesAsync(string selector, string files, PageSetInputFilesOptions options = default);

        Task SetInputFilesAsync(string selector, IEnumerable<string> files, PageSetInputFilesOptions options = default);

        Task SetInputFilesAsync(string selector, FilePayload files, PageSetInputFilesOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, string values, PageSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<string> values, PageSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, PageSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IElementHandle values, PageSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, PageSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, SelectOptionValue values, PageSelectOptionOptions options = default);

        Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> pageEvent, PageWaitForEventOptions options = default, Func<Task> action = default);

        Task ExposeBindingAsync(string name, Action<BindingSource> callback);

        Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback);

        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback);

        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback);

        Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback);

        Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback);

        Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback);

        Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback);

        Task ExposeFunctionAsync<T>(string name, Action<T> callback);

        Task ExposeFunctionAsync<TResult>(string name, Func<TResult> callback);

        Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> callback);

        Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> callback);

        Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> callback);

        Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> callback);
    }
}
