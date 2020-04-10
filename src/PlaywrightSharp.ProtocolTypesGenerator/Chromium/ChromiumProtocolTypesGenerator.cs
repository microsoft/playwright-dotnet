using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator.Chromium
{
    internal class ChromiumProtocolTypesGenerator : StandardProtocolTypesGeneratorBase
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        protected override string Project { get; } = "Chromium";

        protected override async Task<ProtocolDomainsContainer> RetrieveProtocolAsync(RevisionInfo revision)
        {
            using var process = Process.Start(revision.ExecutablePath, "--remote-debugging-port=9222 --headless");
            await using var stream = await _httpClient.GetStreamAsync(new Uri("http://localhost:9222/json/protocol")).ConfigureAwait(false);
            var response = await JsonSerializer.DeserializeAsync<ProtocolDomainsContainer>(stream, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }).ConfigureAwait(false);
            process.Kill();

            return response;
        }
    }
}
