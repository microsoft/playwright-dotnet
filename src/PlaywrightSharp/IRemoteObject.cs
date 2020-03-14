namespace PlaywrightSharp
{
    /// <summary>
    /// RemoteObject interface.
    /// </summary>
    internal interface IRemoteObject
    {
        /// <summary>
        /// Object subtype hint. Specified for `object` type values only.
        /// </summary>
        string Subtype { get; }

        /// <summary>
        /// Primitive value which can not be JSON-stringified does not have `value`, but gets this
        /// property.
        /// </summary>
        string UnserializableValue { get; }

        /// <summary>
        /// Unique object identifier (for non-primitive values).
        /// </summary>
        string ObjectId { get; }

        /// <summary>
        /// Remote object value in case of primitive values or JSON values (if it was requested).
        /// </summary>
        object Value { get; }
    }
}
