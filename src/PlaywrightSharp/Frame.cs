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
            throw new System.NotImplementedException();
        }

        public Task<IResponse> GoToAsync(string url, WaitUntilNavigation waitUntil)
        {
            throw new System.NotImplementedException();
        }

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