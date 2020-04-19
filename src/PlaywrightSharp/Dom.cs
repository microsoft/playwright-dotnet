using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp
{
    internal static class Dom
    {
        internal const string SetFileInputFunction = @"async (element, payloads) => {
            const files = await Promise.all(payloads.map(async (file) => {
                const result = await fetch(`data:${file.type};base64,${file.data}`);
                return new File([await result.blob()], file.name);
            }));
            const dt = new DataTransfer();
            for (const file of files)
                dt.items.add(file);
            element.files = dt.files;
            element.dispatchEvent(new Event('input', { 'bubbles': true }));
        }";

        private static readonly Regex _selectorMatch = new Regex("^[a-zA-Z_0-9-]+$", RegexOptions.Compiled);

        internal static Func<IFrameExecutionContext, Task<IJSHandle>> GetWaitForSelectorFunction(string selector, WaitForOption waitFor, int? timeout)
            => async context =>
            {
                selector = NormalizeSelector(selector);
                return await context.EvaluateHandleAsync(
                    @"(injected, selector, visibility, timeout) => {
                        if (visibility !== 'any')
                            return injected.pollRaf(selector, predicate, timeout);
                        return injected.pollMutation(selector, predicate, timeout);

                        function predicate(element) {
                            if (!element)
                                return visibility === 'hidden';
                            if (visibility === 'any')
                                return element;
                            return injected.isVisible(element) === (visibility === 'visible') ? element : false;
                        }
                    }",
                    await context.GetInjectedAsync().ConfigureAwait(false),
                    selector,
                    waitFor.ToString().ToLower(),
                    timeout).ConfigureAwait(false);
            };

        internal static Func<IFrameExecutionContext, Task<IJSHandle>> GetWaitForFunctionTask(string selector, string pageFunction, WaitForFunctionOptions options, params object[] args)
        {
            var polling = options?.Polling ?? WaitForFunctionPollingOption.Raf;
            string predicateBody = pageFunction.IsJavascriptFunction() ? $"return ({pageFunction})(...args)" : $"return ({pageFunction})";
            if (selector != null)
            {
                selector = NormalizeSelector(selector);
            }

            return async context => await context.EvaluateHandleAsync(
                @"(injected, selector, predicateBody, polling, timeout, ...args) => {
                    const innerPredicate = new Function('...args', predicateBody);
                    if (polling === 'raf')
                      return injected.pollRaf(selector, predicate, timeout);
                    if (polling === 'mutation')
                      return injected.pollMutation(selector, predicate, timeout);
                    return injected.pollInterval(selector, polling, predicate, timeout);
                    function predicate(element) {
                      if (selector === undefined)
                        return innerPredicate(...args);
                      return innerPredicate(element, ...args);
                    }
                  }",
                args.Prepend(await context.GetInjectedAsync().ConfigureAwait(false), selector, predicateBody, polling)).ConfigureAwait(false);
        }

        internal static string NormalizeSelector(string selector)
        {
            int eqIndex = selector.IndexOf('=');
            if (eqIndex != -1 && _selectorMatch.IsMatch(selector.Substring(0, eqIndex).Trim()))
            {
                return selector;
            }

            if (selector.StartsWith("//"))
            {
                return "xpath=" + selector;
            }

            if (selector.StartsWith("\""))
            {
                return "text=" + selector;
            }

            return "css=" + selector;
        }
    }
}
