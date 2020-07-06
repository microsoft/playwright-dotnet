using System;
using Castle.DynamicProxy;

namespace PlaywrightSharp.Transport.Channel
{
    public class ChannelProxyFactory
    {
        private static readonly ProxyGenerator Generator = new ProxyGenerator();

        public static T CreateProxy<T>(string guid, ConnectionScope scope) where T : class
        {
            var channelInterceptor = new ChannelInterceptor(guid, scope);
            var proxy = Generator.CreateClassProxy<T>(channelInterceptor);
            return proxy;
        }
    }
}
