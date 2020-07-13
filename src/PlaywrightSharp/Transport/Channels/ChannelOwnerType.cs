using System.Runtime.Serialization;

namespace PlaywrightSharp.Transport.Channels
{
    internal enum ChannelOwnerType
    {
        [EnumMember(Value = "bindingCall")]
        BindingCall,

        [EnumMember(Value = "browser")]
        Browser,

        [EnumMember(Value = "browserType")]
        BrowserType,

        [EnumMember(Value = "context")]
        Context,

        [EnumMember(Value = "consoleMessage")]
        ConsoleMessage,

        [EnumMember(Value = "dialog")]
        Dialog,

        [EnumMember(Value = "download")]
        Download,

        [EnumMember(Value = "elementHandle")]
        ElementHandle,

        [EnumMember(Value = "frame")]
        Frame,

        [EnumMember(Value = "jsHandle")]
        JSHandle,

        [EnumMember(Value = "page")]
        Page,

        [EnumMember(Value = "request")]
        Request,

        [EnumMember(Value = "response")]
        Response,

        [EnumMember(Value = "route")]
        Route,

        [EnumMember(Value = "playwright")]
        Playwright,

        [EnumMember(Value = "browserServer")]
        BrowserServer,
    }
}
