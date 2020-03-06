using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class FrameExecutionContext : IFrameExecutionContext
    {
        private readonly IExecutionContextDelegate _executionContextDelegate;
        private readonly Frame _frame;

        public FrameExecutionContext(IExecutionContextDelegate executionContextDelegate, Frame frame)
        {
            _executionContextDelegate = executionContextDelegate;
            _frame = frame;
        }

        public Frame Frame { get; set; }

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