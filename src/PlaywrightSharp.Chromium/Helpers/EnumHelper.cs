using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium.Helpers
{
    internal static class EnumHelper
    {
        public static TargetType GetTargetType(this Protocol.Target.TargetInfo targetInfo)
            => targetInfo.Type switch
            {
                "page" => TargetType.Page,
                "service_worker" => TargetType.ServiceWorker,
                "browser" => TargetType.Browser,
                "background_page" => TargetType.BackgroundPage,
                "worker" => TargetType.Worker,
                "javascript" => TargetType.Javascript,
                "network" => TargetType.Network,
                "deprecation" => TargetType.Deprecation,
                "security" => TargetType.Security,
                "recommendation" => TargetType.Recommendation,
                "shared_worker" => TargetType.SharedWorker,
                "iframe" => TargetType.IFrame,
                _ => TargetType.Other
            };
    }
}
