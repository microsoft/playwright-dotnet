using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Partial <see cref="IResponse"/>.
    /// </summary>
    public partial interface IResponse
    {
        /// <summary>
        /// <seealso cref="GetJsonAsync{T}"/>.
        /// </summary>
        /// <param name="options">The Document options.</param>
        /// <returns>Returns a <see cref="JsonDocument"/> representation.</returns>
        Task<JsonDocument> GetJsonAsync(JsonDocumentOptions options = default);
    }
}
