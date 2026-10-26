using BenchmarkDotNet.Attributes;
using g4;

namespace Geometry4Sharp.Benchmarks;

/// <summary>Compare the existing depth-cycling midpoint partition with
/// a longest-centroid-axis midpoint partition. Tree creation is measured.</summary>
[MemoryDiagnoser]
public class BvhSplitHeuristicBenchmarks
{
    [Params(1, 10, 100, 1000)]
    public int BoxCount { get; set; }

    private NTMesh3 _mesh = null!;

    [GlobalSetup]
    public void Setup() => _mesh = FixtureMeshes.CreateBoxes(BoxCount);

    [Benchmark(Baseline = true)]
    public NTMeshAABBTree3 Build_DepthCyclingMidpoint() => Build(NTMeshAABBTree3.BuildStrategy.TopDownMidpoint);

    [Benchmark]
    public NTMeshAABBTree3 Build_LongestCentroidAxis() => Build(NTMeshAABBTree3.BuildStrategy.TopDownLongestAxisMidpoint);

    private NTMeshAABBTree3 Build(NTMeshAABBTree3.BuildStrategy strategy)
    {
        var tree = new NTMeshAABBTree3(_mesh) { UseAdaptiveStorageBlocks = true };
        tree.Build(strategy);
        return tree;
    }
}

/// <summary>Test the optional root-level AABB rejection for both
/// overlapping and disjoint mesh pairs, using the exact same built trees.</summary>
[MemoryDiagnoser]
public class BvhCollisionBenchmarks
{
    [Params(1, 10, 100)]
    public int BoxCount { get; set; }

    private NTMeshAABBTree3 _baseline = null!;
    private NTMeshAABBTree3 _rootCheck = null!;
    private NTMeshAABBTree3 _overlap = null!;
    private NTMeshAABBTree3 _disjoint = null!;

    [GlobalSetup]
    public void Setup()
    {
        NTMesh3 mesh = FixtureMeshes.CreateBoxes(BoxCount);
        _baseline = MakeTree(mesh, false);
        _rootCheck = MakeTree(mesh, true);
        _overlap = MakeTree(FixtureMeshes.CreateBoxes(BoxCount, 0.1), false);
        _disjoint = MakeTree(FixtureMeshes.CreateBoxes(BoxCount, 10000), false);

        if (_baseline.TestIntersection(_overlap) != _rootCheck.TestIntersection(_overlap))
            throw new InvalidOperationException("Collision disagreement: overlapping trees");
        if (_baseline.TestIntersection(_disjoint) != _rootCheck.TestIntersection(_disjoint))
            throw new InvalidOperationException("Collision disagreement: disjoint trees");
        if (_baseline.TestIntersection(_disjoint))
            throw new InvalidOperationException("Expected disjoint meshes to have no intersection");
    }

    private static NTMeshAABBTree3 MakeTree(NTMesh3 mesh, bool rootCheck)
    {
        var tree = new NTMeshAABBTree3(mesh)
        {
            UseAdaptiveStorageBlocks = true,
            UseRootBoundsEarlyOut = rootCheck
        };
        tree.Build();
        return tree;
    }

    [Benchmark(Baseline = true)]
    public bool Disjoint_Baseline() => _baseline.TestIntersection(_disjoint);

    [Benchmark]
    public bool Disjoint_RootCheck() => _rootCheck.TestIntersection(_disjoint);

    [Benchmark]
    public bool Overlap_Baseline() => _baseline.TestIntersection(_overlap);

    [Benchmark]
    public bool Overlap_RootCheck() => _rootCheck.TestIntersection(_overlap);
}

/// <summary>Compare query time after building two different layouts once.
/// The test includes collisions against many separate small target meshes.</summary>
[MemoryDiagnoser]
public class BvhSplitCollisionBenchmarks
{
    [Params(10, 100, 1000)]
    public int BoxCount { get; set; }

    private NTMeshAABBTree3 _depth = null!;
    private NTMeshAABBTree3 _longest = null!;
    private NTMeshAABBTree3[] _targets = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mesh = FixtureMeshes.CreateBoxes(BoxCount);
        _depth = new NTMeshAABBTree3(mesh) { UseAdaptiveStorageBlocks = true };
        _longest = new NTMeshAABBTree3(mesh) { UseAdaptiveStorageBlocks = true };
        _depth.Build(NTMeshAABBTree3.BuildStrategy.TopDownMidpoint);
        _longest.Build(NTMeshAABBTree3.BuildStrategy.TopDownLongestAxisMidpoint);
        _targets = new NTMeshAABBTree3[16];
        for (int i = 0; i < _targets.Length; i++)
        {
            // Mix intersecting and non-intersecting targets with different positions.
            double offset = i % 2 == 0 ? i * 0.01 : 10000 + i;
            _targets[i] = new NTMeshAABBTree3(FixtureMeshes.CreateBoxes(1, offset))
            {
                UseAdaptiveStorageBlocks = true
            };
            _targets[i].Build();
            if (_depth.TestIntersection(_targets[i]) != _longest.TestIntersection(_targets[i]))
                throw new InvalidOperationException($"Heuristic collision mismatch at {i}");
        }
    }

    private int TestAll(NTMeshAABBTree3 tree)
    {
        int total = 0;
        foreach (var target in _targets)
            total += tree.TestIntersection(target) ? 1 : 0;
        return total;
    }

    [Benchmark(Baseline = true)]
    public int Collision_DepthCyclingMidpoint() => TestAll(_depth);

    [Benchmark]
    public int Collision_LongestCentroidAxis() => TestAll(_longest);
}
