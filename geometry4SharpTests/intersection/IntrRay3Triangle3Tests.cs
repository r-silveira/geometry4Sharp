using System.Collections.Generic;
using Shouldly;

using g4;

namespace geometry4SharpTests
{
    /// <summary>
    /// Tests for the handleCoplanarRays flag on IntrRay3Triangle3 (Find / Compute / Intersects).
    ///
    /// Setup: a 10x10x0.2 box (10 in x, 10 in z, 0.2 in y, bottom face at y=0).
    /// For a box vertex v the ray origin is (v.x, boxTop + lift, v.z) -- the vertex's x and z
    /// positions, raised above the geometry -- and the ray points straight down.
    /// That ray line runs exactly along the box's corner vertical edge, so it is coplanar with
    /// the two vertical side faces meeting at that corner: it "grazes" them.
    /// A grazing coplanar ray must be reported as a hit only when handleCoplanarRays is true.
    /// </summary>
    public class IntrRay3Triangle3Tests
    {
        const double HalfSize = 5.0;      // box extent in x and z is 10
        const double Thickness = 0.2;     // box extent in y
        const double Lift = 1.0;          // ray start height above the box top face
        const double Tol = 1e-9;

        Vector3d[] _v;                    // the 8 box vertices
        Triangle3d[] _xPlus, _xMinus;     // vertical side faces x = +-5
        Triangle3d[] _zPlus, _zMinus;     // vertical side faces z = +-5
        Triangle3d[] _topFace, _bottomFace;

        public IntrRay3Triangle3Tests()
        {
            _v = new[]
            {
                new Vector3d(-HalfSize, 0.0, -HalfSize),        // 0
                new Vector3d( HalfSize, 0.0, -HalfSize),        // 1
                new Vector3d( HalfSize, 0.0,  HalfSize),        // 2
                new Vector3d(-HalfSize, 0.0,  HalfSize),        // 3
                new Vector3d(-HalfSize, Thickness, -HalfSize),  // 4
                new Vector3d( HalfSize, Thickness, -HalfSize),  // 5
                new Vector3d( HalfSize, Thickness,  HalfSize),  // 6
                new Vector3d(-HalfSize, Thickness,  HalfSize),  // 7
            };
            _xPlus    = SplitQuad(1, 2, 6, 5);   // face x = +5
            _xMinus   = SplitQuad(0, 3, 7, 4);   // face x = -5
            _zPlus    = SplitQuad(2, 3, 7, 6);   // face z = +5
            _zMinus   = SplitQuad(0, 1, 5, 4);   // face z = -5
            _topFace  = SplitQuad(4, 5, 6, 7);   // face y = 0.2
            _bottomFace = SplitQuad(0, 1, 2, 3); // face y = 0
        }

        Triangle3d[] AllTriangles
        {
            get
            {
                return new[] { _xPlus, _xMinus, _zPlus, _zMinus, _topFace, _bottomFace }
                    .SelectMany(f => f).ToArray();
            }
        }

        static Triangle3d[] SplitQuad(Vector3d[] v, int a, int b, int c, int d)
        {
            return new[]
            {
                new Triangle3d(v[a], v[b], v[c]),
                new Triangle3d(v[a], v[c], v[d]),
            };
        }

        Triangle3d[] SplitQuad(int a, int b, int c, int d) => SplitQuad(_v, a, b, c, d);

        Ray3d DownwardRayAtVertex(Vector3d vertex)
        {
            // the vertex's x and z, raised so the ray starts above the geometry, pointing down
            return new Ray3d(new Vector3d(vertex.x, Thickness + Lift, vertex.z),
                new Vector3d(0, -1, 0), bIsNormalized: true);
        }

        List<Triangle3d> SideFaceTrianglesAt(int vi)
        {
            var list = new List<Triangle3d>();
            list.AddRange(_v[vi].x > 0 ? _xPlus : _xMinus);
            list.AddRange(_v[vi].z > 0 ? _zPlus : _zMinus);
            return list;
        }


