using g4;
using Shouldly;

namespace geometry4SharpTests.mesh_ops
{
    public class NTMeshPlaneCutTests
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

        #endregion

        [Theory]
        [InlineData(0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 12.0)]                            // Splitting cube in half (y-axis)
        [InlineData(0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 24.0)]                            // Plane is coplanar with top face (same normal as face)
        [InlineData(0.0, 1.0001, 0.0, 0.0, 1.0, 0.0, 24.0)]                         // Cube is entirely behind the plane
        [InlineData(0.0, 0.9999999, 0.0, 0.0, 1.0, 0.0, 20.0)]                      // "Only" top face is in front of the plane
        [InlineData(0.0, -1.0, 0.0, 0.0, 1.0, 0.0, 4.0)]                            // Plane is coplanar with bottom face (opposite normal as face)
        [InlineData(0.0, -1.0001, 0.0, 0.0, 1.0, 0.0, 0.0)]                         // Cube is entirely in front of the plane
        [InlineData(0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 12.0)]                            // Splitting cube in half (x-axis)
        [InlineData(0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 12.0)]                            // Splitting cube in half (z-axis)
        [InlineData(0.1, 0.1, 0.1, 0.57735027, 0.57735027, 0.57735027, 13.8)]       // Splitting cube with displaced/rotated plane
        public void NTMeshPlaneCut_Cut_Cube(double cx, double cy, double cz, 
            double nx, double ny, double nz, double expectedTotalArea)
        {
            const double EPS = 1e-3;
            var mesh = CreateCube();

            var planeCenter = new Vector3d(cx, cy, cz);
            var planeNormal = new Vector3d(nx, ny, nz).Normalized;

            var planeCut = new NTMeshPlaneCut(mesh, planeCenter, planeNormal);
            planeCut.Cut();

            var totalArea = mesh
                .TriangleIndices()
                .Sum(mesh.GetTriArea);

            totalArea.ShouldBe(expectedTotalArea, EPS);
        }
    }
}