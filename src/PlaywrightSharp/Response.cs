using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IResponse"/>
    public class Response : IResponse
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

        /// <inheritdoc cref="IResponse.Status"/>
        public HttpStatusCode Status { get; }

        /// <inheritdoc cref="IResponse.StatusText"/>
        public string StatusText { get; }

        /// <inheritdoc cref="IResponse.Frame"/>
        public IFrame Frame => Request.Frame;

        /// <inheritdoc cref="IResponse.Url"/>
        public string Url { get; }

        /// <inheritdoc cref="IResponse.Headers"/>
        public IDictionary<string, string> Headers { get; }

        /// <inheritdoc cref="IResponse.Ok"/>
        public bool Ok { get; }

        /// <inheritdoc cref="IResponse.Request"/>
        IRequest IResponse.Request => Request;

        /// <inheritdoc cref="IResponse.Request"/>
        public Request Request { get; }

        internal Task Finished => _finishedTsc.Task;

        /// <inheritdoc cref="IResponse.GetBufferAsync()"/>
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

        /// <inheritdoc cref="IResponse.GetJsonAsync(JsonDocumentOptions)"/>
        public async Task<JsonDocument> GetJsonAsync(JsonDocumentOptions options = default)
        {
            string content = await GetTextAsync().ConfigureAwait(false);
            return JsonDocument.Parse(content, options);
        }

        /// <inheritdoc cref="IResponse.GetJsonAsync{T}(JsonSerializerOptions)"/>
        public async Task<T> GetJsonAsync<T>(JsonSerializerOptions options = null)
        {
            string content = await GetTextAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(content, options ?? JsonHelper.DefaultJsonSerializerOptions);
        }

        /// <inheritdoc cref="IResponse.GetTextAsync()"/>
        public async Task<string> GetTextAsync()
        {
            var buffer = await GetBufferAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(buffer);
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
