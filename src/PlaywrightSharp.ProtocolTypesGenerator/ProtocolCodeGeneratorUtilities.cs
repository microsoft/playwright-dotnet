using System.Globalization;
using System.Text;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal static class ProtocolCodeGeneratorUtilities
    {
        public static void AppendAutoGeneratedPrefix(this StringBuilder builder)
            => builder.Append(@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

");

        public static void AppendAutoGeneratedSuffix(this StringBuilder builder)
            => builder.AppendLine("#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member");

        public static string ToEnumField(this string value) => $"[System.Runtime.Serialization.EnumMember(Value = \"{value}\")]{value.ToPascalCase()}";

        public static string ToPascalCase(this string value)
        {
            var builder = new StringBuilder();
            bool shouldUppercase = true;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetter(value[i]))
                {
                    if (char.IsUpper(value[i]))
                    {
                        shouldUppercase = false;
                        builder.Append(char.ToUpper(value[i], CultureInfo.InvariantCulture));
                    }
                    else if (shouldUppercase && char.IsLower(value[i]))
                    {
                        shouldUppercase = false;
                        builder.Append(char.ToUpper(value[i], CultureInfo.InvariantCulture));
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
        }
    }
}
