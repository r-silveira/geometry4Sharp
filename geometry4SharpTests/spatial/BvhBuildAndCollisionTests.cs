using g4;
using Xunit;

namespace geometry4SharpTests.spatial;

public class BvhBuildAndCollisionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void LongestAxisBuildPreservesQueriesAndCoverage(int boxes)
    {
        var mesh = CreateBoxes(boxes, 0.0);
        var standard = Make(mesh, NTMeshAABBTree3.BuildStrategy.TopDownMidpoint, false);
        var longest = Make(mesh, NTMeshAABBTree3.BuildStrategy.TopDownLongestAxisMidpoint, false);
        standard.TestCoverage();
        longest.TestCoverage();

        var hit = Make(CreateBoxes(boxes, 0.1), NTMeshAABBTree3.BuildStrategy.TopDownMidpoint, false);
        var miss = Make(CreateBoxes(boxes, 10000.0), NTMeshAABBTree3.BuildStrategy.TopDownMidpoint, false);
        Assert.Equal(standard.TestIntersection(hit), longest.TestIntersection(hit));
        Assert.Equal(standard.TestIntersection(miss), longest.TestIntersection(miss));

        int side = (int)Math.Ceiling(Math.Sqrt(boxes));
        for (int i = 0; i < boxes; ++i)
        {
            var p = new Vector3d(i % side * 6 + 0.12, i / side * 6 + 0.12, 5);
            var ray = new Ray3d(p, new Vector3d(0, 0, -1));
            var t0 = standard.FindNearestTriangle(p);
            var t1 = longest.FindNearestTriangle(p);
            Assert.NotEqual(NTMesh3.InvalidID, t0);
            Assert.NotEqual(NTMesh3.InvalidID, t1);
            double d0 = MeshQueries.TriDistanceSqr(mesh, t0, p);
            double d1 = MeshQueries.TriDistanceSqr(mesh, t1, p);
            Assert.True(Math.Abs(d0 - d1) < 1e-7, $"Different nearest distances: {d0} vs {d1}");
            Assert.Equal(standard.FindAllHitTriangles(ray), longest.FindAllHitTriangles(ray));
            Assert.Equal(
                standard.FindNearestHitTriangle(ray) != NTMesh3.InvalidID,
                longest.FindNearestHitTriangle(ray) != NTMesh3.InvalidID);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void RootEarlyOutMatchesOriginalCollisionQueries(int boxes)
    {
        var mesh = CreateBoxes(boxes, 0.0);
        var baseline = Make(mesh, NTMeshAABBTree3.BuildStrategy.TopDownMidpoint, false);
        var rootCheck = Make(mesh, NTMeshAABBTree3.BuildStrategy.TopDownMidpoint, true);
        var hit = Make(CreateBoxes(boxes, 0.1), NTMeshAABBTree3.BuildStrategy.TopDownMidpoint, false);
        var miss = Make(CreateBoxes(boxes, 10000), NTMeshAABBTree3.BuildStrategy.TopDownMidpoint, false);

        foreach (var other in new[] { hit, miss })
        {
            Assert.Equal(baseline.TestIntersection(other), rootCheck.TestIntersection(other));
            Assert.Equal(baseline.TestIntersectionAndGetTriangles(other).Item1,
                         rootCheck.TestIntersectionAndGetTriangles(other).Item1);
        }
        Assert.False(rootCheck.TestIntersection(miss));

        Func<Vector3d, Vector3d> farTranslation = v => new Vector3d(v.x + 10000, v.y, v.z);
        Assert.Equal(baseline.TestIntersection(hit, farTranslation),
                     rootCheck.TestIntersection(hit, farTranslation));
    }

    private static NTMeshAABBTree3 Make(NTMesh3 mesh, NTMeshAABBTree3.BuildStrategy strategy, bool rootCheck)
    {
        var tree = new NTMeshAABBTree3(mesh)
        {
            UseAdaptiveStorageBlocks = true,
            UseRootBoundsEarlyOut = rootCheck
        };
        tree.Build(strategy);
        return tree;
    }

    private static NTMesh3 CreateBoxes(int count, double offsetX)
    {
        var mesh = new NTMesh3(false, false, false, false);
        int side = (int)Math.Ceiling(Math.Sqrt(count));
        for (int i = 0; i < count; i++)
        {
            double x = (i % side) * 6 + offsetX, y = (i / side) * 6;
            int[] v =
            {
                mesh.AppendVertex(new Vector3d(x, y, 0)),
                mesh.AppendVertex(new Vector3d(x+1, y, 0)),
                mesh.AppendVertex(new Vector3d(x+1, y+1, 0)),
                mesh.AppendVertex(new Vector3d(x, y+1, 0)),
                mesh.AppendVertex(new Vector3d(x, y, 2)),
                mesh.AppendVertex(new Vector3d(x+1, y, 2)),
                mesh.AppendVertex(new Vector3d(x+1, y+1, 2)),
                mesh.AppendVertex(new Vector3d(x, y+1, 2))
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
