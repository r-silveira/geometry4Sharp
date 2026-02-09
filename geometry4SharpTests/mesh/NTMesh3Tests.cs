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

        #endregion

        [Fact]
        public void Test1()
        {
            var mesh = TestData1();
            mesh.CollapseEdge(1, 0, out var _);
            mesh.TriangleCount.ShouldBe(0);
        }

        [Fact]
        public void Test2()
        {
            var mesh = TestData2();
            mesh.CollapseEdge(1, 0, out var _);
            mesh.TriangleCount.ShouldBe(1);
        }

        [Fact]
        public void Test3()
        {
            var mesh = TestData3();
            mesh.CollapseEdge(1, 0, out var _);
            mesh.TriangleCount.ShouldBe(3);
        }

    }
}