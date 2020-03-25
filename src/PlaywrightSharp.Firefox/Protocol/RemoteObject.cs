using PlaywrightSharp.Firefox.Helper;

namespace PlaywrightSharp.Firefox.Protocol.Runtime
{
    internal partial class RemoteObject : IRemoteObject
    {
        string IRemoteObject.Type => Type.ToString();

        string IRemoteObject.Subtype => Subtype.ToString();

        string IRemoteObject.UnserializableValue => UnserializableValue.ToStringValue();
    }
}
