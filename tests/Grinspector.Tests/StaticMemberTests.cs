using Xunit;

namespace Grinspector.Tests;

[PrivatesAvailable(typeof(StaticTestClass))]
public class StaticMemberTests
{
    [Fact]
    public void CanCallPrivateStaticMethod()
    {
        // Act
        var result = StaticTestClass_Privates_Static.Add(5, 3);

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void CanAccessPrivateStaticField()
    {
        // Arrange
        StaticTestClass_Privates_Static._counter = 42;

        // Act
        var value = StaticTestClass_Privates_Static._counter;

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void CanAccessPrivateStaticProperty()
    {
        // Arrange
        StaticTestClass_Privates_Static.SharedSecret = "test value";

        // Act
        var value = StaticTestClass_Privates_Static.SharedSecret;

        // Assert
        Assert.Equal("test value", value);
    }

    [Fact]
    public void StaticMethodCanModifyStaticField()
    {
        // Arrange
        StaticTestClass_Privates_Static._counter = 10;

        // Act
        StaticTestClass_Privates_Static.IncrementCounter();
        StaticTestClass_Privates_Static.IncrementCounter();

        // Assert
        Assert.Equal(12, StaticTestClass_Privates_Static._counter);
    }
}

public class StaticTestClass
{
    private static int _counter = 0;
    private static string SharedSecret { get; set; } = "default";

    private static int Add(int a, int b)
    {
        return a + b;
    }

    private static void IncrementCounter()
    {
        _counter++;
    }
}
