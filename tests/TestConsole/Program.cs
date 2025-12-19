using Grinspector;

namespace TestConsole;

[PrivatesAvailable(typeof(Foo))]
[PrivatesAvailable(typeof(Bar))]
class Program
{
    static void Main()
    {
        Console.WriteLine("=== Testing Instance Members ===");
        var foo = new Foo();
        var inspector = new Foo_Privates(foo);
        
        var result = inspector.Bar(1, 2);
        Console.WriteLine($"Bar(1, 2) = {result}");

        Console.WriteLine("\n=== Testing Static Members ===");
        Foo_Privates_Static._counter = 10;
        Console.WriteLine($"Counter before: {Foo_Privates_Static._counter}");
        Foo_Privates_Static.IncrementCounter();
        Console.WriteLine($"Counter after: {Foo_Privates_Static._counter}");
        
        var sum = Foo_Privates_Static.Add(5, 7);
        Console.WriteLine($"Add(5, 7) = {sum}");

        Console.WriteLine("\n=== Testing Private Constructor ===");
        var barInstance = Bar_Privates_Static.CreateInstance("TestApp", 100);
        Console.WriteLine($"Created instance: {barInstance.GetInfo()}");
        
        var barInspector = new Bar_Privates(barInstance);
        var doubled = barInspector.Double();
        Console.WriteLine($"Double() = {doubled}");
        
        Console.WriteLine("\n=== Testing Compilation Cost ===");
        CompilationCostDemo.Run();
    }
}

public class Foo
{
    private static int _counter = 0;
    
    private int Bar(int a, int b)
    {
        return a + b;
    }

    private static int Add(int a, int b)
    {
        return a + b;
    }

    private static void IncrementCounter()
    {
        _counter++;
    }
}

public class Bar
{
    private readonly string _name;
    private readonly int _value;

    private Bar(string name, int value)
    {
        _name = name;
        _value = value;
    }

    public string GetInfo() => $"{_name}: {_value}";

    private int Double()
    {
        return _value * 2;
    }
}
