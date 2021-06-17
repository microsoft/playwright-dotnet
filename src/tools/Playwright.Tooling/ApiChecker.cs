using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Playwright.Tooling.Extensions;
using Playwright.Tooling.Models.Api;
using Playwright.Tooling.Models.Mismatch;
using Playwright.Tooling.Options;

namespace Playwright.Tooling
{
    internal class ApiChecker
    {
        public string BasePath { get; set; }

        public string AssemblyPath { get; set; }

        public async Task<bool> ExecuteAsync()
        {
            var assembly = Assembly.LoadFrom(AssemblyPath);

            var report = new StringBuilder("<html><body><ul>");
            string json = await File.ReadAllTextAsync(Path.Combine(BasePath, "src", "Playwright", ".playwright", "api.json")).ConfigureAwait(false);

            var api = JsonSerializer.Deserialize<PlaywrightEntity[]>(json, new()
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase),
                },
            });

            string mismatchJsonFile = Path.Combine(BasePath, "src", "Playwright", ".playwright", "expected_api_mismatch.json");
            string mismatchJson = await File.ReadAllTextAsync(mismatchJsonFile).ConfigureAwait(false);
            Mismatch mismatches;

            try
            {
                mismatches = JsonSerializer.Deserialize<Mismatch>(mismatchJson, new()
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (Exception ex)
            {
                throw new($"Unable to parse file {mismatchJsonFile} with content {mismatchJson}", ex);
            }

            foreach (var entity in api)
            {
                EvaluateEntity(assembly, entity.Name, entity, report, mismatches);
            }

            report.Append("</ul></body></html>");
            await File.WriteAllTextAsync(
                Path.Combine(BasePath, "src", "Playwright", ".playwright", "report.html"),
                report.ToString()).ConfigureAwait(false);

            return true;
        }

        internal static Task RunAsync(ApiCheckerOptions o)
        {
            ApiChecker apiChecker = new ApiChecker
            {
                BasePath = o.BasePath,
                AssemblyPath = Path.Combine(o.BasePath, "src", "Playwright", "bin", "Debug", "net5.0", "Microsoft.Playwright.dll"),
            };
            return apiChecker.ExecuteAsync();
        }

        private static string TranslateMethodName(string memberName)
            => memberName
                .Replace("$$eval", "evalOnSelectorAll")
                .Replace("$eval", "evalOnSelector")
                .Replace("$$", "querySelectorAll")
                .Replace("$", "querySelector");

        private static bool IsParameterNameMatch(string argumentName, string playwrightName)
            => string.Equals(argumentName, playwrightName, StringComparison.OrdinalIgnoreCase) ||
                (playwrightName == "urlOrPredicate" && (argumentName == "url" || argumentName == "predicate"));

        private static Type GetBaseType(Type parameterType)
        {
            if (parameterType.IsArray)
            {
                return parameterType.GetElementType();
            }

            if (typeof(IEnumerable).IsAssignableFrom(parameterType) && parameterType.GenericTypeArguments.Length == 1)
            {
                return parameterType.GenericTypeArguments[0];
            }

            return Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        }

        private void EvaluateEntity(Assembly assembly, string name, PlaywrightEntity entity, StringBuilder report, Mismatch mismatches)
        {
            var playwrightSharpType = assembly.GetType($"Microsoft.Playwright.I{name}");

            if (playwrightSharpType == null)
            {
                playwrightSharpType = assembly.GetType($"Microsoft.Playwright.{name}");
            }

            if (playwrightSharpType == null)
            {
                playwrightSharpType = assembly.GetType($"Microsoft.Playwright.Chromium.{name}");
            }

            if (playwrightSharpType == null)
            {
                playwrightSharpType = assembly.GetType($"Microsoft.Playwright.{name}EventArgs");
            }

            if (playwrightSharpType != null)
            {
                var membersQueue = new List<object>();
                membersQueue.AddRange(playwrightSharpType.GetProperties());
                membersQueue.AddRange(playwrightSharpType.GetEvents());
                membersQueue.AddRange(playwrightSharpType.GetMethods().Where(m =>
                    !m.IsSpecialName &&
                    !new[] { "GetType", "ToString", "Equals", "GetHashCode" }.Contains(m.Name)));

                report.AppendLine("<li>");
                report.AppendLine($"{name}: found as {playwrightSharpType.Name}");

                report.AppendLine("<ul>");

                foreach (var member in entity.Members.Where(m => !m.Langs.Only.Any()))
                {
                    switch (member.Kind)
                    {
                        case PlaywrightMemberKind.Method:
                            EvaluateMethod(member, playwrightSharpType, report, membersQueue, mismatches);
                            break;
                        case PlaywrightMemberKind.Event:
                            EvaluateEvent(member, playwrightSharpType, report, membersQueue, mismatches);
                            break;
                        case PlaywrightMemberKind.Property:
                            EvaluateProperty(member, playwrightSharpType, report, mismatches);
                            break;
                    }
                }

                foreach (object memberInPlaywrightSharp in membersQueue)
                {
                    report.AppendLine("<li style='color: blue'>");
                    report.AppendLine($"{memberInPlaywrightSharp} FOUND IN PLAYWRIGHT SHARP");
                    report.AppendLine("</li>");
                }

                report.AppendLine("</ul>");
                report.AppendLine("</li>");
            }
            else
            {
                var mismatch = mismatches.Entities.FirstOrDefault(e => e.UpstreamClassName == name);

                if (mismatch == null)
                {
                    LogWarning("PW006", $"{name} entity not found");

                    report.AppendLine("<li style='color: red'>");
                    report.AppendLine($"{name} NOT FOUND (PW006)");
                    report.AppendLine("</li>");
                }
                else
                {
                    report.AppendLine($"<li style='color: coral'>{name} NOT FOUND ==> {mismatch.Justification}</li>");
                }
            }
        }

        private void LogWarning(string warningCode, string message) => Console.WriteLine($"{warningCode}: {message}");

        private void EvaluateMethod(
            PlaywrightMember member,
            Type playwrightSharpType,
            StringBuilder report,
            List<object> membersQueue,
            Mismatch mismatches)
        {
            string memberName = TranslateMethodName(member.Name);
            var typeToCheck = playwrightSharpType;
            MethodInfo playwrightSharpMethod = null;

            if (memberName == "toString")
            {
                report.AppendLine("<li>");
                report.AppendLine($"{memberName}: found as ToString");
                report.AppendLine("</li>");

                return;
            }

            while (typeToCheck != null)
            {
                playwrightSharpMethod = typeToCheck.GetMethods().FirstOrDefault(m => string.Equals(m.Name, memberName, StringComparison.OrdinalIgnoreCase));

                if (playwrightSharpMethod == null)
                {
                    playwrightSharpMethod = typeToCheck.GetMethods().FirstOrDefault(m => m.Name.ToLower() == memberName.ToLower() + "async");
                }

                if (playwrightSharpMethod == null)
                {
                    playwrightSharpMethod = typeToCheck.GetMethods().FirstOrDefault(m => m.Name.ToLower() == $"get{memberName.ToLower()}async");
                }

                if (playwrightSharpMethod == null)
                {
                    playwrightSharpMethod = typeToCheck.GetMethods().FirstOrDefault(m => m.Name.ToLower() == $"get{memberName.ToLower()}");
                }

                if (playwrightSharpMethod != null)
                {
                    break;
                }

                typeToCheck = typeToCheck.GetTypeInfo().ImplementedInterfaces.FirstOrDefault();
            }

            if (playwrightSharpMethod != null)
            {
                membersQueue.Remove(playwrightSharpMethod);

                report.AppendLine("<li>");
                report.AppendLine($"{memberName}: found as {playwrightSharpMethod.Name}");

                report.AppendLine("<ul>");

                if (member.Args != null)
                {
                    foreach (var arg in member.Args.Where(a => !a.Langs.Only.Any()))
                    {
                        var matchingMethod = GetBestMethodOverload(playwrightSharpType, playwrightSharpMethod.Name, arg.Name);

                        // we flatten options
                        if (arg.Type.Properties?.Any() == true && (arg.Name == "options" || matchingMethod == null))
                        {
                            foreach (var property in arg.Type.Properties.Where(p => !p.Langs.Only.Any()))
                            {
                                EvaluateArgument(property, typeToCheck, playwrightSharpMethod, report, membersQueue, mismatches);
                            }
                        }
                        else
                        {
                            EvaluateArgument(arg, typeToCheck, matchingMethod ?? playwrightSharpMethod, report, membersQueue, mismatches);
                        }
                    }
                }

                report.AppendLine("</ul>");
                report.AppendLine("</li>");
            }
            else
            {
                var playwrightSharpProperty = playwrightSharpType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, memberName, StringComparison.OrdinalIgnoreCase));

                if (playwrightSharpProperty == null && memberName.StartsWith("set"))
                {
                    playwrightSharpProperty = playwrightSharpType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, memberName.Substring(3, memberName.Length - 3), StringComparison.OrdinalIgnoreCase));
                }

                if (playwrightSharpProperty != null)
                {
                    membersQueue.Remove(playwrightSharpProperty);
                    report.AppendLine("<li>");
                    report.AppendLine($"{memberName}: found as as Property {playwrightSharpProperty.Name}");
                    report.AppendLine("</li>");
                }
                else
                {
                    var mismatch = mismatches.Entities.FirstOrDefault(e => e.ClassName == playwrightSharpType.Name)?
                        .Members.FirstOrDefault(m => m.UpstreamMemberName == memberName);

                    if (mismatch == null)
                    {
                        LogWarning("PW007", $"{playwrightSharpType.Name}.{memberName} not found");
                        report.AppendLine("<li style='color: red'>");
                        report.AppendLine($"{memberName} NOT FOUND (PW007)");
                        report.AppendLine("</li>");
                    }
                    else
                    {
                        report.AppendLine("<li style='color: coral'>");
                        report.AppendLine($"{memberName} NOT FOUND ==> {mismatch.Justification}</span>");
                        report.AppendLine("</li>");
                    }
                }
            }
        }

        private MethodInfo GetBestMethodOverload(Type playwrightSharpType, string methodName, string paramName)
            => playwrightSharpType.GetMethods().FirstOrDefault(m =>
                m.Name == methodName &&
                m.GetParameters().Any(p => IsParameterNameMatch(p.Name, paramName)));

        private MethodInfo GetBestMethodOverload(Type playwrightSharpType, string methodName, string paramName, PlaywrightType type)
            => playwrightSharpType.GetMethods().FirstOrDefault(m =>
                m.Name == methodName &
                m.GetParameters().Any(p => IsParameterNameMatch(p.Name, paramName) && IsSameType(p.ParameterType, type)));

        private void EvaluateArgument(
            PlaywrightMember arg,
            Type playwrightSharpType,
            MethodInfo playwrightSharpMethod,
            StringBuilder report,
            List<object> membersQueue,
            Mismatch mismatches)
        {
            var types = arg.Type.Union.Any() ? arg.Type.Union : new[] { arg.Type };

            foreach (var type in types.Where(t => t.Name != "null"))
            {
                var playwrightSharpArgument = playwrightSharpMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, arg.Name));
                var mismatch = mismatches.Entities.FirstOrDefault(e => e.ClassName == playwrightSharpType.Name)?
                    .Members.FirstOrDefault(m => m.MemberName == playwrightSharpMethod.Name)?
                    .Arguments.FirstOrDefault(m => m.UpstreamArgumentName == arg.Name);

                if (playwrightSharpArgument != null)
                {
                    if (!IsSameType(playwrightSharpArgument.ParameterType, type))
                    {
                        // Look for a matching overload.
                        var overloadMethod = GetBestMethodOverload(playwrightSharpType, playwrightSharpMethod.Name, arg.Name, type);

                        if (overloadMethod != null)
                        {
                            membersQueue.Remove(overloadMethod);
                            playwrightSharpArgument = overloadMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, arg.Name));
                            report.AppendLine("<li>");
                            report.AppendLine($"{arg.Name} ({type.Name.ToHtml()}): found as {playwrightSharpArgument.Name} ({playwrightSharpArgument.ParameterType})");
                        }
                        else
                        {
                            report.AppendLine("<li style='color: coral'>");
                            report.AppendLine($"{playwrightSharpType.Name}.{arg.Name} ({type.Name.ToHtml()}): found as {playwrightSharpArgument.Name} but with type {playwrightSharpArgument.ParameterType} (PW001)");

                            if (mismatch == null && type.Name != "Object" && type.Name != "Array")
                            {
                                LogWarning("PW001", $"{playwrightSharpType.Name}.{playwrightSharpMethod.Name} => {arg.Name} ({type.Name.ToHtml()}): found as {playwrightSharpArgument.Name} but with type {playwrightSharpArgument.ParameterType}");
                            }
                        }
                    }
                    else
                    {
                        report.AppendLine("<li>");
                        report.AppendLine($"{arg.Name} ({type.Name.ToHtml()}): found as {playwrightSharpArgument.Name} ({playwrightSharpArgument.ParameterType})");
                    }

                    if (type.Name == "Object" || type.Name == "Array")
                    {
                        report.AppendLine("<ul>");

                        if (arg.Type.Properties != null)
                        {
                            foreach (var prop in arg.Type.Properties.Where(p => !p.Langs.Only.Any()))
                            {
                                // Look for a matching overload.
                                var overrideMethod = GetBestMethodOverload(playwrightSharpType, playwrightSharpMethod.Name, arg.Name.ToLower(), type);

                                if (overrideMethod != null)
                                {
                                    playwrightSharpArgument = overrideMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, arg.Name));
                                }

                                if ((
                                        playwrightSharpArgument.ParameterType.IsInterface ||
                                        playwrightSharpArgument.ParameterType.IsClass) &&
                                    playwrightSharpArgument.ParameterType != typeof(string))
                                {
                                    EvaluateProperty(prop, GetBaseType(playwrightSharpArgument.ParameterType), report, mismatches);
                                }
                                else
                                {
                                    EvaluateArgument(prop, playwrightSharpType, playwrightSharpMethod, report, membersQueue, mismatches);
                                }
                            }
                        }

                        report.AppendLine("</ul>");
                    }

                    report.AppendLine("</li>");
                }
                else
                {
                    // Look for a matching override.
                    var overrideMethod = playwrightSharpType.GetMethods().FirstOrDefault(m =>
                        m.Name == playwrightSharpMethod.Name &&
                        m.GetParameters().Any(p => IsParameterNameMatch(p.Name, arg.Name.ToLower()) && IsSameType(p.ParameterType, type)));

                    if (overrideMethod != null)
                    {
                        membersQueue.Remove(overrideMethod);
                        playwrightSharpArgument = overrideMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, arg.Name));
                        report.AppendLine("<li>");
                        report.AppendLine($"{arg.Name} ({type.Name.ToHtml()}): found as {playwrightSharpArgument.Name} ({playwrightSharpArgument.ParameterType})");
                    }
                    else
                    {
                        if (mismatch == null)
                        {
                            LogWarning("PW002", $"{playwrightSharpType.Name}.{playwrightSharpMethod.Name} => {arg.Name} argument not found.");
                            report.AppendLine("<li style='color: red'>");
                            report.AppendLine($"{arg.Name} NOT FOUND (PW002)");
                            report.AppendLine("</li>");
                        }
                        else
                        {
                            report.AppendLine("<li style='color: coral'>");
                            report.AppendLine($"{arg.Name} NOT FOUND => {mismatch.Justification}");
                            report.AppendLine("</li>");
                        }
                    }
                }
            }
        }

        private bool IsSameType(Type parameterType, PlaywrightType playwrightParameterType)
        {
            var types = playwrightParameterType.Union.Any() ? playwrightParameterType.Union : new[] { playwrightParameterType };
            foreach (var type in types.Where(t => t.Name != "null"))
            {
                string paramName = type.Name;
                var baseParameterType = GetBaseType(parameterType);

                if (type.Templates.Any())
                {
                    paramName += "<" + string.Join(",", type.Templates.Select(t => t.Name)) + ">";
                }

                if (paramName.StartsWith("\""))
                {
                    return baseParameterType.IsEnum || (baseParameterType.GenericTypeArguments.Length > 0 && baseParameterType.GenericTypeArguments[0].IsEnum);
                }

                if (paramName.StartsWith("function", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(MulticastDelegate).IsAssignableFrom(baseParameterType);
                }

                if (paramName switch
                {
                    "string" => baseParameterType == typeof(string) || baseParameterType.IsEnum,
                    "int" => baseParameterType == typeof(int) || baseParameterType == typeof(int?),
                    "float" => baseParameterType == typeof(int) || baseParameterType == typeof(decimal) || baseParameterType == typeof(int?) || baseParameterType == typeof(decimal?),
                    "Array<string>" => (parameterType.IsArray || parameterType == typeof(IEnumerable)) && (baseParameterType == typeof(string) || baseParameterType.IsEnum),
                    "boolean" => baseParameterType == typeof(bool) || baseParameterType == typeof(bool?),
                    "Object" =>
                        baseParameterType == typeof(Dictionary<string, string>) ||
                        baseParameterType == typeof(Dictionary<string, object>) ||
                        baseParameterType.IsInterface ||
                        baseParameterType.IsClass,
                    "Object<string,union>" => baseParameterType == typeof(Dictionary<string, string>) || baseParameterType == typeof(Dictionary<string, object>),
                    "Object<string,string>" => baseParameterType == typeof(Dictionary<string, string>),
                    "Dictionary" => baseParameterType == typeof(Dictionary<string, string>) || baseParameterType == typeof(Dictionary<string, object>),
                    "RegExp" => baseParameterType == typeof(Regex),
                    "EvaluationArgument" => baseParameterType == typeof(object),
                    "ElementHandle" => baseParameterType.Name == "IElementHandle",
                    "Buffer" => baseParameterType == typeof(string) || baseParameterType == typeof(byte[]),
                    "path" => baseParameterType == typeof(string),
                    "Serializable" => true,
                    _ => baseParameterType.Name == "I" + paramName || baseParameterType.Name == paramName || baseParameterType.GetInterfaces().FirstOrDefault()?.Name == "I" + paramName,
                })
                {
                    return true;
                }
            }

            return false;
        }

        private void EvaluateProperty(
            PlaywrightMember property,
            Type playwrightSharpType,
            StringBuilder report,
            Mismatch mismatches)
        {
            var playwrightSharpProperty = playwrightSharpType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));
            var mismatch = mismatches.Entities.FirstOrDefault(e => e.ClassName == playwrightSharpType.Name)?
                .Members.FirstOrDefault(m => m.UpstreamMemberName == property.Name);

            if (playwrightSharpProperty != null)
            {
                if (!IsSameType(playwrightSharpProperty.PropertyType, property.Type) && mismatch == null)
                {
                    report.AppendLine("<li style='color: coral'>");

                    LogWarning("PW003", $"{playwrightSharpType.Name}.{property.Name} ({property.Type.Name.ToHtml()}): found as as Property {playwrightSharpProperty.Name} with type ({playwrightSharpProperty.PropertyType})");
                }
                else
                {
                    report.AppendLine("<li>");
                }

                report.AppendLine($"{playwrightSharpType.Name}.{property.Name} ({property.Type.Name.ToHtml()}): found as as Property {playwrightSharpProperty.Name} with type ({playwrightSharpProperty.PropertyType})");
                report.AppendLine("</li>");
            }
            else
            {
                if (mismatch == null)
                {
                    LogWarning("PW004", $"{playwrightSharpType.Name}.{property.Name} not found");
                    report.AppendLine("<li style='color: red'>");
                    report.AppendLine($"{property.Name} NOT FOUND (PW004)");
                    report.AppendLine("</li>");
                }
                else
                {
                    report.AppendLine("<li style='color: coral'>");
                    report.AppendLine($"{property.Name} NOT FOUND ==> {mismatch.Justification}");
                    report.AppendLine("</li>");
                }
            }
        }

        private void EvaluateEvent(
            PlaywrightMember e,
            Type playwrightSharpType,
            StringBuilder report,
            List<object> membersQueue,
            Mismatch mismatches)
        {
            var playwrightSharpEvent = playwrightSharpType.GetEvents().FirstOrDefault(e => string.Equals(e.Name, e.Name, StringComparison.OrdinalIgnoreCase));

            if (playwrightSharpEvent != null)
            {
                membersQueue.Remove(playwrightSharpEvent);

                report.AppendLine("<li>");
                report.AppendLine($"{e.Name}: found as {playwrightSharpEvent.Name}");
                report.AppendLine("</li>");
            }
            else
            {
                var mismatch = mismatches.Entities.FirstOrDefault(e => e.ClassName == playwrightSharpType.Name)?
                    .Members.FirstOrDefault(m => m.UpstreamMemberName == e.Name);

                if (mismatch == null)
                {
                    LogWarning("PW005", $"{playwrightSharpType.Name}.{e.Name} not found");
                    report.AppendLine("<li style='color: red'>");
                    report.AppendLine($"{e.Name} NOT FOUND (PW005)");
                    report.AppendLine("</li>");
                }
                else
                {
                    report.AppendLine($"<li style='color: coral'>{playwrightSharpType.Name}.{e.Name} NOT FOUND ==> {mismatch.Justification}</li>");
                }
            }
        }
    }
}
