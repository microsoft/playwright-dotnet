using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator.Chromium
{
    internal class ChromiumProtocolTypesGenerator : ProtocolTypesGeneratorBase
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IDictionary<string, string> _knownTypes = new Dictionary<string, string>();

        protected override string Project { get; } = "Chromium";

        protected override async Task GenerateTypesAsync(StringBuilder builder, RevisionInfo revision)
        {
            using var process = Process.Start(revision.ExecutablePath, "--remote-debugging-port=9222 --headless");
            using var stream = await _httpClient.GetStreamAsync(new Uri("http://localhost:9222/json/protocol")).ConfigureAwait(false);
            var response = await JsonSerializer.DeserializeAsync<ChromiumProtocolDomainsContainer>(stream, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }).ConfigureAwait(false);
            process.Kill();

            PrepareArrayTypes(response);

            foreach (var domain in response.Domains)
            {
                builder.Append("namespace ").Append(NamespacePrefix).Append('.').AppendLine(domain.Domain);
                builder.AppendLine("{");

                GenerateTypes(builder, domain);

                GenerateCommands(builder, domain);

                GenerateEvents(builder, domain);

                builder.AppendLine("}");
            }
        }

        private void PrepareArrayTypes(ChromiumProtocolDomainsContainer response)
        {
            foreach (var domain in response.Domains)
            {
                foreach (var type in domain.Types ?? Array.Empty<ChromiumProtocolDomainType>())
                {
                    if (type.Type == "array")
                    {
                        string itemType = ConvertJsToCsharp(type?.Items?.Type);
                        if (itemType != null)
                        {
                            _knownTypes[type.Id] = itemType + "[]";
                            _knownTypes[$"{domain.Domain}.{type.Id}"] = itemType + "[]";
                        }
                    }
                    else if (type.Type == "string" && type.Enum == null)
                    {
                        _knownTypes[type.Id] = "string";
                        _knownTypes[$"{domain.Domain}.{type.Id}"] = "string";
                    }
                    else if (type.Type == "integer")
                    {
                        _knownTypes[type.Id] = "int?";
                        _knownTypes[$"{domain.Domain}.{type.Id}"] = "int?";
                    }
                    else if (type.Type == "number")
                    {
                        _knownTypes[type.Id] = "double?";
                        _knownTypes[$"{domain.Domain}.{type.Id}"] = "double?";
                    }
                }
            }

            // work around, too lazy to solve this
            _knownTypes["Headers"] = "System.Collections.Generic.IDictionary<string, string>";
            _knownTypes["ArrayOfStrings"] = "int[]";
            _knownTypes["StringIndex"] = "int";
        }

        private void GenerateTypes(StringBuilder builder, ChromiumProtocolDomain domain)
        {
            if (domain.Types == null)
            {
                return;
            }

            foreach (var type in domain.Types)
            {
                if (type.Enum != null)
                {
                    builder.AppendLine("/// <summary>");
                    builder.Append("/// ").AppendLine(FormatDocs(type.Description));
                    builder.AppendLine("/// </summary>");
                    builder.Append("internal enum ").AppendLine(type.Id);
                    builder.AppendLine("{");
                    builder.AppendJoin(",\n", NormalizeEnum(type.Enum));
                    builder.AppendLine("}");
                }
                else if (type.Type == "object")
                {
                    builder.AppendLine("/// <summary>");
                    builder.Append("/// ").AppendLine(FormatDocs(type.Description));
                    builder.AppendLine("/// </summary>");
                    builder.Append("internal class ").AppendLine(type.Id);
                    builder.AppendLine("{");
                    builder.AppendJoin("\n", NormalizeProperties(type.Properties));
                    builder.AppendLine("}");
                }
            }
        }

        private void GenerateCommands(StringBuilder builder, ChromiumProtocolDomain domain)
        {
            if (domain.Commands == null)
            {
                return;
            }

            foreach (var command in domain.Commands)
            {
                // request
                string baseName = $"{domain.Domain}{command.Name.ToPascalCase()}";
                builder.AppendLine("/// <summary>");
                builder.Append("/// ").AppendLine(FormatDocs(command.Description));
                builder.AppendLine("/// </summary>");
                builder.AppendLine("/// <remarks>");
                builder.Append("/// Will send the command <c>").Append(domain.Domain).Append('.').Append(command.Name).AppendLine("</c>");
                builder.AppendLine("/// </remarks>");
                if (command.Description?.StartsWith("Deprecated", StringComparison.OrdinalIgnoreCase) == true)
                {
                    builder.Append("[System.Obsolete(\"").Append(command.Description.Replace("\n", "\\n", StringComparison.OrdinalIgnoreCase)).AppendLine("\")]");
                }

                builder.Append("internal class ").Append(baseName).Append("Request : IChromiumRequest<").Append(baseName).AppendLine("Response>");
                builder.AppendLine("{");
                builder.AppendLine("[System.Text.Json.Serialization.JsonIgnore]");
                builder.Append("public string Command { get; } = \"").Append(domain.Domain).Append('.').Append(command.Name).AppendLine("\";");
                builder.AppendJoin("\n", NormalizeProperties(command.Parameters));
                builder.AppendLine("}");

                // response
                builder.AppendLine("/// <summary>");
                builder.Append("/// Response from <see cref=\"").Append(baseName).AppendLine("Request\"/>");
                builder.AppendLine("/// </summary>");
                builder.Append("internal class ").Append(baseName).AppendLine("Response : IChromiumResponse");
                builder.AppendLine("{");
                builder.AppendJoin("\n", NormalizeProperties(command.Returns));
                builder.AppendLine("}");
            }
        }

        private void GenerateEvents(StringBuilder builder, ChromiumProtocolDomain domain)
        {
            if (domain.Events == null)
            {
                return;
            }

            foreach (var e in domain.Events)
            {
                string eventName = e.Name.ToPascalCase();
                builder.AppendLine("/// <summary>");
                builder.Append("/// ").AppendLine(FormatDocs(e.Description));
                builder.AppendLine("/// </summary>");
                builder.AppendLine("/// <remarks>");
                builder.Append("/// Matches on the event <c>").Append(domain.Domain).Append('.').Append(e.Name).AppendLine("</c>");
                builder.AppendLine("/// </remarks>");
                builder.Append("internal class ").Append(domain.Domain).Append(eventName).AppendLine("ChromiumEvent : IChromiumEvent");
                builder.AppendLine("{");
                builder.Append("public string InternalName { get; } = \"").Append(domain.Domain).Append('.').Append(e.Name).AppendLine("\";");
                builder.AppendJoin("\n", NormalizeProperties(e.Parameters));
                builder.AppendLine("}");
            }
        }

        private string FormatDocs(string docs)
            => docs?
            .Replace("\n", "\n/// ", StringComparison.OrdinalIgnoreCase)
            .Replace("<", "&lt;", StringComparison.OrdinalIgnoreCase)
            .Replace(">", "&gt;", StringComparison.OrdinalIgnoreCase);

        private string GetTypeOfProperty(ChromiumProtocolDomainProperty property)
        {
            if (property.Ref != null)
            {
                return ConvertRefToCsharp(property.Ref);
            }

            return property.Type switch
            {
                "array" => ConvertItemsProperty(property.Items),
                _ => ConvertJsToCsharp(property.Type)
            };
        }

        private string ConvertItemsProperty(ChromiumProtocolDomainItems items)
            => (items.Type != null ? ConvertJsToCsharp(items.Type) : ConvertRefToCsharp(items.Ref)) + "[]";

        private string ConvertRefToCsharp(string refValue)
            => _knownTypes.TryGetValue(refValue, out string refClass) ? refClass : refValue;

        private string ConvertJsToCsharp(string type)
            => type switch
            {
                "string" => "string",
                "number" => "double?",
                "integer" => "int?",
                "boolean" => "bool?",
                "binary" => "byte[]",
                "any" => "JsonElement?",
                "object" => "JsonElement?",
                _ => null
            };

        private string[] NormalizeProperties(ChromiumProtocolDomainProperty[] properties)
        {
            if (properties == null)
            {
                return Array.Empty<string>();
            }

            return Array.ConvertAll(properties, property =>
            {
                var builder = new StringBuilder()
                    .AppendLine("/// <summary>")
                    .Append("/// ").AppendLine(FormatDocs(property.Description))
                    .AppendLine("/// </summary>")
                    .Append("public ")
                    .Append(GetTypeOfProperty(property))
                    .Append(' ').Append(property.Name.ToPascalCase()).Append(' ')
                    .Append("{ get; set; }");

                return builder.ToString();
            });
        }

        private string[] NormalizeEnum(string[] values)
            => Array.ConvertAll(values, value => value.ToEnumField());

