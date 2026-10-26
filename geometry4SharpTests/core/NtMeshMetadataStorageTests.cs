using System;
using System.Linq;
using g4;
using Xunit;

namespace geometry4SharpTests.core;

public class NtMeshMetadataStorageTests
{
    [Fact]
    public void RefCountVector_FreeIndicesGrowBeyondInitialBlockAndAreReusable()
    {
        var refs = new RefCountVector();
        for (int i = 0; i < 300; i++)
            Assert.Equal(i, refs.allocate());

        Assert.Equal(300, refs.count);
        for (int i = 0; i < 300; i++)
            refs.decrement(i);

        Assert.Equal(0, refs.count);
        Assert.False(refs.is_dense);
        for (int i = 0; i < 300; i++)
        {
            var id = refs.allocate();
            Assert.InRange(id, 0, 299);
            Assert.True(refs.isValid(id));
        }

        Assert.Equal(300, refs.count);
        Assert.True(refs.is_dense);
    }

    [Fact]
    public void RefCountVector_CopyAndRebuildPreserveFreeIndexSemantics()
    {
        var refs = new RefCountVector();
        for (int i = 0; i < 120; i++)
            refs.allocate();
        for (int i = 0; i < 120; i += 2)
            refs.decrement(i);

        var copy = new RefCountVector(refs);
        Assert.Equal(refs.count, copy.count);
        Assert.False(copy.is_dense);
        copy.rebuild_free_list();
        Assert.Equal(60, copy.count);
        for (int i = 0; i < 60; i++)
            Assert.InRange(copy.allocate(), 0, 119);
        Assert.Equal(120, copy.count);
    }

    [Fact]
    public void SmallListSet_LinkedOverflowAndBlockReuseSurviveGrowth()
    {
        var lists = new SmallListSet();
        lists.AllocateAt(0);
        for (int i = 0; i < 200; i++)
            lists.Insert(0, i);

        Assert.Equal(200, lists.Count(0));
        Assert.Equal(200, lists.ValueItr(0).Distinct().Count());
        Assert.True(lists.Contains(0, 199));
        Assert.True(lists.Remove(0, 101));
        Assert.False(lists.Contains(0, 101));
        Assert.Equal(199, lists.Count(0));

        var copy = new SmallListSet(lists);
        Assert.Equal(199, copy.Count(0));
        Assert.True(copy.Contains(0, 199));

        lists.Clear(0);
        Assert.Equal(0, lists.Count(0));
        lists.AllocateAt(0);
        for (int i = 200; i < 400; i++)
            lists.Insert(0, i);
        Assert.Equal(200, lists.Count(0));
        Assert.True(lists.Contains(0, 399));
    }

    [Fact]
    public void SmallListSet_FreeBlockBufferGrowsBeyondOneBlock()
    {
        var lists = new SmallListSet();
        for (int i = 0; i < 150; i++)
        {
            lists.AllocateAt(i);
            lists.Insert(i, i + 1000);
        }
        for (int i = 0; i < 150; i++)
            lists.Clear(i);
        for (int i = 0; i < 150; i++)
        {
            lists.AllocateAt(i);
            lists.Insert(i, i + 2000);
            Assert.Equal(i + 2000, lists.First(i));
        }
    }

    [Fact]
    public void StaticNtMeshBuildPreservesTriangleAndRayQueries()
    {
        var mesh = new NTMesh3(false, false, false, false);
        int a = mesh.AppendVertex(new Vector3d(0, 0, 0));
        int b = mesh.AppendVertex(new Vector3d(1, 0, 0));
        int c = mesh.AppendVertex(new Vector3d(0, 1, 0));
        int t = mesh.AppendTriangle(a, b, c);

        Assert.True(mesh.IsTriangle(t));
        Assert.Equal(3, mesh.VertexCount);
        Assert.Equal(1, mesh.TriangleCount);

        var tree = new NTMeshAABBTree3(mesh);
        tree.Build();
        var ray = new Ray3d(new Vector3d(0.25, 0.25, 1), new Vector3d(0, 0, -1));
        Assert.Equal(t, tree.FindNearestHitTriangle(ray));
    }
}
