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

        Task SetInputFilesAsync(string selector, string files, FrameSetInputFilesOptions options = default);

        Task SetInputFilesAsync(string selector, IEnumerable<string> files, FrameSetInputFilesOptions options = default);

        Task SetInputFilesAsync(string selector, FilePayload files, FrameSetInputFilesOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, string values, FrameSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<string> values, FrameSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, FrameSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IElementHandle values, FrameSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, FrameSelectOptionOptions options = default);

        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, SelectOptionValue values, FrameSelectOptionOptions options = default);
    }
}
