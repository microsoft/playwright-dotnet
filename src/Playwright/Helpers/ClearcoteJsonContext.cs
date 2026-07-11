using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Helpers;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(VersionPlan))]
[JsonSerializable(typeof(ProDownloadOptions))]
[JsonSerializable(typeof(CatalogPlatform))]
[JsonSerializable(typeof(CatalogBuild))]
[JsonSerializable(typeof(Catalog))]
internal partial class ClearcoteJsonContext : JsonSerializerContext
{
}
