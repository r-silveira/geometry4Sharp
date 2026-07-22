using System.Linq;
using g4;
using Shouldly;

namespace geometry4SharpTests.spatial
{
    public class PolygonAABBTreeTests
    {
        private static Polygon2d CreateRectanglePolygon()
        {
            return new Polygon2d(new[]
            {
                new Vector2d(-1.0, 0.0),
                new Vector2d(1.0, 0.0),
                new Vector2d(1.0, 2.0),
                new Vector2d(-1.0, 2.0)
            });
        }

        private static Polygon2d CreateTrianglePolygon()
        {
            return new Polygon2d(new[]
            {
                new Vector2d(0.0, 0.0),
                new Vector2d(2.0, 0.0),
                new Vector2d(0.0, 2.0)
            });
        }

        private static Polygon2d CreateSkinnyPolygon()
        {
            return new Polygon2d(new[]
            {
                new Vector2d(-2.0, 0.0),
                new Vector2d(2.0, 0.0),
                new Vector2d(2.0, 0.1),
                new Vector2d(-2.0, 0.1)
            });
        }

        private static Polygon2d CreateConcavePolygon()
        {
            return new Polygon2d(new[]
            {
                new Vector2d(0.0, 0.0),
                new Vector2d(3.0, 0.0),
                new Vector2d(3.0, 3.0),
                new Vector2d(1.5, 1.5),
                new Vector2d(0.0, 3.0)
            });
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldReturnExpectedHit()
        {
            var polygon = CreateRectanglePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(0.0, -1.0), new Vector2d(0.0, 1.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            segmentIndex.ShouldBe(0);
            rayT.ShouldBe(1.0, 1e-9);
            hitPoint.x.ShouldBe(0.0, 1e-9);
            hitPoint.y.ShouldBe(0.0, 1e-9);
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldHitLeftEdgeWhenRayApproachesFromOutside()
        {
            var polygon = CreateRectanglePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(-2.0, 1.0), new Vector2d(1.0, 0.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            segmentIndex.ShouldBe(3);
            rayT.ShouldBe(1.0, 1e-9);
            hitPoint.x.ShouldBe(-1.0, 1e-9);
            hitPoint.y.ShouldBe(1.0, 1e-9);
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldReturnFalseWhenRayMissesPolygon()
        {
            var polygon = CreateRectanglePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(2.5, 1.0), new Vector2d(1.0, 0.0));

            var hit = tree.FindNearestHitSegment(ray, out _, out _, out _);

            hit.ShouldBeFalse();
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldHandleRayOriginInsidePolygon()
        {
            var polygon = CreateRectanglePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(0.0, 1.0), new Vector2d(1.0, 0.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            segmentIndex.ShouldBe(1);
            rayT.ShouldBe(1.0, 1e-9);
            hitPoint.x.ShouldBe(1.0, 1e-9);
            hitPoint.y.ShouldBe(1.0, 1e-9);
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldHandleVertexTouchingRay()
        {
            var polygon = CreateRectanglePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(-2.0, 0.0), new Vector2d(1.0, 0.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            new[] { 0, 3 }.Contains(segmentIndex).ShouldBeTrue();
            rayT.ShouldBe(1.0, 1e-9);
            hitPoint.x.ShouldBe(-1.0, 1e-9);
            hitPoint.y.ShouldBe(0.0, 1e-9);
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldReturnFalseForParallelRay()
        {
            var polygon = CreateRectanglePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(-2.0, 3.0), new Vector2d(1.0, 0.0));

            var hit = tree.FindNearestHitSegment(ray, out _, out _, out _);

            hit.ShouldBeFalse();
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldWorkForTrianglePolygon()
        {
            var polygon = CreateTrianglePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(-0.5, 0.5), new Vector2d(1.0, 0.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            segmentIndex.ShouldBe(2);
            rayT.ShouldBe(0.5, 1e-9);
            hitPoint.x.ShouldBe(0.0, 1e-9);
            hitPoint.y.ShouldBe(0.5, 1e-9);
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldHandleSkinnyPolygon()
        {
            var polygon = CreateSkinnyPolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(0.0, -1.0), new Vector2d(0.0, 1.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            segmentIndex.ShouldBe(0);
            rayT.ShouldBe(1.0, 1e-9);
            hitPoint.x.ShouldBe(0.0, 1e-9);
            hitPoint.y.ShouldBe(0.0, 1e-9);
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldHandleConcavePolygon()
        {
            var polygon = CreateConcavePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(2.0, -1.0), new Vector2d(0.0, 1.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            segmentIndex.ShouldBe(0);
            rayT.ShouldBe(1.0, 1e-9);
            hitPoint.x.ShouldBe(2.0, 1e-9);
            hitPoint.y.ShouldBe(0.0, 1e-9);
        }

        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldHandleCornerTouchingRay()
        {
            var polygon = CreateRectanglePolygon();
            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(-2.0, -1.0), new Vector2d(1.0, 1.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            new[] { 0, 3 }.Contains(segmentIndex).ShouldBeTrue();
            rayT.ShouldBe(Math.Sqrt(2.0), 1e-9);
            hitPoint.x.ShouldBe(-1.0, 1e-9);
            hitPoint.y.ShouldBe(0.0, 1e-9);
        }
    }
}
