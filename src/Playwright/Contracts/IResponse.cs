using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Partial <see cref="IResponse"/>.
    /// </summary>
    public partial interface IResponse
    {
        /// <inheritdoc cref="JsonAsync{T}"/>
        Task<JsonDocument> JsonAsync(JsonDocumentOptions options = default);
    }
}
