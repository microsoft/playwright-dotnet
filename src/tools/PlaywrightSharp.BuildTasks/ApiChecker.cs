using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PlaywrightSharp.BuildTasks.Extensions;
using PlaywrightSharp.BuildTasks.Models.Api;
using PlaywrightSharp.BuildTasks.Models.Mismatch;

namespace PlaywrightSharp.BuildTasks
{
    public class ApiChecker : Microsoft.Build.Utilities.Task
    {
        public string BasePath { get; set; }

        public string AssemblyPath { get; set; }

        public bool IsBuildTask { get; set; } = true;

        public override bool Execute()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string assemblySearchPath = Path.Combine(new FileInfo(typeof(ApiChecker).Assembly.Location).Directory.FullName, e.Name.Split(',')[0] + ".dll");
                if (File.Exists(assemblySearchPath))
                {
                    return Assembly.LoadFrom(assemblySearchPath);
                }

                return null;
            };

            var assembly = Assembly.LoadFrom(Path.Combine(AssemblyPath, "PlaywrightSharp.dll"));

            var report = new StringBuilder("<html><body><ul>");
            string json = File.ReadAllText(Path.Combine(BasePath, "src", "PlaywrightSharp", "runtimes", "api.json"));
            var api = JsonSerializer.Deserialize<Dictionary<string, PlaywrightEntity>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            string mismatchJsonFile = Path.Combine(BasePath, "src", "PlaywrightSharp", "runtimes", "expected_api_mismatch.json");
            string mismatchJson = File.ReadAllText(mismatchJsonFile);
            Mismatch mismatches;

            try
            {
                mismatches = JsonSerializer.Deserialize<Mismatch>(mismatchJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to parse file {mismatchJsonFile} with content {mismatchJson}", ex);
            }

            foreach (var kv in api)
            {
                EvaluateEntity(assembly, kv.Key, kv.Value, report, mismatches);
            }

            report.Append("</ul></body></html>");
            File.WriteAllText(
                Path.Combine(BasePath, "src", "PlaywrightSharp", "runtimes", "report.html"),
                report.ToString());

            return true;
        }

