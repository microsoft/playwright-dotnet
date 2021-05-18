using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IFrame
    {
        public Task<JsonElement?> EvaluateAsync(string expression, object arg = default);

        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg);

        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = default);

        Task SetInputFilesAsync(string selector, string files, bool? noWaitAfter = default, float? timeout = default);

        Task SetInputFilesAsync(string selector, IEnumerable<string> files, bool? noWaitAfter = default, float? timeout = default);

        Task SetInputFilesAsync(string selector, FilePayload files, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, string values, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<string> values, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IElementHandle values, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, bool? noWaitAfter = default, float? timeout = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, SelectOptionValue values, bool? noWaitAfter = default, float? timeout = default);
    }
}
