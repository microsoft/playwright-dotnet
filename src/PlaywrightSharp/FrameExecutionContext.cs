using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class FrameExecutionContext : IFrameExecutionContext
    {
        public Task<T> EvaluateAsync<T>(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}