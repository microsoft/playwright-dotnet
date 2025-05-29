namespace System.Diagnostics.CodeAnalysis;

// This can be dropped when we drop netstandard2.0
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
{
    public bool ReturnValue { get; } = returnValue;
}
