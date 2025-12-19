using Xunit;
using System.Threading.Tasks;

namespace Grinspector.Tests;

[PrivatesAvailable(typeof(AsyncTestClass))]
public class AsyncMethodTests
{
    [Fact]
    public async Task CanCallPrivateAsyncMethod()
    {
        // Arrange
        var instance = new AsyncTestClass();
        var inspector = new AsyncTestClass_Privates(instance);

        // Act
        var result = await inspector.AddAsync(5, 3);

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public async Task CanCallPrivateAsyncVoidMethod()
    {
        // Arrange
        var instance = new AsyncTestClass();
        var inspector = new AsyncTestClass_Privates(instance);

        // Act
        await inspector.DelayAsync();

        // Assert
        Assert.Equal(1, instance.GetCallCount());
    }

    [Fact]
    public async Task CanCallPrivateStaticAsyncMethod()
    {
        // Act
        var result = await AsyncTestClass_Privates_Static.MultiplyAsync(4, 7);

        // Assert
        Assert.Equal(28, result);
    }

    [Fact]
    public async Task CanCallPrivateAsyncMethodWithComplexReturnType()
    {
        // Arrange
        var instance = new AsyncTestClass();
        var inspector = new AsyncTestClass_Privates(instance);

        // Act
        var result = await inspector.GetDataAsync();

        // Assert
        Assert.Equal("async data", result);
    }
}

public class AsyncTestClass
{
    private int _callCount = 0;

    private async Task<int> AddAsync(int a, int b)
    {
        await Task.Delay(1);
        return a + b;
    }

    private async Task DelayAsync()
    {
        await Task.Delay(1);
        _callCount++;
    }

    private static async Task<int> MultiplyAsync(int a, int b)
    {
        await Task.Delay(1);
        return a * b;
    }

    private async Task<string> GetDataAsync()
    {
        await Task.Delay(1);
        return "async data";
    }

    public int GetCallCount() => _callCount;
}
