using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContext" />
    public class BrowserContext : IChannelOwner<BrowserContext>, IBrowserContext
    {
        private static readonly Dictionary<ContextEvent, EventInfo> _contextEventsMap = ((ContextEvent[])Enum.GetValues(typeof(ContextEvent)))
            .ToDictionary(x => x, x => typeof(BrowserContext).GetEvent(x.ToString()));

        private readonly ConnectionScope _scope;
        private readonly BrowserContextChannel _channel;
        private readonly List<Page> _crBackgroundPages = new List<Page>();
        private readonly TaskCompletionSource<bool> _closeTcs = new TaskCompletionSource<bool>();
        private readonly List<(ContextEvent contextEvent, TaskCompletionSource<bool> waitTcs)> _waitForCancellationTcs = new List<(ContextEvent contextEvent, TaskCompletionSource<bool> waitTcs)>();
        private readonly TimeoutSettings _timeoutSettings = new TimeoutSettings();
        private bool _isClosedOrClosing;

        internal BrowserContext(ConnectionScope scope, string guid, BrowserContextInitializer initializer)
        {
            _scope = scope.CreateChild(guid);
            _channel = new BrowserContextChannel(guid, scope, this);
            _channel.Closed += Channel_Closed;
            _channel.OnPage += Channel_OnPage;

            if (initializer.Pages != null)
            {
                foreach (var pageChannel in initializer.Pages)
                {
                    var page = pageChannel.Object;
                    PagesList.Add(page);
                    page.BrowserContext = this;
                }
            }

            if (initializer.CrBackgroundPages != null)
            {
                foreach (var pageChannel in initializer.CrBackgroundPages)
                {
                    var page = pageChannel.Object;
                    _crBackgroundPages.Add(page);
                    page.BrowserContext = this;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Closed;

        /// <inheritdoc/>
        public event EventHandler<PageEventArgs> PageCreated;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        Channel<BrowserContext> IChannelOwner<BrowserContext>.Channel => _channel;

        /// <inheritdoc />
        public BrowserContextOptions Options { get; }

        /// <inheritdoc />
        public IPage[] Pages => PagesList.ToArray();

        /// <inheritdoc />
        public Browser Browser { get; internal set; }

        /// <inheritdoc />
        public int DefaultTimeout
        {
            get => _timeoutSettings.Timeout;
            set
            {
                _timeoutSettings.SetDefaultTimeout(value);
                _ = _channel.SetDefaultTimeoutNoReplyAsync(value);
            }
        }

        internal Page OwnerPage { get; set; }

        internal List<Page> PagesList { get; } = new List<Page>();

        /// <inheritdoc />
        public async Task<IPage> NewPageAsync(string url = null)
        {
            if (OwnerPage != null)
            {
                throw new PlaywrightSharpException("Please use Browser.NewContextAsync()");
            }

            return (await _channel.NewPageAsync(url).ConfigureAwait(false)).Object;
        }

        /// <inheritdoc />
        public Task CloseAsync()
        {
            if (!_isClosedOrClosing)
            {
                _isClosedOrClosing = true;
                return _channel.CloseAsync();
            }

            return _closeTcs.Task;
        }

        /// <inheritdoc />
        public Task<IEnumerable<NetworkCookie>> GetCookiesAsync(params string[] urls) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetCookiesAsync(params SetNetworkCookieParam[] cookies) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClearCookiesAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetGeolocationAsync(GeolocationOption geolocation) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClearPermissionsAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public IEnumerable<IPage> GetExistingPages() => throw new NotImplementedException();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public async Task<T> WaitForEvent<T>(ContextEvent e, WaitForEventOptions<T> options = null)
        {
            var info = _contextEventsMap[e];
            ValidateArgumentsTypes();
            var eventTsc = new TaskCompletionSource<T>();
            void ContextEventHandler(object sender, T e)
            {
                if (options?.Predicate == null || options.Predicate(e))
                {
                    eventTsc.TrySetResult(e);
                    info.RemoveEventHandler(this, (EventHandler<T>)ContextEventHandler);
                }
            }

            info.AddEventHandler(this, (EventHandler<T>)ContextEventHandler);
            var disconnectedTcs = new TaskCompletionSource<bool>();
            _waitForCancellationTcs.Add((e, disconnectedTcs));
            await Task.WhenAny(eventTsc.Task, disconnectedTcs.Task).WithTimeout(options?.Timeout ?? DefaultTimeout).ConfigureAwait(false);
            if (disconnectedTcs.Task.IsCompleted)
            {
                await disconnectedTcs.Task.ConfigureAwait(false);
            }

            return await eventTsc.Task.ConfigureAwait(false);

            void ValidateArgumentsTypes()
            {
                if ((info.EventHandlerType.GenericTypeArguments.Length == 0 && typeof(T) == typeof(EventArgs))
                    || info.EventHandlerType.GenericTypeArguments[0] == typeof(T))
                {
                    return;
                }

                throw new ArgumentOutOfRangeException(nameof(e), $"{e} - {typeof(T).FullName}");
            }
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            _isClosedOrClosing = true;
            if (Browser != null)
            {
                Browser.BrowserContextsList.Remove(this);
            }

            _closeTcs.TrySetResult(true);
            RejectPendingOperations();
            Closed?.Invoke(this, EventArgs.Empty);
            _scope.Dispose();
        }

        private void Channel_OnPage(object sender, BrowserContextOnPageEventArgs e)
        {
            var page = e.PageChannel.Object;
            page.BrowserContext = this;
            PagesList.Add(page);
            PageCreated?.Invoke(this, new PageEventArgs { Page = page });
        }

        private void RejectPendingOperations()
        {
            foreach (var (_, waitTcs) in _waitForCancellationTcs.Where(e => e.contextEvent != ContextEvent.Closed))
            {
                waitTcs.TrySetException(new TargetClosedException("Context closed"));
            }

            _waitForCancellationTcs.Clear();
        }
    }
}
