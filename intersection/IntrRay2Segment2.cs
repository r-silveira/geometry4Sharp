using System;

namespace g4
{
    public class IntrRay2Segment2
    {
        Ray2d ray;
        public Ray2d Ray
        {
            get { return ray; }
            set { ray = value; Result = IntersectionResult.NotComputed; }
        }

        Segment2d segment;
        public Segment2d Segment
        {
            get { return segment; }
            set { segment = value; Result = IntersectionResult.NotComputed; }
        }

        public int Quantity = 0;
        public IntersectionResult Result = IntersectionResult.NotComputed;
        public IntersectionType Type = IntersectionType.Empty;

        public bool IsSimpleIntersection
        {
            get { return Result == IntersectionResult.Intersects && Type == IntersectionType.Point; }
        }

        public double RayParameter;
        public Vector2d Point = Vector2d.Zero;

        public IntrRay2Segment2(Ray2d r, Segment2d s)
        {
            ray = r;
            segment = s;
        }

        public IntrRay2Segment2 Compute()
        {
            Find();
            return this;
        }

        public bool Find()
        {
            if (Result != IntersectionResult.NotComputed)
                return (Result == IntersectionResult.Intersects);

            if (ray.Direction.IsNormalized == false)
            {
                Type = IntersectionType.Empty;
                Result = IntersectionResult.InvalidQuery;
                return false;
            }

            double rayT;
            Vector2d hitPoint;
            bool hit = Intersects(ref ray, ref segment, out rayT, out hitPoint);
            if (hit)
            {
                RayParameter = rayT;
                Point = hitPoint;
                Quantity = 1;
                Type = IntersectionType.Point;
                Result = IntersectionResult.Intersects;
                return true;
            }

            Result = IntersectionResult.NoIntersection;
            return false;
        }

        public static bool Intersects(ref Ray2d ray, ref Segment2d segment)
        {
            double t;
            Vector2d point;
            return Intersects(ref ray, ref segment, out t, out point);
        }

        public static bool Intersects(ref Ray2d ray, ref Segment2d segment, out double rayT, out Vector2d point)
        {
            rayT = double.MaxValue;
            point = Vector2d.Zero;

            Vector2d q = ray.Origin;
            Vector2d r = ray.Direction;
            Vector2d p = segment.P0;
            Vector2d s = segment.P1 - segment.P0;

            double denom = r.DotPerp(s);
            if (Math.Abs(denom) <= MathUtil.ZeroTolerance)
                return false;

            Vector2d qp = p - q;
            double t = qp.DotPerp(s) / denom;
            double u = qp.DotPerp(r) / denom;

            if (t < 0.0 || u < -MathUtil.ZeroTolerance || u > 1.0 + MathUtil.ZeroTolerance)
                return false;

            rayT = t;
            point = q + t * r;
            return true;
        }
    }
}
