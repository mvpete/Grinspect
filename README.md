# Grinspector

A .NET source generator library that provides strongly-typed access to private methods, properties, and fields for testing purposes.

## Overview

Instead of using reflection:

```csharp
var method = typeof(Foo).GetMethod("Bar", BindingFlags.NonPublic | BindingFlags.Instance);
method.Invoke(fooInstance, new object[] { 1, 2 });
```

Use Grinspector:

```csharp
var inspector = new Internals_Foo(fooInstance);
inspector.Bar(1, 2);
```

## Features

- ✅ Strongly-typed access to private methods, properties, and fields
- ✅ IntelliSense support
- ✅ Compile-time safety
- ✅ Refactoring-safe: Renaming private members causes compiler errors, not runtime failures
- ✅ Generated wrapper classes (uses reflection internally)
- ✅ Simple, declarative API

## Installation

```bash
dotnet add package Grinspector
```

## Usage

1. Mark your test method/class with the `[InternalsAvailable(typeof(T))]` attribute to generate a wrapper class:

```csharp
[Fact]
[InternalsAvailable(typeof(MyClass))]
public void TestPrivateMethods()
{
    var obj = new MyClass();
    var inspector = new Internals_MyClass(obj);
    
    // Access private methods with full type safety
    int result = inspector.Add(5, 3);        // returns 8
    string secret = inspector.GetSecret();   // returns "secret"
    inspector.DoSomething();                 // void method
    
    // Access private properties and fields
    inspector.SomeProperty = "value";        // set private property
    int value = inspector._privateField;     // read private field
}
```

2. The target class with private members:

```csharp
public class MyClass
{
    private int _privateField;
    private string SomeProperty { get; set; }
    
    private int Add(int a, int b) => a + b;
    private string GetSecret() => "secret";
    private void DoSomething() { /* ... */ }
}
```

The source generator will create an `Internals_MyClass` class with public methods that call the private methods using reflection.

## How It Works

Grinspector uses C# source generators to:

1. Scan for test methods/classes decorated with `[InternalsAvailable(typeof(T))]`
2. Analyze the target type `T` for private instance members (methods, properties, fields)
3. Generate an `Internals_T` wrapper class with public accessors that use reflection at runtime

This provides:

- **Type Safety**: The compiler knows about all private members and their signatures
- **IntelliSense**: Full IDE support with autocomplete and documentation
- **Refactoring Safety**: When you rename a private method/property/field, your test code breaks at compile-time instead of runtime, and IDEs can find all references
- **Opt-In**: Only generates wrappers for types you explicitly mark with the attribute
- **Test-Friendly**: Designed specifically for testing scenarios where you need to access private implementation details

**Note**: Generated code uses reflection internally (`MethodInfo.Invoke`, `PropertyInfo.GetValue/SetValue`, `FieldInfo.GetValue/SetValue`), so there is reflection overhead at runtime. The benefit is compile-time type safety and discoverability through IntelliSense.

## Building from Source

```bash
dotnet build
```

## Running Tests

```bash
dotnet test
```

## Requirements

- .NET 10.0 or later

## License

MIT
