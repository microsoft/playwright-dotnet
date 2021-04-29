using System.Linq;

namespace Microsoft.Playwright.Tests
{
    internal static class PageExtensions
    {
        public static IFrame FirstChildFrame(this IPage page) => page.Frames.FirstOrDefault(f => f.ParentFrame == page.MainFrame);
    }
}
