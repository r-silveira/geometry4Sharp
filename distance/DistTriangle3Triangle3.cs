using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace g4
{
    public class DistTriangle3Triangle3
    {
        Triangle3d triangle0;
        public Triangle3d Triangle0
        {
            get { return triangle0; }
            set { triangle0 = value; DistanceSquared = -1.0; }
        }

        Triangle3d triangle1;
        public Triangle3d Triangle1
        {
            get { return triangle1; }
            set { triangle1 = value; DistanceSquared = -1.0; }
        }

        public double DistanceSquared = -1.0;

        public Vector3d Triangle0Closest;
        public Vector3d Triangle0BaryCoords;
        public Vector3d Triangle1Closest;
        public Vector3d Triangle1BaryCoords;


        public DistTriangle3Triangle3(Triangle3d Triangle0in, Triangle3d Triangle1in)
        {
            this.triangle0 = Triangle0in; this.triangle1 = Triangle1in;
        }

        public DistTriangle3Triangle3 Compute()
        {
            GetSquared();
            return this;
        }

        public double Get()
        {
            return Math.Sqrt(GetSquared());
        }



        public double GetSquared()
        {
            if (DistanceSquared >= 0)
                return DistanceSquared;

            double sqrDist = double.MaxValue;
            double sqrDistTmp;
            double ratio;

            int i0, i1;

            Segment3d edge = new Segment3d();

            // Reuse the same query for all six triangle edges.
            var queryST = new DistSegment3Triangle3(edge, triangle1);

            // Compare edges of triangle0 to the interior of triangle1.
            for (i0 = 2, i1 = 0; i1 < 3; i0 = i1++)
            {
                edge.SetEndpoints(triangle0[i0], triangle0[i1]);

                // Updates the segment and invalidates the cached distance.
                queryST.Segment = edge;

                sqrDistTmp = queryST.GetSquared();

                if (sqrDistTmp < sqrDist)
                {
                    Triangle0Closest = queryST.SegmentClosest;
                    Triangle1Closest = queryST.TriangleClosest;
                    sqrDist = sqrDistTmp;

                    ratio = queryST.SegmentParam / edge.Extent;

                    Triangle0BaryCoords = Vector3d.Zero;
                    Triangle0BaryCoords[i0] = 0.5 * (1 - ratio);
                    Triangle0BaryCoords[i1] = 1 - Triangle0BaryCoords[i0];
                    Triangle0BaryCoords[3 - i0 - i1] = 0;

                    Triangle1BaryCoords = queryST.TriangleBaryCoords;

                    if (sqrDist <= MathUtil.ZeroTolerance)
                    {
                        DistanceSquared = 0;
                        return 0;
                    }
                }
            }

            // Switch to triangle0 for the second set of edge queries.
            // This also invalidates the cached distance.
            queryST.Triangle = triangle0;

            // Compare edges of triangle1 to the interior of triangle0.
            for (i0 = 2, i1 = 0; i1 < 3; i0 = i1++)
            {
                edge.SetEndpoints(triangle1[i0], triangle1[i1]);

                // Reuse the same query instance.
                queryST.Segment = edge;

                sqrDistTmp = queryST.GetSquared();

                if (sqrDistTmp < sqrDist)
                {
                    Triangle1Closest = queryST.SegmentClosest;
                    Triangle0Closest = queryST.TriangleClosest;
                    sqrDist = sqrDistTmp;

                    ratio = queryST.SegmentParam / edge.Extent;

                    Triangle1BaryCoords = Vector3d.Zero;
                    Triangle1BaryCoords[i0] = 0.5 * (1 - ratio);
                    Triangle1BaryCoords[i1] = 1 - Triangle1BaryCoords[i0];
                    Triangle1BaryCoords[3 - i0 - i1] = 0;

                    Triangle0BaryCoords = queryST.TriangleBaryCoords;

                    if (sqrDist <= MathUtil.ZeroTolerance)
                    {
                        DistanceSquared = 0;
                        return 0;
                    }
                }
            }

            DistanceSquared = sqrDist;
            return DistanceSquared;
        }

    }

}