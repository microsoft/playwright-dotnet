using System.Runtime.Serialization;

namespace PlaywrightSharp.BuildTasks.Models.Api
{
    public enum PlaywrightMemberKind
    {
        [EnumMember(Value = "method")]
        Method,
        [EnumMember(Value = "event")]
        Event,
        [EnumMember(Value = "property")]
        Property
    }
}
