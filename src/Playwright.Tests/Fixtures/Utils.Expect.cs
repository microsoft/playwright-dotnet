/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>The matchers of <c>expect</c> that NUnit does not have.</summary>
public static partial class Utils
{
    /// <summary>
    /// <c>expect(actual).toEqual(expected)</c> for objects and nested arrays. Both sides are compared as
    /// JavaScript values, whatever they are in .NET: a <c>JsonElement</c> or an <c>ExpandoObject</c> from
    /// <c>EvaluateAsync</c>, a dictionary, a list, an anonymous object, or an object of the API such as a
    /// cookie. A property of a .NET object is a key with a lower-case first letter, and it is absent when
    /// it is null, as <c>undefined</c> is for <c>toEqual</c>. A null in a dictionary or in JSON is a <c>null</c>.
    /// -0 is not 0, and NaN is NaN. The expected value may hold <see cref="AnyNumber"/>, <see cref="ObjectContaining"/>
    /// and the other asymmetric matchers of this class.
    /// </summary>
    public static void ExpectToEqual(object actual, object expected, string message = null)
    {
        var actualValue = Normalize(actual);
        var expectedValue = Normalize(expected);
        if (!NormalizedEquals(actualValue, expectedValue))
        {
            Assert.Fail($"{(message == null ? string.Empty : message + "\n\n")}expect(received).toEqual(expected)\n\nExpected: {Render(expectedValue)}\nReceived: {Render(actualValue)}");
        }
    }

    /// <summary><c>expect(actual).not.toEqual(expected)</c>.</summary>
    public static void ExpectNotToEqual(object actual, object expected)
    {
        var actualValue = Normalize(actual);
        if (NormalizedEquals(actualValue, Normalize(expected)))
        {
            Assert.Fail($"expect(received).not.toEqual(expected)\n\nReceived: {Render(actualValue)}");
        }
    }

    /// <summary>The comparison of <see cref="ExpectToEqual"/>, as a value.</summary>
    public static bool DeepEquals(object left, object right) => NormalizedEquals(Normalize(left), Normalize(right));

    /// <summary><c>Object.is(a, b)</c>: as equality, but NaN is NaN and -0 is not 0.</summary>
    public static bool ObjectIs(object a, object b)
    {
        var left = Normalize(a);
        var right = Normalize(b);
        return left is Number || right is Number ? NormalizedEquals(left, right) : Equals(a, b);
    }

    /// <summary><c>expect.any(Number)</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object AnyNumber { get; } = new Matcher("Any<Number>", value => value is Number);

    /// <summary><c>expect.any(String)</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object AnyString { get; } = new Matcher("Any<String>", value => value is string);

    /// <summary><c>expect.any(Boolean)</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object AnyBoolean { get; } = new Matcher("Any<Boolean>", value => value is bool);

    /// <summary><c>expect.any(Object)</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object AnyObject { get; } = new Matcher("Any<Object>", value => value != null);

    /// <summary><c>expect.anything()</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object Anything { get; } = new Matcher("Anything", value => value != null);

    /// <summary><c>expect.objectContaining({ ... })</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object ObjectContaining(object properties)
    {
        var expected = Normalize(properties) as SortedDictionary<string, object> ?? throw new ArgumentException("ObjectContaining takes an object", nameof(properties));
        return new Matcher(
            "ObjectContaining " + Render(expected),
            value => value is SortedDictionary<string, object> actual && expected.All(pair => actual.TryGetValue(pair.Key, out var other) && NormalizedEquals(other, pair.Value)));
    }

    /// <summary><c>expect.arrayContaining([ ... ])</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object ArrayContaining(IEnumerable items)
    {
        var expected = (List<object>)Normalize(items);
        return new Matcher(
            "ArrayContaining " + Render(expected),
            value => value is List<object> actual && expected.All(item => actual.Any(other => NormalizedEquals(other, item))));
    }

    /// <summary><c>expect.stringContaining('text')</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object StringContaining(string text) => new Matcher($"StringContaining {JsonSerializer.Serialize(text)}", value => value is string actual && actual.Contains(text, StringComparison.Ordinal));

    /// <summary><c>expect.stringMatching(/re/)</c> inside the expected value of <see cref="ExpectToEqual"/>.</summary>
    public static object StringMatching(Regex regex) => new Matcher($"StringMatching /{regex}/", value => value is string actual && regex.IsMatch(actual));

    private static object Normalize(object value) => Normalize(value, [], []);

