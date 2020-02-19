using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IProtocolTypesGenerator[] generators =
            {
                new ChromiumProtocolTypesGenerator()
            };

            var revision = new RevisionInfo("test")
            {
                ExecutablePath = @"D:\Playground\Playground.cs\ConsoleApp1\ConsoleApp1\.local-chromium\Win64-706915\chrome-win\chrome.exe",
                Local = true
            };

            foreach (var generator in generators)
            {
                await generator.GenerateTypesAsync(revision);
            }
        }
    }

    public interface IProtocolTypesGenerator
    {
        Task GenerateTypesAsync(RevisionInfo revision);
    }

    public class ChromiumProtocolTypesGenerator : IProtocolTypesGenerator
    {
        public async Task GenerateTypesAsync(RevisionInfo revision)
        {
            string output = Path.Join("..", "..", "PlaywrightSharp.Chromium", "Protocol.Generated.cs");
            if (revision.Local && File.Exists(output))
            {
                return;
            }

            using (var process = Process.Start(revision.ExecutablePath, "--remote-debugging-port=9222"))
            using (var stream = await new HttpClient().GetStreamAsync("http://localhost:9222/json/protocol"))
            {   
                var response = await JsonSerializer.DeserializeAsync<ChromiumProtocolDomainsContainer>(stream, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                process.Kill();

                return;
            }
        }

        public class ChromiumProtocolDomainsContainer
        {
            public ChromiumProtocolDomain[] Domains { get; set; }
        }

        public class ChromiumProtocolDomain
        {
            public string Domain { get; set; }
            public bool Experemental { get; set; }
            public string[] Dependencies { get; set; }
            public ChromiumProtocolDomainType[] Types { get; set; }
        }

        public class ChromiumProtocolDomainType
        {
            public string Id { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
        }
    }
}
