using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium.Helpers
{
    internal static class EnumHelper
    {
        public static TargetType GetTargetType(this Protocol.Target.TargetInfo targetInfo) => targetInfo.Type.ToEnum<TargetType>();
    }
}
