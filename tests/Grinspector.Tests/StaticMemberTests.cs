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
        // Arrange - set via private field
        StaticTestClass_Privates_Static._counter = 42;

        // Act - read via public getter
        var publicValue = StaticTestClass.GetCounter();

        // Assert - private set affected public get
        Assert.Equal(42, publicValue);
    }

    [Fact]
    public void CanAccessPrivateStaticProperty()
    {
        // Arrange - set via private setter
        StaticTestClass_Privates_Static.SharedSecret = "test value";

        // Act - read via public getter
        var publicValue = StaticTestClass.GetSharedSecret();

        // Assert - private set affected public get
        Assert.Equal("test value", publicValue);
    }

    [Fact]
    public void StaticMethodCanModifyStaticField()
    {
        // Arrange
        StaticTestClass_Privates_Static._counter = 10;

        // Act - call private method
        StaticTestClass_Privates_Static.IncrementCounter();
        StaticTestClass_Privates_Static.IncrementCounter();

        // Assert - verify via public getter
        Assert.Equal(12, StaticTestClass.GetCounter());
    }
}

public class StaticTestClass
{
    private static int _counter = 0;
    private static string SharedSecret { get; set; } = "default";

    // Public getters to verify private modifications
    public static int GetCounter() => _counter;
    public static string GetSharedSecret() => SharedSecret;

    private static int Add(int a, int b)
    {
        return a + b;
    }

    private static void IncrementCounter()
    {
        _counter++;
    }
}
