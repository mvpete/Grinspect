using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Grinspector;
using System.Reflection;

namespace Grinspector.Benchmarks;

[PrivatesAvailable(typeof(TestClass))]
public class ReflectionBenchmarks
{
    private TestClass _instance = null!;
    private TestClass_Privates _inspector = null!;
    private MethodInfo _methodInfo = null!;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new TestClass();
        _inspector = new TestClass_Privates(_instance);
        _methodInfo = typeof(TestClass).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!;
    }

    [Benchmark(Baseline = true)]
    public int DirectCall()
    {
        // This is what we WISH we could do, but can't because it's private
        // Included for comparison to show the overhead
        return _instance.PublicAdd(5, 3);
    }

    [Benchmark]
    public int GrinspectorCall()
    {
        return _inspector.Add(5, 3);
    }

    [Benchmark]
    public int RawReflectionCall()
    {
        return (int)_methodInfo.Invoke(_instance, new object[] { 5, 3 })!;
    }

    [Benchmark]
    public int StaticGrinspectorCall()
    {
        return TestClass_Privates_Static.Multiply(5, 3);
    }

    [Benchmark]
    public void VoidMethod()
    {
        _inspector.DoNothing();
    }

    [Benchmark]
    public int PropertyAccess()
    {
        _inspector.Value = 42;
        return _inspector.Value;
    }

    [Benchmark]
    public int FieldAccess()
    {
        _inspector._field = 42;
        return _inspector._field;
    }
}

public class TestClass
{
    private int _field;
    private int Value { get; set; }

    private int Add(int a, int b) => a + b;
    
    private static int Multiply(int a, int b) => a * b;
    
    private void DoNothing() { }

    // Public method for baseline comparison
    public int PublicAdd(int a, int b) => a + b;
}
// Separate classes for cold-start testing to force recompilation
[PrivatesAvailable(typeof(ColdStartTestClass1))]
[PrivatesAvailable(typeof(ColdStartTestClass2))]
[PrivatesAvailable(typeof(ColdStartTestClass3))]
public class ColdStartBenchmarks
{
    private MethodInfo _methodInfo1 = null!;
    private MethodInfo _methodInfo2 = null!;
    private MethodInfo _methodInfo3 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _methodInfo1 = typeof(ColdStartTestClass1).GetMethod("Calculate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _methodInfo2 = typeof(ColdStartTestClass2).GetMethod("Calculate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _methodInfo3 = typeof(ColdStartTestClass3).GetMethod("Calculate", BindingFlags.Instance | BindingFlags.NonPublic)!;
    }

    [Benchmark(Baseline = true)]
    public int RawReflection_SingleCall()
    {
        var instance = new ColdStartTestClass1();
        return (int)_methodInfo1.Invoke(instance, new object[] { 5, 3 })!;
    }

    [Benchmark]
    public int Grinspector_SingleCall()
    {
        // This triggers Expression tree compilation on first access
        var instance = new ColdStartTestClass2();
        var inspector = new ColdStartTestClass2_Privates(instance);
        return inspector.Calculate(5, 3);
    }

    [Benchmark]
    public int Grinspector_ThreeCalls()
    {
        // Amortize compilation cost over 3 calls
        var instance = new ColdStartTestClass3();
        var inspector = new ColdStartTestClass3_Privates(instance);
        int result = inspector.Calculate(5, 3);
        result += inspector.Calculate(10, 2);
        result += inspector.Calculate(7, 4);
        return result;
    }
}

public class ColdStartTestClass1
{
    private int Calculate(int a, int b) => a + b;
}

public class ColdStartTestClass2
{
    private int Calculate(int a, int b) => a + b;
}

public class ColdStartTestClass3
{
    private int Calculate(int a, int b) => a + b;
}
public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ReflectionBenchmarks>();
    }
}
