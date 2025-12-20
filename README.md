# Grinspector 🎄

[![Build Status](https://github.com/mvpete/Grinspect/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/mvpete/Grinspect/actions/workflows/ci-cd.yml)
[![Tests](https://img.shields.io/badge/tests-30%2F30-brightgreen)](https://github.com/mvpete/Grinspect/actions)
[![NuGet](https://img.shields.io/nuget/v/Grinspector.svg)](https://www.nuget.org/packages/Grinspector/)

*"You're a mean one, Mr. Private Method..."* 🎅

A .NET source generator library that sneaks into your private methods, properties, and fields for testing purposes - just like the Grinch sneaking into Whoville! No more wrestling with reflection strings and binding flags.

## Overview 🔍

Why let private members hide like the Grinch in his cave? Instead of using clunky reflection:

```csharp
var method = typeof(Foo).GetMethod("Bar", BindingFlags.NonPublic | BindingFlags.Instance);
method.Invoke(fooInstance, new object[] { 1, 2 });
```

Use Grinspector:

```csharp
var inspector = new Foo_Privates(fooInstance);
inspector.Bar(1, 2);
```

## Features 🎁

- 🎄 Strongly-typed access to private instance methods, properties, and fields
- 🌟 Static member support: Access private static methods, properties, and fields
- 🏗️ Private constructor support: Create instances via private constructors
- ⭐ IntelliSense support (even Santa's elves would be jealous)
- ✨ Compile-time safety
- 🎅 Refactoring-safe: Renaming private members causes compiler errors, not runtime failures
- 🔔 Generated wrapper classes (uses reflection internally)
- 🎁 Simple, declarative API

## ⚠️ A Word of Caution

**Needing to test private members is usually a code smell.** Like the Grinch himself, this tool exists because sometimes the world isn't perfect. 

If you find yourself reaching for Grinspector, consider:
- **Refactoring**: Can the private member be extracted into a separate, testable class?
- **Access modifiers**: Should it be `internal` with `InternalsVisibleTo` instead?
- **Design**: Are you testing implementation details instead of behavior?

That said, legacy code exists, tight deadlines happen, and sometimes you need to test the Grinch's cave before you can renovate it. When you're in that situation, Grinspector beats raw reflection. But always prefer proper design over testing private implementation details.

*Use responsibly, like spiked eggnog.* 🥃

## Installation 📦

```bash
dotnet add package Grinspector
```

*Unwrap the gift of type-safe private access!*

## Usage 🎅

### Instance Members

1. Mark your test method/class with the `[PrivatesAvailable(typeof(T))]` attribute to generate a wrapper class (think of it as your "Naughty List" for private members):

```csharp
[Fact]
[PrivatesAvailable(typeof(MyClass))]
public void TestPrivateMethods()
{
    var obj = new MyClass();
    var inspector = new MyClass_Privates(obj);
    
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

### Static Members

Access private static members through the generated `_Static` class:

```csharp
[Fact]
[PrivatesAvailable(typeof(MyClass))]
public void TestPrivateStaticMembers()
{
    // Access private static methods
    int result = MyClass_Privates_Static.MultiplyBy2(5);  // returns 10
    
    // Access private static fields
    MyClass_Privates_Static._counter = 42;
    int value = MyClass_Privates_Static._counter;
    
    // Access private static properties
    MyClass_Privates_Static.Configuration = "test";
}
```

```csharp
public class MyClass
{
    private static int _counter;
    private static string Configuration { get; set; }
    
    private static int MultiplyBy2(int x) => x * 2;
}
```

### Private Constructors

Create instances through private constructors:

```csharp
[Fact]
[PrivatesAvailable(typeof(Singleton))]
public void TestPrivateConstructor()
{
    // Create instance via private constructor
    var instance = Singleton_Privates_Static.CreateInstance("config", 123);
    
    // Then inspect its private members
    var inspector = new Singleton_Privates(instance);
    inspector.Initialize();
}
```

```csharp
public class Singleton
{
    private Singleton(string config, int value)
    {
        // Private constructor logic
    }
    
    private void Initialize() { /* ... */ }
}
```

The source generator will create:
- `MyClass_Privates` class for instance members
- `MyClass_Privates_Static` class for static members and constructors

## How It Works 🎪

Grinspector uses C# source generators to steal... er, *inspect* your private members:

### Source Generator
1. Scans for test methods/classes decorated with `[PrivatesAvailable(typeof(T))]`
2. Analyzes the target type `T` for private members (instance and static)
3. Generates wrapper classes:
   - `T_Privates` for instance members
   - `T_Privates_Static` for static members and constructors

This provides:

- **Type Safety**: The compiler knows about all private members and their signatures
- **IntelliSense**: Full IDE support with autocomplete and documentation
- **Refactoring Safety**: When you rename a private method/property/field, the generator re-runs and your test code breaks at compile-time with standard compiler errors
- **Opt-In**: Only generates wrappers for types you explicitly mark with the attribute
- **Test-Friendly**: Designed specifically for testing scenarios where you need to access private implementation details

**Note**: Generated code uses reflection internally (`MethodInfo.Invoke`, `PropertyInfo.GetValue/SetValue`, `FieldInfo.GetValue/SetValue`), so there is reflection overhead at runtime. The benefit is compile-time type safety and discoverability through IntelliSense.

## Building from Source 🏗️

```bash
dotnet build
```

## Running Tests 🧪

```bash
dotnet test
```

*All tests passing? Your heart just grew three sizes!* ❤️

## Requirements ❄️

- .NET 10.0 or later
- A willingness to peek at private members (we won't judge! 😉)

## License 📜

MIT - Free as a sleigh ride!
