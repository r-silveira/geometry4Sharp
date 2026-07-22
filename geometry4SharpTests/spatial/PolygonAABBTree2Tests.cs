using g4;
using Shouldly;

namespace geometry4SharpTests.spatial
{
    public class PolygonAABBTreeTests
    {
        [Fact]
        public void PolygonAABBTree_FindNearestHitSegment_ShouldReturnExpectedHit()
        {
            var polygon = new Polygon2d(new[]
            {
                new Vector2d(-1.0, 0.0),
                new Vector2d(1.0, 0.0),
                new Vector2d(1.0, 2.0),
                new Vector2d(-1.0, 2.0)
            });

            var tree = new PolygonAABBTree(polygon, true);
            var ray = new Ray2d(new Vector2d(0.0, -1.0), new Vector2d(0.0, 1.0));

            var hit = tree.FindNearestHitSegment(ray, out var segmentIndex, out var rayT, out var hitPoint);

            hit.ShouldBeTrue();
            segmentIndex.ShouldBe(0);
            rayT.ShouldBe(1.0, 1e-9);
            hitPoint.x.ShouldBe(0.0, 1e-9);
            hitPoint.y.ShouldBe(0.0, 1e-9);
        }
    }
}
