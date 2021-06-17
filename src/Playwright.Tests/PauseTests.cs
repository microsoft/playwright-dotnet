using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PauseTests : PageTestEx
    {
        [Test]
        public async Task ShouldNotFail()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.PauseAsync();
        }
    }
}
