using System;
using System.Text.Json;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

#nullable enable
#pragma warning disable IDE0018 // Inline variable declaration will cause issues due to Roslyn design: https://github.com/dotnet/roslyn/issues/54711

namespace Microsoft.Playwright.Helpers;

internal static class ChannelHelpers
{
    internal static string? GetString(this JsonElement? element, string name, bool optional)
    {
        if (!element.HasValue)
        {
            return null;
        }

        System.Text.Json.JsonElement retElement;
        if (element.Value.TryGetProperty(name, out retElement))
        {
            return retElement.ToString();
        }

        if (optional)
        {
            return null;
        }

        throw new ArgumentNullException($"Element {name} was expected to not be optional, yet was not present in the data.");
    }

    internal static T? GetObject<T>(this JsonElement? element, string name, Connection connection)
        where T : ChannelOwnerBase, IChannelOwner<T>
    {
        if (!element.HasValue)
        {
            return null;
        }

        JsonElement retElement;
        if (!element.Value.TryGetProperty(name, out retElement))
        {
            return null;
        }

        var guid = retElement.GetProperty("guid").ToString();
        return connection.GetObject(guid) as T;
    }

    internal static T GetObject<T>(this JsonElement element, string name, Connection connection)
        where T : ChannelOwnerBase, IChannelOwner<T>
    {
        var result = GetObject<T>((JsonElement?)element, name, connection);
        if (result == null)
        {
            throw new ArgumentNullException($"Element {name} expected, but was not found.");
        }

        return result;
    }

    // this method is needed, because a converter for <see cref="Exception"/> is no longer supported
    // since .NET5 and throws an exception of "Serialization and deserialization of 'System.Type' instances are not supported and should be avoided since they can lead to security issues."}
    internal static dynamic ToObject(this Exception exception)
    {
        if (exception == null)
        {
            return new { };
        }

        return new
        {
            error = new
            {
                message = exception.Message,
                stack = exception.StackTrace,
                name = exception.GetType().Name,
            },
        };
    }
}

#nullable disable
#pragma warning restore IDE0018 // Inline variable declaration
