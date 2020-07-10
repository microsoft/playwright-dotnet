using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;

namespace PlaywrightSharp.Helpers
{
    internal class AsyncDictionary<TKey, TValue>
    {
        private readonly string _timeoutMessage;
        private readonly MultiMap<TKey, TaskCompletionSource<TValue>> _pendingRequests;
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

        public AsyncDictionary(ConcurrentDictionary<TKey, TValue> dictionary, string timeoutMessage)
        {
            _dictionary = dictionary;
            _timeoutMessage = timeoutMessage;
            _pendingRequests = new MultiMap<TKey, TaskCompletionSource<TValue>>();
        }

        internal async Task<TValue> GetItemAsync(TKey key)
        {
            var tcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests.Add(key, tcs);

            if (_dictionary.TryGetValue(key, out var item))
            {
                _pendingRequests.Delete(key, tcs);
                return item;
            }

            return await tcs.Task.WithTimeout(new Action(() =>
                throw new PlaywrightSharpException(string.Format(CultureInfo.InvariantCulture, _timeoutMessage, key)))).ConfigureAwait(false);
        }

        internal void AddItem(TKey key, TValue value)
        {
            _dictionary[key] = value;
            foreach (var tcs in _pendingRequests.Get(key))
            {
                tcs.TrySetResult(value);
            }
        }
    }
}
