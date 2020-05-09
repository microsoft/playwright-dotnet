using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Helper;
using PlaywrightSharp.Firefox.Input;
using PlaywrightSharp.Firefox.Messaging;
using PlaywrightSharp.Firefox.Protocol;
using PlaywrightSharp.Firefox.Protocol.Network;
using PlaywrightSharp.Firefox.Protocol.Page;
using PlaywrightSharp.Firefox.Protocol.Runtime;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Messaging;
using FirefoxJsonHelper = PlaywrightSharp.Firefox.Helper.JsonHelper;

namespace PlaywrightSharp.Firefox
{
    /// <inheritdoc cref="IPageDelegate"/>
    internal class FirefoxPage : IPageDelegate
    {
        private const string UtilityWorldName = "__playwright_utility_world__";

        private readonly FirefoxSession _session;
        private readonly IBrowserContext _context;
        private readonly Func<Task<Page>> _openerResolver;
        private readonly ConcurrentDictionary<string, WorkerSession> _workers = new ConcurrentDictionary<string, WorkerSession>();
        private readonly FirefoxNetworkManager _networkManager;

        public FirefoxPage(FirefoxSession session, IBrowserContext context, Func<Task<Page>> openerResolver)
        {
            _session = session;
            _context = context;
            _openerResolver = openerResolver;

            RawKeyboard = new FirefoxRawKeyboard(session);
            RawMouse = new FirefoxRawMouse(session);
            Page = new Page(this, _context);
            _networkManager = new FirefoxNetworkManager(session, Page);

            session.MessageReceived += OnMessageReceived;
            Page.FrameDetached += RemoveContextsForFrame;
        }

        public IRawKeyboard RawKeyboard { get; }

        public IRawMouse RawMouse { get; }

        public ConcurrentDictionary<object, FrameExecutionContext> ContextIdToContext { get; } = new ConcurrentDictionary<object, FrameExecutionContext>();

        internal Page Page { get; }

        public async Task<ElementHandle> AdoptElementHandleAsync(ElementHandle handle, FrameExecutionContext to)
        {
            var result = await _session.SendAsync(new PageAdoptNodeRequest
            {
                FrameId = handle.Context.Frame.Id,
                ObjectId = handle.RemoteObject.ObjectId,
                ExecutionContextId = ((FirefoxExecutionContext)to.Delegate).ExecutionContextId,
            }).ConfigureAwait(false);
            return to.CreateHandle(result.RemoteObject) as ElementHandle;
        }

        public Task ClosePageAsync(bool runBeforeUnload)
            => _session.SendAsync(new PageCloseRequest { RunBeforeUnload = runBeforeUnload });

        public async Task<Quad[][]> GetContentQuadsAsync(ElementHandle elementHandle)
        {
            var result = await _session.SendAsync(new PageGetContentQuadsRequest
            {
                FrameId = elementHandle.Context.Frame.Id,
                ObjectId = elementHandle.RemoteObject.ObjectId,
            }).ConfigureAwait(false);
            return Array.ConvertAll(result.Quads, quad => (Quad[])quad);
        }

        public Task<LayoutMetric> GetLayoutViewportAsync()
            => Page.EvaluateAsync<LayoutMetric>("() => ({ width: innerWidth, height: innerHeight })");

        public bool CanScreenshotOutsideViewport() => true;

        public Task ResetViewportAsync(Viewport viewport) => throw new NotImplementedException();

        public Task SetBackgroundColorAsync(Color? color = null) => throw new NotImplementedException();

        public async Task<byte[]> TakeScreenshotAsync(ScreenshotFormat format, ScreenshotOptions options, Viewport viewport)
        {
            var response = await _session.SendAsync(new PageScreenshotRequest
            {
                MimeType = format == ScreenshotFormat.Jpeg ? ScreenshotMimeType.ImageJpeg : ScreenshotMimeType.ImagePng,
                FullPage = options.FullPage,
                Clip = options.Clip,
            }).ConfigureAwait(false);
            return Convert.FromBase64String(response.Data);
        }

        public bool IsElementHandle(IRemoteObject remoteObject) => ((RemoteObject)remoteObject).Subtype == RemoteObjectSubtype.Node;