        private void EvaluateEntity(Assembly assembly, string name, PlaywrightEntity entity, StringBuilder report, Mismatch mismatches)
        {
            var playwrightSharpType = assembly.GetType($"PlaywrightSharp.I{name}");

            if (playwrightSharpType == null)
            {
                playwrightSharpType = assembly.GetType($"PlaywrightSharp.{name}");
            }

            if (playwrightSharpType == null)
            {
                playwrightSharpType = assembly.GetType($"PlaywrightSharp.Chromium.{name}");
            }

            if (playwrightSharpType == null)
            {
                playwrightSharpType = assembly.GetType($"PlaywrightSharp.{name}EventArgs");
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

                foreach (var kv in entity.Methods)
                {
                    EvaluateMethod(kv.Key, kv.Value, playwrightSharpType, report, membersQueue, mismatches);
                }

                foreach (var kv in entity.Events)
                {
                    EvaluateEvent(kv.Key, playwrightSharpType, report, membersQueue, mismatches);
                }

                foreach (object memberInPLaywrightSharp in membersQueue)
                {
                    report.AppendLine("<li style='color: blue'>");
                    report.AppendLine($"{memberInPLaywrightSharp} FOUND IN PLAYWRIGHT SHARP");
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

        private void LogWarning(string warningCode, string message)
        {
            if (IsBuildTask)
            {
                Log.LogWarning("ApiChecker", warningCode, null, null, 0, 0, 0, 0, message);
            }
            else
            {
                Console.WriteLine($"{warningCode}: {message}");
            }
        }

        private void EvaluateMethod(
            string memberName,
            PlaywrightMember member,
            Type playwrightSharpType,
            StringBuilder report,
            List<object> membersQueue,
            Mismatch mismatches
            )
        {
            memberName = TranslateMethodName(memberName);
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
                playwrightSharpMethod = typeToCheck.GetMethods().FirstOrDefault(m => m.Name.ToLower() == memberName.ToLower());

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
                    foreach (var kv in member.Args)
                    {
                        var matchingMethod = GetBestMethodOverload(playwrightSharpType, playwrightSharpMethod.Name, kv.Key);

                        // we flatten options
                        if (kv.Value.Type.Properties?.Any() == true && (kv.Key == "options" || matchingMethod == null))
                        {
                            foreach (var arg in kv.Value.Type.Properties)
                            {
                                EvaluateArgument(arg.Key, arg.Value, typeToCheck, playwrightSharpMethod, report, membersQueue, mismatches);
                            }
                        }
                        else
                        {
                            EvaluateArgument(kv.Key, kv.Value, typeToCheck, matchingMethod ?? playwrightSharpMethod, report, membersQueue, mismatches);
                        }
                    }
                }

                report.AppendLine("</ul>");
                report.AppendLine("</li>");

            }
            else
            {
                var playwrightSharpProperty = playwrightSharpType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == memberName.ToLower());

                if (playwrightSharpProperty == null && memberName.StartsWith("set"))
                {
                    playwrightSharpProperty = playwrightSharpType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == memberName.Substring(3, memberName.Length - 3).ToLower());
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

        private MethodInfo GetBestMethodOverload(Type playwrightSharpType, string methodName, string paramName, string type)
            => playwrightSharpType.GetMethods().FirstOrDefault(m =>
                m.Name == methodName &
                m.GetParameters().Any(p => IsParameterNameMatch(p.Name, paramName) && IsSameType(p.ParameterType, type)));

        private static string TranslateMethodName(string memberName)
            => memberName
                .Replace("$$eval", "evalOnSelectorAll")
                .Replace("$eval", "evalOnSelector")
                .Replace("$$", "querySelectorAll")
                .Replace("$", "querySelector");

        private void EvaluateArgument(
            string name,
            PlaywrightArgument arg,
            Type playwrightSharpType,
            MethodInfo playwrightSharpMethod,
            StringBuilder report,
            List<object> membersQueue,
            Mismatch mismatches)
        {
            foreach (string type in CurateType(arg.Type.Name).Split('|').Where(t => t != "null"))
            {
                var playwrightSharpArgument = playwrightSharpMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, name));
                var mismatch = mismatches.Entities.FirstOrDefault(e => e.ClassName == playwrightSharpType.Name)?
                    .Members.FirstOrDefault(m => m.MemberName == playwrightSharpMethod.Name)?
                    .Arguments.FirstOrDefault(m => m.UpstreamArgumentName == name);

                if (playwrightSharpArgument != null)
                {
                    if (!IsSameType(playwrightSharpArgument.ParameterType, type))
                    {
                        //Look for a matching overload
                        var overloadMethod = GetBestMethodOverload(playwrightSharpType, playwrightSharpMethod.Name, name, type);

                        if (overloadMethod != null)
                        {
                            membersQueue.Remove(overloadMethod);
                            playwrightSharpArgument = overloadMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, name));
                            report.AppendLine("<li>");
                            report.AppendLine($"{name} ({type.ToHtml()}): found as {playwrightSharpArgument.Name} ({playwrightSharpArgument.ParameterType})");
                        }
                        else
                        {
                            report.AppendLine("<li style='color: coral'>");
                            report.AppendLine($"{playwrightSharpType.Name}.{name} ({type.ToHtml()}): found as {playwrightSharpArgument.Name} but with type {playwrightSharpArgument.ParameterType} (PW001)");

                            if (mismatch == null && !type.Contains("Object"))
                            {
                                LogWarning("PW001", $"{playwrightSharpType.Name}.{playwrightSharpMethod.Name} => {name} ({type.ToHtml()}): found as {playwrightSharpArgument.Name} but with type {playwrightSharpArgument.ParameterType}");
                            }
                        }
                    }
                    else
                    {
                        report.AppendLine("<li>");
                        report.AppendLine($"{name} ({type.ToHtml()}): found as {playwrightSharpArgument.Name} ({playwrightSharpArgument.ParameterType})");
                    }

                    if (type.Contains("Object"))
                    {
                        report.AppendLine("<ul>");

                        if (arg.Type.Properties != null)
                        {
                            foreach (var kv in arg.Type.Properties)
                            {
                                //Look for a matching overload
                                var overrideMethod = GetBestMethodOverload(playwrightSharpType, playwrightSharpMethod.Name, name.ToLower(), type);

                                if (overrideMethod != null)
                                {
                                    playwrightSharpArgument = overrideMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, name));
                                }

                                if ((
                                        playwrightSharpArgument.ParameterType.IsInterface ||
                                        playwrightSharpArgument.ParameterType.IsClass
                                    ) &&
                                    playwrightSharpArgument.ParameterType != typeof(string))
                                {
                                    EvaluateProperty(kv.Key, kv.Value, GetBaseType(playwrightSharpArgument.ParameterType), report, mismatches);
                                }
                                else
                                {
                                    EvaluateArgument(kv.Key, kv.Value, playwrightSharpType, playwrightSharpMethod, report, membersQueue, mismatches);
                                }
                            }
                        }