        [Fact]
        public void CoplanarRay_GrazingCornerEdge_HitsSideFaceTrianglesOnlyWithFlag()
        {
            var ray = DownwardRayAtVertex(_v[6]);   // from (5, 1.2, 5) straight down

            // without the flag: the coplanar grazing ray is a "parallel" miss
            foreach (var tri in SideFaceTrianglesAt(6))
            {
                var q = new IntrRay3Triangle3(ray, tri);
                q.Find(false).ShouldBeFalse();
                q.Result.ShouldBe(IntersectionResult.NoIntersection);
            }

            // with the flag: every side face triangle at the corner reports the first contact
            // of the ray with that triangle; all contacts lie on the corner vertical edge
            var t0 = _xPlus[0];   // (V1, V2, V6)
            var t1 = _xPlus[1];   // (V1, V6, V5)
            var t2 = _zPlus[0];   // (V2, V3, V7)
            var t3 = _zPlus[1];   // (V2, V7, V6)

            var expected = new (Triangle3d tri, double t, Vector3d point)[]
            {
                (t0, Lift,             new Vector3d( HalfSize, Thickness, HalfSize)), // (5, 0.2, 5)
                (t1, Lift,             new Vector3d( HalfSize, Thickness, HalfSize)), // (5, 0.2, 5)
                (t2, Lift + Thickness, new Vector3d( HalfSize, 0.0,       HalfSize)), // (5, 0,   5)
                (t3, Lift,             new Vector3d( HalfSize, Thickness, HalfSize)), // (5, 0.2, 5)
            };

            foreach (var (tri, t, point) in expected)
            {
                var q = new IntrRay3Triangle3(ray, tri);
                q.Find(true).ShouldBeTrue();
                q.Result.ShouldBe(IntersectionResult.Intersects);
                q.Type.ShouldBe(IntersectionType.Point);
                q.Quantity.ShouldBe(1);
                q.RayParameter.ShouldBe(t, Tol);

                var hit = ray.PointAt(q.RayParameter);
                hit.Distance(point).ShouldBeLessThan(Tol);
                tri.PointAt(q.TriangleBaryCoords).Distance(hit).ShouldBeLessThan(Tol);
            }

            // t0 = (V1, V2, V6): the first contact is the top corner itself, a vertex of the
            // triangle, so the barycentric coords are exactly (0, 0, 1)
            var q0 = new IntrRay3Triangle3(ray, t0);
            q0.Compute(true);
            q0.RayParameter.ShouldBe(Lift, Tol);
            q0.TriangleBaryCoords.x.ShouldBe(0.0, Tol);
            q0.TriangleBaryCoords.y.ShouldBe(0.0, Tol);
            q0.TriangleBaryCoords.z.ShouldBe(1.0, Tol);
        }


        [Fact]
        public void CoplanarRay_AtEveryBoxVertex_HitsAdjacentSideFacesOnlyWithFlag()
        {
            for (int vi = 0; vi < 8; vi++)
            {
                var ray = DownwardRayAtVertex(_v[vi]);
                var adjacent = SideFaceTrianglesAt(vi);
                adjacent.Count.ShouldBe(4);

                foreach (var tri in adjacent)
                {
                    var qOff = new IntrRay3Triangle3(ray, tri);
                    qOff.Find(false).ShouldBeFalse($"vertex {vi}");
                    qOff.Result.ShouldBe(IntersectionResult.NoIntersection);

                    var qOn = new IntrRay3Triangle3(ray, tri);
                    qOn.Find(true).ShouldBeTrue($"vertex {vi}");
                    qOn.Result.ShouldBe(IntersectionResult.Intersects);

                    // the first contact with the corner vertical edge lies between the
                    // top and bottom of the side face
                    var t = qOn.RayParameter;
                    (t >= Lift - Tol && t <= Lift + Thickness + Tol)
                        .ShouldBeTrue($"vertex {vi}, t = {t}");

                    var hit = ray.PointAt(t);
                    hit.x.ShouldBe(_v[vi].x, Tol);
                    hit.z.ShouldBe(_v[vi].z, Tol);
                    (hit.y >= -Tol && hit.y <= Thickness + Tol)
                        .ShouldBeTrue($"vertex {vi}, hit.y = {hit.y}");

                    tri.PointAt(qOn.TriangleBaryCoords).Distance(hit).ShouldBeLessThan(Tol);
                }
            }
        }


