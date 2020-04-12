using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class Response : IResponse
    {
        private readonly TaskCompletionSource<bool> _finishedTsc;
        private readonly Func<Task<byte[]>> _getResponseBodyCallback;
        private Task<byte[]> _contentTask;

        internal Response(Request request, HttpStatusCode status, string statusText, IDictionary<string, string> headers, Func<Task<byte[]>> getResponseBodyCallback)
        {
            Request = request;
            Status = status;
            StatusText = statusText;
            Url = request.Url;
            Headers = headers;
            Ok = status == 0 || (status >= HttpStatusCode.OK && status < (HttpStatusCode)300);
            _getResponseBodyCallback = getResponseBodyCallback;
            _finishedTsc = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            request.SetResponse(this);
        }

        public HttpStatusCode Status { get; }

        public string StatusText { get; }

        public IFrame Frame => Request.Frame;

        public string Url { get; }

        public IDictionary<string, string> Headers { get; }

        public bool Ok { get; }

        IRequest IResponse.Request => Request;

        public Request Request { get; }

        internal Task Finished => _finishedTsc.Task;

        public Task<byte[]> GetBufferAsync()
        {
            if (_contentTask == null)
            {
                _contentTask = ContentTask();
            }

            return _contentTask;
            async Task<byte[]> ContentTask()
            {
                await Finished.ConfigureAwait(false);
                return await _getResponseBodyCallback().ConfigureAwait(false);
            }
        }

        public Task<JsonDocument> GetJsonAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<T> GetJsonAsync<T>()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetTextAsync()
        {
            throw new System.NotImplementedException();
        }

        internal void RequestFinished(Exception exception = null)
        {
            if (exception == null)
            {
                _finishedTsc.TrySetResult(true);
            }
            else
            {
                _finishedTsc.TrySetException(exception);
            }
        }
    }
}
