using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Input;
using PlaywrightSharp.Chromium.Protocol;
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
        private readonly Dictionary<int, FrameExecutionContext> _contextIdToContext = new Dictionary<int, FrameExecutionContext>();

        public ChromiumPage(ChromiumSession client, ChromiumBrowser browser, IBrowserContext browserContext)
        {
            Client = client;
            _browser = browser;
            _browserContext = browserContext;
            _networkManager = new ChromiumNetworkManager(Client, this);
            RawKeyboard = new ChromiumRawKeyboard(client);
            RawMouse = new ChromiumRawMouse(client);
            Page = new Page(this, browserContext);

            client.MessageReceived += Client_MessageReceived;
        }

        public ChromiumTarget Target { get; set; }

        public IRawKeyboard RawKeyboard { get; }

        public IRawMouse RawMouse { get; }

        internal Page Page { get; }

        internal ChromiumSession Client { get; }

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

        public Task<IElementHandle> AdoptElementHandleAsync(object arg, FrameExecutionContext frameExecutionContext)
        {
            throw new NotImplementedException();
        }

        public bool IsElementHandle(IRemoteObject remoteObject) => remoteObject?.Subtype == "node";

        public async Task<Quad[][]> GetContentQuadsAsync(ElementHandle handle)
        {
            var result = await Client.SendAsync(new DOMGetContentQuadsRequest
            {
                ObjectId = handle.RemoteObject.ObjectId,
            }).ConfigureAwait(false);

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

        public Task ResetViewportAsync(Size viewportSize)
            => Client.SendAsync(new EmulationSetDeviceMetricsOverrideRequest
            {
                Mobile = false,
                Width = 0,
                Height = 0,
                DeviceScaleFactor = 0,
            });

        public Task SetBackgroundColorAsync(Color? color = null) => throw new NotImplementedException();

        public async Task<byte[]> TakeScreenshotAsync(ScreenshotFormat format, ScreenshotOptions options, Viewport viewport)
        {
            await Client.SendAsync(new PageBringToFrontRequest()).ConfigureAwait(false);
            var clip = options.Clip != null ? options.Clip.ToViewportProtocol() : null;

            var result = await Client.SendAsync(new PageCaptureScreenshotRequest
            {
                Format = format.ToStringFormat(),
                Quality = options.Quality,
                Clip = clip,
            }).ConfigureAwait(false);
            return result.Data;
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

        private async void Client_MessageReceived(object sender, IChromiumEvent e)
        {
            try
            {
                switch (e)
                {
                    case PageFrameAttachedChromiumEvent pageFrameAttached:
                        OnFrameAttached(pageFrameAttached);
                        break;
                    case PageFrameNavigatedChromiumEvent pageFrameNavigated:
                        OnFrameNavigated(pageFrameNavigated.Frame, false);
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
                }
            }
            catch (Exception ex)
            {
                // TODO Add Logger
                /*
                var message = $"Page failed to process {e.MessageID}. {ex.Message}. {ex.StackTrace}";
                _logger.LogError(ex, message);
                */
                Client.OnClosed(ex.Message);
            }
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

            var context = _contextIdToContext[e.ExecutionContextId.Value];
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
                switch (e)
                {
                    case RuntimeExecutionContextCreatedChromiumEvent runtimeExecutionContextCreated:

                        worker.CreateExecutionContext(new ChromiumExecutionContext(session, runtimeExecutionContextCreated.Context));
                        session.MessageReceived -= HandleRuntimeExecutionContextCreated;
                        break;

                    case RuntimeConsoleAPICalledChromiumEvent runtimeConsoleAPICalled:
                        var args = runtimeConsoleAPICalled.Args.Select(o =>
                            worker.ExistingExecutionContext.CreateHandle(o));
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

            _networkManager.InstrumentNetworkEvents(session);
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
            foreach (int contextId in _contextIdToContext.Keys)
            {
                OnExecutionContextDestroyed(contextId);
            }
        }

        private void OnExecutionContextDestroyed(int executionContextId)
        {
            if (!_contextIdToContext.TryGetValue(executionContextId, out var context))
            {
                return;
            }

            _contextIdToContext.Remove(executionContextId);
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

            _contextIdToContext[contextPayload.Id.Value] = context;
        }

        private void OnLifecycleEvent(PageLifecycleEventChromiumEvent e)
        {
            if (e.Name == "load")
            {
                Page.FrameManager.FrameLifecycleEvent(e.FrameId, "load");
            }
            else if (e.Name == "DOMContentLoaded")
            {
                Page.FrameManager.FrameLifecycleEvent(e.FrameId, "domcontentloaded");
            }
        }

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

        private Task EmulateTimezoneAsync(string timezoneId)
        {
            throw new NotImplementedException();
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

        private void OnFrameAttached(string frameId, string parentFrameId) => Page.FrameManager.FrameAttached(frameId, parentFrameId);

        private PageErrorEventArgs ExceptionToError(ExceptionDetails exceptionDetails)
            => new PageErrorEventArgs(exceptionDetails.ToExceptionMessage());
    }
}