        [Fact]
        public void DownwardRayAtTopCorner_AlsoTouchesTopFace_WithoutFlag()
        {
            // the grazing ray runs along the corner edge, which is also a boundary of the top
            // face: hitting the top face head-on at that corner is a regular (non-coplanar)
            // intersection, so it happens with either flag setting
            var ray = DownwardRayAtVertex(_v[6]);   // (5, 1.2, 5) pointing down

            foreach (var tri in _topFace)           // both top triangles share corner V6
            {
                var q = new IntrRay3Triangle3(ray, tri);
                q.Find(false).ShouldBeTrue();
                q.RayParameter.ShouldBe(Lift, Tol);
                ray.PointAt(q.RayParameter).Distance(_v[6]).ShouldBeLessThan(Tol);
            }
        }


        [Fact]
        public void NonCoplanarDownwardRay_StillHitsTopFace_WithEitherFlagSetting()
        {
            // a ray straight down from above the interior of the top face is a regular hit;
            // the coplanar flag must not change regular intersections
            var ray = new Ray3d(new Vector3d(0, Thickness + 2.0, -2),
                new Vector3d(0, -1, 0), bIsNormalized: true);

            // (0, -2) lies strictly inside top triangle (V4, V5, V6), outside (V4, V6, V7)
            var qIn = new IntrRay3Triangle3(ray, _topFace[0]);
            qIn.Find(false).ShouldBeTrue();
            qIn.RayParameter.ShouldBe(2.0, Tol);
            ray.PointAt(qIn.RayParameter).Distance(new Vector3d(0, Thickness, -2)).ShouldBeLessThan(Tol);
            qIn.TriangleBaryCoords.x.ShouldBe(0.5, Tol);
            qIn.TriangleBaryCoords.y.ShouldBe(0.2, Tol);
            qIn.TriangleBaryCoords.z.ShouldBe(0.3, Tol);

            var qIn2 = new IntrRay3Triangle3(ray, _topFace[0]);
            qIn2.Compute(true).RayParameter.ShouldBe(2.0, Tol);

            var qOut = new IntrRay3Triangle3(ray, _topFace[1]);
            qOut.Find(true).ShouldBeFalse();
        }


        [Fact]
        public void ParallelRayBesideBox_HoversAboveNoFace_NeverHits()
        {
            // a downward ray beside the box is parallel to every vertical side face, but not
            // coplanar with any of them (and misses the top/bottom faces)
            var ray = new Ray3d(new Vector3d(HalfSize + 1.0, Thickness + Lift, 4),
                new Vector3d(0, -1, 0), bIsNormalized: true);

            foreach (var tri in AllTriangles)
                new IntrRay3Triangle3(ray, tri).Find(true).ShouldBeFalse();
        }


        [Fact]
        public void CoplanarRay_BeyondSideFaceEdge_MissesAllTriangles()
        {
            // a downward ray in the plane of the x=+5 face, but outside its z range:
            // coplanar with that face's plane, yet never reaching the face
            var ray = new Ray3d(new Vector3d(HalfSize, Thickness + Lift, HalfSize + 1.0),
                new Vector3d(0, -1, 0), bIsNormalized: true);

            foreach (var tri in AllTriangles)
                new IntrRay3Triangle3(ray, tri).Find(true).ShouldBeFalse();
        }


        [Fact]
        public void StaticIntersects_AgreesWithInstanceFind_ForCoplanarRay()
        {
            var ray = DownwardRayAtVertex(_v[6]);
            var tri = _xPlus[0];
            double t;

            IntrRay3Triangle3.Intersects(ref ray, ref tri.V0, ref tri.V1, ref tri.V2,
                out t, handleCoplanarRays: true).ShouldBeTrue();
            t.ShouldBe(Lift, Tol);

            IntrRay3Triangle3.Intersects(ref ray, ref tri.V0, ref tri.V1, ref tri.V2,
                out t, handleCoplanarRays: false).ShouldBeFalse();
        }


        [Fact]
        public void CachedResult_IsRecomputed_WhenFlagFlips()
        {
            var ray = DownwardRayAtVertex(_v[6]);
            var tri = _xPlus[0];
            var q = new IntrRay3Triangle3(ray, tri);

            q.Find(false).ShouldBeFalse();
            q.Result.ShouldBe(IntersectionResult.NoIntersection);

            q.Find(true).ShouldBeTrue();                        // must not return the cached no-hit
            q.Result.ShouldBe(IntersectionResult.Intersects);
            q.RayParameter.ShouldBe(Lift, Tol);

            q.Find(false).ShouldBeFalse();                      // and back
            q.Result.ShouldBe(IntersectionResult.NoIntersection);
        }
    }
}
