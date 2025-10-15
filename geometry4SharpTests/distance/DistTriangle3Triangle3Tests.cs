using Shouldly;

using g4;

namespace geometry4SharpTests.distance
{
    public class DistTriangle3Triangle3Tests
    {
        [Fact]
        public void DistTriangle3Triangle3_ReturningPoints_InCorrectOrder()
        {
            var triangleA = new Triangle3d(
                new Vector3d(-22.03933907, 3.80000305, -6.62411737),
                new Vector3d(-22.03933907, 3.80000305, 4.11130095),
                new Vector3d(-10.74934673, 3.80000305, 4.11130095));

            var triangleB = new Triangle3d(
                new Vector3d(-21.01844597, 5.92519999, -5.53531647),
                new Vector3d(-21.01844597, 5.92519999, 3.46468329),
                new Vector3d(-21.04069710, 5.92770720, 3.46468329));

            var triangleDistance = new DistTriangle3Triangle3(triangleA, triangleB).Compute();

            triangleDistance.Triangle0Closest.y.ShouldBe(3.80, 0.1);
            triangleDistance.Triangle1Closest.y.ShouldBe(5.92, 0.1);
        }

        [Fact]
        public void DistTriangle3Triangle3_ReturningPoints_InCorrectOrder_2()
        {
            var triangleA = new Triangle3d(
                new Vector3d(-22.03933907, 3.80000305, - 6.62411737),
                new Vector3d(-22.03933907, 3.80000305, 4.11130095),
                new Vector3d(-10.74934673, 3.80000305, 4.11130095));

            var triangleB = new Triangle3d(
                new Vector3d(-17.62496758, 5.95020008, - 5.59591961),
                new Vector3d(-17.32496643, 5.95020008, 3.40408039),
                new Vector3d(-17.62496758, 5.95020008, 3.40408039));

            var triangleDistance = new DistTriangle3Triangle3(triangleA, triangleB).Compute();

            triangleDistance.Triangle0Closest.y.ShouldBe(3.80, 0.1);
            triangleDistance.Triangle1Closest.y.ShouldBe(5.95, 0.1);
        }

        [Fact]
        public void DistTriangle3Triangle3_ReturningPoints_InCorrectOrder_3()
        {
            var triangleA = new Triangle3d(
                new Vector3d(-22.03933907, 3.80000305, -6.62411737),
                new Vector3d(-22.03933907, 3.80000305, 4.11130095),
                new Vector3d(-10.74934673, 3.80000305, 4.11130095));

            var triangleB = new Triangle3d(
                new Vector3d(-21.01844597, 5.92519999, - 5.53531647),
                new Vector3d(-21.01844597, 5.92519999, 3.46468329),
                new Vector3d(-21.04069710, 5.92770720, 3.46468329));

            var triangleDistance = new DistTriangle3Triangle3(triangleA, triangleB).Compute();

            triangleDistance.Triangle0Closest.y.ShouldBe(3.80, 0.1);
            triangleDistance.Triangle1Closest.y.ShouldBe(5.92, 0.1);
        }

        [Fact]
        public void DistTriangle3Triangle3_ReturningPoints_InCorrectOrder_4()
        {
            var triangleA = new Triangle3d(
                new Vector3d(-22.44020844, 1.51857758, -7.87406683),
                new Vector3d(-1.97481048, 1.51858521, -7.87406683),
                new Vector3d(-22.44020653, 1.08472443, -12.07386684));

            var triangleB = new Triangle3d(
                new Vector3d(-22.44020844, 4.93565130, -7.95134544),
                new Vector3d(-22.43932915, 4.93565130, -8.02902508),
                new Vector3d(-22.43814850, 0.00000000, -8.13332558));

            var triangleDistance = new DistTriangle3Triangle3(triangleA, triangleB).Compute();

            triangleDistance.DistanceSquared.ShouldBe(0, 0.01);
        }
    }
}