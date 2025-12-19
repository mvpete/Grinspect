using Grinspector;

namespace TestConsole;

[PrivatesAvailable(typeof(Foo))]
class Program
{
    static void Main()
    {
        var foo = new Foo();
        var inspector = new Foo_Privates(foo);
        
        var result = inspector.Bar(1, 2);
        Console.WriteLine($"Result: {result}");
    }
}

public class Foo
{
    private int Bar(int a, int b)
    {
        return a + b;
    }
}
