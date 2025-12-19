namespace Grinspector;

/// <summary>
/// Marks a test method or class to make internal members of the specified type accessible.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class InternalsAvailableAttribute : Attribute
{
    public InternalsAvailableAttribute(Type targetType)
    {
        TargetType = targetType;
    }

    public Type TargetType { get; }
}
