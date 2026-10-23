using System;
using System.Collections.Generic;
using Xunit;
using g4;

namespace geometry4SharpTests.mesh
{
    public class NtMeshCompactStorageTests
    {
        [Theory]
        [InlineData(64)]
        [InlineData(256)]
        [InlineData(2048)]
        public void CompactAndDefaultMeshesHaveSameGeometry(int blockSize)
        {
            var baseline = new NTMesh3(false, false, false, false);
            var compact = new NTMesh3(blockSize, false, false, false, false);

            for (int i = 0; i < 250; ++i)
            {
                var v = new Vector3d(i * .1, i % 3, i % 7);
                Assert.Equal(baseline.AppendVertex(v), compact.AppendVertex(v));
            }

            for (int i = 0; i + 2 < 250; i += 3)
                Assert.Equal(baseline.AppendTriangle(i, i + 1, i + 2),
                             compact.AppendTriangle(i, i + 1, i + 2));

            Assert.Equal(baseline.VertexCount, compact.VertexCount);
            Assert.Equal(baseline.TriangleCount, compact.TriangleCount);

            for (int i = 0; i < baseline.VertexCount; i++)
                Assert.Equal(baseline.GetVertex(i), compact.GetVertex(i));
            for (int i = 0; i < baseline.TriangleCount; i++)
            {
                Assert.Equal(baseline.GetTriangle(i), compact.GetTriangle(i));
                Assert.Equal(baseline.GetTriCentroid(i), compact.GetTriCentroid(i));
                Assert.Equal(baseline.GetTriBounds(i).Center,
                             compact.GetTriBounds(i).Center);
            }
        }

        [Theory]
        [InlineData(64)]
        [InlineData(256)]
        public void CompactMeshCanRemoveAndReuseTriangles(int blockSize)
        {
            var mesh = new NTMesh3(blockSize, false, false, false, false);
            int a = mesh.AppendVertex(new Vector3d(0, 0, 0));
            int b = mesh.AppendVertex(new Vector3d(1, 0, 0));
            int c = mesh.AppendVertex(new Vector3d(0, 1, 0));
            int tri = mesh.AppendTriangle(a, b, c);
            Assert.NotEqual(NTMesh3.InvalidID, tri);
            Assert.Equal(MeshResult.Ok, mesh.RemoveTriangle(tri, false));
            int added = mesh.AppendTriangle(a, b, c);
            Assert.NotEqual(NTMesh3.InvalidID, added);
            Assert.Equal(1, mesh.TriangleCount);
        }

        [Fact]
        public void InvalidSizesThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new NTMesh3(63, false, false, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new NTMesh3(0, false, false, false, false));
        }
    }
}
