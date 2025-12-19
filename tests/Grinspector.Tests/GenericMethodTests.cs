using Xunit;

namespace Grinspector.Tests;

[PrivatesAvailable(typeof(GenericTestClass))]
public class GenericMethodTests
{
    [Fact]
    public void CanCallPrivateGenericMethod()
    {
        // Arrange
        var instance = new GenericTestClass();
        var inspector = new GenericTestClass_Privates(instance);

        // Act
        var result = inspector.Identity(42);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void CanCallPrivateGenericMethodWithReferenceType()
    {
        // Arrange
        var instance = new GenericTestClass();
        var inspector = new GenericTestClass_Privates(instance);

        // Act
        var result = inspector.Identity("hello");

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void CanCallPrivateGenericMethodWithMultipleTypeParameters()
    {
        // Arrange
        var instance = new GenericTestClass();
        var inspector = new GenericTestClass_Privates(instance);

        // Act
        var result = inspector.CreatePair(10, "test");

        // Assert
        Assert.Equal((10, "test"), result);
    }

    [Fact]
    public void CanCallPrivateStaticGenericMethod()
    {
        // Act
        var result = GenericTestClass_Privates_Static.Max(5, 10);

        // Assert
        Assert.Equal(10, result);
    }
}

public class GenericTestClass
{
    private T Identity<T>(T value)
    {
        return value;
    }

    private (T1, T2) CreatePair<T1, T2>(T1 first, T2 second)
    {
        return (first, second);
    }

    private static T Max<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b) > 0 ? a : b;
    }
}
