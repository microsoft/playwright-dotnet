using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;

namespace PlaywrightSharp.ProtocolTypesGenerator.Firefox
{
    internal class FirefoxProtocolTypesGenerator : ProtocolTypesGeneratorBase
    {
        private readonly IDictionary<string, string> _knownTypes = new Dictionary<string, string>();
        private readonly IDictionary<string, string> _specialEnumFields = new Dictionary<string, string>
        {
            ["RemoteObjectUnserializableValue.Infinity"] = "Infinity",
            ["RemoteObjectUnserializableValue.-Infinity"] = "NegativeInfinity",
            ["RemoteObjectUnserializableValue.-0"] = "NegativeZero",
            ["RemoteObjectUnserializableValue.NaN"] = "NaN",
            ["SetEmulatedMediaType."] = "Empty",
        };

        protected override string Project { get; } = "Firefox";

        protected override async Task GenerateTypesAsync(StringBuilder builder, RevisionInfo revision)
        {
            string json = await ConvertProtocolJsToJson(revision).ConfigureAwait(false);
            var document = JsonDocument.Parse(json);

            var enumBuilder = new StringBuilder();
            foreach (var property in document.RootElement.GetProperty("domains").EnumerateObject())
            {
                builder.AppendLine($"namespace {NamespacePrefix}.{property.Name}");
                builder.AppendLine("{");

                GenerateTypes(builder, enumBuilder, property);
                builder.Append(enumBuilder);
                enumBuilder.Clear();

                builder.AppendLine("}");
            }

            foreach (var property in document.RootElement.GetProperty("domains").EnumerateObject())
            {
                builder.AppendLine($"namespace {NamespacePrefix}.{property.Name}");
                builder.AppendLine("{");

                GenerateEvents(builder, enumBuilder, property);
                GenerateMethods(builder, enumBuilder, property);
                builder.Append(enumBuilder);
                enumBuilder.Clear();

                builder.AppendLine("}");
            }
        }

        private void GenerateEvents(StringBuilder builder, StringBuilder enumBuilder, JsonProperty domain)
        {
            foreach (var eventDef in domain.Value.GetProperty("events").EnumerateObject())
            {
                string eventName = eventDef.Name.ToPascalCase();
                GenerateEventDefinition(builder, domain.Name + eventName);
                builder.AppendLine("{");
                builder.Append("public string InternalName { get; } = \"").Append(domain.Name).Append('.').Append(eventDef.Name).AppendLine("\";");
                foreach (var propertyDef in eventDef.Value.EnumerateObject())
                {
                    if (_knownTypes.TryGetValue(propertyDef.Value.GetRawText(), out string typeName))
                    {
                        builder.AppendLine($"public {typeName} {propertyDef.Name.ToPascalCase()} {{ get; set; }}");
                        continue;
                    }

                    string csharpType = ConvertJsTypeToCsharp(builder, domain.Name, eventName, propertyDef.Name, propertyDef.Value, enumBuilder, false);
                    builder.AppendLine($"public {csharpType} {propertyDef.Name.ToPascalCase()} {{ get; set; }}");
                }

                builder.AppendLine("}");
            }
        }

        private void GenerateMethods(StringBuilder builder, StringBuilder enumBuilder, JsonProperty domain)
        {
            foreach (var methodDef in domain.Value.GetProperty("methods").EnumerateObject())
            {
                string method = domain.Name + methodDef.Name.ToPascalCase();
                GenerateRequestDefinition(builder, method);
                builder.AppendLine("{");
                builder.AppendLine("[System.Text.Json.Serialization.JsonIgnore]");
                builder.Append("public string Command { get; } = \"").Append(domain.Name).Append('.').Append(methodDef.Name).AppendLine("\";");
                if (methodDef.Value.TryGetProperty("params", out var paramsDef))
                {
                    foreach (var propertyDef in paramsDef.EnumerateObject())
                    {
                        if (_knownTypes.TryGetValue(propertyDef.Value.GetRawText(), out string typeName))
                        {
                            builder.AppendLine($"public {typeName} {propertyDef.Name.ToPascalCase()} {{ get; set; }}");
                            continue;
                        }

                        string csharpType = ConvertJsTypeToCsharp(builder, domain.Name, methodDef.Name, propertyDef.Name, propertyDef.Value, enumBuilder, false);
                        builder.AppendLine($"public {csharpType} {propertyDef.Name.ToPascalCase()} {{ get; set; }}");
                    }
                }

                builder.AppendLine("}");
                GenerateResponseDefinition(builder, method);
                builder.AppendLine("{");

                if (methodDef.Value.TryGetProperty("returns", out var returnesDef))
                {
                    foreach (var propertyDef in returnesDef.EnumerateObject())
                    {
                        if (_knownTypes.TryGetValue(propertyDef.Value.GetRawText(), out string typeName))
                        {
                            builder.AppendLine($"public {typeName} {propertyDef.Name.ToPascalCase()} {{ get; set; }}");
                            continue;
                        }

                        string csharpType = ConvertJsTypeToCsharp(builder, domain.Name, methodDef.Name, propertyDef.Name, propertyDef.Value, enumBuilder, true);
                        builder.AppendLine($"public {csharpType} {propertyDef.Name.ToPascalCase()} {{ get; set; }}");
                    }
                }

                builder.AppendLine("}");
            }
        }

