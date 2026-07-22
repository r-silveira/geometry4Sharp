using System;

namespace g4
{
    public class IntrRay2AxisAlignedBox2
    {
        Ray2d ray;
        public Ray2d Ray
        {
            get { return ray; }
            set { ray = value; Result = IntersectionResult.NotComputed; }
        }

        AxisAlignedBox2d box;
        public AxisAlignedBox2d Box
        {
            get { return box; }
            set { box = value; Result = IntersectionResult.NotComputed; }
        }

        public int Quantity = 0;
        public IntersectionResult Result = IntersectionResult.NotComputed;
        public IntersectionType Type = IntersectionType.Empty;

        public bool IsSimpleIntersection
        {
            get { return Result == IntersectionResult.Intersects && Type == IntersectionType.Point; }
        }

        public double RayParam0, RayParam1;
        public Vector2d Point0 = Vector2d.Zero;
        public Vector2d Point1 = Vector2d.Zero;

        public IntrRay2AxisAlignedBox2(Ray2d r, AxisAlignedBox2d b)
        {
            ray = r;
            box = b;
        }

        public IntrRay2AxisAlignedBox2 Compute()
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

            RayParam0 = 0.0;
            RayParam1 = double.MaxValue;
            Quantity = 0;
            Point0 = Vector2d.Zero;
            Point1 = Vector2d.Zero;
            Type = IntersectionType.Empty;

            bool hit = FindRayIntersectT(ref ray, ref box, out RayParam0);
            if (hit)
            {
                Quantity = 1;
                Type = IntersectionType.Point;
                Point0 = ray.PointAt(RayParam0);
                Result = IntersectionResult.Intersects;
                return true;
            }

            Result = IntersectionResult.NoIntersection;
            return false;
        }

        public bool Test()
        {
            return Intersects(ref ray, ref box);
        }

        public static bool Intersects(ref Ray2d ray, ref AxisAlignedBox2d box)
        {
            double t;
            return FindRayIntersectT(ref ray, ref box, out t);
        }

        public static bool FindRayIntersectT(ref Ray2d ray, ref AxisAlignedBox2d box, out double rayParam)
        {
            rayParam = double.MaxValue;
            double tMin = 0.0;
            double tMax = double.MaxValue;
            Vector2d origin = ray.Origin;
            Vector2d dir = ray.Direction;

            for (int axis = 0; axis < 2; ++axis)
            {
                double min = (axis == 0) ? box.Min.x : box.Min.y;
                double max = (axis == 0) ? box.Max.x : box.Max.y;
                double originValue = (axis == 0) ? origin.x : origin.y;
                double dirValue = (axis == 0) ? dir.x : dir.y;

                if (Math.Abs(dirValue) <= MathUtil.ZeroTolerance)
                {
                    if (originValue < min || originValue > max)
                        return false;
                }
                else
                {
                    double invDir = 1.0 / dirValue;
                    double t1 = (min - originValue) * invDir;
                    double t2 = (max - originValue) * invDir;
                    if (t1 > t2)
                    {
                        double tmp = t1;
                        t1 = t2;
                        t2 = tmp;
                    }

                    if (t1 > tMin)
                        tMin = t1;
                    if (t2 < tMax)
                        tMax = t2;
                    if (tMin > tMax)
                        return false;
                }
            }

            if (tMax < 0.0)
                return false;

            rayParam = Math.Max(tMin, 0.0);
            return true;
        }
    }
}
