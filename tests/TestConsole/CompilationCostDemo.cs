using System;
using System.Diagnostics;
using Grinspector;

namespace TestConsole;

[PrivatesAvailable(typeof(MyClass))]
public class CompilationCostDemo
{
    public static void Run()
    {
        Console.WriteLine("\n=== Expression Tree Compilation Cost Demo ===\n");
        
        var instance = new MyClass();
        var inspector = new MyClass_Privates(instance);
        
        // First call - pays compilation cost
        var sw = Stopwatch.StartNew();
        var result1 = inspector.Calculate(5, 3);
        sw.Stop();
        Console.WriteLine($"First call:  {sw.Elapsed.TotalMicroseconds:F2} μs (includes ~1-5 μs compilation)");
        
        // Second call - uses cached delegate
        sw.Restart();
        var result2 = inspector.Calculate(10, 2);
        sw.Stop();
        Console.WriteLine($"Second call: {sw.Elapsed.TotalMicroseconds:F2} μs (cached delegate)");
        
        // Third call - also uses cached delegate
        sw.Restart();
        var result3 = inspector.Calculate(7, 4);
        sw.Stop();
        Console.WriteLine($"Third call:  {sw.Elapsed.TotalMicroseconds:F2} μs (cached delegate)");
        
        Console.WriteLine($"\nAll subsequent calls reuse the compiled delegate!");
        Console.WriteLine($"Results: {result1}, {result2}, {result3}");
        
        // Simulate multiple test scenario
        Console.WriteLine("\n=== Simulating Test Suite (10 tests, same class) ===\n");
        
        var totalTime = 0.0;
        for (int i = 0; i < 10; i++)
        {
            var testInstance = new MyClass();
            var testInspector = new MyClass_Privates(testInstance);
            
            sw.Restart();
            testInspector.Calculate(i, i + 1);
            sw.Stop();
            
            totalTime += sw.Elapsed.TotalMicroseconds;
            Console.WriteLine($"Test {i + 1}: {sw.Elapsed.TotalMicroseconds:F3} μs");
        }
        
        Console.WriteLine($"\nAverage: {totalTime / 10:F3} μs per test");
        Console.WriteLine("Note: Only Test 1 paid compilation cost, rest used cached delegate!");
    }
}

public class MyClass
{
    private int Calculate(int a, int b) => a + b;
}
