using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PauseTests : PageTestEx
    {
        [Test]
        public async Task ShouldNotFail()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.PauseAsync();
        }
    }
}
