using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using PlaywrightSharp;

namespace ApiChecker
{
    class Program
    {
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
                playwrightSharpEntity = assembly.GetType($"PlaywrightSharp.{name}EventArgs");
            }

            if (playwrightSharpEntity != null)
            {
                report.AppendLine("<li>");
                report.AppendLine($"{name}: found as {playwrightSharpEntity.Name}");

                report.AppendLine("<ul>");

                foreach (var kv in entity.Members)
                {
                    EvaluateMember(kv.Key, kv.Value, entity, playwrightSharpEntity, report);
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

        private static void EvaluateMember(string memberName, PlaywrightMember member, PlaywrightEntity entity, Type playwrightSharpEntity, StringBuilder report)
        {
            switch (member.Kind)
            {
                case "event":
                    EvaluateEvent(memberName, member, entity, playwrightSharpEntity, report);
                    return;
                case "method":
                    EvaluateMethod(memberName, member, playwrightSharpEntity, report);
                    return;
            }
        }

        private static void EvaluateMethod(string memberName, PlaywrightMember member, Type playwrightSharpEntity, StringBuilder report)
        {
            memberName = TranslateMethodName(memberName);
            var typeToCheck = playwrightSharpEntity;
            MethodInfo playwrightSharpMethod = null;

            while (typeToCheck != null && playwrightSharpMethod == null)
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

                typeToCheck = typeToCheck.GetTypeInfo().ImplementedInterfaces.FirstOrDefault();
            }

            if (playwrightSharpMethod != null)
            {
                report.AppendLine("<li>");
                report.AppendLine($"{memberName}: found as {playwrightSharpMethod.Name}");

                report.AppendLine("<ul>");

                if (member.Args != null)
                {
                    int position = 0;

                    foreach (var kv in member.Args)
                    {
                        // we flatten options
                        if (kv.Key == "options")
                        {

                            foreach (var arg in kv.Value.Type.Properties)
                            {
                                EvaluateArgument(arg.Key, arg.Value, playwrightSharpMethod, report, position);
                                position++;
                            }
                        }
                        else
                        {
                            EvaluateArgument(kv.Key, kv.Value, playwrightSharpMethod, report, position);
                            position++;
                        }
                    }
                }

                report.AppendLine("</ul>");
                report.AppendLine("</li>");

            }
            else
            {
                var playwrightSharpProperty = playwrightSharpEntity.GetProperties().FirstOrDefault(p => p.Name.ToLower() == memberName.ToLower());

                if (playwrightSharpProperty != null)
                {
                    report.AppendLine("<li>");
                    report.AppendLine($"{memberName}: found as as Property {playwrightSharpProperty.Name}");
                    report.AppendLine("</li>");
                }
                else
                {
                    report.AppendLine("<li style='color: red'>");
                    report.AppendLine($"{memberName} NOT FOUND");
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

        private static void EvaluateArgument(string name, PlaywrightArgument arg, MethodInfo playwrightSharpMethod, StringBuilder report, int position)
        {
            var playwrightSharpArgument = playwrightSharpMethod.GetParameters().FirstOrDefault(p => p.Name.ToLower() == name.ToLower());

            if (playwrightSharpArgument != null)
            {
                if (!IsSameType(playwrightSharpArgument.ParameterType, arg.Type.Name))
                {
                    report.AppendLine("<li style='color: coral'>");
                    report.AppendLine($"{name} ({arg.Type.Name.Replace("<", "{").Replace(">", "}")}): found as {playwrightSharpArgument.Name} but with type {playwrightSharpArgument.ParameterType}");
                }
                else
                {
                    report.AppendLine("<li>");
                    report.AppendLine($"{name} ({arg.Type.Name}): found as {playwrightSharpArgument.Name} ({playwrightSharpArgument.ParameterType})");
                }

                if (arg.Type.Name.Contains("Object"))
                {
                    report.AppendLine("<ul>");

                    foreach (var kv in arg.Type.Properties)
                    {
                        EvaluateProperty(kv.Key, kv.Value, playwrightSharpArgument.ParameterType, report);
                    }

                    report.AppendLine("</ul>");
                }

                report.AppendLine("</li>");
            }
        }

        private static bool IsSameType(Type parameterType, string plywrightType)
        {
            if (plywrightType.Contains("Array<"))
            {
                plywrightType = plywrightType.Replace("Array<", "").Replace(">", "");
                return parameterType.Name.ToLower() == plywrightType;
            }

            return plywrightType.Replace("null|", string.Empty) switch
            {
                "string" => parameterType == typeof(string),
                "number" => parameterType == typeof(int) || parameterType == typeof(decimal) || parameterType == typeof(int?) || parameterType == typeof(decimal?),
                "Array<string>" => parameterType == typeof(Array) && parameterType.GenericTypeArguments[0] == typeof(string),
                "boolean" => parameterType == typeof(bool) || parameterType == typeof(Nullable<bool>),
                "Object<string,string>" => parameterType == typeof(Dictionary<string, string>),
                "Object" => true,
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

                report.AppendLine($"{memberName} ({arg.Type.Name}): found as as Property {playwrightSharpProperty.Name} with type ({playwrightSharpProperty.PropertyType})");
                report.AppendLine("</li>");
            }
        }

        private static void EvaluateEvent(string memberName, PlaywrightMember member, PlaywrightEntity entity, Type playwrightSharpEntity, StringBuilder report)
        {
            var playwrightSharpEvent = playwrightSharpEntity.GetEvents().FirstOrDefault(e => e.Name.ToLower() == memberName.ToLower());

            if (playwrightSharpEvent != null)
            {
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
    }
}
