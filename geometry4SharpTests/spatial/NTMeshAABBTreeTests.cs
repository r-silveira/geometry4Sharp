using g4;
using Shouldly;

namespace geometry4SharpTests.spatial
{
    public class NTMeshAABBTreeTests
    {
        #region Test Data

        private static NTMesh3 TestData()
        {
            var mesh = new NTMesh3();

            var a1 = mesh.AppendVertex(new Vector3d(0.0, 1.0, 2.0));
            var b1 = mesh.AppendVertex(new Vector3d(-1.0, 0.0, 2.0));
            var c1 = mesh.AppendVertex(new Vector3d(1.0, 0.0, 2.0));

            var a2 = mesh.AppendVertex(new Vector3d(-3.0, 1.0, 2.0));
            var b2 = mesh.AppendVertex(new Vector3d(-4.0, 0.0, 2.0));
            var c2 = mesh.AppendVertex(new Vector3d(-2.0, 0.0, 2.0));

            var a3 = mesh.AppendVertex(new Vector3d(-3.0, 3.0, 2.0));
            var b3 = mesh.AppendVertex(new Vector3d(-4.0, 2.0, 2.0));
            var c3 = mesh.AppendVertex(new Vector3d(-2.0, 2.0, 2.0));

            var a4 = mesh.AppendVertex(new Vector3d(3.0, 1.0, 2.0));
            var b4 = mesh.AppendVertex(new Vector3d(2.0, 0.0, 2.0));
            var c4 = mesh.AppendVertex(new Vector3d(4.0, 0.0, 2.0));

            var a5 = mesh.AppendVertex(new Vector3d(3.0, 3.0, 2.0));
            var b5 = mesh.AppendVertex(new Vector3d(2.0, 2.0, 2.0));
            var c5 = mesh.AppendVertex(new Vector3d(4.0, 2.0, 2.0));

            mesh.AppendTriangle(a1, b1, c1);
            mesh.AppendTriangle(a2, b2, c2);
            mesh.AppendTriangle(a3, b3, c3);
            mesh.AppendTriangle(a4, b4, c4);
            mesh.AppendTriangle(a5, b5, c5);

            return mesh;
        }

        #endregion

        [Theory]
        [InlineData(0.0, 0.5, 2.0, 0)]
        [InlineData(-3.0, 0.5, 2.0, 1)]
        [InlineData(-3.0, 2.5, 2.0, 2)]
        [InlineData(3.0, 0.5, 2.0, 3)]
        [InlineData(3.0, 2.5, 2.0, 4)]
        [InlineData(0.0, 0.5, 0.0, 0)]
        [InlineData(-3.0, 0.5, 0.0, 1)]
        [InlineData(-3.0, 2.5, 0.0, 2)]
        [InlineData(3.0, 0.5, 0.0, 3)]
        [InlineData(3.0, 2.5, 0.0, 4)]
        public void NTMeshAABBTree_FindNearestTriangle_ShouldReturnExpectedTriangle(double x, double y, double z, int tid)
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var queryPoint = new Vector3d(x, y, z);

            tree.FindNearestTriangle(queryPoint).ShouldBe(tid);
            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Theory]
        [InlineData(0.0, 0.5, 2.0, 0.0)]
        [InlineData(-3.0, 0.5, 2.0, 0.0)]
        [InlineData(-3.0, 2.5, 2.0, 0.0)]
        [InlineData(3.0, 0.5, 2.0, 0.0)]
        [InlineData(3.0, 2.5, 2.0, 0.0)]
        [InlineData(0.0, 0.5, 0.0, 2.0)]
        [InlineData(-3.0, 0.5, 0.0, 2.0)]
        [InlineData(-3.0, 2.5, 0.0, 2.0)]
        [InlineData(3.0, 0.5, 0.0, 2.0)]
        [InlineData(3.0, 2.5, 0.0, 2.0)]
        public void NTMeshAABBTree_FindNearestTriangle_DistanceShouldBeExpectedValue(double x, double y, double z, double dist)
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var queryPoint = new Vector3d(x, y, z);

