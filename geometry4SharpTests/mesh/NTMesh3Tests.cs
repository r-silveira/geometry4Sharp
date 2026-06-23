using g4;
using Shouldly;

namespace geometry4SharpTests.mesh
{
    public class NTMesh3Tests
    {
        #region Test Data

        private static NTMesh3 TestData1()
        {
            var mesh = new NTMesh3();

            var a = mesh.AppendVertex(new Vector3d(0.0, 1.0, 0.0));
            var b = mesh.AppendVertex(new Vector3d(0.0, -1.0, 0.0));
            var c = mesh.AppendVertex(new Vector3d(-1.0, 0.0, 0.0));
            var d = mesh.AppendVertex(new Vector3d(1.0, 0.0, 0.0));

            mesh.AppendTriangle(a, b, d);
            mesh.AppendTriangle(a, c, b);

            return mesh;
        }

        private static NTMesh3 TestData2()
        {
            var mesh = new NTMesh3();

            var a = mesh.AppendVertex(new Vector3d(0.0, 1.0, 0.0));
            var b = mesh.AppendVertex(new Vector3d(0.0, -1.0, 0.0));
            var c = mesh.AppendVertex(new Vector3d(-1.0, 0.0, 0.0));
            var d = mesh.AppendVertex(new Vector3d(1.0, 0.0, 0.0));
            var e = mesh.AppendVertex(new Vector3d(-0.5, 1.5, 0.0));

            mesh.AppendTriangle(a, b, d);
            mesh.AppendTriangle(a, c, b);
            mesh.AppendTriangle(a, e, c);

            return mesh;
        }

