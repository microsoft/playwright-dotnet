using System;
using PlaywrightSharp.Firefox.Helper;

namespace PlaywrightSharp.Firefox.Protocol.Runtime
{
    internal partial class RemoteObject : IRemoteObject
    {
        string IRemoteObject.Type => Type.ToString();

        string IRemoteObject.Subtype => Subtype.ToString();

        string IRemoteObject.UnserializableValue => UnserializableValue?.ToStringValue();

        internal static RemoteObjectUnserializableValue? GetUnserializableValueFromRaw(string value)
            => value switch
            {
                "Infinity" => RemoteObjectUnserializableValue.Infinity,
                "-Infinity" => RemoteObjectUnserializableValue.NegativeInfinity,
                "-0" => RemoteObjectUnserializableValue.NegativeZero,
                "NaN" => RemoteObjectUnserializableValue.NaN,
                _ => null
            };
    }
}
