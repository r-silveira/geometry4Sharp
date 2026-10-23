using BenchmarkDotNet.Attributes;
using g4;

namespace Geometry4Sharp.Benchmarks;

/// <summary>
/// Emulates the SmallListSet block_store pattern: each new adjacency block
/// inserts at an index beyond the DVector's current logical size, causing
/// resize() and repeated crossings of segment boundaries.
/// </summary>
[MemoryDiagnoser]
public class DVectorGrowthBenchmarks
{
    [Params(64, 256, 2048)]
    public int BlockSize { get; set; }

    [Params(100, 1000, 10000)]
    public int BlockInsertions { get; set; }

    [Benchmark]
    public DVector<int> InsertAdjacencyBlocks()
    {
        var buffer = new DVector<int>(BlockSize);
        for (int i = 0; i < BlockInsertions; ++i)
        {
            // A SmallListSet block consists of count, 8 values and next pointer.
            int index = buffer.size + 9;
            buffer.insert(i, index);
        }
        return buffer;
    }
}