        private static NTMesh3 TestData3()
        {
            var mesh = new NTMesh3();

            var a = mesh.AppendVertex(new Vector3d(0.0, 1.0, 0.0));
            var b = mesh.AppendVertex(new Vector3d(0.0, -1.0, 0.0));
            var c = mesh.AppendVertex(new Vector3d(-1.0, 0.0, 0.0));
            var d = mesh.AppendVertex(new Vector3d(1.0, 0.0, 0.0));
            var e = mesh.AppendVertex(new Vector3d(-0.5, 1.5, 0.0));

            var x = mesh.AppendVertex(new Vector3d(0.0, 0.0, 1.0));
            var y = mesh.AppendVertex(new Vector3d(0.0, 1.0, 1.0));
            var z = mesh.AppendVertex(new Vector3d(0.0, -1.0, 1.0));

            mesh.AppendTriangle(a, b, d);
            mesh.AppendTriangle(a, c, b);
            mesh.AppendTriangle(a, e, c);

            mesh.AppendTriangle(a, x, y);
            mesh.AppendTriangle(b, z, x);

            return mesh;
        }

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
        public void NTMesh3_CollapseEdge_Test1()
        {
            var mesh = TestData1();
            mesh.CollapseEdge(1, 0, out var collapseInfo);
            mesh.TriangleCount.ShouldBe(0);

            collapseInfo.eRemoved.Count.ShouldBe(5);
            collapseInfo.tRemoved.Count.ShouldBe(2);

            var vertexIndices = mesh.VertexIndices().ToList();
            var edgeIndices = mesh.EdgeIndices().ToList();
            var triangleIndices = mesh.TriangleIndices().ToList();

            vertexIndices.Count.ShouldBe(0);
            edgeIndices.Count.ShouldBe(0);
            triangleIndices.Count.ShouldBe(0);

            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Fact]
        public void NTMesh3_CollapseEdge_Test2()
        {
            var mesh = TestData2();
            mesh.CollapseEdge(1, 0, out var collapseInfo);
            mesh.TriangleCount.ShouldBe(1);

            collapseInfo.eRemoved.Count.ShouldBe(4);
            collapseInfo.tRemoved.Count.ShouldBe(2);

            var vertexIndices = mesh.VertexIndices().ToList();
            var edgeIndices = mesh.EdgeIndices().ToList();
            var triangleIndices = mesh.TriangleIndices().ToList();

            var vertexRefcounts = vertexIndices.Select(mesh.vertices_refcount.refCount).ToList();

            vertexIndices.Count.ShouldBe(3);
            edgeIndices.Count.ShouldBe(3);
            triangleIndices.Count.ShouldBe(1);

            var vertexEdges = vertexIndices.Select(vid => mesh.vertex_edges.ValueItr(vid).ToList()).ToList();
            var edgeTriangles = edgeIndices.Select(eid => mesh.edge_triangles.ValueItr(eid).ToList()).ToList();

            vertexEdges.Count.ShouldBe(3);
            edgeTriangles.Count.ShouldBe(3);

            var edgesV0 = vertexEdges[0];
            var edgesV1 = vertexEdges[1];
            var edgesV2 = vertexEdges[2];

            var trianglesE0 = edgeTriangles[0];
            var trianglesE1 = edgeTriangles[1];
            var trianglesE2 = edgeTriangles[2];

            edgesV0.Count.ShouldBe(2);
            edgesV1.Count.ShouldBe(2);
            edgesV2.Count.ShouldBe(2);

            trianglesE0.Count.ShouldBe(1);
            trianglesE1.Count.ShouldBe(1);
            trianglesE2.Count.ShouldBe(1);

            edgesV0.Contains(4).ShouldBe(true);
            edgesV0.Contains(5).ShouldBe(true);

            edgesV1.Contains(4).ShouldBe(true);
            edgesV1.Contains(6).ShouldBe(true);

            edgesV2.Contains(5).ShouldBe(true);
            edgesV2.Contains(6).ShouldBe(true);

            trianglesE0.Contains(2);
            trianglesE1.Contains(2);
            trianglesE2.Contains(2);

            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Fact]
        public void NTMesh3_CollapseEdge_Test3()
        {
            var mesh = TestData3();
            mesh.CollapseEdge(1, 0, out var collapseInfo);
            mesh.TriangleCount.ShouldBe(3);

            collapseInfo.eRemoved.Count.ShouldBe(5);
            collapseInfo.tRemoved.Count.ShouldBe(2);

            var vertexIndices = mesh.VertexIndices().ToList();
            var edgeIndices = mesh.EdgeIndices().ToList();
            var triangleIndices = mesh.TriangleIndices().ToList();

            vertexIndices.Count.ShouldBe(6);
            edgeIndices.Count.ShouldBe(8);
            triangleIndices.Count.ShouldBe(3);

            var vertexEdges = vertexIndices.Select(vid => mesh.vertex_edges.ValueItr(vid).ToList()).ToList();
            var edgeTriangles = edgeIndices.Select(eid => mesh.edge_triangles.ValueItr(eid).ToList()).ToList();

            vertexEdges.Count.ShouldBe(6);
            edgeTriangles.Count.ShouldBe(8);

            var edgesV0 = vertexEdges[0];
            var edgesV1 = vertexEdges[1];
            var edgesV2 = vertexEdges[2];
            var edgesV3 = vertexEdges[3];
            var edgesV4 = vertexEdges[4];
            var edgesV5 = vertexEdges[5];

            var trianglesE0 = edgeTriangles[0];
            var trianglesE1 = edgeTriangles[1];
            var trianglesE2 = edgeTriangles[2];
            var trianglesE3 = edgeTriangles[3];
            var trianglesE4 = edgeTriangles[4];
            var trianglesE5 = edgeTriangles[5];
            var trianglesE6 = edgeTriangles[6];
            var trianglesE7 = edgeTriangles[7];

            edgesV0.Count.ShouldBe(5);
            edgesV1.Count.ShouldBe(2);
            edgesV2.Count.ShouldBe(2);
            edgesV3.Count.ShouldBe(3);
            edgesV4.Count.ShouldBe(2);
            edgesV5.Count.ShouldBe(2);

            trianglesE0.Count.ShouldBe(1);
            trianglesE1.Count.ShouldBe(1);
            trianglesE2.Count.ShouldBe(1);
            trianglesE3.Count.ShouldBe(1);
            trianglesE4.Count.ShouldBe(1);
            trianglesE5.Count.ShouldBe(1);
            trianglesE6.Count.ShouldBe(1);
            trianglesE7.Count.ShouldBe(2);

            edgesV0.Contains(4).ShouldBe(true);
            edgesV0.Contains(5).ShouldBe(true);
            edgesV0.Contains(9).ShouldBe(true);
            edgesV0.Contains(10).ShouldBe(true);
            edgesV0.Contains(12).ShouldBe(true);

            edgesV1.Contains(4).ShouldBe(true);
            edgesV1.Contains(6).ShouldBe(true);

            edgesV2.Contains(5).ShouldBe(true);
            edgesV2.Contains(6).ShouldBe(true);

            edgesV3.Contains(8).ShouldBe(true);
            edgesV3.Contains(11).ShouldBe(true);
            edgesV3.Contains(12).ShouldBe(true);

            edgesV4.Contains(8).ShouldBe(true);
            edgesV4.Contains(9).ShouldBe(true);

            edgesV5.Contains(10).ShouldBe(true);
            edgesV5.Contains(11).ShouldBe(true);

            trianglesE0.Contains(2);
            trianglesE1.Contains(2);
            trianglesE2.Contains(2);

            trianglesE3.Contains(3);
            trianglesE4.Contains(3);

            trianglesE5.Contains(4);
            trianglesE6.Contains(4);
            
            trianglesE7.Contains(3);
            trianglesE7.Contains(4);

            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Fact]
        public void NTMesh3_ComputeNormals_NormalsShouldHaveExpectedValue()
        {
            var mesh = CreateCube();
            mesh.ComputeNormals();

            var n0 = new Vector3f(-0.40824828, 0.81649655, -0.40824828);
            var n1 = new Vector3f(-0.81649655, 0.40824828, 0.40824828);
            var n2 = new Vector3f(0.40824828, 0.40824828, -0.81649655);
            var n3 = new Vector3f(0.57735026, 0.57735026, 0.57735026);
            var n4 = new Vector3f(-0.57735026, -0.57735026, -0.57735026);
            var n5 = new Vector3f(-0.40824828, -0.40824828, 0.81649655);
            var n6 = new Vector3f(0.81649655, -0.40824828, -0.40824828);
            var n7 = new Vector3f(0.40824828, -0.81649655, 0.40824828);

            mesh.GetVertexNormal(0).Distance(n0).ShouldBe(0.0f, 1e-6f);
            mesh.GetVertexNormal(1).Distance(n1).ShouldBe(0.0f, 1e-6f);
            mesh.GetVertexNormal(2).Distance(n2).ShouldBe(0.0f, 1e-6f);
            mesh.GetVertexNormal(3).Distance(n3).ShouldBe(0.0f, 1e-6f);
            mesh.GetVertexNormal(4).Distance(n4).ShouldBe(0.0f, 1e-6f);
            mesh.GetVertexNormal(5).Distance(n5).ShouldBe(0.0f, 1e-6f);
            mesh.GetVertexNormal(6).Distance(n6).ShouldBe(0.0f, 1e-6f);
            mesh.GetVertexNormal(7).Distance(n7).ShouldBe(0.0f, 1e-6f);
        }
    }
}