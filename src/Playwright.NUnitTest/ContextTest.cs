using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.NUnitTest
{
    public class ContextTest : BrowserTest
    {
        public IBrowserContext Context { get; private set; }

        public virtual BrowserNewContextOptions ContextOptions() => null;

        [SetUp]
        public async Task ContextSetup()
        {
            Context = await Browser.NewContextAsync(ContextOptions());
        }

        [TearDown]
        public async Task ContextTeardown()
        {
            if (TestOk())
            {
                await Context.CloseAsync();
            }
        }
    }
}