        public Task<AccessibilityTree> GetAccessibilityTreeAsync(IElementHandle needle) => throw new NotImplementedException();

        public async Task<GotoResult> NavigateFrameAsync(IFrame frame, string url, string referrer)
        {
            var response = await _session.SendAsync(new PageNavigateRequest
            {
                Url = url,
                Referer = referrer,
                FrameId = frame.Id,
            }).ConfigureAwait(false);
            return new GotoResult { NewDocumentId = response.NavigationId, IsSameDocument = response.NavigationId == null };
        }

        public Task SetViewportAsync(Viewport viewport) => _session.SendAsync(new PageSetViewportRequest
        {
            Viewport = new Protocol.Page.Viewport
            {
                Width = viewport.Width,
                Height = viewport.Height,
                IsMobile = viewport.IsMobile,
                DeviceScaleFactor = viewport.DeviceScaleFactor,
                HasTouch = viewport.IsMobile,
                IsLandscape = viewport.Width > viewport.Height,
            },
        });

        public async Task<Rect> GetBoundingBoxForScreenshotAsync(ElementHandle handle)
        {
            var response = await _session.SendAsync(new PageGetBoundingBoxRequest
            {
                FrameId = handle.Context.Frame.Id,
                ObjectId = handle.RemoteObject.ObjectId,
            }).ConfigureAwait(false);
            return response.BoundingBox;
        }

        public async Task<Rect> GetBoundingBoxAsync(ElementHandle handle)
        {
            var quads = await GetContentQuadsAsync(handle).ConfigureAwait(false);
            if (quads == null || quads.Length == 0)
            {
                return null;
            }

            double minX = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;
            foreach (var quad in quads)
            {
                foreach (var point in quad)
                {
                    minX = Math.Min(minX, point.X);
                    maxX = Math.Max(maxX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxY = Math.Max(maxY, point.Y);
                }
            }

            return new Rect
            {
                X = minX,
                Y = minY,
                Width = maxX - minX,
                Height = maxY - minY,
            };
        }

        public async Task ExposeBindingAsync(string name, string functionString)
        {
            await _session.SendAsync(new PageAddBindingRequest { Name = name }).ConfigureAwait(false);
            await _session.SendAsync(new PageAddScriptToEvaluateOnNewDocumentRequest
            {
                Script = functionString,
            }).ConfigureAwait(false);
            await Task.WhenAll(Array.ConvertAll(Page.Frames, frame => frame.EvaluateAsync(functionString))).ConfigureAwait(false);
        }

        public Task EvaluateOnNewDocumentAsync(string source)
            => _session.SendAsync(new PageAddScriptToEvaluateOnNewDocumentRequest
            {
                Script = source,
            });

        public async Task<string> GetOwnerFrameAsync(ElementHandle elementHandle)
        {
            var result = await _session.SendAsync(new PageDescribeNodeRequest
            {
                FrameId = elementHandle.Context.Frame.Id,
                ObjectId = elementHandle.RemoteObject.ObjectId,
            }).ConfigureAwait(false);
            return result.OwnerFrameId;
        }

        public async Task<IFrame> GetContentFrameAsync(ElementHandle elementHandle)
        {
            var response = await _session.SendAsync(new PageDescribeNodeRequest
            {
                FrameId = elementHandle.Context.Frame.Id,
                ObjectId = elementHandle.RemoteObject.ObjectId,
            }).ConfigureAwait(false);
            if (string.IsNullOrEmpty(response.ContentFrameId))
            {
                return null;
            }

            return Page.FrameManager.Frames[response.ContentFrameId];
        }

        public Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers)
            => _session.SendAsync(new NetworkSetExtraHTTPHeadersRequest
            {
                Headers = headers.Select(pair => new HTTPHeader
                {
                    Name = pair.Key,
                    Value = pair.Value,
                }).ToArray(),
            });

        public Task ReloadAsync() => _session.SendAsync(new PageReloadRequest { FrameId = Page.MainFrame.Id });

        public async Task<bool> GoBackAsync()
        {
            var result = await _session.SendAsync(new PageGoBackRequest { FrameId = Page.MainFrame.Id }).ConfigureAwait(false);
            return result.NavigationId != null;
        }

