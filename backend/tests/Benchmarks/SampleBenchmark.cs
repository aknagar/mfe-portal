using BenchmarkDotNet.Attributes;

namespace Benchmarks;

/// <summary>
/// Sample benchmark demonstrating BenchmarkDotNet usage.
/// Replace this with actual benchmarks for your services.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class SampleBenchmark
{
    private const int N = 10000;
    private readonly string[] _data = new string[N];

    [GlobalSetup]
    public void Setup()
    {
        for (int i = 0; i < N; i++)
        {
            _data[i] = $"Item_{i}";
        }
    }

    [Benchmark]
    public void StringConcatenation()
    {
        string result = "";
        for (int i = 0; i < 100; i++)
        {
            result += _data[i];
        }
    }

    [Benchmark]
    public void StringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            sb.Append(_data[i]);
        }
        _ = sb.ToString();
    }

    [Benchmark]
    public void StringJoin()
    {
        _ = string.Join("", _data.Take(100));
    }
}
