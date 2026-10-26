using BenchmarkDotNet.Running;
using g4;

namespace Geometry4Sharp.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--retained-mesh-sized")
        {
            if (args.Length != 4 ||
                !int.TryParse(args[1], out int meshCount) ||
                !int.TryParse(args[2], out int triangleCount) ||
                !int.TryParse(args[3], out int blockSize) ||
                meshCount <= 0 || triangleCount < 0 ||
                blockSize <= 0 || (blockSize & (blockSize - 1)) != 0)
            {
                throw new ArgumentException(
                    "Use --retained-mesh-sized <meshCount> <triangleCount> <blockSize>");
            }
            MeasureMeshRetainedSized(meshCount, triangleCount, blockSize);
            return;
        }

        if (args.Length > 0 && args[0] == "--retained-mesh")
        {
            if (args.Length != 3 ||
                !int.TryParse(args[1], out int meshCount) ||
                !int.TryParse(args[2], out int triangleCount) ||
                meshCount <= 0 || triangleCount < 0)
            {
                throw new ArgumentException(
                    "Use --retained-mesh <meshCount> <triangleCount> (e.g. --retained-mesh 100 12)");
            }

            MeasureMeshRetained(meshCount, triangleCount);
            return;
        }

        if (args.Length > 0 && args[0] == "--mesh-selfcheck")
        {
            foreach (int count in new[] { 0, 1, 12, 100, 1000, 10000 })
            {
                NTMesh3 mesh = NtMeshSizeFixtures.Create(count);
                if (mesh.TriangleCount != count)
                    throw new InvalidOperationException($"Unexpected triangle count: {count}");

                for (int t = 0; t < mesh.TriangleCount; ++t)
                {
                    Index3i indices = mesh.GetTriangle(t);
                    if (!mesh.IsVertex(indices.a) ||
                        !mesh.IsVertex(indices.b) ||
                        !mesh.IsVertex(indices.c))
                        throw new InvalidOperationException($"Invalid triangle {t}.");

                    Vector3d a = default, b = default, c = default;
                    mesh.GetTriVertices(t, ref a, ref b, ref c);
                    _ = mesh.GetTriBounds(t);
                    _ = mesh.GetTriCentroid(t);
                }
                Console.WriteLine($"OK: {count} triangles, {mesh.VertexCount} vertices");
            }
            return;
        }

        if (args.Length > 0 && args[0] == "--retained")
        {
            // Run baseline and adaptive in SEPARATE processes for a fairer comparison.
            if (args.Length < 2 || (args[1] != "baseline" && args[1] != "adaptive"))
                throw new ArgumentException("Use --retained baseline|adaptive [treeCount] [Column|Room|Slab]");
            bool adaptive = args[1] == "adaptive";
            int count = args.Length >= 3 ? int.Parse(args[2]) : 1000;
            string geometry = args.Length >= 4 ? args[3] : "Column";
            MeasureRetained(adaptive, count, geometry);
            return;
        }
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    private static void MeasureMeshRetainedSized(int meshCount, int triangleCount, int blockSize)
    {
        var meshes = new NTMesh3[meshCount];
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long beforeRetained = GC.GetTotalMemory(true);
        long beforeAllocated = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < meshCount; i++)
            meshes[i] = NtMeshSizeFixtures.Create(triangleCount, blockSize);

        long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
        long retained = GC.GetTotalMemory(true) - beforeRetained;
        Console.WriteLine($"Meshes: {meshCount:N0}; triangles/mesh: {triangleCount:N0}; block size: {blockSize}");
        Console.WriteLine($"Allocated: {allocated:N0} bytes ({allocated / (double)meshCount:N0} per mesh)");
        Console.WriteLine($"Approx. retained: {retained:N0} bytes ({retained / (double)meshCount:N0} per mesh)");
        GC.KeepAlive(meshes);
    }

    private static void MeasureMeshRetained(int meshCount, int triangleCount)
    {
        // This is a separate-process experiment. Keep the meshes reachable through
        // both measurements; the array allocation is outside the measured region.
        var meshes = new NTMesh3[meshCount];

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long beforeRetained = GC.GetTotalMemory(true);
        long beforeAllocated = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < meshCount; i++)
            meshes[i] = NtMeshSizeFixtures.Create(triangleCount);

        long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
        long retained = GC.GetTotalMemory(true) - beforeRetained;

        Console.WriteLine($"Meshes: {meshCount:N0}; triangles per mesh: {triangleCount:N0}");
        Console.WriteLine($"Allocated: {allocated:N0} bytes ({allocated / (double)meshCount:N0} per mesh)");
        Console.WriteLine($"Approx. retained managed: {retained:N0} bytes ({retained / (double)meshCount:N0} per mesh)");
        Console.WriteLine("Run each configuration in a fresh process. Retained values are GC-derived estimates.");

        GC.KeepAlive(meshes);
    }

    private static void MeasureRetained(bool adaptive, int treeCount, string geometry)
    {
        if (treeCount <= 0) throw new ArgumentOutOfRangeException(nameof(treeCount));
        // Share one small mesh: measure TREE overhead, not NTMesh overhead.
        var mesh = FixtureMeshes.CreateElement(geometry);
        var trees = new NTMeshAABBTree3[treeCount];
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long beforeRetained = GC.GetTotalMemory(true);
        long beforeAllocated = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < treeCount; i++)
        {
            var tree = new NTMeshAABBTree3(mesh) { UseAdaptiveStorageBlocks = adaptive };
            tree.Build();
            trees[i] = tree;
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
        long retained = GC.GetTotalMemory(true) - beforeRetained;
        Console.WriteLine($"Mode: {(adaptive ? "adaptive" : "baseline")}; geometry: {geometry}");
        Console.WriteLine($"Trees: {treeCount}");
        Console.WriteLine($"Allocated bytes during construction: {allocated:N0} ({allocated / (double)treeCount:N0} per tree)");
        Console.WriteLine($"Approx. retained managed bytes: {retained:N0} ({retained / (double)treeCount:N0} per tree)");
        Console.WriteLine("Compare separate process runs; GC-derived retained numbers are approximate.");
        GC.KeepAlive(trees);
        GC.KeepAlive(mesh);
    }
}
