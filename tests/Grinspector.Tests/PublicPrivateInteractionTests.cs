using Xunit;

namespace Grinspector.Tests;

[InternalsAvailable(typeof(Counter))]
public class PublicPrivateInteractionTests
{
    [Fact]
    public void CanReadPrivateFieldAfterPublicMethodChangesIt()
    {
        // Arrange
        var counter = new Counter();
        var inspector = new Internals_Counter(counter);

        // Act - call public method that changes private field
        counter.Increment();
        counter.Increment();
        counter.Increment();

        // Assert - read private field value
        Assert.Equal(3, inspector._count);
    }

    [Fact]
    public void CanReadPublicPropertyAfterPrivateMethodChangesIt()
    {
        // Arrange
        var counter = new Counter();
        var inspector = new Internals_Counter(counter);

        // Act - call private method that changes private field
        inspector.SetCount(42);

        // Assert - read public property
        Assert.Equal(42, counter.Count);
    }

    [Fact]
    public void CanUsePrivatePropertyAfterPublicMethodChangesIt()
    {
        // Arrange
        var counter = new Counter();
        var inspector = new Internals_Counter(counter);

        // Act - call public method that changes private property
        counter.SetMessage("Hello");

        // Assert - read private property
        Assert.Equal("Hello", inspector.Message);
    }

    [Fact]
    public void CanReadPublicMethodResultAfterPrivatePropertyChanges()
    {
        // Arrange
        var counter = new Counter();
        var inspector = new Internals_Counter(counter);

        // Act - change private property
        inspector.Message = "Test Message";

        // Assert - read via public method
        Assert.Equal("Test Message", counter.GetMessage());
    }

    [Fact]
    public void PrivateAndPublicMethodsWorkTogether()
    {
        // Arrange
        var counter = new Counter();
        var inspector = new Internals_Counter(counter);

        // Act
        counter.Increment();
        inspector.DoubleCount();
        counter.Increment();

        // Assert
        Assert.Equal(3, counter.Count);
        Assert.Equal(3, inspector._count);
    }
}

public class Counter
{
    private int _count;
    private string Message { get; set; } = "";

    public int Count => _count;

    public void Increment()
    {
        _count++;
    }

    public void SetMessage(string message)
    {
        Message = message;
    }

    public string GetMessage()
    {
        return Message;
    }

    private void SetCount(int value)
    {
        _count = value;
    }

    private void DoubleCount()
    {
        _count *= 2;
    }
}
