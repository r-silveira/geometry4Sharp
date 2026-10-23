using g4;
using Xunit;

namespace geometry4SharpTests.spatial;

public class BvhStorageRegressionTests
{
    [Fact]
    public void DefaultDVectorRemains2048()
    {
        var vec = new DVector<int>();
        Assert.Equal(2048, vec.BlockCount);
        vec.Add(42);
        Assert.Equal(42, vec[0]);
    }

    [Fact]
    public void SmallDVectorCrossesSeveralBlocksAndCopiesCorrectly()
    {
        var vec = new DVector<int>(64);
        for (int i = 0; i < 200; i++) vec.Add(i * 3);
        Assert.Equal(64, vec.BlockCount);
        Assert.Equal(200, vec.Length);
        for (int i = 0; i < 200; i++) Assert.Equal(i * 3, vec[i]);
        vec[64] = 777;
        var copy = new DVector<int>(vec);
        Assert.Equal(64, copy.BlockCount);
        Assert.Equal(777, copy[64]);
        Assert.Equal(597, copy[199]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-8)]
    public void InvalidBlockSizesAreRejected(int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DVector<int>(size));

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void AdaptiveTreeMatchesOriginal(int boxCount)
    {
        var mesh = CreateBoxes(boxCount);
        var baseline = new NTMeshAABBTree3(mesh) { UseAdaptiveStorageBlocks = false };
        var adaptive = new NTMeshAABBTree3(mesh) { UseAdaptiveStorageBlocks = true };
        baseline.Build();
        adaptive.Build();
        baseline.TestCoverage();
        adaptive.TestCoverage();

        // Exercise the distance predicate already optimized on the feature branch.
        // Geometry and query results must be identical for both storage policies.
        Assert.Equal(
            baseline.HasTrianglesWithinDistance(adaptive, 0.5),
            adaptive.HasTrianglesWithinDistance(baseline, 0.5));

        int side = (int)Math.Ceiling(Math.Sqrt(boxCount));
        for (int i = 0; i < boxCount; i++)
        {
            var origin = new Vector3d((i % side) * 6 + 0.11, (i / side) * 6 + 0.17, 7);
            var ray = new Ray3d(origin, new Vector3d(0, 0, -1));
            Assert.Equal(baseline.FindNearestHitTriangle(ray), adaptive.FindNearestHitTriangle(ray));
            Assert.Equal(baseline.FindNearestTriangle(origin), adaptive.FindNearestTriangle(origin));
            var a = new List<int>();
            var b = new List<int>();
            Assert.Equal(baseline.FindAllHitTriangles(ray, a), adaptive.FindAllHitTriangles(ray, b));
            Assert.Equal(a.OrderBy(x => x), b.OrderBy(x => x));
        }
    }

    private static NTMesh3 CreateBoxes(int count)
    {
        var mesh = new NTMesh3(false, false, false, false);
        int side = (int)Math.Ceiling(Math.Sqrt(count));
        for (int i = 0; i < count; i++)
        {
            double x = (i % side) * 6;
            double y = (i / side) * 6;
            int[] v =
            {
                mesh.AppendVertex(new Vector3d(x, y, 0)),
                mesh.AppendVertex(new Vector3d(x+1, y, 0)),
                mesh.AppendVertex(new Vector3d(x+1, y+1, 0)),
                mesh.AppendVertex(new Vector3d(x, y+1, 0)),
                mesh.AppendVertex(new Vector3d(x, y, 2)),
                mesh.AppendVertex(new Vector3d(x+1, y, 2)),
                mesh.AppendVertex(new Vector3d(x+1, y+1, 2)),
                mesh.AppendVertex(new Vector3d(x, y+1, 2)),
            };
            int[,] faces =
            {
                {0,2,1}, {0,3,2}, {4,5,6}, {4,6,7},
                {0,4,7}, {0,7,3}, {1,2,6}, {1,6,5},
                {0,1,5}, {0,5,4}, {3,7,6}, {3,6,2}
            };
            for (int j = 0; j < 12; ++j)
                mesh.AppendTriangle(v[faces[j,0]], v[faces[j,1]], v[faces[j,2]]);
        }
        return mesh;
    }
}
