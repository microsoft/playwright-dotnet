using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Browser context delegate.
    /// </summary>
    internal interface IBrowserContextDelegate
    {
        /// <summary>
        /// Creates a new page in the context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the new page is created, yielding the <see cref="IPage"/>.</returns>
        Task<IPage> NewPage();
    }
}
