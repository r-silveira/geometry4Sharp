using Shouldly;

using g4;

namespace geometry4SharpTests
{
    public class Triangle3Tests
    {
        [Fact]
        public void IsPointInTriangle_PointOutside()
        {
            var triangleA = new Triangle3d(
                new Vector3d(-22.03933907, 3.80000305, -6.62411737),
                new Vector3d(-22.03933907, 3.80000305, 4.11130095),
                new Vector3d(-10.74934673, 3.80000305, 4.11130095));

            var pointA = new Vector3d(-21.01844597, 5.9251999899999994, -5.53531647);

            var isPointOnTri = triangleA.IsPointInTriangle(pointA);

            isPointOnTri.ShouldBeFalse();


            var pointB = new Vector3d(-21.01844597, 3.80000305, -5.53531647);

            triangleA.IsPointInTriangle(pointB).ShouldBeTrue();
        }

        [Fact]
        public void IsPointInTriangle_PointInside()
        {
            var triangleA = new Triangle3d(
                new Vector3d(-22.03933907, 3.80000305, -6.62411737),
                new Vector3d(-22.03933907, 3.80000305, 4.11130095),
                new Vector3d(-10.74934673, 3.80000305, 4.11130095));

            var pointB = new Vector3d(-21.01844597, 3.80000305, -5.53531647);

            triangleA.IsPointInTriangle(pointB).ShouldBeTrue();
        }
    }
}