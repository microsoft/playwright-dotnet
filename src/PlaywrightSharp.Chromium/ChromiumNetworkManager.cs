using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Fetch;
using PlaywrightSharp.Chromium.Protocol.Network;
using AuthChallengeResponse = PlaywrightSharp.Chromium.Protocol.Fetch.AuthChallengeResponse;
using RequestPattern = PlaywrightSharp.Chromium.Protocol.Fetch.RequestPattern;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumNetworkManager
    {
        private readonly ChromiumSession _client;
        private readonly Page _page;
        private readonly ConcurrentDictionary<string, ChromiumInterceptableRequest> _requestIdToRequest =
            new ConcurrentDictionary<string, ChromiumInterceptableRequest>();

        private readonly ConcurrentDictionary<string, string> _requestIdToInterceptionId = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, NetworkRequestWillBeSentChromiumEvent> _requestIdToRequestWillBeSentEvent =
            new ConcurrentDictionary<string, NetworkRequestWillBeSentChromiumEvent>();

        private readonly IList<string> _attemptedAuthentications = new List<string>();
        private bool _userCacheDisabled;
        private bool _protocolRequestInterceptionEnabled = false;
        private bool _userRequestInterceptionEnabled = false;
        private Credentials _credentials = null;
        private bool _offline;

        public ChromiumNetworkManager(ChromiumSession client, Page page)
        {
            _client = client;
            _page = page;
            _client.MessageReceived += Client_MessageReceived;
        }

        internal Task InitializeAsync() => _client.SendAsync(new NetworkEnableRequest());

        internal Task SetUserAgentAsync(string userAgent)
            => _client.SendAsync(new NetworkSetUserAgentOverrideRequest { UserAgent = userAgent });

        internal Task SetCacheEnabledAsync(bool enabled)
        {
            _userCacheDisabled = !enabled;
            return UpdateProtocolCacheDisabledAsync();
        }

        internal Task SetRequestInterceptionAsync(bool value)
        {
            _userRequestInterceptionEnabled = value;
            return UpdateProtocolRequestInterceptionAsync();
        }

        internal Task AuthenticateAsync(Credentials credentials)
        {
            _credentials = credentials;
            return UpdateProtocolRequestInterceptionAsync();
        }

        internal Task SetOfflineModeAsync(bool value)
        {
            _offline = value;
            return _client.SendAsync(new NetworkEmulateNetworkConditionsRequest
            {
                Offline = _offline,
                Latency = 0,
                DownloadThroughput = -1,
                UploadThroughput = -1,
            });
        }

        private async void Client_MessageReceived(object sender, IChromiumEvent e)
        {
            try
            {
                switch (e)
                {
                    case NetworkResponseReceivedChromiumEvent networkResponseReceived:
                        OnResponseReceived(networkResponseReceived);
                        break;
                    case NetworkRequestWillBeSentChromiumEvent networkRequestWillBeSent:
                        OnRequestWillBeSent(networkRequestWillBeSent);
                        break;
                    case FetchRequestPausedChromiumEvent fetchRequestPaused:
                        await OnRequestPausedAsync(fetchRequestPaused).ConfigureAwait(false);
                        break;
                    case NetworkLoadingFinishedChromiumEvent networkLoadingFinished:
                        OnLoadingFinished(networkLoadingFinished);
                        break;
                    case NetworkLoadingFailedChromiumEvent networkLoadingFailed:
                        OnLoadingFailed(networkLoadingFailed);
                        break;
                    case FetchAuthRequiredChromiumEvent fetchAuthRequired:
                        await OnAuthRequiredAsync(fetchAuthRequired).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                // TODO Add Logger
                /*
                var message = $"Page failed to process {e.MessageID}. {ex.Message}. {ex.StackTrace}";
                _logger.LogError(ex, message);
                */
                System.Diagnostics.Debug.WriteLine(ex);
                _client.OnClosed(ex.ToString());
            }
        }

        private async Task OnAuthRequiredAsync(FetchAuthRequiredChromiumEvent e)
        {
            string response = "Default";
            if (_attemptedAuthentications.Contains(e.RequestId))
            {
                response = "CancelAuth";
            }
            else if (_credentials != null)
            {
                response = "ProvideCredentials";
                _attemptedAuthentications.Add(e.RequestId);
            }

            try
            {
                await _client.SendAsync(new FetchContinueWithAuthRequest
                {
                    RequestId = e.RequestId,
                    AuthChallengeResponse = new AuthChallengeResponse
                    {
                        Response = response,
                        Username = _credentials?.Username,
                        Password = _credentials?.Password,
                    },
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void OnLoadingFailed(NetworkLoadingFailedChromiumEvent e)
        {
            // For certain requestIds we never receive requestWillBeSent event.
            // @see https://crbug.com/750469
            if (!_requestIdToRequest.TryRemove(e.RequestId, out var request))
            {
                return;
            }

            request.Request.Response?.RequestFinished();

            if (!string.IsNullOrEmpty(request.InterceptionId))
            {
                _attemptedAuthentications.Remove(request.InterceptionId);
            }

            request.Request.SetFailureText(e.ErrorText);
            _page.FrameManager.RequestFailed(request.Request, e.Canceled.Value);
        }

        private void OnLoadingFinished(NetworkLoadingFinishedChromiumEvent e)
        {
            // For certain requestIds we never receive requestWillBeSent event.
            // @see https://crbug.com/750469
            if (!_requestIdToRequest.TryRemove(e.RequestId, out var request))
            {
                return;
            }

            // Under certain conditions we never get the Network.responseReceived
            // event from protocol. @see https://crbug.com/883475
            var response = request.Request.Response;
            response?.RequestFinished();

            if (!string.IsNullOrEmpty(request.InterceptionId))
            {
                _attemptedAuthentications.Remove(request.InterceptionId);
            }

            _page.FrameManager.RequestFinished(request.Request);
        }

        private void OnRequest(NetworkRequestWillBeSentChromiumEvent e, string interceptionId)
        {
            if (e.Request.Url.StartsWith("data:"))
            {
                return;
            }

            var redirectChain = new List<Request>();

            if (e.RedirectResponse != null)
            {
                // If we connect late to the target, we could have missed the requestWillBeSent event.
                if (_requestIdToRequest.TryGetValue(e.RequestId, out var requestRedirected))
                {
                    HandleRequestRedirect(requestRedirected, e.RedirectResponse);
                    redirectChain = requestRedirected.Request.RedirectChain;
                }
            }

            if (!_page.FrameManager.Frames.TryGetValue(e.FrameId, out var frame))
            {
                return;
            }

            bool isNavigationRequest = e.RequestId == e.LoaderId && e.Type == Protocol.Network.ResourceType.Document;
            string documentId = isNavigationRequest ? e.LoaderId : null;
            var request = new ChromiumInterceptableRequest(
                _client,
                frame,
                interceptionId,
                documentId,
                _userRequestInterceptionEnabled,
                e,
                redirectChain);
            _requestIdToRequest.TryAdd(e.RequestId, request);
            _page.FrameManager.RequestStarted(request.Request);
        }

        private void HandleRequestRedirect(ChromiumInterceptableRequest request, Protocol.Network.Response responsePayload)
        {
            var response = CreateResponse(request, responsePayload);
            request.Request.RedirectChain.Add(request.Request);
            response.RequestFinished(new PlaywrightSharpException("Response body is unavailable for redirect responses"));
            _requestIdToRequest.TryRemove(request.RequestId, out _);

            if (!string.IsNullOrEmpty(request.InterceptionId))
            {
                _attemptedAuthentications.Remove(request.InterceptionId);
            }

            _page.FrameManager.RequestReceivedResponse(response);
            _page.FrameManager.RequestFinished(request.Request);
        }

        private void OnResponseReceived(NetworkResponseReceivedChromiumEvent e)
        {
            // FileUpload sends a response without a matching request.
            if (!_requestIdToRequest.TryGetValue(e.RequestId, out var request))
            {
                return;
            }

            var response = CreateResponse(request, e.Response);
            _page.FrameManager.RequestReceivedResponse(response);
        }

        private Response CreateResponse(ChromiumInterceptableRequest request, Protocol.Network.Response responsePayload)
        {
            var getResponseBody = new Func<Task<byte[]>>(async () =>
            {
                var response = await _client.SendAsync(new NetworkGetResponseBodyRequest { RequestId = request.RequestId }).ConfigureAwait(false);
                return response.Base64Encoded.Value
                    ? Convert.FromBase64String(response.Body)
                    : Encoding.UTF8.GetBytes(response.Body);
            });

            return new Response(request.Request, (HttpStatusCode)responsePayload.Status.Value, responsePayload.StatusText, responsePayload.Headers, getResponseBody);
        }

        private async Task OnRequestPausedAsync(FetchRequestPausedChromiumEvent e)
        {
            if (!_userRequestInterceptionEnabled && _protocolRequestInterceptionEnabled)
            {
                try
                {
                    await _client.SendAsync(new FetchContinueRequestRequest { RequestId = e.RequestId }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            if (string.IsNullOrEmpty(e.NetworkId) || e.Request.Url.StartsWith("data:"))
            {
                return;
            }

            string requestId = e.NetworkId;
            string interceptionId = e.RequestId;

            if (_requestIdToRequestWillBeSentEvent.TryRemove(requestId, out var requestWillBeSentEvent))
            {
                OnRequest(requestWillBeSentEvent, interceptionId);
            }
            else
            {
                _requestIdToInterceptionId[requestId] = interceptionId;
            }
        }

        private void OnRequestWillBeSent(NetworkRequestWillBeSentChromiumEvent e)
        {
            // Request interception doesn't happen for data URLs with Network Service.
            if (_protocolRequestInterceptionEnabled && !e.Request.Url.StartsWith(":data:"))
            {
                string requestId = e.RequestId;
                if (_requestIdToInterceptionId.TryRemove(requestId, out string interceptionId))
                {
                    OnRequest(e, interceptionId);
                }
                else
                {
                    _requestIdToRequestWillBeSentEvent[e.RequestId] = e;
                }

                return;
            }

            OnRequest(e, null);
        }

        private Task UpdateProtocolCacheDisabledAsync()
            => _client.SendAsync(new NetworkSetCacheDisabledRequest
            {
                CacheDisabled = _userCacheDisabled || _protocolRequestInterceptionEnabled,
            });

        private async Task UpdateProtocolRequestInterceptionAsync()
        {
            bool enabled = _userRequestInterceptionEnabled || _credentials != null;
            if (enabled == _protocolRequestInterceptionEnabled)
            {
                return;
            }

            _protocolRequestInterceptionEnabled = enabled;

            if (enabled)
            {
                await Task.WhenAll(
                    UpdateProtocolCacheDisabledAsync(),
                    _client.SendAsync(new FetchEnableRequest
                    {
                        HandleAuthRequests = true,
                        Patterns = new[]
                        {
                            new RequestPattern
                            {
                                UrlPattern = "*",
                            },
                        },
                    })).ConfigureAwait(false);
            }
            else
            {
                await Task.WhenAll(
                    UpdateProtocolCacheDisabledAsync(),
                    _client.SendAsync(new FetchDisableRequest())).ConfigureAwait(false);
            }
        }
    }
}
