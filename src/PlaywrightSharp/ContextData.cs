using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class ContextData
    {
        public TaskCompletionSource<FrameExecutionContext> ContextTsc { get; set; } = new TaskCompletionSource<FrameExecutionContext>();

        public List<RerunnableTask> RerunnableTasks { get; } = new List<RerunnableTask>();

        public Task<FrameExecutionContext> ContextTask => ContextTsc.Task;

        public FrameExecutionContext Context { get; set; }
    }
}
