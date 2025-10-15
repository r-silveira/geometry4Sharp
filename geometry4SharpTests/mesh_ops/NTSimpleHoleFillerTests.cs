using Shouldly;

using g4;
using gs;

namespace geometry4SharpTests.mesh_ops
{
    public class NTSimpleHoleFillerTests
    {
        #region Test Data
        private static NTMesh3 CreateCube()
        {
            var mesh = new NTMesh3();

            var v0 = mesh.AppendVertex(new Vector3d(-1.0, 1.0, -1.0));
            var v1 = mesh.AppendVertex(new Vector3d(-1.0, 1.0, 1.0));
            var v2 = mesh.AppendVertex(new Vector3d(1.0, 1.0, -1.0));
            var v3 = mesh.AppendVertex(new Vector3d(1.0, 1.0, 1.0));

            var v4 = mesh.AppendVertex(new Vector3d(-1.0, -1.0, -1.0));
            var v5 = mesh.AppendVertex(new Vector3d(-1.0, -1.0, 1.0));
            var v6 = mesh.AppendVertex(new Vector3d(1.0, -1.0, -1.0));
            var v7 = mesh.AppendVertex(new Vector3d(1.0, -1.0, 1.0));

            mesh.AppendTriangle(v3, v2, v0);
            mesh.AppendTriangle(v0, v1, v3);
            mesh.AppendTriangle(v4, v6, v7);
            mesh.AppendTriangle(v7, v5, v4);

            mesh.AppendTriangle(v6, v2, v3);
            mesh.AppendTriangle(v3, v7, v6);
            mesh.AppendTriangle(v1, v0, v4);
            mesh.AppendTriangle(v4, v5, v1);

            mesh.AppendTriangle(v5, v7, v3);
            mesh.AppendTriangle(v3, v1, v5);
            mesh.AppendTriangle(v2, v6, v4);
            mesh.AppendTriangle(v4, v0, v2);

            return mesh;
        }

        private static NTMesh3 CreateCubeWithOneMissingTriangle()
        {
            var mesh = new NTMesh3();

            var v0 = mesh.AppendVertex(new Vector3d(-1.0, 1.0, -1.0));
            var v1 = mesh.AppendVertex(new Vector3d(-1.0, 1.0, 1.0));
            var v2 = mesh.AppendVertex(new Vector3d(1.0, 1.0, -1.0));
            var v3 = mesh.AppendVertex(new Vector3d(1.0, 1.0, 1.0));

            var v4 = mesh.AppendVertex(new Vector3d(-1.0, -1.0, -1.0));
            var v5 = mesh.AppendVertex(new Vector3d(-1.0, -1.0, 1.0));
            var v6 = mesh.AppendVertex(new Vector3d(1.0, -1.0, -1.0));
            var v7 = mesh.AppendVertex(new Vector3d(1.0, -1.0, 1.0));

            mesh.AppendTriangle(v0, v1, v3);
            mesh.AppendTriangle(v4, v6, v7);
            mesh.AppendTriangle(v7, v5, v4);

            mesh.AppendTriangle(v6, v2, v3);
            mesh.AppendTriangle(v3, v7, v6);
            mesh.AppendTriangle(v1, v0, v4);
            mesh.AppendTriangle(v4, v5, v1);

            mesh.AppendTriangle(v5, v7, v3);
            mesh.AppendTriangle(v3, v1, v5);
            mesh.AppendTriangle(v2, v6, v4);
            mesh.AppendTriangle(v4, v0, v2);

            return mesh;
        }

        private static NTMesh3 CreateCubeWithTwoMissingTriangles()
        {
            var mesh = new NTMesh3();

            var v0 = mesh.AppendVertex(new Vector3d(-1.0, 1.0, -1.0));
            var v1 = mesh.AppendVertex(new Vector3d(-1.0, 1.0, 1.0));
            var v2 = mesh.AppendVertex(new Vector3d(1.0, 1.0, -1.0));
            var v3 = mesh.AppendVertex(new Vector3d(1.0, 1.0, 1.0));

            var v4 = mesh.AppendVertex(new Vector3d(-1.0, -1.0, -1.0));
            var v5 = mesh.AppendVertex(new Vector3d(-1.0, -1.0, 1.0));
            var v6 = mesh.AppendVertex(new Vector3d(1.0, -1.0, -1.0));
            var v7 = mesh.AppendVertex(new Vector3d(1.0, -1.0, 1.0));

            mesh.AppendTriangle(v4, v6, v7);
            mesh.AppendTriangle(v7, v5, v4);

            mesh.AppendTriangle(v6, v2, v3);
            mesh.AppendTriangle(v3, v7, v6);
            mesh.AppendTriangle(v1, v0, v4);
            mesh.AppendTriangle(v4, v5, v1);

            mesh.AppendTriangle(v5, v7, v3);
            mesh.AppendTriangle(v3, v1, v5);
            mesh.AppendTriangle(v2, v6, v4);
            mesh.AppendTriangle(v4, v0, v2);

            return mesh;
        }
        #endregion

        [Fact]
        public void NTSimpleHoleFiller_FillHoles_Cube()
        {
            var mesh = CreateCube();
            var timestamp = mesh.Timestamp;
            var numTriangles = mesh.TriangleCount;

            NTSimpleHoleFiller.FillHoles(mesh);

            // Mesh should still be the same as the original
            mesh.Timestamp.ShouldBe(timestamp);
            mesh.TriangleCount.ShouldBe(numTriangles);
            mesh.IsClosed().ShouldBeTrue();
        }

        [Fact]
        public void NTSimpleHoleFiller_FillHoles_CubeWithOneMissingTriangle()
        {
            var mesh = CreateCubeWithOneMissingTriangle();
            var timestamp = mesh.Timestamp;
            var numTriangles = mesh.TriangleCount;

            NTSimpleHoleFiller.FillHoles(mesh);

            // A single triangle was missing, 
            mesh.Timestamp.ShouldBe(timestamp + 1);
            mesh.TriangleCount.ShouldBe(numTriangles + 1);
            mesh.IsClosed().ShouldBeTrue();
        }

        [Fact]
        public void NTSimpleHoleFiller_FillHoles_CubeWithTwoMissingTriangles()
        {
            var mesh = CreateCubeWithTwoMissingTriangles();
            var timestamp = mesh.Timestamp;
            var numTriangles = mesh.TriangleCount;

            NTSimpleHoleFiller.FillHoles(mesh);

            // Both triangles of the top face were missing, resulting in a 4 vertices loop.
            // The algorithm fills the hole by adding a new vertex at its cetroid, and
            // generating a triangle fan with the loop vertices, resulting in 4 new
            // triangles.
            mesh.Timestamp.ShouldBe(timestamp + 5);         // 4 new triangles and 1 new vertex
            mesh.TriangleCount.ShouldBe(numTriangles + 4);  // 4 new triangles (triangle fan)
            mesh.IsClosed().ShouldBeTrue();
        }
    }
}