using BenchmarkDotNet.Attributes;
using g4;

namespace Geometry4Sharp.Benchmarks;

/// <summary>
/// Exercises read-only NTMesh3 APIs with the mesh created once in GlobalSetup.
/// Each invocation scans all vertices/triangles; divide Mean by TriangleCount
/// to estimate time per triangle. No tree construction is included.
/// </summary>
[MemoryDiagnoser]
public class NtMeshSizeReadBenchmarks
{
    [Params(1, 12, 100, 1000, 10000)]
    public int TriangleCount { get; set; }

    private NTMesh3 _mesh = null!;

    [GlobalSetup]
    public void Setup() => _mesh = NtMeshSizeFixtures.Create(TriangleCount);

    [Benchmark]
    public double GetVertices()
    {
        double sum = 0;
        int count = _mesh.VertexCount;
        for (int vid = 0; vid < count; ++vid)
        {
            Vector3d v = _mesh.GetVertex(vid);
            sum += v.x + v.y * 0.1 + v.z * 0.01;
        }
        return sum;
    }

    [Benchmark]
    public double GetTriangles()
    {
        double sum = 0;
        int count = _mesh.TriangleCount;
        for (int tid = 0; tid < count; ++tid)
        {
            Index3i t = _mesh.GetTriangle(tid);
            sum += t.a + t.b * 0.1 + t.c * 0.01;
        }
        return sum;
    }

    [Benchmark]
    public double GetTriVertices()
    {
        double sum = 0;
        int count = _mesh.TriangleCount;
        Vector3d a = default, b = default, c = default;
        for (int tid = 0; tid < count; ++tid)
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
        int count = _mesh.TriangleCount;
        for (int tid = 0; tid < count; ++tid)
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
        int count = _mesh.TriangleCount;
        for (int tid = 0; tid < count; ++tid)
        {
            AxisAlignedBox3d bounds = _mesh.GetTriBounds(tid);
            sum += bounds.Center.x;
        }
        return sum;
    }
}