                        report.AppendLine("</ul>");
                    }

                    report.AppendLine("</li>");
                }
                else
                {
                    //Look for a matching override
                    var overrideMethod = playwrightSharpType.GetMethods().FirstOrDefault(m =>
                        m.Name == playwrightSharpMethod.Name &&
                        m.GetParameters().Any(p => IsParameterNameMatch(p.Name, name.ToLower()) && IsSameType(p.ParameterType, type)));

                    if (overrideMethod != null)
                    {
                        membersQueue.Remove(overrideMethod);
                        playwrightSharpArgument = overrideMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, name));
                        report.AppendLine("<li>");
                        report.AppendLine($"{name} ({type.ToHtml()}): found as {playwrightSharpArgument.Name} ({playwrightSharpArgument.ParameterType})");
                    }
                    else
                    {
                        if (mismatch == null)
                        {
                            LogWarning("PW002", $"{playwrightSharpType.Name}.{playwrightSharpMethod.Name} => {name} argument not found.");
                            report.AppendLine("<li style='color: red'>");
                            report.AppendLine($"{name} NOT FOUND (PW002)");
                            report.AppendLine("</li>");
                        }
                        else
                        {
                            report.AppendLine("<li style='color: coral'>");
                            report.AppendLine($"{name} NOT FOUND => {mismatch.Justification}");
                            report.AppendLine("</li>");
                        }
                    }
                }
            }
        }

        private static bool IsParameterNameMatch(string argumentName, string playwrightName)
            => argumentName.ToLower() == playwrightName.ToLower() ||
                (playwrightName == "urlOrPredicate" && (argumentName == "url" || argumentName == "predicate"));


        private static Type GetBaseType(Type parameterType)
        {
            if (parameterType.IsArray)
            {
                return parameterType.GetElementType();
            }

            if ((typeof(IEnumerable).IsAssignableFrom(parameterType) && parameterType.GenericTypeArguments.Length == 1))
            {
                return parameterType.GenericTypeArguments[0];
            }

            return Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        }

        private string CurateType(string type)
            => type switch
            {
                "Object<string, string|number|boolean>" => "Dictionary",
                _ => type,
            };

        private static bool IsSameType(Type parameterType, string playwrightParameterType)
        {
            foreach (string item in playwrightParameterType.Split('|').Where(t => t != "null"))
            {
                string playwrightType = item;
                if (playwrightType.Contains("Array<"))
                {
                    playwrightType = playwrightType.Replace("Array<", "").Replace(">", "");
                }
                parameterType = GetBaseType(parameterType);

                if (playwrightType.StartsWith("\""))
                {
                    return parameterType.IsEnum || (parameterType.GenericTypeArguments.Length > 0 && parameterType.GenericTypeArguments[0].IsEnum);
                }

                if (playwrightType.ToLower().StartsWith("function"))
                {
                    return typeof(MulticastDelegate).IsAssignableFrom(parameterType);
                }

                if (playwrightType switch
                {
                    "string" => parameterType == typeof(string) || parameterType.IsEnum,
                    "number" => parameterType == typeof(int) || parameterType == typeof(decimal) || parameterType == typeof(int?) || parameterType == typeof(decimal?),
                    "Array<string>" => parameterType == typeof(Array) && parameterType.GenericTypeArguments[0] == typeof(string),
                    "boolean" => parameterType == typeof(bool) || parameterType == typeof(bool?),
                    "Object<string, string" => parameterType == typeof(Dictionary<string, string>) || parameterType == typeof(Dictionary<string, object>),
                    "Dictionary" => parameterType == typeof(Dictionary<string, string>) || parameterType == typeof(Dictionary<string, object>),
                    "Object<string, string>" => parameterType == typeof(Dictionary<string, string>),
                    "RegExp" => parameterType == typeof(Regex),
                    "EvaluationArgument" => parameterType == typeof(object),
                    "ElementHandle" => parameterType.Name == "IElementHandle",
                    "Page" => parameterType.Name == "IPage",
                    "Buffer" => parameterType == typeof(string) || parameterType == typeof(byte[]),
                    "Object" => parameterType != typeof(string),
                    "Serializable" => true,
                    _ => false,
                })
                {
                    return true;
                }
            }

            return false;
        }

        private void EvaluateProperty(
            string memberName,
            PlaywrightArgument arg,
            Type playwrightSharpType,
            StringBuilder report,
            Mismatch mismatches)
        {
            var playwrightSharpProperty = playwrightSharpType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == memberName.ToLower());
            var mismatch = mismatches.Entities.FirstOrDefault(e => e.ClassName == playwrightSharpType.Name)?
                .Members.FirstOrDefault(m => m.UpstreamMemberName == memberName);

            if (playwrightSharpProperty != null)
            {
                if (!IsSameType(playwrightSharpProperty.PropertyType, arg.Type.Name) && mismatch == null)
                {
                    report.AppendLine("<li style='color: coral'>");

                    LogWarning("PW003", $"{playwrightSharpType.Name}.{memberName} ({arg.Type.Name.ToHtml()}): found as as Property {playwrightSharpProperty.Name} with type ({playwrightSharpProperty.PropertyType})");
                }
                else
                {
                    report.AppendLine("<li>");
                }

                report.AppendLine($"{playwrightSharpType.Name}.{memberName} ({arg.Type.Name.ToHtml()}): found as as Property {playwrightSharpProperty.Name} with type ({playwrightSharpProperty.PropertyType})");
                report.AppendLine("</li>");
            }
            else
            {
                if (mismatch == null)
                {
                    LogWarning("PW004", $"{playwrightSharpType.Name}.{memberName} not found");
                    report.AppendLine("<li style='color: red'>");
                    report.AppendLine($"{memberName} NOT FOUND (PW004)");
                    report.AppendLine("</li>");
                }
                else
                {
                    report.AppendLine("<li style='color: coral'>");
                    report.AppendLine($"{memberName} NOT FOUND ==> {mismatch.Justification}");
                    report.AppendLine("</li>");
                }
            }
        }

        private void EvaluateEvent(
            string memberName,
            Type playwrightSharpType,
            StringBuilder report,
            List<object> membersQueue,
            Mismatch mismatches)
        {
            var playwrightSharpEvent = playwrightSharpType.GetEvents().FirstOrDefault(e => e.Name.ToLower() == memberName.ToLower());

            if (playwrightSharpEvent != null)
            {
                membersQueue.Remove(playwrightSharpEvent);

                report.AppendLine("<li>");
                report.AppendLine($"{memberName}: found as {playwrightSharpEvent.Name}");
                report.AppendLine("</li>");
            }
            else
            {

                var mismatch = mismatches.Entities.FirstOrDefault(e => e.ClassName == playwrightSharpType.Name)?
                    .Members.FirstOrDefault(m => m.UpstreamMemberName == memberName);

                if (mismatch == null)
                {
                    LogWarning("PW005", $"{playwrightSharpType.Name}.{memberName} not found");
                    report.AppendLine("<li style='color: red'>");
                    report.AppendLine($"{memberName} NOT FOUND (PW005)");
                    report.AppendLine("</li>");
                }
                else
                {
                    report.AppendLine($"<li style='color: coral'>{playwrightSharpType.Name}.{memberName} NOT FOUND ==> {mismatch.Justification}</li>");
                }
            }
        }
    }
}
