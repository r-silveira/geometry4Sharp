using Shouldly;

using g4;
using gs;

namespace geometry4SharpTests.mesh_ops
{
    public class NTMeshRepairOrientationTests
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

        private static NTMesh3 CreateCubeSingleInvertedTriangle()
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
            mesh.AppendTriangle(v3, v1, v0); // Inverted triangle
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

        private static NTMesh3 CreateInvertedCube()
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

            mesh.AppendTriangle(v0, v2, v3);
            mesh.AppendTriangle(v3, v1, v0);
            mesh.AppendTriangle(v7, v6, v4);
            mesh.AppendTriangle(v4, v5, v7);

            mesh.AppendTriangle(v3, v2, v6);
            mesh.AppendTriangle(v6, v7, v3);
            mesh.AppendTriangle(v4, v0, v1);
            mesh.AppendTriangle(v1, v5, v4);

            mesh.AppendTriangle(v3, v7, v5);
            mesh.AppendTriangle(v5, v1, v3);
            mesh.AppendTriangle(v4, v6, v2);
            mesh.AppendTriangle(v2, v0, v4);

            return mesh;
        }
        #endregion

        [Fact]
        public void NTMeshRepairOrientation_OrientComponents_Cube()
        {
            var mesh = CreateCube();
            var timestamp = mesh.Timestamp;

            var repair = new NTMeshRepairOrientation(mesh);
            repair.OrientComponents();

            // Mesh should still be the same as the original
            mesh.Timestamp.ShouldBe(timestamp);
        }

        [Fact]
        public void NTMeshRepairOrientation_OrientComponents_CubeSingleInvertedTriangle()
        {
            var mesh = CreateCubeSingleInvertedTriangle();
            var timestamp = mesh.Timestamp;

            var repair = new NTMeshRepairOrientation(mesh);
            repair.OrientComponents();

            // A single triangle must be inverted (the second one)
            mesh.Timestamp.ShouldBe(timestamp + 1);
        }

        [Fact]
        public void NTMeshRepairOrientation_OrientComponents_InvertedCube()
        {
            var mesh = CreateInvertedCube();
            var timestamp = mesh.Timestamp;

            var repair = new NTMeshRepairOrientation(mesh);
            repair.OrientComponents();

            // Mesh should still be the same as the original
            mesh.Timestamp.ShouldBe(timestamp);
        }

        [Fact]
        public void NTMeshRepairOrientation_SolveGlobalOrientation_Cube()
        {
            var mesh = CreateCube();
            var timestamp = mesh.Timestamp;

            var repair = new NTMeshRepairOrientation(mesh);
            repair.OrientComponents();
            repair.SolveGlobalOrientation();

            // All triangles face outwards, so the mesh should still be the same as the original
            mesh.Timestamp.ShouldBe(timestamp);
        }

        [Fact]
        public void NTMeshRepairOrientation_SolveGlobalOrientation_InvertedCube()
        {
            var mesh = CreateInvertedCube();
            var timestamp = mesh.Timestamp;

            var repair = new NTMeshRepairOrientation(mesh);
            repair.OrientComponents();
            mesh.Timestamp.ShouldBe(timestamp);

            repair.SolveGlobalOrientation();

            // All triangles face inwards, all 12 triangles of the cube are inverted.
            // Besides, we also invert the normal of all 8 vertices, although we do
            // not use them.
            mesh.Timestamp.ShouldBe(timestamp + 20);
        }
    }
}