            tree.FindNearestTriangle(queryPoint, out var nearestDistSqr);
            nearestDistSqr.ShouldBe(dist * dist, 1e-6);

            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Theory]
        [InlineData(0.0, 0.5, 2.0, 0.000001, 0)]
        [InlineData(-3.0, 0.5, 2.0, 0.000001, 1)]
        [InlineData(-3.0, 2.5, 2.0, 0.000001, 2)]
        [InlineData(3.0, 0.5, 2.0, 0.000001, 3)]
        [InlineData(3.0, 2.5, 2.0, 0.000001, 4)]
        [InlineData(0.0, 0.5, 0.0, 0.000001, NTMesh3.InvalidID)]
        [InlineData(-3.0, 0.5, 0.0, 0.000001, NTMesh3.InvalidID)]
        [InlineData(-3.0, 2.5, 0.0, 0.000001, NTMesh3.InvalidID)]
        [InlineData(3.0, 0.5, 0.0, 0.000001, NTMesh3.InvalidID)]
        [InlineData(3.0, 2.5, 0.0, 0.000001, NTMesh3.InvalidID)]
        [InlineData(0.0, 0.5, 0.0, 2.000001, 0)]
        [InlineData(-3.0, 0.5, 0.0, 2.000001, 1)]
        [InlineData(-3.0, 2.5, 0.0, 2.000001, 2)]
        [InlineData(3.0, 0.5, 0.0, 2.000001, 3)]
        [InlineData(3.0, 2.5, 0.0, 2.000001, 4)]
        public void NTMeshAABBTree_FindNearestTriangleWithinMaximumDistance_ShouldReturnExpectedTriangle(double x, double y, double z, double maxDist, int tid)
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var queryPoint = new Vector3d(x, y, z);

            tree.FindNearestTriangle(queryPoint, maxDist).ShouldBe(tid);
            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Theory]
        [InlineData(0.0, 1.0, 0.0, NTMesh3.InvalidID)]
        [InlineData(0.0, 0.5, 2.0, 0)]
        [InlineData(-3.0, 0.5, 2.0, 1)]
        [InlineData(-3.0, 2.5, 2.0, 2)]
        [InlineData(3.0, 0.5, 2.0, 3)]
        [InlineData(3.0, 2.5, 2.0, 4)]
        public void NTMeshAABBTree_FindNearestHitTriangle_ShouldReturnExpectedTriangle(double tx, double ty, double tz, int tid)
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var origin = Vector3f.Zero;
            var target = new Vector3f(tx, ty, tz);
            var ray = new Ray3d(origin, target - origin);

            tree.FindNearestHitTriangle(ray).ShouldBe(tid);
            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Theory]
        [InlineData(0.0, 0.5, 2.0, 1e-6, 0)]
        [InlineData(-3.0, 0.5, 2.0, 1e-6, 1)]
        [InlineData(-3.0, 2.5, 2.0, 1e-6, 2)]
        [InlineData(3.0, 0.5, 2.0, 1e-6, 3)]
        [InlineData(3.0, 2.5, 2.0, 1e-6, 4)]
        [InlineData(0.0, 0.5, 0.0, -1e-6, NTMesh3.InvalidID)]
        [InlineData(-3.0, 0.5, 0.0, -1e-6, NTMesh3.InvalidID)]
        [InlineData(-3.0, 2.5, 0.0, -1e-6, NTMesh3.InvalidID)]
        [InlineData(3.0, 0.5, 0.0, -1e-6, NTMesh3.InvalidID)]
        [InlineData(3.0, 2.5, 0.0, -1e-6, NTMesh3.InvalidID)]
        public void NTMeshAABBTree_FindNearestHitTriangleWithinMaximumDistance_ShouldReturnExpectedTriangle(double tx, double ty, double tz, double maxDist, int tid)
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var origin = Vector3f.Zero;
            var target = new Vector3f(tx * 2, ty * 2, tz * 2);
            var ray = new Ray3d(Vector3f.Zero, target - origin);

            tree.FindNearestHitTriangle(ray, target.Length + maxDist).ShouldBe(tid);
            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }


        [Fact]
        public void NTMeshAABBTree_FindNearestHitTriangle_NotHandleCoplanarRays_ShouldReturnInvalidTriangle()
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var origin = new Vector3f(-5.0, 0.0, 2.0);
            var target = new Vector3f(0.0, 0.0, 2.0);
            var ray = new Ray3d(origin, target - origin);

            tree.FindNearestHitTriangle(ray).ShouldBe(NTMesh3.InvalidID);
            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Fact]
        public void NTMeshAABBTree_FindNearestHitTriangle_HandleCoplanarRays_ShouldReturnInvalidTriangle()
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var origin = new Vector3f(-5.0, 0.0, 2.0);
            var target = new Vector3f(0.0, 0.0, 2.0);
            var ray = new Ray3d(origin, target - origin);

            tree.FindNearestHitTriangle(ray, handleCoplanarRays: true).ShouldBe(1);
            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Fact]
        public void NTMeshAABBTree_FindAllHitTriangles_NotHandleCoplanarRays_ShouldReturnInvalidTriangle()
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var origin = new Vector3f(-5.0, 0.0, 2.0);
            var target = new Vector3f(0.0, 0.0, 2.0);
            var ray = new Ray3d(origin, target - origin);

            var hitTriangles = new List<int>();
            tree.FindAllHitTriangles(ray, hitTriangles).ShouldBe(0);
            hitTriangles.Count().ShouldBe(0);

            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Fact]
        public void NTMeshAABBTree_FindAllHitTrianglesWithinMaximumDistance_HandleCoplanarRays_ShouldReturnInvalidTriangle()
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var origin = new Vector3f(-5.0, 0.0, 2.0);
            var target = new Vector3f(0.0, 0.0, 2.0);
            var ray = new Ray3d(origin, target - origin);

            var maxDist = 1.000001;
            var hitTriangles = new List<int>();
            tree.FindAllHitTriangles(ray, hitTriangles, maxDist, handleCoplanarRays: true).ShouldBe(1);
            hitTriangles.Count.ShouldBe(1);
            hitTriangles.Contains(1).ShouldBe(true);

            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Theory]
        [InlineData(0.0, 1.5, 0.0, 0.5, 0)]
        [InlineData(-3.0, 1.5, 0.0, 0.5, 1)]
        [InlineData(-3.0, 3.5, 0.0, 0.5, 2)]
        [InlineData(3.0, 1.5, 0.0, 0.5, 3)]
        [InlineData(3.0, 3.5, 0.0, 0.5, 4)]
        [InlineData(0.0, 1.5, 0.0, 0.499999, NTMesh3.InvalidID)]
        [InlineData(-3.0, 1.5, 0.0, 0.499999, NTMesh3.InvalidID)]
        [InlineData(-3.0, 3.5, 0.0, 0.499999, NTMesh3.InvalidID)]
        [InlineData(3.0, 1.5, 0.0, 0.499999, NTMesh3.InvalidID)]
        [InlineData(3.0, 3.5, 0.0, 0.499999, NTMesh3.InvalidID)]
        public void NTMeshAABBTree_FindNearestBeamHitTriangle_ShouldReturnExpectedTriangle(double ox, double oy, double oz, double radius, int tid)
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var origin = new Vector3d(ox, oy, oz);
            var direction = Vector3d.AxisZ;
            var ray = new Ray3d(origin, direction);

            tree.FindNearestBeamHitTriangle(ray, radius).ShouldBe(tid);
            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }

        [Theory]
        [InlineData(0.0, 1.5, 0.0, 0.5, 2.000001, 0)]
        [InlineData(-3.0, 1.5, 0.0, 0.5, 2.000001, 1)]
        [InlineData(-3.0, 3.5, 0.0, 0.5, 2.000001, 2)]
        [InlineData(3.0, 1.5, 0.0, 0.5, 2.000001, 3)]
        [InlineData(3.0, 3.5, 0.0, 0.5, 2.000001, 4)]
        [InlineData(0.0, 1.5, 0.0, 0.5, 2.0, NTMesh3.InvalidID)]
        [InlineData(-3.0, 1.5, 0.0, 0.5, 2.0, NTMesh3.InvalidID)]
        [InlineData(-3.0, 3.5, 0.0, 0.5, 2.0, NTMesh3.InvalidID)]
        [InlineData(3.0, 1.5, 0.0, 0.5, 2.0, NTMesh3.InvalidID)]
        [InlineData(3.0, 3.5, 0.0, 0.5, 2.0, NTMesh3.InvalidID)]
        public void NTMeshAABBTree_FindNearestBeamHitTriangleWithinMaximumDistance_ShouldReturnExpectedTriangle(double ox, double oy, double oz, double radius, double maxDistance, int tid)
        {
            var mesh = TestData();
            var tree = new NTMeshAABBTree3(mesh, true);

            var origin = new Vector3d(ox, oy, oz);
            var direction = Vector3d.AxisZ;
            var ray = new Ray3d(origin, direction);

            tree.FindNearestBeamHitTriangle(ray, radius, maxDistance).ShouldBe(tid);
            mesh.CheckValidity(FailMode.ReturnOnly).ShouldBe(true);
        }
    }
}