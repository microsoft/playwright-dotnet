using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// JSHandle represents an in-page JavaScript object. JSHandles can be created with the <see cref="IPage.EvaluateHandleAsync(string, object[])"/> method.
    /// </summary>
    public interface IJSHandle
    {
        /// <summary>
        /// Returns a JSON representation of the object.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <remarks>
        /// The method will return an empty JSON if the referenced object is not stringifiable. It will throw an error if the object has circular references.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the evaluation is completed, yielding an <see cref="object"/> with the json value of the handle.</returns>
        Task<T> GetJsonValueAsync<T>();

        /// <summary>
        /// Fetches a single property from the referenced object.
        /// </summary>
        /// <param name="propertyName">property to get.</param>
        /// <returns>A <see cref="Task"/> that completes when the evaluation is completed, yielding a <see cref="IJSHandle"/> from the referenced object.</returns>
        Task<IJSHandle> GetPropertyAsync(string propertyName);

        /// <summary>
        /// Returns a <see cref="IReadOnlyDictionary{TKey, TValue}"/> with property names as keys and <see cref="IJSHandle"/> instances for the property values.
        /// </summary>
        /// <returns>a <see cref="Task"/> that completes when the evaluation is completed, yielding a <see cref="IReadOnlyDictionary{TKey, TValue}"/>.</returns>
        /// <example>
        /// <code>
        /// var handle = await page.EvaluateExpressionHandle("({window, document})");
        /// var properties = await handle.GetPropertiesAsync();
        /// var windowHandle = properties["window"];
        /// var documentHandle = properties["document"];
        /// await handle.DisposeAsync();
        /// </code>
        /// </example>
        Task<IReadOnlyDictionary<string, IJSHandle>> GetPropertiesAsync();

        /// <summary>
        /// Disposes the Handle. It will mark the JSHandle as disposed and release the <see cref="IJSHandle"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the handle is disposed.</returns>
        internal Task DisposeAsync();
    }
}
