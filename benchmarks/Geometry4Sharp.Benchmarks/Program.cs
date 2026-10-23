using BenchmarkDotNet.Running;
using g4;

namespace Geometry4Sharp.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
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
