using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    public class ChromiumProtocolTypesGenerator : IProtocolTypesGenerator
    {
        private static readonly string NamespacePrefix = "PlaywrightSharp.Chromium.Protocol";
        private readonly IDictionary<string, string> knownTypes = new Dictionary<string, string>();

        public async Task GenerateTypesAsync(RevisionInfo revision)
        {
            string directory = Path.Join("..", "..", "..", "..", "PlaywrightSharp.Chromium", "Protocol");
            string output = Path.Join(directory, "Protocol.Generated.cs");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (revision.Local && File.Exists(output))
            {
                return;
            }

            using (var process = Process.Start(revision.ExecutablePath, "--remote-debugging-port=9222 --headless"))
            using (var stream = await new HttpClient().GetStreamAsync("http://localhost:9222/json/protocol"))
            {
                var response = await JsonSerializer.DeserializeAsync<ChromiumProtocolDomainsContainer>(stream, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                process.Kill();

                PrepareArrayTypes(response);

                var builder = new StringBuilder();
                GenerateConstants(builder);

                foreach (var domain in response.Domains)
                {
                    builder.AppendLine($"namespace {NamespacePrefix}.{domain.Domain}");
                    builder.AppendLine("{");

                    GenerateTypes(builder, domain);

                    GenerateCommands(builder, domain);

                    GenerateEvents(builder, domain);

                    builder.AppendLine("}");
                }
                builder.AppendLine("#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member");
                await File.WriteAllTextAsync(output, builder.ToString());

                return;
            }
        }

        private void GenerateConstants(StringBuilder builder)
        {
            builder.Append(@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

");
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
                            knownTypes[type.Id] = itemType + "[]";
                            knownTypes[$"{domain.Domain}.{type.Id}"] = itemType + "[]";
                        }
                    }
                    else if (type.Type == "string")
                    {
                        knownTypes[type.Id] = "string";
                        knownTypes[$"{domain.Domain}.{type.Id}"] = "string";
                    }
                    else if (type.Type == "integer")
                    {
                        knownTypes[type.Id] = "int";
                        knownTypes[$"{domain.Domain}.{type.Id}"] = "int";
                    }
                    else if (type.Type == "number")
                    {
                        knownTypes[type.Id] = "long";
                        knownTypes[$"{domain.Domain}.{type.Id}"] = "long";
                    }
                }
            }

            // work around, too lazy to solve this
            knownTypes["Headers"] = "System.Collections.Generic.IDictionary<string, string>";
            knownTypes["ArrayOfStrings"] = "int[]";
            knownTypes["StringIndex"] = "int";
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
                    builder.AppendLine($"/// {FormatDocs(type.Description)}");
                    builder.AppendLine("/// </summary>");
                    builder.AppendLine($"internal enum {type.Id}");
                    builder.AppendLine("{");
                    builder.AppendJoin(",\n", NormalizeEnum(type.Enum));
                    builder.AppendLine("}");
                }
                else if (type.Type == "object")
                {
                    builder.AppendLine("/// <summary>");
                    builder.AppendLine($"/// {FormatDocs(type.Description)}");
                    builder.AppendLine("/// </summary>");
                    builder.AppendLine($"public class {type.Id}");
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
                string baseName = $"{domain.Domain}{char.ToUpper(command.Name[0])}{command.Name.Substring(1)}";
                builder.AppendLine("/// <summary>");
                builder.AppendLine($"/// {FormatDocs(command.Description)}");
                builder.AppendLine("/// </summary>");
                builder.AppendLine("/// <remarks>");
                builder.AppendLine($"/// Will send the command <c>{domain.Domain}.{command.Name}</c>");
                builder.AppendLine("/// </remarks>");
                if (command.Description?.StartsWith("Deprecated") == true)
                {
                    builder.AppendLine($"[System.Obsolete(\"{command.Description.Replace("\n", "\\n")}\")]");
                }
                builder.AppendLine($"internal class {baseName}Request : IChromiumRequest<{baseName}Response>");
                builder.AppendLine("{");
                builder.AppendLine("[System.Text.Json.Serialization.JsonIgnore]");
                builder.AppendLine($"public string Command {{ get; }} = \"{domain.Domain}.{command.Name}\";");
                builder.AppendJoin("\n", NormalizeProperties(command.Parameters));
                builder.AppendLine("}");

                // response
                builder.AppendLine("/// <summary>");
                builder.AppendLine($"/// Response from <see cref=\"{baseName}Request\"/>");
                builder.AppendLine("/// </summary>");
                builder.AppendLine($"internal class {baseName}Response : IChromiumResponse");
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
                string eventName = char.ToUpper(e.Name[0]) + e.Name.Substring(1);
                builder.AppendLine("/// <summary>");
                builder.AppendLine($"/// {FormatDocs(e.Description)}");
                builder.AppendLine("/// </summary>");
                builder.AppendLine("/// <remarks>");
                builder.AppendLine($"/// Matches on the event <c>{domain.Domain}.{e.Name}</c>");
                builder.AppendLine("/// </remarks>");
                builder.Append("internal class ").Append(domain.Domain).Append(eventName).AppendLine("ChromiumEvent : ChromiumEvent");
                builder.AppendLine("{");
                builder.AppendLine($"public override string InternalName {{ get; }} = \"{domain.Domain}.{e.Name}\";");
                builder.AppendJoin("\n", NormalizeProperties(e.Parameters));
                builder.AppendLine("}");
            }
        }

        public string FormatDocs(string docs) => docs?
            .Replace("\n", "\n/// ", StringComparison.OrdinalIgnoreCase)
            .Replace("<", "&lt;", StringComparison.OrdinalIgnoreCase)
            .Replace(">", "&gt;", StringComparison.OrdinalIgnoreCase);

        public string GetTypeOfProperty(ChromiumProtocolDomainProperty property)
        {
            if (property.Ref != null)
            {
                return ConvertRefToCsharp(property.Ref);
            }

            try
            {
                return property.Type switch
                {
                    "array" => ConvertItemsProperty(property.Items),
                    _ => ConvertJsToCsharp(property.Type)
                };
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string ConvertItemsProperty(ChromiumProtocolDomainItems items) => (items.Type != null ? ConvertJsToCsharp(items.Type) : ConvertRefToCsharp(items.Ref)) + "[]";

        public string ConvertRefToCsharp(string refValue) => knownTypes.TryGetValue(refValue, out string refClass) ? refClass : refValue;

        public string ConvertJsToCsharp(string type) => type switch
        {
            "string" => "string",
            "number" => "double",
            "integer" => "int",
            "boolean" => "bool",
            "binary" => "byte[]",
            "any" => "object",
            "object" => "object",
            _ => null
        };

        public string[] NormalizeProperties(ChromiumProtocolDomainProperty[] properties)
        {
            if (properties == null) return Array.Empty<string>();

            return Array.ConvertAll(properties, property =>
            {
                var builder = new StringBuilder()
                    .AppendLine("/// <summary>")
                    .AppendLine($"/// {FormatDocs(property.Description)}")
                    .AppendLine("/// </summary>")
                    .Append("public ")
                    .Append(GetTypeOfProperty(property))
                    .Append($" {char.ToUpper(property.Name[0])}{property.Name.Substring(1)} ")
                    .Append("{ get; set; }");

                return builder.ToString();
            });
        }

        public string[] NormalizeEnum(string[] values)
            => Array.ConvertAll(values, value =>
            {
                var builder = new StringBuilder().Append($"[System.Runtime.Serialization.EnumMember(Value = \"{value}\")]");
                bool shouldUppercase = true;
                for (int i = 0; i < value.Length; i++)
                {
                    if (char.IsLetter(value[i]))
                    {
                        if (char.IsUpper(value[i]))
                        {
                            shouldUppercase = false;
                            builder.Append(char.ToUpper(value[i]));
                        }
                        else if (shouldUppercase && char.IsLower(value[i]))
                        {
                            shouldUppercase = false;
                            builder.Append(char.ToUpper(value[i]));
                        }
                        else
                        {
                            builder.Append(value[i]);
                        }
                    }
                    else if (char.IsDigit(value[i]))
                    {
                        builder.Append(value[i]);
                        shouldUppercase = true;
                    }
                    else
                    {
                        shouldUppercase = true;
                    }
                }

                return builder.ToString();
            });

        public string GenerateIdType(string name, string rawType) => @$"internal readonly struct {name} : System.IComparable<{name}>, System.IEquatable<{name}>
        {{
            public {rawType} Value {{ get; }}

            public {name}({rawType} value)
            {{
                Value = value;
            }}

            public bool Equals({name} other) => this.Value.Equals(other.Value);
            public int CompareTo({name} other) => Value.CompareTo(other.Value);

            public override bool Equals(object obj)
            {{
                if (ReferenceEquals(null, obj)) return false;
                return obj is {name} other && Equals(other);
            }}

            public override int GetHashCode() => Value.GetHashCode();
            public override string ToString() => Value.ToString();

            public static bool operator ==({name} a, {name} b) => a.CompareTo(b) == 0;
            public static bool operator !=({name} a, {name} b) => !(a == b);
        }}";

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
    }
}