        public async Task<bool> GoForwardAsync()
        {
            var result = await _session.SendAsync(new PageGoForwardRequest { FrameId = Page.MainFrame.Id }).ConfigureAwait(false);
            return result.NavigationId != null;
        }

        public Task SetInputFilesAsync(ElementHandle handle, IEnumerable<FilePayload> files)
            => handle.EvaluateAsync(Dom.SetFileInputFunction, files);

        public Task SetFileChooserInterceptedAsync(bool enabled)
            => _session.SendAsync(new PageSetInterceptFileChooserDialogRequest { Enabled = enabled });

        public Task SetCacheEnabledAsync(bool enabled)
            => _session.SendAsync(new PageSetCacheDisabledRequest { CacheDisabled = !enabled });

        public Task SetRequestInterceptionAsync(bool enabled) => throw new NotImplementedException();

        public Task AuthenticateAsync(Credentials credentials) => throw new NotImplementedException();

        public Task SetOfflineModeAsync(bool enabled) => throw new NotImplementedException();

        public Task SetEmulateMediaAsync(MediaType? mediaType, ColorScheme? colorScheme) => throw new NotImplementedException();

        internal async Task InitializeAsync()
        {
            var tasks = new List<Task>
            {
               _session.SendAsync(new RuntimeEnableRequest()),
               _session.SendAsync(new NetworkEnableRequest()),
               _session.SendAsync(new PageEnableRequest()),
            };

            if (_context.Options != null)
            {
                var options = _context.Options;

                if (options.Viewport != null)
                {
                    tasks.Add(SetViewportAsync(options.Viewport));
                }

                if (options.BypassCSP)
                {
                    tasks.Add(_session.SendAsync(new PageSetBypassCSPRequest { Enabled = true }));
                }

                if (!options.JavaScriptEnabled)
                {
                    tasks.Add(_session.SendAsync(new PageSetJavascriptEnabledRequest { Enabled = false }));
                }

                if (options.UserAgent != null)
                {
                    tasks.Add(_session.SendAsync(new PageSetUserAgentRequest { UserAgent = options.UserAgent }));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            await EnsureIsolatedWorldAsync(UtilityWorldName).ConfigureAwait(false);
        }

        internal void DidClose() => Page.DidClose();

        private Task EnsureIsolatedWorldAsync(string name)
            => _session.SendAsync(new PageAddScriptToEvaluateOnNewDocumentRequest
            {
                Script = string.Empty,
                WorldName = name,
            });

        private void OnMessageReceived(object sender, IFirefoxEvent e)
        {
            switch (e)
            {
                case PageEventFiredFirefoxEvent pageEventFired:
                    OnEventFired(pageEventFired);
                    break;
                case PageFrameAttachedFirefoxEvent pageFrameAttached:
                    OnFrameAttached(pageFrameAttached);
                    break;
                case PageFrameDetachedFirefoxEvent pageFrameDetached:
                    OnFrameDetached(pageFrameDetached);
                    break;
                case PageNavigationAbortedFirefoxEvent pageNavigationAborted:
                    OnNavigationAborted(pageNavigationAborted);
                    break;
                case PageNavigationCommittedFirefoxEvent pageNavigationCommitted:
                    OnNavigationCommitted(pageNavigationCommitted);
                    break;
                case PageNavigationStartedFirefoxEvent pageNavigationStarted:
                    OnNavigationStarted(pageNavigationStarted);
                    break;
                case PageSameDocumentNavigationFirefoxEvent pageSameDocumentNavigation:
                    OnSameDocumentNavigation(pageSameDocumentNavigation);
                    break;
                case RuntimeExecutionContextCreatedFirefoxEvent runtimeExecutionContextCreated:
                    OnExecutionContextCreated(runtimeExecutionContextCreated);
                    break;
                case RuntimeExecutionContextDestroyedFirefoxEvent runtimeExecutionContextDestroyed:
                    OnExecutionContextDestroyed(runtimeExecutionContextDestroyed);
                    break;
                case PageUncaughtErrorFirefoxEvent pageUncaughtError:
                    break;
                case RuntimeConsoleFirefoxEvent runtimeConsole:
                    OnConsole(runtimeConsole);
                    break;
                case PageDialogOpenedFirefoxEvent pageDialogOpened:
                    break;
                case PageBindingCalledFirefoxEvent pageBindingCalled:
                    OnBindingCalled(pageBindingCalled);
                    break;
                case PageFileChooserOpenedFirefoxEvent pageFileChooserOpened:
                    OnFileChooserOpened(pageFileChooserOpened);
                    break;
                case PageWorkerCreatedFirefoxEvent pageWorkerCreated:
                    OnWorkerCreated(pageWorkerCreated);
                    break;
                case PageWorkerDestroyedFirefoxEvent pageWorkerDestroyed:
                    OnWorkerDestroyed(pageWorkerDestroyed);
                    break;
                case PageDispatchMessageFromWorkerFirefoxEvent pageDispatchMessageFromWorker:
                    OnDispatchMessageFromWorker(pageDispatchMessageFromWorker);
                    break;
            }
        }

        private void OnEventFired(PageEventFiredFirefoxEvent e)
        {
            if (e.Name == EventFiredName.Load)
            {
                Page.FrameManager.FrameLifecycleEvent(e.FrameId, WaitUntilNavigation.Load);
            }

            if (e.Name == EventFiredName.DOMContentLoaded)
            {
                Page.FrameManager.FrameLifecycleEvent(e.FrameId, WaitUntilNavigation.DOMContentLoaded);
            }
        }

        private void OnNavigationAborted(PageNavigationAbortedFirefoxEvent e)
        {
            var frame = Page.FrameManager.Frames[e.FrameId];
            foreach (var watcher in Page.FrameManager.LifecycleWatchers.ToArray())
            {
                watcher.OnAbortedNewDocumentNavigation(frame, e.NavigationId, e.ErrorText);
            }
        }

        private void OnNavigationStarted(PageNavigationStartedFirefoxEvent e)
        {
        }

        private void OnFrameAttached(PageFrameAttachedFirefoxEvent e)
            => Page.FrameManager.FrameAttached(e.FrameId, e.ParentFrameId);

        private void OnFrameDetached(PageFrameDetachedFirefoxEvent e)
            => Page.FrameManager.FrameDetached(e.FrameId);

        private void OnNavigationCommitted(PageNavigationCommittedFirefoxEvent e)
        {
            foreach (var pair in _workers)
            {
                if (pair.Key == e.FrameId)
                {
                    OnWorkerDestroyed(new PageWorkerDestroyedFirefoxEvent { WorkerId = pair.Key });
                }
            }

            Page.FrameManager.FrameCommittedNewDocumentNavigation(e.FrameId, e.Url, e.Name ?? string.Empty, e.NavigationId ?? string.Empty, false);
        }

        private void OnSameDocumentNavigation(PageSameDocumentNavigationFirefoxEvent e)
            => Page.FrameManager.FrameCommittedSameDocumentNavigation(e.FrameId, e.Url);

        private void OnExecutionContextCreated(RuntimeExecutionContextCreatedFirefoxEvent e)
        {
            var auxData = e.AuxData != null
                ? ((JsonElement)e.AuxData).ToObject<AuxData>()
                : null;
            if (auxData?.FrameId != null && Page.FrameManager.Frames.TryGetValue(auxData.FrameId, out var frame))
            {
                var firefoxDelegate = new FirefoxExecutionContext(_session, e.ExecutionContextId);
                var context = new FrameExecutionContext(firefoxDelegate, frame);
                if (auxData.Name == UtilityWorldName)
                {
                    frame.ContextCreated(ContextType.Utility, context);
                }
                else if (string.IsNullOrEmpty(auxData.Name))
                {
                    frame.ContextCreated(ContextType.Main, context);
                }

                ContextIdToContext[e.ExecutionContextId] = context;
            }
        }

        private void OnExecutionContextDestroyed(RuntimeExecutionContextDestroyedFirefoxEvent e)
        {
            if (ContextIdToContext.TryRemove(e.ExecutionContextId, out var context))
            {
                context.Frame.ContextDestroyed(context);
            }
        }

        private void RemoveContextsForFrame(object sender, FrameEventArgs e)
        {
            foreach (var pair in ContextIdToContext)
            {
                if (pair.Value.Frame == e.Frame)
                {
                    ContextIdToContext.TryRemove(pair.Key, out _);
                }
            }
        }

        private void OnBindingCalled(PageBindingCalledFirefoxEvent e)
        {
            var context = ContextIdToContext[e.ExecutionContextId];
            _ = Page.OnBindingCalledAsync(e.Payload.ToString(), context);
        }

        private void OnConsole(RuntimeConsoleFirefoxEvent e)
        {
            var context = ContextIdToContext[e.ExecutionContextId];

            var type = e.GetConsoleType();
            var location = e.ToConsoleMessageLocation();

            Page.AddConsoleMessage(type, Array.ConvertAll(e.Args, arg => context.CreateHandle(arg)), location);
        }

        private void OnFileChooserOpened(PageFileChooserOpenedFirefoxEvent e)
        {
            var context = ContextIdToContext[e.ExecutionContextId];
            var handle = context.CreateHandle(e.Element) as ElementHandle;
            _ = Page.OnFileChooserOpenedAsync(handle);
        }

        private void OnWorkerCreated(PageWorkerCreatedFirefoxEvent e)
        {
            string workerId = e.WorkerId;
            var worker = new Worker(e.Url);
            FirefoxSession tempWorkerSession = null;
            var workerSession = new FirefoxSession(_session.Connection, "worker", workerId, async (id, request) =>
            {
                try
                {
                    await _session.SendAsync(new PageSendMessageToWorkerRequest
                    {
                        FrameId = e.FrameId,
                        WorkerId = workerId,
                        Message = new ConnectionRequest
                        {
                            Id = id,
                            Method = request.Command,
                            Params = request,
                        }.ToJson(FirefoxJsonHelper.DefaultJsonSerializerOptions),
                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    tempWorkerSession.OnMessage(new ConnectionResponse
                    {
                        Id = id,
                        Method = string.Empty,
                        Error = new ConnectionError
                        {
                            Message = e.Message,
                        },
                    });
                }
            });
            tempWorkerSession = workerSession;

            _workers[workerId] = new WorkerSession(e.FrameId, workerSession);
            Page.AddWorker(workerId, worker);
            void HandleRuntimeExecutionContextCreated(object sender, IFirefoxEvent e)
            {
                if (e is RuntimeExecutionContextCreatedFirefoxEvent runtimeExecutionContextCreated)
                {
                    worker.CreateExecutionContext(new FirefoxExecutionContext(workerSession, runtimeExecutionContextCreated.ExecutionContextId));
                    workerSession.MessageReceived -= HandleRuntimeExecutionContextCreated;
                }
            }

            workerSession.MessageReceived += HandleRuntimeExecutionContextCreated;
            workerSession.MessageReceived += (sender, e) =>
            {
                if (e is RuntimeConsoleFirefoxEvent runtimeConsole)
                {
                    var context = worker.ExistingExecutionContext;
                    var type = runtimeConsole.GetConsoleType();
                    var location = runtimeConsole.ToConsoleMessageLocation();

                    Page.AddConsoleMessage(type, Array.ConvertAll(runtimeConsole.Args, arg => context.CreateHandle(arg)), location);
                }
            };
        }

        private void OnWorkerDestroyed(PageWorkerDestroyedFirefoxEvent e)
        {
            string workerId = e.WorkerId;
            if (_workers.TryRemove(workerId, out var worker))
            {
                worker.Session.OnClosed(e.InternalName);
                Page.RemoveWorker(workerId);
            }
        }

        private void OnDispatchMessageFromWorker(PageDispatchMessageFromWorkerFirefoxEvent pageDispatchMessageFromWorker)
        {
            throw new NotImplementedException();
        }

        private class WorkerSession
        {
            public WorkerSession(string frameId, FirefoxSession session) => (FrameId, Session) = (frameId, session);

            public string FrameId { get; }

            public FirefoxSession Session { get; }
        }

        private class AuxData
        {
            public string FrameId { get; set; }

            public string Name { get; set; }
        }
    }
}
