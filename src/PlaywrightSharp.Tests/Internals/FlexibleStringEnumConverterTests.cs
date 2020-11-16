using System;
using System.Runtime.Serialization;
using System.Text.Json;
using PlaywrightSharp.Helpers;
using Xunit;

namespace PlaywrightSharp.Tests.Internals
{
    /// <summary>
    /// FlexibleStringEnumConverterTests
    /// </summary>
    public class FlexibleStringEnumConverterTests
    {
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new FlexibleStringEnumConverter<TestEnum>(TestEnum.Default)
            },
        };

        private enum TestEnum
        {
            Default,
            Document,
            [EnumMember(Value = "usingenummember")]
            StyleSheet,
        }

        /// <summary>
        /// Should work with the enum name.
        /// </summary>
        [Fact]
        public void ShouldWorkWithName()
            => Assert.Equal(TestEnum.Document, JsonSerializer.Deserialize<TestEnum>("\"document\"", _options));

        /// <summary>
        /// Should work with the EnumMember value.
        /// </summary>
        [Fact]
        public void ShouldWorkWithEnumMemberName()
            => Assert.Equal(TestEnum.StyleSheet, JsonSerializer.Deserialize<TestEnum>("\"usingenummember\"", _options));

        /// <summary>
        /// Should fallback to the default value.
        /// </summary>
        [Fact]
        public void ShouldFallback()
            => Assert.Equal(TestEnum.Default, JsonSerializer.Deserialize<TestEnum>("\"foobar\"", _options));
    }
}
