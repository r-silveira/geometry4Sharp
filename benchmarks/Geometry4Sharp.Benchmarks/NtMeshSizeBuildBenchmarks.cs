using BenchmarkDotNet.Attributes;
using g4;

namespace Geometry4Sharp.Benchmarks;

/// <summary>
/// Measures complete NTMesh3 creation, including its topology and initial buffers.
/// MemoryDiagnoser reports TOTAL allocated bytes, not memory retained after GC.
/// </summary>
[MemoryDiagnoser]
public class NtMeshSizeBuildBenchmarks
{
    [Params(1, 12, 100, 1000, 10000)]
    public int TriangleCount { get; set; }

    [Benchmark]
    public NTMesh3 CreateMesh() => NtMeshSizeFixtures.Create(TriangleCount);
}
