using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class ContextData
    {
        public TaskCompletionSource<IFrameExecutionContext> ContextTsc { get; set; } = new TaskCompletionSource<IFrameExecutionContext>();

        public List<RerunnableTask> RerunnableTasks { get; } = new List<RerunnableTask>();

        public Task<IFrameExecutionContext> ContextTask => ContextTsc.Task;

        public IFrameExecutionContext Context { get; set; }
    }
}
