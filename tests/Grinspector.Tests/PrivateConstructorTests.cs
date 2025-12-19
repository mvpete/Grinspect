using Xunit;

namespace Grinspector.Tests;

[PrivatesAvailable(typeof(PrivateConstructorClass))]
public class PrivateConstructorTests
{
    [Fact]
    public void CanCreateInstanceViaPrivateParameterlessConstructor()
    {
        // Act
        var instance = PrivateConstructorClass_Privates_Static.CreateInstance();

        // Assert
        Assert.NotNull(instance);
        Assert.Equal("default:0", instance.GetValue());
    }

    [Fact]
    public void CanCreateInstanceViaPrivateConstructorWithParameters()
    {
        // Act
        var instance = PrivateConstructorClass_Privates_Static.CreateInstance("test value", 42);

        // Assert
        Assert.NotNull(instance);
        Assert.Equal("test value:42", instance.GetValue());
    }

    [Fact]
    public void CanAccessPrivateMethodsOnConstructedInstance()
    {
        // Arrange
        var instance = PrivateConstructorClass_Privates_Static.CreateInstance("hello", 100);
        var inspector = new PrivateConstructorClass_Privates(instance);

        // Act
        var result = inspector.Multiply(2);

        // Assert
        Assert.Equal(200, result);
    }
}

public class PrivateConstructorClass
{
    private readonly string _name;
    private readonly int _value;

    private PrivateConstructorClass()
    {
        _name = "default";
        _value = 0;
    }

    private PrivateConstructorClass(string name, int value)
    {
        _name = name;
        _value = value;
    }

    public string GetValue() => $"{_name}:{_value}";

    private int Multiply(int multiplier)
    {
        return _value * multiplier;
    }
}
