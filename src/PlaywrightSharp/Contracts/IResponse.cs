using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Partial <see cref="IResponse"/>.
    /// </summary>
    public partial interface IResponse
    {
        /// <inheritdoc cref="GetJsonAsync{T}"/>
        Task<JsonDocument> GetJsonAsync(JsonDocumentOptions options = default);
    }
}
