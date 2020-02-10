using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// JSHandle represents an in-page JavaScript object. JSHandles can be created with the <see cref="IPage.EvaluateHandleAsync(string)"/> and <see cref="IPage.EvaluateHandleAsync(string, object[])"/> methods.
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
        /// Disposes the Handle. It will mark the JSHandle as disposed and release the <see cref="IJSHandle"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the handle is disposed.</returns>
        Task DisposeAsync();
    }
}
