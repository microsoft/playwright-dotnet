using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport.Channel
{
    internal class ChannelInterceptor : IInterceptor
    {
        private readonly string _guid;
        private readonly ConnectionScope _scope;

        public ChannelInterceptor(string guid, ConnectionScope scope)
        {
            _guid = guid;
            _scope = scope;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.MethodInvocationTarget.ReturnType == typeof(Task))
            {
                invocation.ReturnValue = InterceptAsync(invocation);
            }
        }

        private async Task<object> InterceptAsync(IInvocation invocation)
        {
            var jsonResult = await _scope.SendMessageToServer(_guid, ToJavascriptFunction(invocation.Method.Name), invocation.Arguments).ConfigureAwait(false);
            var returnType = invocation.MethodInvocationTarget.ReturnType.GenericTypeArguments.FirstOrDefault();

            return returnType != null ? jsonResult?.ToObject(returnType) : null;
        }

        private string ToJavascriptFunction(string method)
        {
            string javascriptified = char.ToLower(method[0]) + method.Substring(1);

            return javascriptified.EndsWith("Async") ? javascriptified.Substring(0, javascriptified.Length - 5) : javascriptified;
        }
    }
}