        private void GenerateTypes(StringBuilder builder, StringBuilder enumBuilder, JsonProperty domain)
        {
            foreach (var typeDef in domain.Value.GetProperty("types").EnumerateObject())
            {
                string type = $"{domain.Name}.{typeDef.Name.ToPascalCase()}";
                if (!_knownTypes.TryGetValue(typeDef.Value.GetRawText(), out string typeName))
                {
                    string jsonText = typeDef.Value.GetRawText();
                    string jsonTextNullable = jsonText.Substring(0, jsonText.Length - 1) + ",\"$nullable\":true}";
                    string jsonTextOptional = jsonText.Substring(0, jsonText.Length - 1) + ",\"$optional\":true}";
                    _knownTypes.Add(jsonText, type);
                    _knownTypes.Add(jsonTextNullable, type);
                    _knownTypes.Add(jsonTextOptional, type);
                }

                GenerateTypeDefinition(builder, typeDef.Name.ToPascalCase());
                builder.AppendLine("{");
                foreach (var propertyDef in typeDef.Value.EnumerateObject())
                {
                    string csharpType = ConvertJsTypeToCsharp(builder, domain.Name, typeDef.Name, propertyDef.Name, propertyDef.Value, enumBuilder, false);
                    builder.AppendLine($"public {csharpType} {propertyDef.Name.ToPascalCase()} {{ get; set; }}");
                }

                builder.AppendLine("}");
            }
        }

        private string ConvertJsTypeToCsharp(StringBuilder builder, string domain, string objectName, string name, JsonElement obj, StringBuilder enumBuilder, bool isResponse)
        {
            try
            {
                if (!obj.TryGetProperty("$type", out var typeElement))
                {
                    return _knownTypes[obj.GetRawText()];
                }

                string type = typeElement.GetString();
                if (type == "enum" && obj.TryGetProperty("$values", out var values)
                    && !_knownTypes.ContainsKey(values.GetRawText()))
                {
                    string enumName = objectName.ToPascalCase() + name.ToPascalCase();
                    _knownTypes.Add(values.GetRawText(), $"{domain}.{enumName}");
                    enumBuilder.AppendLine($"internal enum {enumName}");
                    enumBuilder.AppendLine("{");
                    foreach (var value in values.EnumerateArray())
                    {
                        if (_specialEnumFields.TryGetValue($"{enumName}.{value.ToString()}", out string fieldName))
                        {
                            enumBuilder.Append(ProtocolCodeGeneratorUtilities.CreateEnumField(value.ToString(), fieldName));
                        }
                        else
                        {
                            enumBuilder.Append(value.ToString().ToEnumField());
                        }

                        enumBuilder.AppendLine(",");
                    }

                    enumBuilder.AppendLine("}");
                }

                bool optional = false;
                if (obj.TryGetProperty("$optional", out var optionalElement))
                {
                    optional = optionalElement.GetBoolean();
                }

                return type switch
                {
                    "string" => "string",
                    "boolean" => "bool?",
                    "number" => "double?",
                    "enum" when _knownTypes.TryGetValue(obj.GetProperty("$values").GetRawText(), out string enumName) => enumName + (optional ? "?" : string.Empty),
                    "any" => isResponse ? "JsonElement?" : "object",
                    "object" => isResponse ? "JsonElement?" : "object",
                    "array" => ConvertJsTypeToCsharp(builder, domain, name, objectName, obj.GetProperty("$items"), enumBuilder, false) + "[]",
                    "ref" => obj.GetProperty("$ref").GetString(),
                    _ => throw new ArgumentOutOfRangeException(nameof(obj), type, $"[{domain}.{name}] invalid object type")
                };
            }
            catch (Exception e)
            {
                throw new Exception("ERROR", e);
            }
        }

        // INodeServices is deprecated...
#pragma warning disable CS0618 // Type or member is obsolete
        private async Task<string> ConvertProtocolJsToJson(RevisionInfo revision)
        {
            string protocolJs = "chrome/juggler/content/protocol/Protocol.js";
            string zipFile = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine(Directory.GetParent(revision.ExecutablePath).FullName, "..", "Resources", "omni.ja")
                : Path.Combine(Directory.GetParent(revision.ExecutablePath).FullName, "omni.ja");
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
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
