using BenchmarkDotNet.Attributes;
using g4;

namespace Geometry4Sharp.Benchmarks;

[MemoryDiagnoser]
//[ShortRunJob]
public class BvhBuildBenchmarks
{
    [Params(1, 10, 100, 1000)]
    public int BoxCount { get; set; }

    private NTMesh3 _mesh = null!;

    [GlobalSetup]
    public void Setup() => _mesh = FixtureMeshes.CreateBoxes(BoxCount);

    [Benchmark]
    public NTMeshAABBTree3 Build_Adaptive()
    {
        var tree = new NTMeshAABBTree3(_mesh) { UseAdaptiveStorageBlocks = true };
        tree.Build();
        return tree;
    }

    [Benchmark(Baseline = true)]
    public NTMeshAABBTree3 Build_2048()
    {
        var tree = new NTMeshAABBTree3(_mesh) { UseAdaptiveStorageBlocks = false };
        tree.Build();
        return tree;
    }
}

// Query benchmarks intentionally separate raycast and nearest-point baselines.
// Both trees are built during GlobalSetup, outside the measured query methods.
public abstract class BvhQueryFixture
{
    [Params(1, 10, 100, 1000)]
    public int BoxCount { get; set; }

    protected NTMeshAABBTree3 Baseline = null!;
    protected NTMeshAABBTree3 Adaptive = null!;
    protected Ray3d[] Rays = null!;
    protected Vector3d[] Points = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mesh = FixtureMeshes.CreateBoxes(BoxCount);
        Baseline = new NTMeshAABBTree3(mesh) { UseAdaptiveStorageBlocks = false };
        Adaptive = new NTMeshAABBTree3(mesh) { UseAdaptiveStorageBlocks = true };
        Baseline.Build();
        Adaptive.Build();
        Rays = FixtureMeshes.CreateRays(BoxCount);
        Points = Rays.Select(ray => ray.Origin).ToArray();
        for (int i = 0; i < Rays.Length; i++)
        {
            int expected = Baseline.FindNearestHitTriangle(Rays[i], 100);
            int actual = Adaptive.FindNearestHitTriangle(Rays[i], 100);
            if (expected != actual)
                throw new InvalidOperationException($"Ray result mismatch at {i}: {expected} vs {actual}");
        }
    }

    protected long Raycast(NTMeshAABBTree3 tree)
    {
        long result = 0;
        foreach (var ray in Rays)
            result += tree.FindNearestHitTriangle(ray, 100);
        return result;
    }

    protected long NearestPoint(NTMeshAABBTree3 tree)
    {
        long result = 0;
        foreach (var point in Points)
            result += tree.FindNearestTriangle(point);
        return result;
    }
}

[MemoryDiagnoser]
//[ShortRunJob]
public class BvhRaycastBenchmarks : BvhQueryFixture
{
    [Benchmark(Baseline = true)]
    public long Raycast_2048() => Raycast(Baseline);

    [Benchmark]
    public long Raycast_Adaptive() => Raycast(Adaptive);
}

[MemoryDiagnoser]
//[ShortRunJob]
public class BvhNearestPointBenchmarks : BvhQueryFixture
{
    [Benchmark(Baseline = true)]
    public long NearestPoint_2048() => NearestPoint(Baseline);

    [Benchmark]
    public long NearestPoint_Adaptive() => NearestPoint(Adaptive);
}

// Particularly relevant to the C32 rule: many small, separately-built BVHs.
// All runs use one shared 12-triangle mesh to isolate tree construction cost.
[MemoryDiagnoser]
//[ShortRunJob]
public class BvhManySmallTreesBenchmarks
{
    [Params(1, 10, 100)]
    public int TreeCount { get; set; }

    private NTMesh3 _mesh = null!;

    [GlobalSetup]
    public void Setup() => _mesh = FixtureMeshes.CreateElement("Column");

    [Benchmark(Baseline = true)]
    public NTMeshAABBTree3[] BuildMany_2048() => BuildMany(false);

    [Benchmark]
    public NTMeshAABBTree3[] BuildMany_Adaptive() => BuildMany(true);

    private NTMeshAABBTree3[] BuildMany(bool adaptive)
    {
        var trees = new NTMeshAABBTree3[TreeCount];
        for (int i = 0; i < trees.Length; i++)
        {
            var tree = new NTMeshAABBTree3(_mesh)
            {
                UseAdaptiveStorageBlocks = adaptive
            };
            tree.Build();
            trees[i] = tree;
        }
        return trees;
    }
}

[MemoryDiagnoser]
//[ShortRunJob]
public class BvhSingleElementBenchmarks
{
    [Params("Column", "Room", "Slab")]
    public string Geometry { get; set; } = "Column";

    private NTMesh3 _mesh = null!;

    [GlobalSetup]
    public void Setup() => _mesh = FixtureMeshes.CreateElement(Geometry);

    [Benchmark(Baseline = true)]
    public NTMeshAABBTree3 Build_2048()
    {
        var tree = new NTMeshAABBTree3(_mesh) { UseAdaptiveStorageBlocks = false };
        tree.Build();
        return tree;
    }

    [Benchmark]
    public NTMeshAABBTree3 Build_Adaptive()
    {
        var tree = new NTMeshAABBTree3(_mesh) { UseAdaptiveStorageBlocks = true };
        tree.Build();
        return tree;
    }
}
