using System.Threading.Tasks;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxExecutionContext : IExecutionContextDelegate
    {
        private readonly FirefoxSession _session;
        private readonly string _executionContextId;

        public FirefoxExecutionContext(FirefoxSession workerSession, string executionContextId)
        {
            _session = workerSession;
            _executionContextId = executionContextId;
        }

        public Task<T> EvaluateAsync<T>(FrameExecutionContext frameExecutionContext, bool returnByValue, string script, object[] args)
        {
            throw new System.NotImplementedException();
        }

        public Task ReleaseHandleAsync(JSHandle handle)
        {
            throw new System.NotImplementedException();
        }
    }
}
