# Benchmarks

Performance benchmarks using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Running Benchmarks

Run all benchmarks:
```bash
dotnet run -c Release --project backend/tests/Benchmarks
```

Run specific benchmark:
```bash
dotnet run -c Release --project backend/tests/Benchmarks -- --filter *SampleBenchmark*
```

Run with memory diagnostics:
```bash
dotnet run -c Release --project backend/tests/Benchmarks -- --memory
```

## Creating New Benchmarks

1. Create a new class in this project
2. Add `[MemoryDiagnoser]` and `[SimpleJob]` attributes to the class
3. Mark methods with `[Benchmark]` attribute
4. Use `[GlobalSetup]` for initialization code

Example:
```csharp
[MemoryDiagnoser]
[SimpleJob]
public class MyBenchmark
{
    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data
    }

    [Benchmark]
    public void MyMethod()
    {
        // Code to benchmark
    }
}
```

## Best Practices

- Always run benchmarks in **Release** mode
- Use `[MemoryDiagnoser]` to track memory allocations
- Warm up code before benchmarking
- Use `[GlobalSetup]` for expensive initialization
- Avoid benchmarking trivial operations
- Compare multiple implementations side-by-side

## Output

Benchmark results are saved to `BenchmarkDotNet.Artifacts/` directory with:
- HTML reports
- CSV/Markdown tables
- Statistical analysis
