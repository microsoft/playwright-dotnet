using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator.Webkit
{
    internal class WebkitProtocolTypesGenerator : StandardProtocolTypesGeneratorBase
    {
        protected override string Project { get; } = "Webkit";

        protected override async Task<ProtocolDomainsContainer> RetrieveProtocolAsync(RevisionInfo revision)
        {
            string json = await File.ReadAllTextAsync(Path.Combine(revision.FolderPath, "protocol.json")).ConfigureAwait(false);
            var domains = JsonSerializer.Deserialize<ProtocolDomain[]>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            return new ProtocolDomainsContainer { Domains = domains };
        }
    }
}
