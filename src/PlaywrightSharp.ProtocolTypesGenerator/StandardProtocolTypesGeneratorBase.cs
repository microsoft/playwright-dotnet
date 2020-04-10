using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal abstract class StandardProtocolTypesGeneratorBase : ProtocolTypesGeneratorBase
    {
        private readonly IDictionary<string, string> _knownTypes = new Dictionary<string, string>();

        protected abstract Task<ProtocolDomainsContainer> RetrieveProtocolAsync(RevisionInfo revision);

        protected override async Task GenerateTypesAsync(StringBuilder builder, RevisionInfo revision)
        {
            var response = await RetrieveProtocolAsync(revision).ConfigureAwait(false);

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

        private void PrepareArrayTypes(ProtocolDomainsContainer response)
        {
            foreach (var domain in response.Domains)
            {
                foreach (var type in domain.Types ?? Array.Empty<ProtocolDomainType>())
                {
                    switch (type.Type)
                    {
                        case "array":

                            string itemType = ConvertJsToCsharp(type?.Items?.Type, false);
                            if (itemType != null)
                            {
                                _knownTypes[type.Id] = itemType + "[]";
                                _knownTypes[$"{domain.Domain}.{type.Id}"] = itemType + "[]";
                            }

                            break;

                        case "string" when type.Enum == null:
                            _knownTypes[type.Id] = "string";
                            _knownTypes[$"{domain.Domain}.{type.Id}"] = "string";
                            break;
                        case "integer":
                            _knownTypes[type.Id] = "int?";
                            _knownTypes[$"{domain.Domain}.{type.Id}"] = "int?";
                            break;
                        case "number":
                            _knownTypes[type.Id] = "double?";
                            _knownTypes[$"{domain.Domain}.{type.Id}"] = "double?";
                            break;
                    }
                }
            }

            // work around, too lazy to solve this
            _knownTypes["Headers"] = "System.Collections.Generic.IDictionary<string, string>";
            _knownTypes["ArrayOfStrings"] = "int[]";
            _knownTypes["StringIndex"] = "int";
        }

        private void GenerateTypes(StringBuilder builder, ProtocolDomain domain)
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
                    builder.AppendLine("[JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumMemberConverter))]");
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
                    GenerateTypeDefinition(builder, type.Id);
                    builder.AppendLine("{");
                    builder.AppendJoin("\n", NormalizeProperties(domain, type.Properties, false));
                    builder.AppendLine("}");
                }
            }
        }

        private void GenerateCommands(StringBuilder builder, ProtocolDomain domain)
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

                GenerateRequestDefinition(builder, baseName);
                builder.AppendLine("{");
                builder.AppendLine("[System.Text.Json.Serialization.JsonIgnore]");
                builder.Append("public string Command { get; } = \"").Append(domain.Domain).Append('.').Append(command.Name).AppendLine("\";");
                builder.AppendJoin("\n", NormalizeProperties(domain, command.Parameters, false));
                builder.AppendLine("}");

                // response
                builder.AppendLine("/// <summary>");
                builder.Append("/// Response from <see cref=\"").Append(baseName).AppendLine("Request\"/>");
                builder.AppendLine("/// </summary>");
                GenerateResponseDefinition(builder, baseName);
                builder.AppendLine("{");
                builder.AppendJoin("\n", NormalizeProperties(domain, command.Returns, true));
                builder.AppendLine("}");
            }
        }

        private void GenerateEvents(StringBuilder builder, ProtocolDomain domain)
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
                GenerateEventDefinition(builder, domain.Domain + eventName);
                builder.AppendLine("{");
                builder.Append("public string InternalName { get; } = \"").Append(domain.Domain).Append('.').Append(e.Name).AppendLine("\";");
                builder.AppendJoin("\n", NormalizeProperties(domain, e.Parameters, false));
                builder.AppendLine("}");
            }
        }

        private string FormatDocs(string docs)
            => docs?
            .Replace("\n", "\n/// ", StringComparison.OrdinalIgnoreCase)
            .Replace("<", "&lt;", StringComparison.OrdinalIgnoreCase)
            .Replace(">", "&gt;", StringComparison.OrdinalIgnoreCase);

        private string GetTypeOfProperty(ProtocolDomainProperty property, bool isResponse)
        {
            if (property.Ref != null)
            {
                return ConvertRefToCsharp(property.Ref);
            }

            return property.Type switch
            {
                "array" => ConvertItemsProperty(property.Items, isResponse),
                _ => ConvertJsToCsharp(property.Type, isResponse)
            };
        }

        private string ConvertItemsProperty(ProtocolDomainItems items, bool isResponse)
            => (items.Type != null ? ConvertJsToCsharp(items.Type, isResponse) : ConvertRefToCsharp(items.Ref)) + "[]";

        private string ConvertRefToCsharp(string refValue)
            => _knownTypes.TryGetValue(refValue, out string refClass) ? refClass : refValue;

        private string ConvertJsToCsharp(string type, bool isResponse)
            => type switch
            {
                "string" => "string",
                "number" => "double?",
                "integer" => "int?",
                "boolean" => "bool?",
                "binary" => "byte[]",
                "any" => isResponse ? "JsonElement?" : "object",
                "object" => isResponse ? "JsonElement?" : "object",
                _ => null
            };

        private string[] NormalizeProperties(ProtocolDomain domain, ProtocolDomainProperty[] properties, bool isResponse)
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
                    .AppendLine("/// </summary>");

                if (domain.Types?.Any(t => t.Enum != null && t.Id == property.Ref) == true)
                {
                    builder.AppendLine("[JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumMemberConverter))]");
                }

                builder.Append("public ")
                    .Append(GetTypeOfProperty(property, isResponse))
                    .Append(' ').Append(property.Name.ToPascalCase()).Append(' ')
                    .Append("{ get; set; }");

                return builder.ToString();
            });
        }

        private string[] NormalizeEnum(string[] values)
            => Array.ConvertAll(values, value => value.ToEnumField());

#pragma warning disable CA1812
        public class ProtocolDomainsContainer
        {
            public ProtocolDomain[] Domains { get; set; }
        }

        public class ProtocolDomain
        {
            public string Domain { get; set; }

            public bool Experemental { get; set; }

            public string[] Dependencies { get; set; }

            public ProtocolDomainType[] Types { get; set; }

            public ProtocolDomainCommand[] Commands { get; set; }

            public ProtocolDomainEvent[] Events { get; set; }
        }

        public class ProtocolDomainType
        {
            public string Id { get; set; }

            public string Description { get; set; }

            public string Type { get; set; }

            public string[] Enum { get; set; }

            public ProtocolDomainProperty[] Properties { get; set; }

            public bool? Optional { get; set; }

            public ProtocolDomainItems Items { get; set; }
        }

        public class ProtocolDomainProperty
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public string Type { get; set; }

            [JsonPropertyName("$ref")]
            public string Ref { get; set; }

            public bool? Optional { get; set; }

            public ProtocolDomainItems Items { get; set; }
        }

        public class ProtocolDomainItems
        {
            [JsonPropertyName("$ref")]
            public string Ref { get; set; }

            public string Type { get; set; }
        }

        public class ProtocolDomainCommand
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public bool? Experimental { get; set; }

            public ProtocolDomainProperty[] Parameters { get; set; }

            public ProtocolDomainProperty[] Returns { get; set; }
        }

        public class ProtocolDomainEvent
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public bool? Experimental { get; set; }

            public ProtocolDomainProperty[] Parameters { get; set; }
        }
#pragma warning restore CA1812
    }
}
