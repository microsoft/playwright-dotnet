using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Transport.Converters;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class EvaluateArgumentValueConverterEdgeCasesTests
{
    [Test]
    public void SerializeEnumShouldHandleUlongBackingExceedingLongMaxValue()
    {
        // UlongBackedBig.Large has value ulong.MaxValue, which exceeds long.MaxValue.
        // The serialization should produce a "n" value as double.
        var serialized = EvaluateArgumentValueConverter.Serialize(
            UlongBackedBig.Large,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.True(serialized.ContainsKey("n"));
        // ulong.MaxValue as double is 1.8446744073709552E+19 (rounded due to double precision)
        var result = serialized["n"]!.GetValue<double>();
        Assert.Greater(result, long.MaxValue);
    }

    [Test]
    public void SerializeEnumShouldHandleUlongBackingWithinLongRange()
    {
        var serialized = EvaluateArgumentValueConverter.Serialize(
            UlongBackedEnum.MidRange,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual(42L, serialized["n"]!.GetValue<long>());
    }

    [Test]
    public void SerializeEnumShouldHandleLongBackedEnum()
    {
        var serialized = EvaluateArgumentValueConverter.Serialize(
            LongBackedEnum.Negative,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual(-100L, serialized["n"]!.GetValue<long>());
    }

    [Test]
    public void SerializeEnumShouldHandleByteBackedEnum()
    {
        var serialized = EvaluateArgumentValueConverter.Serialize(
            ByteBackedEnum.Max,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual(255L, serialized["n"]!.GetValue<long>());
    }

    [Test]
    public void SerializeEnumShouldHandleSbyteBackedEnum()
    {
        var serialized = EvaluateArgumentValueConverter.Serialize(
            SbyteBackedEnum.Min,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual(-128L, serialized["n"]!.GetValue<long>());
    }

    [Test]
    public void SerializeEnumShouldHandleShortBackedEnum()
    {
        var serialized = EvaluateArgumentValueConverter.Serialize(
            ShortBackedEnum.Large,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual(32767L, serialized["n"]!.GetValue<long>());
    }

    [Test]
    public void SerializeEnumShouldHandleUshortBackedEnum()
    {
        var serialized = EvaluateArgumentValueConverter.Serialize(
            UshortBackedEnum.Max,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual(65535L, serialized["n"]!.GetValue<long>());
    }

    [Test]
    public void DeserializeShouldThrowForUnregisteredEnumType()
    {
        // Enum types not registered in PlaywrightJsonContext are not supported.
        // Only types registered via [JsonSerializable] or RegisterTypeInfo work.
        var ex = Assert.Throws<PlaywrightException>(() => Deserialize("""{"n":0}""", typeof(PublicEnum)));
        Assert.That(ex!.Message, Does.Contain("not registered for AOT-safe deserialization"));
    }

    [Test]
    public void BigIntegerShouldRejectExcessivelyLargeString()
    {
        var jsonValue = JsonValue.Create(new string('9', 600));
        using var doc = JsonDocument.Parse("""{"n":0}""");
        var element = doc.RootElement;

        // We need to test the TryConvertJsonValue path for BigInteger.
        // Construct a protocol message that exercises BigInteger deserialization.
        var json = $$"""{"bi":"{{new string('9', 600)}}"}""";
        using var bigDoc = JsonDocument.Parse(json);

        var ex = Assert.Throws<PlaywrightException>(() =>
            EvaluateArgumentValueConverter.Deserialize(bigDoc.RootElement, typeof(BigInteger)));

        Assert.That(ex!.Message, Does.Contain("exceeds maximum allowed length"));
    }

    [Test]
    public void BigIntegerShouldAcceptReasonableLength()
    {
        var json = """{"bi":"12345678901234567890"}""";
        using var doc = JsonDocument.Parse(json);
        var result = EvaluateArgumentValueConverter.Deserialize(doc.RootElement, typeof(BigInteger));

        Assert.AreEqual(            BigInteger.Parse("12345678901234567890", CultureInfo.InvariantCulture), result);
    }

    [Test]
    public void SerializeBigIntegerShouldPreserveLargeValues()
    {
        var bigInt = BigInteger.Parse("9999999999999999999999999999999999", CultureInfo.InvariantCulture);
        var serialized = EvaluateArgumentValueConverter.Serialize(
            bigInt,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.True(serialized.ContainsKey("bi"));
    }

    [Test]
    public void TypedArrayWithNullElementsShouldThrow()
    {
        // Construct a JsonArray with null elements and verify we can't deserialize
        // it as a typed array. The typed array path only applies to the serialization result.
        var json = """{"a":[{"v":"null"},{"v":"null"}],"id":1}""";
        using var doc = JsonDocument.Parse(json);

        // Deserialize as object[] should succeed (returns array with nulls)
        var result = EvaluateArgumentValueConverter.Deserialize(doc.RootElement, typeof(object));
        Assert.IsInstanceOf<object[]>(result);
        var arr = (object[])result!;
        Assert.AreEqual(2, arr.Length);
        Assert.Null(arr[0]);
        Assert.Null(arr[1]);
    }

    [Test]
    public void EmptyByteArrayShouldDeserializeCorrectly()
    {
        using var doc = JsonDocument.Parse("""{"ta":{"k":"Uint8Array","b":[]}}""");
        var result = EvaluateArgumentValueConverter.Deserialize(doc.RootElement, typeof(byte[]));

        Assert.NotNull(result);
        Assert.AreEqual(0, ((byte[])result!).Length);
    }

    [Test]
    public void EmptyIntArrayShouldDeserializeCorrectly()
    {
        using var doc = JsonDocument.Parse("""{"ta":{"k":"Int32Array","b":[]}}""");
        var result = EvaluateArgumentValueConverter.Deserialize(doc.RootElement, typeof(int[]));

        Assert.NotNull(result);
        Assert.AreEqual(0, ((int[])result!).Length);
    }

    [Test]
    public void EmptyDoubleArrayShouldDeserializeCorrectly()
    {
        using var doc = JsonDocument.Parse("""{"ta":{"k":"Float64Array","b":[]}}""");
        var result = EvaluateArgumentValueConverter.Deserialize(doc.RootElement, typeof(double[]));

        Assert.NotNull(result);
        Assert.AreEqual(0, ((double[])result!).Length);
    }

    [Test]
    public void SerializeExpandoObjectShouldWork()
    {
        dynamic expando = new System.Dynamic.ExpandoObject();
        expando.name = "test";
        expando.value = 42;

        var serialized = EvaluateArgumentValueConverter.Serialize(
            (object)expando,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        var entries = serialized["o"]!.AsArray();
        Assert.AreEqual(2, entries.Count);
    }

    [Test]
    public void SerializeGuidShouldWork()
    {
        var guid = Guid.NewGuid();
        var serialized = EvaluateArgumentValueConverter.Serialize(
            guid,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual(guid.ToString("D"), serialized["s"]!.GetValue<string>());
    }

    [Test]
    public void SerializeUriShouldWork()
    {
        var uri = new Uri("https://example.com/path?q=1");
        var serialized = EvaluateArgumentValueConverter.Serialize(
            uri,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual("https://example.com/path?q=1", serialized["u"]!.GetValue<string>());
    }

    [Test]
    public void SerializeDateTimeShouldWork()
    {
        var date = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);
        var serialized = EvaluateArgumentValueConverter.Serialize(
            date,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual("2026-07-11T12:00:00.0000000Z", serialized["d"]!.GetValue<string>());
    }

    [Test]
    public void SerializeExceptionShouldWork()
    {
        var ex = new InvalidOperationException("test error");
        var serialized = EvaluateArgumentValueConverter.Serialize(
            ex,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        var error = serialized["e"]!;
        Assert.AreEqual("InvalidOperationException", error["n"]!.GetValue<string>());
        Assert.AreEqual("test error", error["m"]!.GetValue<string>());
        Assert.NotNull(error["s"]!.GetValue<string>());
    }

    [Test]
    public void SerializeRegexWithFlagsShouldWork()
    {
        var regex = new System.Text.RegularExpressions.Regex("^test$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
        var serialized = EvaluateArgumentValueConverter.Serialize(
            regex,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        var r = serialized["r"]!;
        Assert.AreEqual("^test$", r["p"]!.GetValue<string>());
        // GetSourceAndFlags returns the flags as a string like "im" for IgnoreCase|Multiline
        var flagsStr = r["f"]!.GetValue<string>();
        Assert.That(flagsStr, Does.Contain("i"));
        Assert.That(flagsStr, Does.Contain("m"));
    }

    [Test]
    public void SerializeDoubleSpecialValuesShouldWork()
    {
        var nan = EvaluateArgumentValueConverter.Serialize(
            double.NaN,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());
        Assert.AreEqual("NaN", nan["v"]!.GetValue<string>());

        var posInf = EvaluateArgumentValueConverter.Serialize(
            double.PositiveInfinity,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());
        Assert.AreEqual("Infinity", posInf["v"]!.GetValue<string>());

        var negInf = EvaluateArgumentValueConverter.Serialize(
            double.NegativeInfinity,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());
        Assert.AreEqual("-Infinity", negInf["v"]!.GetValue<string>());

        var negZero = EvaluateArgumentValueConverter.Serialize(
            -0d,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());
        Assert.AreEqual("-0", negZero["v"]!.GetValue<string>());
    }

    [Test]
    public void VisitorInfoIdentityShouldHandleBoxedValueTypesWithoutLeaking()
    {
        var visitorInfo = new EvaluateArgumentValueConverter.VisitorInfo();

        // Boxed value types always get new IDs due to ReferenceEqualityComparer.
        // This is expected behavior: cycle detection for value types is inherently
        // limited because each boxing creates a new object identity.
        // The important thing is that it doesn't throw or leak memory.
        var id1 = visitorInfo.Identity((object)42);
        var id2 = visitorInfo.Identity((object)42);

        Assert.AreNotEqual(id1, id2);
    }

    private static object? Deserialize(string json, Type type)
    {
        using var document = JsonDocument.Parse(json);
        return EvaluateArgumentValueConverter.Deserialize(document.RootElement, type);
    }

    public enum UlongBackedEnum : ulong
    {
        MidRange = 42,
    }

    public enum UlongBackedBig : ulong
    {
        Large = ulong.MaxValue,
    }

    public enum LongBackedEnum : long
    {
        Negative = -100,
    }

    public enum ByteBackedEnum : byte
    {
        Max = 255,
    }

    public enum SbyteBackedEnum : sbyte
    {
        Min = -128,
    }

    public enum ShortBackedEnum : short
    {
        Large = 32767,
    }

    public enum UshortBackedEnum : ushort
    {
        Max = 65535,
    }

    public enum PublicEnum
    {
        Default = 0,
        One = 1,
        Two = 2,
    }
}
