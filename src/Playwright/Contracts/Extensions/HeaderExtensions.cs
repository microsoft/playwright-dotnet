using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Contains useful extensions methods.
    /// </summary>
    public static class HeaderExtensions
    {
        /// <summary>
        /// Attempts to get the header value for a <see cref="IRequest"/>.
        /// </summary>
        /// <param name="request">The request for headers.</param>
        /// <param name="name">The name of the header.</param>
        /// <returns>Returns the first (if any) header.</returns>
        public static string GetHeaderValue(this IRequest request, string name)
        {
            return request?.GetHeaderValues(name)?.FirstOrDefault();
        }

        /// <summary>
        /// Attempts to get the header value for a <see cref="IRequest.Headers"/>.
        /// </summary>
        /// <param name="request">The request for headers.</param>
        /// <param name="name">The name of the header.</param>
        /// <returns>Returns the all the values for the header, or null, if none.</returns>
        public static string[] GetHeaderValues(this IRequest request, string name)
        {
            return GetValues(request?.Headers, name);
        }

        /// <summary>
        /// Attempts to get the header value for a <see cref="IResponse"/>.
        /// </summary>
        /// <param name="response">The response containing the headers.</param>
        /// <param name="name">The name of the header.</param>
        /// <returns>Returns the first (if any) header.</returns>
        public static string GetHeaderValue(this IResponse response, string name)
        {
            return response?.GetHeaderValues(name)?.FirstOrDefault();
        }

        /// <summary>
        /// Attempts to get the header value for a <see cref="IResponse.Headers"/>.
        /// </summary>
        /// <param name="response">The response containing the headers.</param>
        /// <param name="name">The name of the header.</param>
        /// <returns>Returns the all the values for the header, or null, if none.</returns>
        public static string[] GetHeaderValues(this IResponse response, string name)
        {
            return GetValues(response?.Headers, name);
        }

        private static string[] GetValues(IEnumerable<KeyValuePair<string, string>> collection, string name)
        {
            if (collection == null)
            {
                return null;
            }

            string[] values = collection
                .Where(x => x.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value).ToArray();

            if (values.Length == 0)
            {
                return null;
            }

            return values;
        }
    }
}
