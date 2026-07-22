using System;

namespace g4
{
    public struct Ray2d
    {
        public Vector2d Origin;
        public Vector2d Direction;

        public Ray2d(Vector2d origin, Vector2d direction, bool bIsNormalized = false)
        {
            this.Origin = origin;
            this.Direction = direction;
            if (bIsNormalized == false && Direction.IsNormalized == false)
                Direction.Normalize();
        }

        public Vector2d PointAt(double d)
        {
            return Origin + d * Direction;
        }

        public double Project(Vector2d p)
        {
            return (p - Origin).Dot(Direction);
        }

        public double DistanceSquared(Vector2d p)
        {
            double t = (p - Origin).Dot(Direction);
            if (t < 0)
            {
                return Origin.DistanceSquared(p);
            }

            Vector2d proj = Origin + t * Direction;
            return (proj - p).LengthSquared;
        }

        public Vector2d ClosestPoint(Vector2d p)
        {
            double t = (p - Origin).Dot(Direction);
            if (t < 0)
            {
                return Origin;
            }

            return Origin + t * Direction;
        }
    }
}
