using BenchmarkDotNet.Attributes;
using g4;

namespace Geometry4Sharp.Benchmarks;

/// <summary>
/// Read-only performance comparison. Mesh creation is intentionally outside
/// the timed region. Each invocation visits EVERY triangle.
/// </summary>
[MemoryDiagnoser]
public class NtMeshStorageReadBenchmarks
{
    [Params(12, 100, 1000, 10000)]
    public int TriangleCount { get; set; }

    [Params(64, 256, 2048)]
    public int BlockSize { get; set; }

    private NTMesh3 _mesh = null!;

    [GlobalSetup]
    public void Setup() => _mesh = NtMeshSizeFixtures.Create(TriangleCount, BlockSize);

    [Benchmark]
    public double GetTriVertices()
    {
        double sum = 0;
        Vector3d a = default, b = default, c = default;
        for (int tid = 0; tid < _mesh.TriangleCount; tid++)
        {
            _mesh.GetTriVertices(tid, ref a, ref b, ref c);
            sum += a.x + b.y * 0.1 + c.z * 0.01;
        }
        return sum;
    }

    [Benchmark]
    public double GetTriCentroid()
    {
        double sum = 0;
        for (int tid = 0; tid < _mesh.TriangleCount; tid++)
        {
            Vector3d c = _mesh.GetTriCentroid(tid);
            sum += c.x + c.y * 0.1 + c.z * 0.01;
        }
        return sum;
    }

    [Benchmark]
    public double GetTriBounds()
    {
        double sum = 0;
        for (int tid = 0; tid < _mesh.TriangleCount; tid++)
            sum += _mesh.GetTriBounds(tid).Center.x;
        return sum;
    }
}
