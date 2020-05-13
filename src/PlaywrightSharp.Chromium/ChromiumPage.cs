using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Input;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Accessibility;
using PlaywrightSharp.Chromium.Protocol.Debugger;
using PlaywrightSharp.Chromium.Protocol.DOM;
using PlaywrightSharp.Chromium.Protocol.Emulation;
using PlaywrightSharp.Chromium.Protocol.Log;
using PlaywrightSharp.Chromium.Protocol.Network;
using PlaywrightSharp.Chromium.Protocol.Page;
using PlaywrightSharp.Chromium.Protocol.Runtime;
using PlaywrightSharp.Chromium.Protocol.Security;
using PlaywrightSharp.Chromium.Protocol.Target;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IPageDelegate"/>
    internal class ChromiumPage : IPageDelegate
    {
        private const string UtilityWorldName = "__playwright_utility_world__";
        private const string EvaluationScriptUrl = "__playwright_evaluation_script__";

        private readonly ChromiumBrowser _browser;
        private readonly IBrowserContext _browserContext;
        private readonly ChromiumNetworkManager _networkManager;
        private readonly ISet<string> _isolatedWorlds = new HashSet<string>();

        public ChromiumPage(ChromiumSession client, ChromiumBrowser browser, IBrowserContext browserContext)
        {
            Client = client;
            _browser = browser;
            _browserContext = browserContext;
            RawKeyboard = new ChromiumRawKeyboard(client);
            RawMouse = new ChromiumRawMouse(client);
            Page = new Page(this, browserContext);
            _networkManager = new ChromiumNetworkManager(Client, Page);

            client.MessageReceived += Client_MessageReceived;
        }

        public ChromiumTarget Target { get; set; }

        public IRawKeyboard RawKeyboard { get; }

        public IRawMouse RawMouse { get; }

        public ConcurrentDictionary<object, FrameExecutionContext> ContextIdToContext { get; } = new ConcurrentDictionary<object, FrameExecutionContext>();

        internal Page Page { get; }

        internal ChromiumSession Client { get; }

        public Task<AccessibilityTree> GetAccessibilityTreeAsync(IElementHandle needle) => GetAccessibilityTreeAsync(Client, needle);

        public async Task<GotoResult> NavigateFrameAsync(IFrame frame, string url, string referrer)
        {
            var response = await Client.SendAsync(new PageNavigateRequest
            {
                Url = url,
                Referrer = referrer ?? string.Empty,
                FrameId = frame.Id,
            }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(response.ErrorText))
            {
                throw new NavigationException(response.ErrorText, url);
            }

            return new GotoResult
            {
                NewDocumentId = response.LoaderId,
                IsSameDocument = string.IsNullOrEmpty(response.LoaderId),
            };
        }

        public Task SetViewportAsync(Viewport viewport)
        {
            bool isLandscape = viewport.Width > viewport.Height;
            var screenOrientation = isLandscape
                ? new ScreenOrientation { Angle = 90, Type = "landscapePrimary" }
                : new ScreenOrientation { Angle = 0, Type = "portraitPrimary" };

            return Task.WhenAll(
                Client.SendAsync(new EmulationSetDeviceMetricsOverrideRequest
                {
                    Mobile = viewport.IsMobile,
                    Width = viewport.Width,
                    Height = viewport.Height,
                    DeviceScaleFactor = viewport.DeviceScaleFactor,
                    ScreenOrientation = screenOrientation,
                }),
                Client.SendAsync(new EmulationSetTouchEmulationEnabledRequest { Enabled = viewport.IsMobile }));
        }

        public Task ClosePageAsync(bool runBeforeUnload)
            => runBeforeUnload ? Client.SendAsync(new PageCloseRequest()) : _browser.ClosePageAsync(this);

        public async Task<ElementHandle> AdoptElementHandleAsync(ElementHandle handle, FrameExecutionContext to)
        {
            var nodeInfo = await Client.SendAsync(new DOMDescribeNodeRequest
            {
                ObjectId = handle.RemoteObject.ObjectId,
            }).ConfigureAwait(false);

            return await AdoptBackendNodeIdAsync(nodeInfo.Node.BackendNodeId.Value, to).ConfigureAwait(false);
        }

        public bool IsElementHandle(IRemoteObject remoteObject) => remoteObject?.Subtype == "node";

        public async Task<Quad[][]> GetContentQuadsAsync(ElementHandle handle)
        {
            DOMGetContentQuadsResponse result = null;

            try
            {
                result = await Client.SendAsync(new DOMGetContentQuadsRequest
                {
                    ObjectId = handle.RemoteObject.ObjectId,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return result?.Quads.Select(quad => new[]
            {
                new Quad
                {
                    X = quad[0].Value,
                    Y = quad[1].Value,
                },
                new Quad
                {
                    X = quad[2].Value,
                    Y = quad[3].Value,
                },
                new Quad
                {
                    X = quad[4].Value,
                    Y = quad[5].Value,
                },
                new Quad
                {
                    X = quad[6].Value,
                    Y = quad[7].Value,
                },
            }).ToArray();
        }

        public async Task<LayoutMetric> GetLayoutViewportAsync()
        {
            var layoutMetrics = await Client.SendAsync(new PageGetLayoutMetricsRequest()).ConfigureAwait(false);

            return new LayoutMetric
            {
                Width = layoutMetrics.LayoutViewport.ClientWidth.Value,
                Height = layoutMetrics.LayoutViewport.ClientHeight.Value,
            };
        }

        public bool CanScreenshotOutsideViewport() => false;

        public Task ResetViewportAsync(Viewport viewport)
            => Client.SendAsync(new EmulationSetDeviceMetricsOverrideRequest
            {
                Mobile = false,
                Width = 0,
                Height = 0,
                DeviceScaleFactor = 0,
            });

        public Task SetBackgroundColorAsync(Color? color = null)
            => Client.SendAsync(new EmulationSetDefaultBackgroundColorOverrideRequest { Color = color });

        public async Task<byte[]> TakeScreenshotAsync(ScreenshotFormat format, ScreenshotOptions options, Viewport viewport)
        {
            await Client.SendAsync(new PageBringToFrontRequest()).ConfigureAwait(false);
            var clip = options.Clip?.ToViewportProtocol();

            var result = await Client.SendAsync(new PageCaptureScreenshotRequest
            {
                Format = format.ToStringFormat(),
                Quality = options.Quality,
                Clip = clip,
            }).ConfigureAwait(false);
            return result.Data;
        }

        public async Task<Rect> GetBoundingBoxForScreenshotAsync(ElementHandle handle)
        {
            var rect = await handle.GetBoundingBoxAsync().ConfigureAwait(false);
            if (rect == null)
            {
                return rect;
            }

            var layout = await Client.SendAsync(new PageGetLayoutMetricsRequest()).ConfigureAwait(false);

            rect.X += layout.LayoutViewport.PageX.Value;
            rect.Y += layout.LayoutViewport.PageY.Value;

            return rect;
        }

        public async Task ExposeBindingAsync(string name, string functionString)
        {
            await Client.SendAsync(new RuntimeAddBindingRequest { Name = name }).ConfigureAwait(false);
            await Client.SendAsync(new PageAddScriptToEvaluateOnNewDocumentRequest { Source = functionString }).ConfigureAwait(false);

            try
            {
                await Task.WhenAll(Page.Frames.Select(frame => frame.EvaluateAsync(functionString))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public Task EvaluateOnNewDocumentAsync(string source)
            => Client.SendAsync(new PageAddScriptToEvaluateOnNewDocumentRequest { Source = source });

        public async Task<string> GetOwnerFrameAsync(ElementHandle handle)
        {
            // document.documentElement has frameId of the owner frame.
            var documentElement = await handle.EvaluateHandleAsync(@"node => {
                const doc = node;
                if (doc.documentElement && doc.documentElement.ownerDocument === doc)
                    return doc.documentElement;
                return node.ownerDocument ? node.ownerDocument.documentElement : null;
            }").ConfigureAwait(false) as ElementHandle;

            if (documentElement == null)
            {
                return null;
            }

            var remoteObject = documentElement.RemoteObject;
            if (string.IsNullOrEmpty(remoteObject.ObjectId))
            {
                return null;
            }

            var nodeInfo = await Client.SendAsync(new DOMDescribeNodeRequest
            {
                ObjectId = remoteObject.ObjectId,
            }).ConfigureAwait(false);

            string frameId = nodeInfo?.Node?.FrameId;
            await documentElement.DisposeAsync().ConfigureAwait(false);
            return frameId;
        }

        public async Task<IFrame> GetContentFrameAsync(ElementHandle handle)
        {
            var nodeInfo = await Client.SendAsync(new DOMDescribeNodeRequest
            {
                ObjectId = handle.RemoteObject.ObjectId,
            }).ConfigureAwait(false);

            if (nodeInfo == null || string.IsNullOrEmpty(nodeInfo.Node.FrameId))
            {
                return null;
            }

            return Page.FrameManager.Frames[nodeInfo.Node.FrameId];
        }

        public async Task<Rect> GetBoundingBoxAsync(ElementHandle handle)
        {
            DOMGetBoxModelResponse result = null;

            try
            {
                result = await Client.SendAsync(new DOMGetBoxModelRequest
                {
                    ObjectId = handle.RemoteObject.ObjectId,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if (result == null)
            {
                return null;
            }

            double?[] quad = result.Model.Border;
            double x = Math.Min(quad[0].Value, Math.Min(quad[2].Value, Math.Min(quad[4].Value, quad[6].Value)));
            double y = Math.Min(quad[1].Value, Math.Min(quad[3].Value, Math.Min(quad[5].Value, quad[7].Value)));
            double width = Math.Max(quad[0].Value, Math.Max(quad[2].Value, Math.Max(quad[4].Value, quad[6].Value))) - x;
            double height = Math.Max(quad[1].Value, Math.Max(quad[3].Value, Math.Max(quad[5].Value, quad[7].Value))) - y;

            return new Rect
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
            };
        }

        public Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers)
            => Client.SendAsync(new NetworkSetExtraHTTPHeadersRequest
            {
                Headers = headers,
            });

        public Task ReloadAsync() => Client.SendAsync(new PageReloadRequest());

        public Task<bool> GoBackAsync() => GoAsync(-1);

        public Task<bool> GoForwardAsync() => GoAsync(1);

        public Task SetInputFilesAsync(ElementHandle handle, IEnumerable<FilePayload> files)
            => handle.EvaluateHandleAsync(Dom.SetFileInputFunction, files);

        public Task SetFileChooserInterceptedAsync(bool enabled)
            => Client.SendAsync(new PageSetInterceptFileChooserDialogRequest { Enabled = enabled });

        public Task SetCacheEnabledAsync(bool enabled) => _networkManager.SetCacheEnabledAsync(enabled);

        public Task SetRequestInterceptionAsync(bool enabled) => _networkManager.SetRequestInterceptionAsync(enabled);

        public Task AuthenticateAsync(Credentials credentials) => _networkManager.AuthenticateAsync(credentials);

        public Task SetOfflineModeAsync(bool enabled) => _networkManager.SetOfflineModeAsync(enabled);

        public Task SetEmulateMediaAsync(MediaType? mediaType, ColorScheme? colorScheme)
        {
            var features = colorScheme != null
                ? new[]
                {
                    new MediaFeature
                    {
                        Name = "prefers-color-scheme",
                        Value = colorScheme.Value.ToValueString(),
                    },
                }
                : Array.Empty<MediaFeature>();

            return Client.SendAsync(new EmulationSetEmulatedMediaRequest
            {
                Media = mediaType != null ? mediaType.Value.ToValueString() : string.Empty,
                Features = features,
            });
        }

        internal async Task InitializeAsync()
        {
            var getFrameTreeTask = Client.SendAsync(new PageGetFrameTreeRequest());

            await Task.WhenAll(
                Client.SendAsync(new PageEnableRequest()),
                getFrameTreeTask).ConfigureAwait(false);

            HandleFrameTree(getFrameTreeTask.Result.FrameTree);

            var tasks = new List<Task>
            {
               Client.SendAsync(new LogEnableRequest()),
               Client.SendAsync(new PageSetLifecycleEventsEnabledRequest { Enabled = true }),
               Client.SendAsync(new RuntimeEnableRequest()),
               _networkManager.InitializeAsync(),
            };

            await EnsureIsolatedWorldAsync(UtilityWorldName).ConfigureAwait(false);

            if (_browserContext.Options != null)
            {
                var options = _browserContext.Options;

                if (options.BypassCSP)
                {
                    tasks.Add(Client.SendAsync(new PageSetBypassCSPRequest { Enabled = true }));
                }

                if (options.IgnoreHTTPSErrors)
                {
                    tasks.Add(Client.SendAsync(new SecuritySetIgnoreCertificateErrorsRequest { Ignore = true }));
                }

                if (options.Viewport != null)
                {
                    tasks.Add(SetViewportAsync(options.Viewport));
                }

                if (!options.JavaScriptEnabled)
                {
                    tasks.Add(Client.SendAsync(new EmulationSetScriptExecutionDisabledRequest { Value = true }));
                }

                if (options.UserAgent != null)
                {
                    tasks.Add(_networkManager.SetUserAgentAsync(options.UserAgent));
                }

                if (options.TimezoneId != null)
                {
                    tasks.Add(EmulateTimezoneAsync(options.TimezoneId));
                }

                if (options.Geolocation != null)
                {
                    tasks.Add(Client.SendAsync(new EmulationSetGeolocationOverrideRequest
                    {
                        Accuracy = options.Geolocation.Accuracy,
                        Latitude = options.Geolocation.Latitude,
                        Longitude = options.Geolocation.Longitude,
                    }));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        internal void DidClose() => Page.DidClose();

        private async Task<bool> GoAsync(int delta)
        {
            var history = await Client.SendAsync(new PageGetNavigationHistoryRequest()).ConfigureAwait(false);
            var entry = history.Entries.ElementAtOrDefault((int)history.CurrentIndex + delta);
            if (entry == null)
            {
                return false;
            }

            await Client.SendAsync(new PageNavigateToHistoryEntryRequest { EntryId = entry.Id }).ConfigureAwait(false);
            return true;
        }

        private async Task<ElementHandle> AdoptBackendNodeIdAsync(int backendNodeId, FrameExecutionContext to)
        {
            DOMResolveNodeResponse result = null;

            try
            {
                result = await Client.SendAsync(new DOMResolveNodeRequest
                {
                    BackendNodeId = backendNodeId,
                    ExecutionContextId = ((ChromiumExecutionContext)to.Delegate).ContextId,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if (result == null || result.Object.Subtype == "null")
            {
                throw new PlaywrightSharpException("Unable to adopt element handle from a different document");
            }

            return to.CreateHandle(result.Object) as ElementHandle;
        }

        private async void Client_MessageReceived(object sender, IChromiumEvent e)
        {
            try
            {
                switch (e)
                {
                    case PageFrameAttachedChromiumEvent pageFrameAttached:
                        OnFrameAttached(pageFrameAttached);
                        break;
                    case PageFrameDetachedChromiumEvent pageFrameDetached:
                        OnFrameDetached(pageFrameDetached);
                        break;
                    case PageFrameStoppedLoadingChromiumEvent pageFrameStoppedLoading:
                        OnFrameStoppedLoading(pageFrameStoppedLoading);
                        break;
                    case PageFrameNavigatedChromiumEvent pageFrameNavigated:
                        OnFrameNavigated(pageFrameNavigated.Frame, false);
                        break;
                    case PageNavigatedWithinDocumentChromiumEvent pageNavigatedWithinDocument:
                        OnFrameNavigatedWithinDocument(pageNavigatedWithinDocument.FrameId, pageNavigatedWithinDocument.Url);
                        break;
                    case PageLifecycleEventChromiumEvent pageLifecycleEvent:
                        OnLifecycleEvent(pageLifecycleEvent);
                        break;
                    case RuntimeExecutionContextCreatedChromiumEvent runtimeExecutionContextCreated:
                        OnExecutionContextCreated(runtimeExecutionContextCreated.Context);
                        break;
                    case RuntimeExecutionContextDestroyedChromiumEvent runtimeExecutionContextDestroyed:
                        OnExecutionContextDestroyed(runtimeExecutionContextDestroyed.ExecutionContextId.Value);
                        break;
                    case RuntimeExecutionContextsClearedChromiumEvent runtimeExecutionContextsCleared:
                        OnExecutionContextsCleared();
                        break;
                    case TargetAttachedToTargetChromiumEvent targetAttachedToTarget:
                        await OnAttachedToTargetAsync(targetAttachedToTarget).ConfigureAwait(false);
                        break;
                    case RuntimeConsoleAPICalledChromiumEvent runtimeConsoleAPICalled:
                        OnConsoleAPI(runtimeConsoleAPICalled);
                        break;
                    case RuntimeBindingCalledChromiumEvent runtimeBindingCalled:
                        await OnBindingCalledAsync(runtimeBindingCalled).ConfigureAwait(false);
                        break;
                    case PageFileChooserOpenedChromiumEvent pageFileChooserOpened:
                        await OnFileChooserOpenedAsync(pageFileChooserOpened).ConfigureAwait(false);
                        break;
                    case PageJavascriptDialogOpeningChromiumEvent pageJavascriptDialogOpening:
                        OnDialog(pageJavascriptDialogOpening);
                        break;
                    case TargetDetachedFromTargetChromiumEvent targetDetachedFromTarget:
                        OnDetachedFromTarget(targetDetachedFromTarget);
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
                Client.OnClosed(ex.ToString());
            }
        }

        private void OnDetachedFromTarget(TargetDetachedFromTargetChromiumEvent targetDetachedFromTarget)
            => Page.RemoveWorker(targetDetachedFromTarget.SessionId);

        private void OnDialog(PageJavascriptDialogOpeningChromiumEvent e)
            => Page?.OnDialog(new Dialog(
                e.Type.ToDialogType(),
                e.Message,
                (accept, promptText) => Client.SendAsync(new PageHandleJavaScriptDialogRequest
                {
                    Accept = accept,
                    PromptText = promptText,
                }),
                e.DefaultPrompt));

        private void OnFrameStoppedLoading(PageFrameStoppedLoadingChromiumEvent e)
            => Page.FrameManager.FrameStoppedLoading(e.FrameId);

        private async Task OnFileChooserOpenedAsync(PageFileChooserOpenedChromiumEvent e)
        {
            if (!Page.FrameManager.Frames.TryGetValue(e.FrameId, out var frame))
            {
                return;
            }

            var utilityContext = await frame.GetUtilityContextAsync().ConfigureAwait(false);
            var handle = await AdoptBackendNodeIdAsync(e.BackendNodeId.Value, utilityContext).ConfigureAwait(false);
            await Page.OnFileChooserOpenedAsync(handle).ConfigureAwait(false);
        }

        private void OnFrameNavigatedWithinDocument(string frameId, string url)
            => Page.FrameManager.FrameCommittedSameDocumentNavigation(frameId, url);

        private void OnFrameDetached(string frameId) => Page.FrameManager.FrameDetached(frameId);

        private Task OnBindingCalledAsync(RuntimeBindingCalledChromiumEvent e)
        {
            var context = ContextIdToContext[e.ExecutionContextId.Value];
            return Page.OnBindingCalledAsync(e.Payload, context);
        }

        private void OnConsoleAPI(RuntimeConsoleAPICalledChromiumEvent e)
        {
            if (!e.ExecutionContextId.HasValue || e.ExecutionContextId == 0)
            {
                // DevTools protocol stores the last 1000 console messages. These
                // messages are always reported even for removed execution contexts. In
                // this case, they are marked with executionContextId = 0 and are
                // reported upon enabling Runtime agent.
                //
                // Ignore these messages since:
                // - there's no execution context we can use to operate with message
                //   arguments
                // - these messages are reported before Playwright clients can subscribe
                //   to the 'console'
                //   page event.
                //
                // @see https://github.com/GoogleChrome/puppeteer/issues/3865
                return;
            }

            var context = ContextIdToContext[e.ExecutionContextId.Value];
            var values = e.Args.Select(arg => context.CreateHandle(arg));
            Page.AddConsoleMessage(e.Type.ToEnum<ConsoleType>(), values.ToArray(), ToConsoleMessageLocation(e.StackTrace));
        }

        private async Task OnAttachedToTargetAsync(TargetAttachedToTargetChromiumEvent e)
        {
            if (e.TargetInfo.Type != "worker")
            {
                return;
            }

            string url = e.TargetInfo.Url;
            var session = ChromiumConnection.FromSession(Client).GetSession(e.SessionId);
            var worker = new Worker(url);
            Page.AddWorker(e.SessionId, worker);

            void HandleRuntimeExecutionContextCreated(object sender, IChromiumEvent e)
            {
                if (e is RuntimeExecutionContextCreatedChromiumEvent runtimeExecutionContextCreated)
                {
                    worker.CreateExecutionContext(new ChromiumExecutionContext(session, runtimeExecutionContextCreated.Context));
                    session.MessageReceived -= HandleRuntimeExecutionContextCreated;
                }
            }

            async void HandleRuntimeExecutionMessages(object sender, IChromiumEvent e)
            {
                try
                {
                    switch (e)
                    {
                        case RuntimeConsoleAPICalledChromiumEvent runtimeConsoleAPICalled:
                            var executionContext = await worker.GetExistingExecutionContextAsync().ConfigureAwait(false);
                            var args = runtimeConsoleAPICalled.Args.Select(o => executionContext.CreateHandle(o));
                            Page.AddConsoleMessage(
                                runtimeConsoleAPICalled.Type.ToEnum<ConsoleType>(),
                                args.ToArray(),
                                ToConsoleMessageLocation(runtimeConsoleAPICalled.StackTrace));
                            break;

                        case RuntimeExceptionThrownChromiumEvent runtimeExceptionThrown:
                            Page.OnPageError(ExceptionToError(runtimeExceptionThrown.ExceptionDetails));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // TODO Add Logger
                    System.Diagnostics.Debug.WriteLine(ex);
                    session.OnClosed(ex.ToString());
                }
            }

            session.MessageReceived += HandleRuntimeExecutionMessages;
            session.MessageReceived += HandleRuntimeExecutionContextCreated;

            try
            {
                await Task.WhenAll(
                    session.SendAsync(new RuntimeEnableRequest()),
                    session.SendAsync(new NetworkEnableRequest())).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private ConsoleMessageLocation ToConsoleMessageLocation(StackTrace stackTrace)
            => stackTrace != null && stackTrace.CallFrames.Length > 0
                ? new ConsoleMessageLocation
                {
                    URL = stackTrace.CallFrames[0].Url,
                    LineNumber = stackTrace.CallFrames[0].LineNumber,
                    ColumnNumber = stackTrace.CallFrames[0].ColumnNumber,
                }
                : new ConsoleMessageLocation();

        private void OnExecutionContextsCleared()
        {
            foreach (int contextId in ContextIdToContext.Keys)
            {
                OnExecutionContextDestroyed(contextId);
            }
        }

        private void OnExecutionContextDestroyed(int executionContextId)
        {
            if (!ContextIdToContext.TryGetValue(executionContextId, out var context))
            {
                return;
            }

            ContextIdToContext.TryRemove(executionContextId, out _);
            context.Frame.ContextDestroyed(context);
        }

        private void OnExecutionContextCreated(ExecutionContextDescription contextPayload)
        {
            var auxData = contextPayload.AuxData != null
                ? ((JsonElement)contextPayload.AuxData).ToObject<ExecutionContextDescriptionAuxData>()
                : null;

            Frame frame = null;

            if (contextPayload.AuxData != null && !Page.FrameManager.Frames.TryGetValue(auxData.FrameId, out frame))
            {
                return;
            }

            if (auxData?.Type == "isolated")
            {
                _isolatedWorlds.Add(contextPayload.Name);
            }

            var executionContextDelegate = new ChromiumExecutionContext(Client, contextPayload);
            var context = new FrameExecutionContext(executionContextDelegate, frame);

            if (auxData?.IsDefault == true)
            {
                frame.ContextCreated(ContextType.Main, context);
            }
            else if (contextPayload.Name == UtilityWorldName)
            {
                frame.ContextCreated(ContextType.Utility, context);
            }

            ContextIdToContext[contextPayload.Id.Value] = context;
        }

        private void OnLifecycleEvent(PageLifecycleEventChromiumEvent e)
        {
            if (e.Name == "load")
            {
                Page.FrameManager.FrameLifecycleEvent(e.FrameId, WaitUntilNavigation.Load);
            }
            else if (e.Name == "DOMContentLoaded")
            {
                Page.FrameManager.FrameLifecycleEvent(e.FrameId, WaitUntilNavigation.DOMContentLoaded);
            }
        }

        private void OnNavigatedWithinDocument(PageNavigatedWithinDocumentChromiumEvent e)
            => Page.FrameManager.FrameCommittedSameDocumentNavigation(e.FrameId, e.Url);

        private void OnFrameNavigated(Protocol.Page.Frame frame, bool initial)
            => Page.FrameManager.FrameCommittedNewDocumentNavigation(frame.Id, frame.Url, frame.Name ?? string.Empty, frame.LoaderId, initial);

        private async Task EnsureIsolatedWorldAsync(string name)
        {
            if (!_isolatedWorlds.Add(name))
            {
                return;
            }

            await Client.SendAsync(new PageAddScriptToEvaluateOnNewDocumentRequest
            {
                Source = $"//# sourceURL={EvaluationScriptUrl}",
                WorldName = name,
            }).ConfigureAwait(false);

            await Task.WhenAll(Page.Frames.Select(frame => Client.SendAsync(new PageCreateIsolatedWorldRequest
            {
                FrameId = frame.Id,
                GrantUniveralAccess = true,
                WorldName = name,
            })).ToArray()).ConfigureAwait(false);
        }

        private async Task EmulateTimezoneAsync(string timezoneId)
        {
            try
            {
                await Client.SendAsync(new EmulationSetTimezoneOverrideRequest { TimezoneId = timezoneId }).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid timezone"))
            {
                throw new PlaywrightSharpException($"Invalid timezone ID: {timezoneId}", ex);
            }
        }

        private void HandleFrameTree(FrameTree frameTree)
        {
            OnFrameAttached(frameTree.Frame.Id, frameTree.Frame.ParentId ?? string.Empty);
            OnFrameNavigated(frameTree.Frame, true);

            if (frameTree.ChildFrames != null)
            {
                foreach (var child in frameTree.ChildFrames)
                {
                    HandleFrameTree(child);
                }
            }
        }

        private void OnFrameAttached(PageFrameAttachedChromiumEvent e) => OnFrameAttached(e.FrameId, e.ParentFrameId);

        private void OnFrameDetached(PageFrameDetachedChromiumEvent e)
            => Page.FrameManager.FrameDetached(e.FrameId);

        private void OnFrameAttached(string frameId, string parentFrameId) => Page.FrameManager.FrameAttached(frameId, parentFrameId);

        private PageErrorEventArgs ExceptionToError(ExceptionDetails exceptionDetails)
            => new PageErrorEventArgs(exceptionDetails.ToExceptionMessage());

        private async Task<AccessibilityTree> GetAccessibilityTreeAsync(ChromiumSession client, IElementHandle needle)
        {
            var result = await client.SendAsync(new AccessibilityGetFullAXTreeRequest()).ConfigureAwait(false);
            var tree = ChromiumAXNode.CreateTree(client, result.Nodes);
            return new AccessibilityTree
            {
                Tree = tree,
                Needle = needle != null ? await tree.FindElementAsync(needle as ElementHandle).ConfigureAwait(false) : null,
            };
        }
    }
}
