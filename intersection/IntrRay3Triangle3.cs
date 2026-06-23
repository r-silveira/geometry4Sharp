using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace g4
{
    public class IntrRay3Triangle3
    {
        Ray3d ray;
        public Ray3d Ray
        {
            get { return ray; }
            set { ray = value; Result = IntersectionResult.NotComputed; }
        }

        Triangle3d triangle;
        public Triangle3d Triangle
        {
            get { return triangle; }
            set { triangle = value; Result = IntersectionResult.NotComputed; }
        }

		public int Quantity = 0;
		public IntersectionResult Result = IntersectionResult.NotComputed;
		public IntersectionType Type = IntersectionType.Empty;

        public bool IsSimpleIntersection {
            get { return Result == IntersectionResult.Intersects && Type == IntersectionType.Point; }
        }


        public double RayParameter;
        public Vector3d TriangleBaryCoords;


		public IntrRay3Triangle3(Ray3d r, Triangle3d t)
		{
			ray = r; triangle = t;
		}


        public IntrRay3Triangle3 Compute()
        {
            Find();
            return this;
        }


        public bool Find()
        {
            if (Result != IntersectionResult.NotComputed)
                return (Result != g4.IntersectionResult.NoIntersection);

            // Compute the offset origin, edges, and normal.
            Vector3d diff = ray.Origin - triangle.V0;
            Vector3d edge1 = triangle.V1 - triangle.V0;
            Vector3d edge2 = triangle.V2 - triangle.V0;
            Vector3d normal = edge1.Cross(edge2);

            // Solve Q + t*D = b1*E1 + b2*E2 (Q = kDiff, D = ray direction,
            // E1 = kEdge1, E2 = kEdge2, N = Cross(E1,E2)) by
            //   |Dot(D,N)|*b1 = sign(Dot(D,N))*Dot(D,Cross(Q,E2))
            //   |Dot(D,N)|*b2 = sign(Dot(D,N))*Dot(D,Cross(E1,Q))
            //   |Dot(D,N)|*t = -sign(Dot(D,N))*Dot(Q,N)
            double DdN = ray.Direction.Dot(normal);
            double sign;
            if (DdN > MathUtil.ZeroTolerance) {
                sign = 1;
            } else if (DdN < -MathUtil.ZeroTolerance) {
                sign = -1;
                DdN = -DdN;
            } else {
                // Ray and triangle are parallel, call it a "no intersection"
                // even if the ray does intersect.
                Result = IntersectionResult.NoIntersection;
                return false;
            }

            double DdQxE2 = sign * ray.Direction.Dot(diff.Cross(edge2));
            if (DdQxE2 >= 0) {
                double DdE1xQ = sign * ray.Direction.Dot(edge1.Cross(diff));
                if (DdE1xQ >= 0) {
                    if (DdQxE2 + DdE1xQ <= DdN) {
                        // Line intersects triangle, check if ray does.
                        double QdN = -sign * diff.Dot(normal);
                        if (QdN >= 0) {
                            // Ray intersects triangle.
                            double inv = (1) / DdN;
                            RayParameter = QdN * inv;
                            double mTriBary1 = DdQxE2 * inv;
                            double mTriBary2 = DdE1xQ * inv;
                            TriangleBaryCoords = new Vector3d(1 - mTriBary1 - mTriBary2, mTriBary1, mTriBary2);
                            Type = IntersectionType.Point;
							Quantity = 1;
                            Result = IntersectionResult.Intersects;
                            return true;
                        }
                        // else: t < 0, no intersection
                    }
                    // else: b1+b2 > 1, no intersection
                }
                // else: b2 < 0, no intersection
            }
            // else: b1 < 0, no intersection

            Result = IntersectionResult.NoIntersection;
            return false;
        }

        // 3. Origin is outside. Check intersection with all 3 edges.
        private static bool CheckEdge(ref Ray3d ray, ref Vector3d start, ref Vector3d E, ref double minT, ref Vector3d normal)
        {
            Vector3d N_edge = E.Cross(ref normal);
            double det = ray.Direction.Dot(ref N_edge);

            var hit = false;
            if (Math.Abs(det) > MathUtil.ZeroTolerance)
            {
                Vector3d startToOrigin = start - ray.Origin;
                double t = startToOrigin.Dot(ref N_edge) / det;

                if (t >= 0 && t < minT)
                {
                    Vector3d P = ray.Origin + (ray.Direction * t);
                    Vector3d P_minus_start = P - start;
                    double u = P_minus_start.Dot(ref E) / E.Dot(ref E);

                    if (u >= -MathUtil.ZeroTolerance && u <= 1.0 + MathUtil.ZeroTolerance)
                    {
                        minT = t;
                        hit = true;
                    }
                }
            }

            return hit;
        }


        private static bool HandleCoplanarEdgeCase(ref Ray3d ray, ref Vector3d V0, ref Vector3d V1, ref Vector3d V2, out double rayT)
        {
            rayT = double.MaxValue;

            // Recalculate the initial triangle data (cheap and keeps the signature clean)
            Vector3d edge1 = V1 - V0;
            Vector3d edge2 = V2 - V0;
            Vector3d normal = edge1.Cross(ref edge2);
            Vector3d diff = ray.Origin - V0;

            // 1. Verify Coplanarity
            double distFromPlane = diff.Dot(ref normal);
            if (Math.Abs(distFromPlane) > MathUtil.ZeroTolerance)
            {
                return false; // Parallel, but hovering above/below the triangle
            }

            bool hit = false;
            double minT = double.MaxValue;

            // Calculate remaining edges and diffs
            Vector3d edge3 = V2 - V1;
            Vector3d edge4 = V0 - V2;
            Vector3d diff1 = ray.Origin - V1;
            Vector3d diff2 = ray.Origin - V2;

            // 2. Is the ray origin inside the triangle?
            Vector3d cross0 = edge1.Cross(ref diff);
            Vector3d cross1 = edge3.Cross(ref diff1);
            Vector3d cross2 = edge4.Cross(ref diff2);

            if (cross0.Dot(ref normal) >= -MathUtil.ZeroTolerance &&
                cross1.Dot(ref normal) >= -MathUtil.ZeroTolerance &&
                cross2.Dot(ref normal) >= -MathUtil.ZeroTolerance)
            {
                rayT = 0.0;
                return true;
            }

            hit |= CheckEdge(ref ray, ref V0, ref edge1, ref minT, ref normal);
            hit |= CheckEdge(ref ray, ref V1, ref edge3, ref minT, ref normal);
            hit |= CheckEdge(ref ray, ref V2, ref edge4, ref minT, ref normal);

            if (hit)
            {
                rayT = minT;
                return true;
            }

            return false;
        }

        /// <summary>
        /// minimal intersection test, computes ray-t
        /// </summary>
        public static bool Intersects(ref Ray3d ray, ref Vector3d V0, ref Vector3d V1, ref Vector3d V2, out double rayT, bool handleCoplanarRays = false)
        {
            // Compute the offset origin, edges, and normal.
            Vector3d diff = ray.Origin - V0;
            Vector3d edge1 = V1 - V0;
            Vector3d edge2 = V2 - V0;
            Vector3d normal = edge1.Cross(ref edge2);

            rayT = double.MaxValue;

            // Solve Q + t*D = b1*E1 + b2*E2 (Q = kDiff, D = ray direction,
            // E1 = kEdge1, E2 = kEdge2, N = Cross(E1,E2)) by
            //   |Dot(D,N)|*b1 = sign(Dot(D,N))*Dot(D,Cross(Q,E2))
            //   |Dot(D,N)|*b2 = sign(Dot(D,N))*Dot(D,Cross(E1,Q))
            //   |Dot(D,N)|*t = -sign(Dot(D,N))*Dot(Q,N)
            double DdN = ray.Direction.Dot(ref normal);
            double sign;
            if (DdN > MathUtil.ZeroTolerance) {
                sign = 1;
            } else if (DdN < -MathUtil.ZeroTolerance) {
                sign = -1;
                DdN = -DdN;
            } else {
                return handleCoplanarRays ? 
                    HandleCoplanarEdgeCase(ref ray, ref V0, ref V1, ref V2, out rayT) : false;
            }

            Vector3d cross = diff.Cross(ref edge2);
            double DdQxE2 = sign * ray.Direction.Dot(ref cross);
            if (DdQxE2 >= 0) {
                cross = edge1.Cross(ref diff);
                double DdE1xQ = sign * ray.Direction.Dot(ref cross);
                if (DdE1xQ >= 0) {
                    if (DdQxE2 + DdE1xQ <= DdN) {
                        // Line intersects triangle, check if ray does.
                        double QdN = -sign * diff.Dot(ref normal);
                        if (QdN >= 0) {
                            // Ray intersects triangle.
                            double inv = (1) / DdN;
                            rayT = QdN * inv;
                            return true;
                        }
                        // else: t < 0, no intersection
                    }
                    // else: b1+b2 > 1, no intersection
                }
                // else: b2 < 0, no intersection
            }
            // else: b1 < 0, no intersection

            return false;
        }

    }
}
