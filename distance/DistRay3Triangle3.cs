using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace g4
{
    /// <summary>
    /// Distance between a 3D Ray and a 3D Triangle.
    /// Follows the standard WildMagic5 / geometry3Sharp distance query pattern.
    /// </summary>
    public class DistRay3Triangle3
    {
        Ray3d ray;
        public Ray3d Ray
        {
            get { return ray; }
            set { ray = value; DistanceSquared = -1.0; }
        }

        Triangle3d triangle;
        public Triangle3d Triangle
        {
            get { return triangle; }
            set { triangle = value; DistanceSquared = -1.0; }
        }

        public double DistanceSquared = -1.0;

        public Vector3d RayClosest;
        public double RayParameter;
        public Vector3d TriangleClosest;
        public Vector3d TriangleBaryCoords;


        public DistRay3Triangle3(Ray3d rayIn, Triangle3d triangleIn)
        {
            this.ray = rayIn; 
            this.triangle = triangleIn;
        }


        static public double MinDistance(Ray3d r, Triangle3d t) 
        {
            return new DistRay3Triangle3(r, t).Get();
        }

        static public double MinDistanceRayParam(Ray3d r, Triangle3d t) 
        {
            return new DistRay3Triangle3(r, t).Compute().RayParameter;
        }


        public DistRay3Triangle3 Compute() 
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

            Vector3d edge0 = triangle.V1 - triangle.V0;
            Vector3d edge1 = triangle.V2 - triangle.V0;
            Vector3d normal = edge0.Cross(edge1);
            double normalSqLen = normal.LengthSquared;

            // 1. Check if the infinite line intersects the triangle plane
            if (normalSqLen > MathUtil.ZeroTolerance * MathUtil.ZeroTolerance)
            {
                double NdotD = normal.Dot(ray.Direction);
                if (Math.Abs(NdotD) > MathUtil.ZeroTolerance)
                {
                    double t = normal.Dot(triangle.V0 - ray.Origin) / NdotD;
                    
                    // If intersection is strictly in front of the ray origin
                    if (t >= 0)
                    {
                        Vector3d hitPt = ray.Origin + t * ray.Direction;
                        Vector3d bary = ComputeBarycentric(hitPt, triangle.V0, edge0, edge1);
                        
                        // Check if the intersection point is inside the triangle
                        if (bary.x >= -MathUtil.ZeroTolerance && 
                            bary.y >= -MathUtil.ZeroTolerance && 
                            bary.z >= -MathUtil.ZeroTolerance)
                        {
                            RayClosest = hitPt;
                            TriangleClosest = hitPt;
                            RayParameter = t;
                            TriangleBaryCoords = bary;
                            DistanceSquared = 0;
                            return 0;
                        }
                    }
                }
            }

            // 2. If no direct intersection, the closest point lies on the boundaries.
            // The boundary of the Triangle consists of its 3 edges.
            Segment3d seg0 = new Segment3d(triangle.V0, triangle.V1);
            Segment3d seg1 = new Segment3d(triangle.V1, triangle.V2);
            Segment3d seg2 = new Segment3d(triangle.V2, triangle.V0);

            double minSqDist = double.MaxValue;
            Vector3d bestRayPt = Vector3d.Zero;
            Vector3d bestTriPt = Vector3d.Zero;
            double bestRayT = 0;

            // Helper to check distance against an edge
            Action<Segment3d> checkEdge = (seg) => {
                double rayT, segT;
                double distSqr = DistRay3Segment3.SquaredDistance(ref ray, ref seg, out rayT, out segT);
                if (distSqr < minSqDist) {
                    minSqDist = distSqr;
                    bestRayPt = ray.Origin + rayT * ray.Direction;
                    bestTriPt = seg.Center + segT * seg.Direction;
                    bestRayT = rayT;
                }
            };

            checkEdge(seg0);
            checkEdge(seg1);
            checkEdge(seg2);

            // 3. The boundary of the Ray is its Origin (t=0).
            // We only need to check this if the Origin projects INSIDE the triangle, 
            // as any projection outside the triangle is already handled by the Edge checks above.
            if (normalSqLen > MathUtil.ZeroTolerance * MathUtil.ZeroTolerance)
            {
                double tPlane = normal.Dot(triangle.V0 - ray.Origin) / normalSqLen;
                Vector3d projPt = ray.Origin + tPlane * normal;
                
                double ptDistSqr = (ray.Origin - projPt).LengthSquared;
                if (ptDistSqr < minSqDist) 
                {
                    Vector3d bary = ComputeBarycentric(projPt, triangle.V0, edge0, edge1);
                    if (bary.x >= -MathUtil.ZeroTolerance && 
                        bary.y >= -MathUtil.ZeroTolerance && 
                        bary.z >= -MathUtil.ZeroTolerance)
                    {
                        minSqDist = ptDistSqr;
                        bestRayPt = ray.Origin;
                        bestTriPt = projPt;
                        bestRayT = 0;
                    }
                }
            }

            // Account for numerical round-off errors.
            if (minSqDist < 0) {
                minSqDist = 0;
            }

            DistanceSquared = minSqDist;
            RayClosest = bestRayPt;
            TriangleClosest = bestTriPt;
            RayParameter = bestRayT;
            TriangleBaryCoords = ComputeBarycentric(bestTriPt, triangle.V0, edge0, edge1);

            return DistanceSquared;
        }

        // Helper method to compute barycentric coordinates (u, v, w) of a point on the triangle plane
        private Vector3d ComputeBarycentric(Vector3d pt, Vector3d v0, Vector3d e0, Vector3d e1)
        {
            Vector3d v2 = pt - v0;
            double d00 = e0.Dot(e0);
            double d01 = e0.Dot(e1);
            double d11 = e1.Dot(e1);
            double d20 = v2.Dot(e0);
            double d21 = v2.Dot(e1);
            double denom = d00 * d11 - d01 * d01;
            
            if (Math.Abs(denom) < MathUtil.ZeroTolerance) {
                return new Vector3d(1, 0, 0); // Degenerate fallback
            }
            
            double v = (d11 * d20 - d01 * d21) / denom;
            double w = (d00 * d21 - d01 * d20) / denom;
            double u = 1.0 - v - w;
            
            return new Vector3d(u, v, w);
        }
    }
}