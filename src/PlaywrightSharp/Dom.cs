using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal static class Dom
    {
        private static readonly Regex _selectorMatch = new Regex("/^[a-zA-Z_0-9-]+$/", RegexOptions.Compiled);

        internal static Func<IFrameExecutionContext, Task<IJSHandle>> GetWaitForSelectorFunction(string selector, WaitForOption waitFor, int? timeout)
            => async (IFrameExecutionContext context) =>
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
