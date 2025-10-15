using Shouldly;

using g4;

namespace geometry4SharpTests.mesh_selection
{
    public class NTMeshConnectedComponentsTests
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

        [Fact]
        public void NTMeshConnectedComponents_Separate_Cube()
        {
            var mesh = CreateCube();
            var components = NTMeshConnectedComponents.Separate(mesh);
            components.Length.ShouldBe(1);
        }

        [Fact]
        public void NTMeshConnectedComponents_Separate_CubeByCoplanarTriangles()
        {
            bool AdjacencyFilterFunction(Triangle3d t0, Triangle3d t1)
            {
                return t0.Normal.Dot(t1.Normal) >= 0.9999;
            }

            var mesh = CreateCube();
            var components = NTMeshConnectedComponents.Separate(mesh, AdjacencyFilterFunction);
            components.Length.ShouldBe(6);
        }
    }
}