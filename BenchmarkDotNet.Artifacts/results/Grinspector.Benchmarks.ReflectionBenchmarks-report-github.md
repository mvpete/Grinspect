```

BenchmarkDotNet v0.14.0, macOS 26.1 (25B78) [Darwin 25.1.0]
Apple M3 Pro, 1 CPU, 11 logical and 11 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.125.57005), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 10.0.1 (10.0.125.57005), Arm64 RyuJIT AdvSIMD


```
| Method                | Mean       | Error     | StdDev    | Ratio | RatioSD |
|---------------------- |-----------:|----------:|----------:|------:|--------:|
| DirectCall            |  0.0000 ns | 0.0000 ns | 0.0000 ns |     ? |       ? |
| GrinspectorCall       |  0.5266 ns | 0.0050 ns | 0.0039 ns |     ? |       ? |
| RawReflectionCall     | 25.8825 ns | 0.1237 ns | 0.1157 ns |     ? |       ? |
| StaticGrinspectorCall |  0.2973 ns | 0.0060 ns | 0.0047 ns |     ? |       ? |
| VoidMethod            |  0.9410 ns | 0.0311 ns | 0.0291 ns |     ? |       ? |
| PropertyAccess        |  2.0381 ns | 0.0149 ns | 0.0124 ns |     ? |       ? |
| FieldAccess           |  2.0438 ns | 0.0096 ns | 0.0075 ns |     ? |       ? |
