using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class Frame : IFrame
    {
        private readonly PageBase _page;
        private readonly string _frameId;
        private readonly IFrame _parentFrame;

        public Frame(PageBase page, string frameId, IFrame parentFrame)
        {
            _page = page;
            _frameId = frameId;
            _parentFrame = parentFrame;
        }

        IFrame[] IFrame.ChildFrames => null;

        public List<Frame> ChildFrames { get; } = new List<Frame>();

        public string Name { get; set; }

        public string Url { get; set; }

        public IFrame ParentFrame => null;

        public bool Detached { get; set; }

        public string Id { get; set; }

        public string LastDocumentId { get; set; }

        public PageBase Page => _page;

        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options)
        {
            throw new System.NotImplementedException();
        }

        public Task ClickAsync(string selector, ClickOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<T> EvaluateAsync<T>(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public Task<JsonElement?> EvaluateAsync(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public Task<IJSHandle> EvaluateHandleAsync(string expression)
        {
            throw new System.NotImplementedException();
        }

        public Task FillAsync(string selector, string text, WaitForSelectorOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<IResponse> GoToAsync(string url, GoToOptions options = null)
        {
            Page.PageState.ExtraHTTPHeaders.TryGetValue("referer", out string referer);

            if (options?.Referer != null)
            {
                if (referer != null && referer != options.Referer)
                {
                    throw new ArgumentException("\"referer\" is already specified as extra HTTP header");
                }


                referer = options.Referer;
            }

            using (var watcher = new LifecycleWatcher(this, options, false))
            {
                try
                {
                    var navigateTask = NavigateAsync(Client, url, referrer, frame.Id);
                    var task = await Task.WhenAny(
                        watcher.TimeoutOrTerminationTask,
                        navigateTask).ConfigureAwait(false);

                    await task;

                    task = await Task.WhenAny(
                        watcher.TimeoutOrTerminationTask,
                        _ensureNewDocumentNavigation ? watcher.NewDocumentNavigationTask : watcher.SameDocumentNavigationTask
                    ).ConfigureAwait(false);

                    await task;
                }
                catch (Exception ex)
                {
                    throw new NavigationException(ex.Message, ex);
                }

                return watcher.NavigationResponse;
            }

            /*
             let referer = (this._page._state.extraHTTPHeaders || {})['referer'];
    if (options && options.referer !== undefined) {
      if (referer !== undefined && referer !== options.referer)
        throw new Error('"referer" is already specified as extra HTTP header');
      referer = options.referer;
    }
    const watcher = new LifecycleWatcher(this, options, false );

            let navigateResult: GotoResult;
            const navigate = async () => {
                try
                {
                    navigateResult = await this._page._delegate.navigateFrame(this, url, referer);
                }
                catch (error)
                {
                    return error;
                }
            };

            let error = await Promise.race([
              navigate(),
              watcher.timeoutOrTerminationPromise,
        
            ]);
            if (!error)
            {
                const promises = [watcher.timeoutOrTerminationPromise];
                if (navigateResult!.newDocumentId)
                {
                    watcher.setExpectedDocumentId(navigateResult!.newDocumentId, url);
                    promises.push(watcher.newDocumentNavigationPromise);
                }
                else if (navigateResult!.isSameDocument)
                {
                    promises.push(watcher.sameDocumentNavigationPromise);
                }
                else
                {
                    promises.push(watcher.sameDocumentNavigationPromise, watcher.newDocumentNavigationPromise);
                }
                error = await Promise.race(promises);
            }
            watcher.dispose();
            if (error)
                throw error;
            return watcher.navigationResponse();
            */
        }

        public Task<IResponse> GoToAsync(string url, WaitUntilNavigation waitUntil)
            => GoToAsync(url, new GoToOptions { WaitUntil = new[] { waitUntil } });

        public void OnDetached()
        {
        }

        public Task<IElementHandle> QuerySelectorAsync(string selector)
        {
            throw new System.NotImplementedException();
        }

        public Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public Task SetContentAsync(string html, NavigationOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil)
        {
            throw new System.NotImplementedException();
        }

        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null)
        {
            throw new System.NotImplementedException();
        }
    }
}