using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PlaywrightSharp;

namespace ApiChecker
{
    public static class Program
    {
        private static readonly Dictionary<(string className, string memberName), string> _memberAnnotations = new Dictionary<(string className, string memberName), string>
        {
            [("IBrowserContext", "addInitScript")] = "C# signature: AddInitScriptAsync(string script = null, object[] arg = null, string path = null, string content = null)",
            [("IBrowserContext", "waitForEvent")] = "event is a reserved word. optionsOrPredicate was flatten: Task<T> WaitForEvent<T>(PlaywrightEvent<T> e, Func<T, bool> predicate = null, int? timeout = null)",
            [("IPage", "addInitScript")] = "C# signature: AddInitScriptAsync(string script = null, object[] arg = null, string path = null, string content = null)",
            [("IPage", "waitForEvent")] = "event is a reserved word. optionsOrPredicate was flatten: Task<T> WaitForEvent<T>(PlaywrightEvent<T> e, Func<T, bool> predicate = null, int? timeout = null)",
            [("IElementHandle", "asElement")] = "Implicit from C# type casting",
            [("IJSHandle", "asElement")] = "Implicit from C# type casting",
            [("Selectors", "register")] = "C# signature: RegisterAsync(string name, string script = null, string path = null, string content = null, bool? contentScript = null)",
            [("IBrowserType", "launch")] = "The ignoreDefaultArgs list is ignoredDefaultArgs | firefoxUserPrefs and env are only exposed as a Dictionary<string, object>",
            [("IBrowserContext", "exposeBinding")] = "handle is inferred from the palywrightBinding. If it's a function with only one argument and it's IJSHandle we will send handle true",
            [("IPage", "exposeBinding")] = "handle is inferred from the palywrightBinding. If it's a function with only one argument and it's IJSHandle we will send handle true",
        };

        static void Main(string[] args)
        {
            var report = new StringBuilder("<html><body><ul>");
            string json = File.ReadAllText(Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "api.json"));
            var api = JsonSerializer.Deserialize<Dictionary<string, PlaywrightEntity>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            foreach (var kv in api)
            {
                EvaluateEntity(kv.Key, kv.Value, report);
            }

