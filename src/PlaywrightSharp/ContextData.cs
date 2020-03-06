using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class ContextData
    {
        public TaskCompletionSource<IFrameExecutionContext> ContextTsc { get; } = new TaskCompletionSource<IFrameExecutionContext>();

        public List<RerunnableTask> RerunnableTasks { get; } = new List<RerunnableTask>();

        public Task<IFrameExecutionContext> ContextTask => ContextTsc.Task;

        public Action<IFrameExecutionContext> ContextResolveCallback { get; set; } = _ => { };

        public IFrameExecutionContext Context { get; set; }
    }
}