    /// <param name="value">The value to turn into null, bool, string, <see cref="Number"/>, list or sorted dictionary.</param>
    /// <param name="ancestors">The objects that hold the value, to find a value that holds itself.</param>
    /// <param name="jsonReferences">The "$id" of the objects of a <c>JsonElement</c> that the client made with reference handling.</param>
    private static object Normalize(object value, List<object> ancestors, Dictionary<string, object> jsonReferences)
    {
        switch (value)
        {
            case null:
                return null;
            case Matcher:
                return value;
            case JsonElement json:
                return NormalizeJson(json, jsonReferences);
            case JsonDocument document:
                return NormalizeJson(document.RootElement, jsonReferences);
            case string or bool:
                return value;
            case char c:
                return c.ToString();
            case float f:
                return new Number(f, true);
            case double or decimal or sbyte or byte or short or ushort or int or uint or long or ulong:
                return new Number(Convert.ToDouble(value, CultureInfo.InvariantCulture), false);
            case Enum e:
                return e.GetType().GetField(e.ToString())?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? e.ToString();
            case DateTime date:
                return "Date(" + date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) + ")";
            case DateTimeOffset date:
                return "Date(" + date.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) + ")";
            case Regex regex:
                return "RegExp(" + regex + ", " + regex.Options + ")";
            case Uri uri:
                return "URL(" + (uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString()) + ")";
        }
        var type = value.GetType();
        // A page, a frame, a request and the other objects of the client are equal when they are the same object.
        if (type.IsPrimitive || type.Namespace == "System.Numerics" || type.Namespace == "Microsoft.Playwright.Core" || (type.Namespace == "System" && !type.IsGenericType && !type.IsArray))
        {
            return value;
        }
        var depth = ancestors.FindLastIndex(ancestor => ReferenceEquals(ancestor, value));
        if (depth != -1)
        {
            return $"[Circular ~{ancestors.Count - depth}]";
        }
        ancestors.Add(value);
        try
        {
            if (value is IDictionary<string, object> expando)
            {
                return new SortedDictionary<string, object>(expando.ToDictionary(pair => pair.Key, pair => Normalize(pair.Value, ancestors, jsonReferences)), StringComparer.Ordinal);
            }
            if (StringKeyedPairs(value) is { } pairs)
            {
                var result = new SortedDictionary<string, object>(StringComparer.Ordinal);
                foreach (var (key, item) in pairs)
                {
                    result[key] = Normalize(item, ancestors, jsonReferences);
                }
                return result;
            }
            if (value is IDictionary dictionary)
            {
                var result = new SortedDictionary<string, object>(StringComparer.Ordinal);
                foreach (DictionaryEntry entry in dictionary)
                {
                    result[Convert.ToString(entry.Key, CultureInfo.InvariantCulture)] = Normalize(entry.Value, ancestors, jsonReferences);
                }
                return result;
            }
            if (value is IEnumerable enumerable)
            {
                var items = enumerable.Cast<object>().Select(item => Normalize(item, ancestors, jsonReferences)).ToList();
                // A Set: toEqual does not look at the order of its items.
                var isSet = type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>));
                return isSet ? new SetValue(items) : items;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return new List<object> { Normalize(type.GetProperty("Key").GetValue(value), ancestors, jsonReferences), Normalize(type.GetProperty("Value").GetValue(value), ancestors, jsonReferences) };
            }
            var properties = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }
                var propertyValue = property.GetValue(value);
                if (propertyValue != null)
                {
                    properties[char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1)] = Normalize(propertyValue, ancestors, jsonReferences);
                }
            }
            return properties;
        }
        finally
        {
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    // A dictionary with string keys that is not an IDictionary, such as the headers of a server request.
    private static IEnumerable<(string Key, object Value)> StringKeyedPairs(object value)
    {
        if (value is IDictionary)
        {
            return null;
        }
        var pairType = value.GetType().GetInterfaces()
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(type => type.GetGenericArguments()[0])
            .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>) && type.GetGenericArguments()[0] == typeof(string));
        if (pairType == null)
        {
            return null;
        }
        var key = pairType.GetProperty("Key");
        var item = pairType.GetProperty("Value");
        return ((IEnumerable)value).Cast<object>().Select(pair => ((string)key.GetValue(pair), item.GetValue(pair))).ToList();
    }

    // EvaluateAsync<JsonElement> writes "$id", "$values" and "$ref", the reference handling of System.Text.Json.
    private static object NormalizeJson(JsonElement json, Dictionary<string, object> references)
    {
        switch (json.ValueKind)
        {
            case JsonValueKind.Undefined or JsonValueKind.Null:
                return null;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Number:
                return new Number(json.GetDouble(), false);
            case JsonValueKind.String:
                return json.GetString();
            case JsonValueKind.Array:
                return json.EnumerateArray().Select(item => NormalizeJson(item, references)).ToList();
        }
        if (json.TryGetProperty("$ref", out var reference) && json.EnumerateObject().Count() == 1)
        {
            return references.TryGetValue(reference.GetString() ?? string.Empty, out var target) ? target : $"[Circular {reference.GetString()}]";
        }
        var id = json.TryGetProperty("$id", out var idProperty) ? idProperty.GetString() : null;
        if (id != null && json.TryGetProperty("$values", out var values) && values.ValueKind == JsonValueKind.Array)
        {
            var list = new List<object>();
            references[id] = $"[Circular {id}]";
            list.AddRange(values.EnumerateArray().Select(item => NormalizeJson(item, references)));
            references[id] = list;
            return list;
        }
        var result = new SortedDictionary<string, object>(StringComparer.Ordinal);
        if (id != null)
        {
            references[id] = $"[Circular {id}]";
        }
        foreach (var property in json.EnumerateObject())
        {
            if (id != null && property.NameEquals("$id"))
            {
                continue;
            }
            result[property.Name] = NormalizeJson(property.Value, references);
        }
        if (id != null)
        {
            references[id] = result;
        }
        return result;
    }

    private static bool NormalizedEquals(object left, object right)
    {
        if (right is Matcher matcher)
        {
            return matcher.Matches(left);
        }
        if (left == null || right == null)
        {
            return left == null && right == null;
        }
        if (left is Number leftNumber && right is Number rightNumber)
        {
            // As Object.is: NaN is NaN, and -0 is not 0.
            if (leftNumber.IsSingle || rightNumber.IsSingle)
            {
                return BitConverter.SingleToInt32Bits((float)leftNumber.Value) == BitConverter.SingleToInt32Bits((float)rightNumber.Value) || (float.IsNaN((float)leftNumber.Value) && float.IsNaN((float)rightNumber.Value));
            }
            return BitConverter.DoubleToInt64Bits(leftNumber.Value) == BitConverter.DoubleToInt64Bits(rightNumber.Value) || (double.IsNaN(leftNumber.Value) && double.IsNaN(rightNumber.Value));
        }
        if (left is SetValue leftSet && right is SetValue rightSet)
        {
            var remaining = new List<object>(rightSet.Items);
            foreach (var item in leftSet.Items)
            {
                var index = remaining.FindIndex(other => NormalizedEquals(item, other));
                if (index == -1)
                {
                    return false;
                }
                remaining.RemoveAt(index);
            }
            return remaining.Count == 0;
        }
        if (left is List<object> leftList && right is List<object> rightList)
        {
            return leftList.Count == rightList.Count && leftList.Zip(rightList).All(pair => NormalizedEquals(pair.First, pair.Second));
        }
        if (left is SortedDictionary<string, object> leftObject && right is SortedDictionary<string, object> rightObject)
        {
            return leftObject.Count == rightObject.Count && leftObject.All(pair => rightObject.TryGetValue(pair.Key, out var other) && NormalizedEquals(pair.Value, other));
        }
        return left.GetType() == right.GetType() && left.Equals(right);
    }

    private static string Render(object value)
    {
        var result = new StringBuilder();
        Render(value, result);
        return result.ToString();
    }

    private static void Render(object value, StringBuilder result)
    {
        switch (value)
        {
            case null:
                result.Append("null");
                break;
            case string text:
                result.Append(JsonSerializer.Serialize(text));
                break;
            case bool flag:
                result.Append(flag ? "true" : "false");
                break;
            case Number number:
                result.Append(double.IsNegative(number.Value) && number.Value == 0 ? "-0" : number.Value.ToString("R", CultureInfo.InvariantCulture));
                break;
            case Matcher matcher:
                result.Append(matcher.Description);
                break;
            case SetValue set:
                result.Append("Set ");
                Render(set.Items, result);
                break;
            case List<object> list:
                result.Append('[');
                for (var i = 0; i < list.Count; i++)
                {
                    result.Append(i == 0 ? string.Empty : ", ");
                    Render(list[i], result);
                }
                result.Append(']');
                break;
            case SortedDictionary<string, object> dictionary:
                result.Append('{');
                var first = true;
                foreach (var (key, item) in dictionary)
                {
                    result.Append(first ? " " : ", ").Append(JsonSerializer.Serialize(key)).Append(": ");
                    Render(item, result);
                    first = false;
                }
                result.Append(first ? "}" : " }");
                break;
            default:
                result.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }

    private readonly record struct Number(double Value, bool IsSingle);

    private sealed record Matcher(string Description, Func<object, bool> Matches);

    private sealed record SetValue(List<object> Items);
}
