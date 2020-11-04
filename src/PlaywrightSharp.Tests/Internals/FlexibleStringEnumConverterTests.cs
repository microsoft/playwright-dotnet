using System;
using System.Runtime.Serialization;
using System.Text.Json;
using PlaywrightSharp.Helpers;
using Xunit;

namespace PlaywrightSharp.Tests.Internals
{
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

        public enum TestEnum
        {
            Default,
            Document,
            [EnumMember(Value = "usingenummember")]
            StyleSheet,
        }

        [Fact]
        public void ShouldWorkWithName()
            => Assert.Equal(TestEnum.Document, JsonSerializer.Deserialize<TestEnum>("\"document\"", _options));

        [Fact]
        public void ShouldWorkWithEnumMemberName()
            => Assert.Equal(TestEnum.StyleSheet, JsonSerializer.Deserialize<TestEnum>("\"usingenummember\"", _options));

        [Fact]
        public void ShouldFallback()
            => Assert.Equal(TestEnum.Default, JsonSerializer.Deserialize<TestEnum>("\"foobar\"", _options));
    }
}
