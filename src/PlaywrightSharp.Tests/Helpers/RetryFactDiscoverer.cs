using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace PlaywrightSharp.Tests.Helpers
{
    /// <inheritdoc />
    public class RetryFactDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly IMessageSink _diagnosticMessageSink;

        /// <summary>
        /// RetryFactDiscoverer Constructor
        /// </summary>
        /// <param name="diagnosticMessageSink"></param>
        public RetryFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        /// <inheritdoc />
        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            int maxRetries = factAttribute.GetNamedArgument<int>("MaxRetries");
            if (maxRetries < 1)
            {
                maxRetries = 3;
            }

            yield return new RetryTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), TestMethodDisplayOptions.All, testMethod, maxRetries);
        }
    }
}
