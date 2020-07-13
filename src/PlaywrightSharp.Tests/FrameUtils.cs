using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests
{
    internal static class FrameUtils
    {
        public static async Task<IFrame> AttachFrameAsync(IPage page, string frameId, string url)
        {
            var handle = await page.EvaluateHandleAsync(@" async (frameId, url) => {
              const frame = document.createElement('iframe');
              frame.src = url;
              frame.id = frameId;
              document.body.appendChild(frame);
              await new Promise(x => frame.onload = x);
              return frame
            }", new { frameId, url }) as IElementHandle;
            return await handle.GetContentFrameAsync();
        }

        public static async Task DetachFrameAsync(IPage page, string frameId)
        {
            await page.EvaluateAsync(@"function detachFrame(frameId) {
              const frame = document.getElementById(frameId);
              frame.remove();
            }", frameId);
        }

        public static IEnumerable<string> DumpFrames(IFrame frame, string indentation = "")
        {
            string description = indentation + Regex.Replace(frame.Url, @":\d{4}", ":<PORT>");
            if (!string.IsNullOrEmpty(frame.Name))
            {
                description += $" ({frame.Name})";
            }
            var result = new List<string>() { description };
            foreach (var child in frame.ChildFrames)
            {
                result.AddRange(DumpFrames(child, "    " + indentation));
            }

            return result;
        }

        internal static async Task NavigateFrameAsync(IPage page, string frameId, string url)
        {
            await page.EvaluateAsync(@"function navigateFrame(frameId, url) {
              const frame = document.getElementById(frameId);
              frame.src = url;
              return new Promise(x => frame.onload = x);
            }", new { frameId, url });
        }
    }
}
