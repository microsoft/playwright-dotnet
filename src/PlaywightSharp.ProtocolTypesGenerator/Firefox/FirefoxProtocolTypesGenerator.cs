using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;

// INodeServices is deprecated...
#pragma warning disable CS0618 // Type or member is obsolete
namespace PlaywrightSharp.ProtocolTypesGenerator.Firefox
{
    internal class FirefoxProtocolTypesGenerator : IProtocolTypesGenerator
    {
        public async Task GenerateTypesAsync(RevisionInfo revision)
        {
            string directory = Path.Join("..", "..", "..", "..", "PlaywrightSharp.Firefox", "Protocol");
            string output = Path.Join(directory, "Protocol.Generated.cs");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (revision.Local && File.Exists(output))
            {
                return;
            }

            string json = await ConvertProtocolJsToJson(revision).ConfigureAwait(false);

            return;
        }

        private async Task<string> ConvertProtocolJsToJson(RevisionInfo revision)
        {
            string protocolJs = "chrome/juggler/content/protocol/Protocol.js";
            string zipFile = Path.Combine(Directory.GetParent(revision.ExecutablePath).FullName, "omni.ja");
            using var zip = ZipFile.OpenRead(zipFile);
            using var reader = new StreamReader(zip.GetEntry(protocolJs).Open());
            string js = await reader.ReadToEndAsync().ConfigureAwait(false);

            return await GetNodejs().InvokeExportAsync<string>("Firefox/firefox-protocol.js", "getJson", js).ConfigureAwait(false);
        }

        private INodeServices GetNodejs()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddNodeServices();
            return services.BuildServiceProvider()
                .GetService<INodeServices>();
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
