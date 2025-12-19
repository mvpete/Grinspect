namespace Grinspector;

/// <summary>
/// Marks a test method or class to make private members of the specified type accessible.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class PrivatesAvailableAttribute : Attribute
{
    public PrivatesAvailableAttribute(Type targetType)
    {
        TargetType = targetType;
    }

    public Type TargetType { get; }
}