            report.Append("</ul></body></html>");
            File.WriteAllText(
                Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "report.html"),
                report.ToString());
        }

        private static void EvaluateEntity(string name, PlaywrightEntity entity, StringBuilder report)
        {
            var assembly = Assembly.GetAssembly(typeof(IBrowserType));
            var playwrightSharpEntity = assembly.GetType($"PlaywrightSharp.I{name}");

            if (playwrightSharpEntity == null)
            {
                playwrightSharpEntity = assembly.GetType($"PlaywrightSharp.{name}");
            }

            if (playwrightSharpEntity == null)
            {
                playwrightSharpEntity = assembly.GetType($"PlaywrightSharp.Chromium.{name}");
            }

            if (playwrightSharpEntity == null)
            {
                playwrightSharpEntity = assembly.GetType($"PlaywrightSharp.{name}EventArgs");
            }

            if (playwrightSharpEntity != null)
            {
                var membersQueue = new List<object>();
                membersQueue.AddRange(playwrightSharpEntity.GetProperties());
                membersQueue.AddRange(playwrightSharpEntity.GetEvents());
                membersQueue.AddRange(playwrightSharpEntity.GetMethods().Where(m =>
                    !m.IsSpecialName &&
                    !new[] { "GetType", "ToString", "Equals", "GetHashCode" }.Contains(m.Name)));

                report.AppendLine("<li>");
                report.AppendLine($"{name}: found as {playwrightSharpEntity.Name}");

                report.AppendLine("<ul>");

                foreach (var kv in entity.Members)
                {
                    EvaluateMember(kv.Key, kv.Value, playwrightSharpEntity, report, membersQueue);
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
                report.AppendLine("<li style='color: red'>");
                report.AppendLine($"{name} NOT FOUND");
                report.AppendLine("</li>");
            }
        }

        private static void EvaluateMember(
            string memberName,
            PlaywrightMember member,
            Type playwrightSharpEntity,
            StringBuilder report,
            List<object> membersQueue)
        {
            switch (member.Kind)
            {
                case "event":
                    EvaluateEvent(memberName, playwrightSharpEntity, report, membersQueue);
                    return;
                case "method":
                    EvaluateMethod(memberName, member, playwrightSharpEntity, report, membersQueue);
                    return;
            }
        }

        private static void EvaluateMethod(
            string memberName,
            PlaywrightMember member,
            Type playwrightSharpEntity,
            StringBuilder report,
            List<object> membersQueue)
        {
            memberName = TranslateMethodName(memberName);
            var typeToCheck = playwrightSharpEntity;
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

                if (_memberAnnotations.ContainsKey((playwrightSharpEntity.Name, memberName)))
                {
                    report.AppendLine($"<span style='color: coral'> ==> {_memberAnnotations[(playwrightSharpEntity.Name, memberName)]}</span>");
                }

                report.AppendLine("<ul>");

                if (member.Args != null)
                {
                    foreach (var kv in member.Args)
                    {
                        // we flatten options
                        if (kv.Key == "options")
                        {
                            foreach (var arg in kv.Value.Type.Properties)
                            {
                                EvaluateArgument(arg.Key, arg.Value, typeToCheck, playwrightSharpMethod, report, membersQueue);
                            }
                        }
                        else
                        {
                            EvaluateArgument(kv.Key, kv.Value, typeToCheck, playwrightSharpMethod, report, membersQueue);
                        }
                    }
                }

                report.AppendLine("</ul>");
                report.AppendLine("</li>");

            }
            else
            {
                var playwrightSharpProperty = playwrightSharpEntity.GetProperties().FirstOrDefault(p => p.Name.ToLower() == memberName.ToLower());

                if (playwrightSharpProperty == null && memberName.StartsWith("set"))
                {
                    playwrightSharpProperty = playwrightSharpEntity.GetProperties().FirstOrDefault(p => p.Name.ToLower() == memberName.Substring(3, memberName.Length - 3).ToLower());
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
                    report.AppendLine("<li style='color: red'>");
                    report.AppendLine($"{memberName} NOT FOUND");

                    if (_memberAnnotations.ContainsKey((playwrightSharpEntity.Name, memberName)))
                    {
                        report.AppendLine($"<span style='color: coral'> ==> {_memberAnnotations[(playwrightSharpEntity.Name, memberName)]}</span>");
                    }

                    report.AppendLine("</li>");
                }
            }
        }

        private static string TranslateMethodName(string memberName)
        {
            return memberName
                .Replace("$$eval", "evalOnSelectorAll")
                .Replace("$eval", "evalOnSelector")
                .Replace("$$", "querySelectorAll")
                .Replace("$", "querySelector");
        }

        private static void EvaluateArgument(
            string name,
            PlaywrightArgument arg,
            Type playwrightSharpEntity,
            MethodInfo playwrightSharpMethod,
            StringBuilder report,
            List<object> membersQueue)
        {
            foreach (string type in arg.Type.Name.Split("|").Where(t => t != "null"))
            {
                var playwrightSharpArgument = playwrightSharpMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, name));

                if (playwrightSharpArgument != null)
                {
                    if (!IsSameType(playwrightSharpArgument.ParameterType, type))
                    {
                        //Look for a matching override
                        var overrideMethod = playwrightSharpEntity.GetMethods().FirstOrDefault(m =>
                            m.Name == playwrightSharpMethod.Name &&
                            m.GetParameters().Any(p => IsParameterNameMatch(p.Name, name) && IsSameType(p.ParameterType, type)));

                        if (overrideMethod != null)
                        {
                            membersQueue.Remove(overrideMethod);
                            playwrightSharpArgument = overrideMethod.GetParameters().FirstOrDefault(p => IsParameterNameMatch(p.Name, name));
                            report.AppendLine("<li>");
                            report.AppendLine($"{name} ({type.ToHtml()}): found as {playwrightSharpArgument.Name} ({playwrightSharpArgument.ParameterType})");
                        }
                        else
                        {
                            report.AppendLine("<li style='color: coral'>");
                            report.AppendLine($"{name} ({type.ToHtml()}): found as {playwrightSharpArgument.Name} but with type {playwrightSharpArgument.ParameterType}");
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
                                EvaluateProperty(kv.Key, kv.Value, GetBaseType(playwrightSharpArgument.ParameterType), report);
                            }
                        }

                        report.AppendLine("</ul>");
                    }

                    report.AppendLine("</li>");
                }
                else
                {
                    //Look for a matching override
                    var overrideMethod = playwrightSharpEntity.GetMethods().FirstOrDefault(m =>
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
                        report.AppendLine("<li style='color: red'>");
                        report.AppendLine($"{name} NOT FOUND");
                        report.AppendLine("</li>");
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

        private static bool IsSameType(Type parameterType, string playwrightType)
        {
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

            return playwrightType switch
            {
                "string" => parameterType == typeof(string) || parameterType.IsEnum,
                "number" => parameterType == typeof(int) || parameterType == typeof(decimal) || parameterType == typeof(int?) || parameterType == typeof(decimal?),
                "Array<string>" => parameterType == typeof(Array) && parameterType.GenericTypeArguments[0] == typeof(string),
                "boolean" => parameterType == typeof(bool) || parameterType == typeof(Nullable<bool>),
                "Object<string, string" => parameterType == typeof(Dictionary<string, string>) || parameterType == typeof(Dictionary<string, object>),
                "Object<string, string>" => parameterType == typeof(Dictionary<string, string>),
                "RegExp" => parameterType == typeof(Regex),
                "EvaluationArgument" => parameterType == typeof(object),
                "ElementHandle" => parameterType == typeof(IElementHandle),
                "Page" => parameterType == typeof(IPage),
                "Buffer" => parameterType == typeof(string) || parameterType == typeof(byte[]),
                "Object" => parameterType != typeof(string),
                "Serializable" => true,
                _ => false,
            };
        }

        private static void EvaluateProperty(string memberName, PlaywrightArgument arg, Type playwrightSharpType, StringBuilder report)
        {
            var playwrightSharpProperty = playwrightSharpType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == memberName.ToLower());

            if (playwrightSharpProperty != null)
            {
                if (!IsSameType(playwrightSharpProperty.PropertyType, arg.Type.Name))
                {
                    report.AppendLine("<li style='color: coral'>");
                }
                else
                {
                    report.AppendLine("<li>");
                }

                report.AppendLine($"{memberName} ({arg.Type.Name.ToHtml()}): found as as Property {playwrightSharpProperty.Name} with type ({playwrightSharpProperty.PropertyType})");
                report.AppendLine("</li>");
            }
            else
            {
                report.AppendLine("<li style='color: red'>");
                report.AppendLine($"{memberName} NOT FOUND");
                report.AppendLine("</li>");
            }
        }

        private static void EvaluateEvent(
            string memberName,
            Type playwrightSharpEntity,
            StringBuilder report,
            List<object> membersQueue)
        {
            var playwrightSharpEvent = playwrightSharpEntity.GetEvents().FirstOrDefault(e => e.Name.ToLower() == memberName.ToLower());

            if (playwrightSharpEvent != null)
            {
                membersQueue.Remove(playwrightSharpEvent);

                report.AppendLine("<li>");
                report.AppendLine($"{memberName}: found as {playwrightSharpEvent.Name}");
                report.AppendLine("</li>");
            }
            else
            {
                report.AppendLine("<li style='color: red'>");
                report.AppendLine($"{memberName} NOT FOUND");
                report.AppendLine("</li>");
            }
        }

        public static string ToHtml(this string value) => WebUtility.HtmlEncode(value);
    }
}
