using Grinspector;

namespace TestConsole;

class Program
{
    static void Main()
    {
        var foo = new Foo();
        var inspector = new Grinspector<Foo>(foo);
        
        // This should work if the generator runs
        var result = inspector.Private.Bar(1, 2);
        Console.WriteLine($"Result: {result}");
    }
}

class Foo
{
    private int Bar(int a, int b)
    {
        return a + b;
    }
}
