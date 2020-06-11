using System;
using System.Text.Json.Serialization;
using PlaywrightSharp.Firefox.Helper;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Firefox.Protocol.Runtime
{
    internal partial class RemoteObject : IRemoteObject
    {
        string IRemoteObject.Type => Type?.ToValueString();

        string IRemoteObject.Subtype => Subtype?.ToValueString();

        string IRemoteObject.UnserializableValue => UnserializableValue?.ToStringValue();

        internal static RemoteObjectUnserializableValue? GetUnserializableValueFromRaw(string value)
            => value switch
            {
                "Infinity" => RemoteObjectUnserializableValue.Infinity,
                "-Infinity" => RemoteObjectUnserializableValue.NegativeInfinity,
                "-0" => RemoteObjectUnserializableValue.NegativeZero,
                "NaN" => RemoteObjectUnserializableValue.NaN,
                null => null,
                _ => throw new ArgumentOutOfRangeException(nameof(value))
            };
    }
}
