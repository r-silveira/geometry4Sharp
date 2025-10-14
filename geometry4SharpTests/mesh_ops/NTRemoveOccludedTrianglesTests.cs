using Shouldly;

using g4;
using gs;

namespace geometry4SharpTests.mesh_ops
{
    public class NTRemoveOccludedTrianglesTests
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

        private static NTMesh3 CreateCubeWithTriangleInside()
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

            var v8 = mesh.AppendVertex(new Vector3d(0.0, 0.0, 0.0));
            var v9 = mesh.AppendVertex(new Vector3d(0.5, 0.0, 0.0));
            var v10 = mesh.AppendVertex(new Vector3d(0.0, 0.0, 0.5));

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

            mesh.AppendTriangle(v8, v9, v10);

            return mesh;
        }
        #endregion

        [Fact]
        public void NTRemoveOccludedTriangles_Cube()
        {
            var mesh = CreateCube();
            var timestamp = mesh.Timestamp;
            var numTriangles = mesh.TriangleCount;

            var repair = new NTRemoveOccludedTriangles(mesh);
            repair.Apply();

            // Mesh should still be the same as the original
            mesh.Timestamp.ShouldBe(timestamp);
            mesh.TriangleCount.ShouldBe(numTriangles);
        }

        [Fact]
        public void NTRemoveOccludedTriangles_CubeWithTriangleInside()
        {
            var mesh = CreateCubeWithTriangleInside();
            var timestamp = mesh.Timestamp;
            var numTriangles = mesh.TriangleCount;

            var repair = new NTRemoveOccludedTriangles(mesh);
            repair.Apply();

            // One triangle from the mesh should be removed
            mesh.Timestamp.ShouldBe(timestamp + 1);
            mesh.TriangleCount.ShouldBe(numTriangles - 1);
        }
    }
}