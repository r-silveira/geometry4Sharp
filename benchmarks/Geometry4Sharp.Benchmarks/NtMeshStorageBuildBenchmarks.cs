using BenchmarkDotNet.Attributes;
using g4;

namespace Geometry4Sharp.Benchmarks;

/// <summary>
/// This benchmarks complete mesh creation. BlockSize = 2048 goes through
/// the original constructor, so it is the actual current baseline.
/// </summary>
[MemoryDiagnoser]
public class NtMeshStorageBuildBenchmarks
{
    [Params(1, 12, 100, 1000, 10000)]
    public int TriangleCount { get; set; }

    [Params(64, 256, 2048)]
    public int BlockSize { get; set; }

    [Benchmark]
    public NTMesh3 CreateMesh() => NtMeshSizeFixtures.Create(TriangleCount, BlockSize);
}
