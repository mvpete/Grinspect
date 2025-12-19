using Xunit;

namespace Grinspector.Tests;

[PrivatesAvailable(typeof(GenericClass<>))]
public class GenericClassTests
{
    [Fact]
    public void CanAccessPrivateMembersOfGenericClass()
    {
        // Arrange
        var instance = GenericClass_Privates_Static<int>.CreateInstance(42);
        var inspector = new GenericClass_Privates<int>(instance);

        // Act
        var value = inspector._value;

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void CanCallPrivateMethodOnGenericClass()
    {
        // Arrange
        var instance = GenericClass_Privates_Static<string>.CreateInstance("hello");
        var inspector = new GenericClass_Privates<string>(instance);

        // Act
        var result = inspector.GetValue();

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void CanAccessStaticMembersOfGenericClass()
    {
        // Arrange
        GenericClass_Privates_Static<int>._counter = 100;

        // Act
        var value = GenericClass_Privates_Static<int>._counter;

        // Assert
        Assert.Equal(100, value);
    }

    [Fact]
    public void CanCallPrivateConstructorOfGenericClass()
    {
        // Act
        var instance = GenericClass_Privates_Static<double>.CreateInstance(3.14);

        // Assert
        Assert.Equal(3.14, instance.GetPublicValue());
    }
}

public class GenericClass<T>
{
    private T _value;
    private static int _counter = 0;

    private GenericClass(T value)
    {
        _value = value;
        _counter++;
    }

    private T GetValue()
    {
        return _value;
    }

    public T GetPublicValue() => _value;
}
