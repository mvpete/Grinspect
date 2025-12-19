using Xunit;

namespace Grinspector.Tests;

[PrivatesAvailable(typeof(Foo))]
public class GrinspectorTests
{
    [Fact]
    public void CanCallPrivateMethod()
    {
        // Arrange
        var foo = new Foo();
        var inspector = new Foo_Privates(foo);

        // Act
        var result = inspector.Bar(1, 2);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void CanCallPrivateMethodWithNoParameters()
    {
        // Arrange
        var foo = new Foo();
        var inspector = new Foo_Privates(foo);

        // Act
        var result = inspector.GetSecret();

        // Assert
        Assert.Equal("secret", result);
    }

    [Fact]
    public void CanCallPrivateVoidMethod()
    {
        // Arrange
        var foo = new Foo();
        var inspector = new Foo_Privates(foo);

        // Act
        inspector.DoSomething();

        // Assert - verify it was called by checking internal state
        Assert.Equal(42, inspector.GetValue());
    }

    [Fact]
    public void CanAccessPrivateField()
    {
        // Arrange
        var foo = new Foo();
        var inspector = new Foo_Privates(foo);

        // Act
        inspector._value = 100;

        // Assert
        Assert.Equal(100, inspector._value);
        Assert.Equal(100, inspector.GetValue());
    }

    [Fact]
    public void CanAccessPrivateProperty()
    {
        // Arrange
        var foo = new Foo();
        var inspector = new Foo_Privates(foo);

        // Act
        inspector.SecretValue = "new secret";

        // Assert
        Assert.Equal("new secret", inspector.SecretValue);
    }

    [Fact]
    public void CanAccessReadOnlyPrivateProperty()
    {
        // Arrange
        var foo = new Foo();
        var inspector = new Foo_Privates(foo);

        // Act
        var result = inspector.ReadOnlyValue;

        // Assert
        Assert.Equal(123, result);
    }
}

// Test class with private methods
public class Foo
{
    private int _value;
    private string _secretField = "field secret";

    private string SecretValue { get; set; } = "property secret";
    
    private int ReadOnlyValue { get; } = 123;

    private int Bar(int a, int b)
    {
        return a + b;
    }

    private string GetSecret()
    {
        return "secret";
    }

    private void DoSomething()
    {
        _value = 42;
    }

    private int GetValue()
    {
        return _value;
    }
}