#pragma warning disable CA1812
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

            public ChromiumProtocolDomainCommand[] Commands { get; set; }

            public ChromiumProtocolDomainEvent[] Events { get; set; }
        }

        public class ChromiumProtocolDomainType
        {
            public string Id { get; set; }

            public string Description { get; set; }

            public string Type { get; set; }

            public string[] Enum { get; set; }

            public ChromiumProtocolDomainProperty[] Properties { get; set; }

            public bool? Optional { get; set; }

            public ChromiumProtocolDomainItems Items { get; set; }
        }

        public class ChromiumProtocolDomainProperty
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public string Type { get; set; }

            [JsonPropertyName("$ref")]
            public string Ref { get; set; }

            public bool? Optional { get; set; }

            public ChromiumProtocolDomainItems Items { get; set; }
        }

        public class ChromiumProtocolDomainItems
        {
            [JsonPropertyName("$ref")]
            public string Ref { get; set; }

            public string Type { get; set; }
        }

        public class ChromiumProtocolDomainCommand
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public bool? Experimental { get; set; }

            public ChromiumProtocolDomainProperty[] Parameters { get; set; }

            public ChromiumProtocolDomainProperty[] Returns { get; set; }
        }

        public class ChromiumProtocolDomainEvent
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public bool? Experimental { get; set; }

            public ChromiumProtocolDomainProperty[] Parameters { get; set; }
        }
#pragma warning restore CA1812
    }
}
