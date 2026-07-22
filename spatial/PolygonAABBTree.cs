using System;
using System.Collections.Generic;
using System.Linq;

namespace g4
{
    public class PolygonAABBTree
    {
        public Polygon2d Polygon;

        private class Node
        {
            public AxisAlignedBox2d Box;
            public int SegmentIndex = -1;
            public int LeftChild = -1;
            public int RightChild = -1;
            public bool IsLeaf { get { return SegmentIndex >= 0; } }
        }

        private readonly List<Node> nodes = new List<Node>();
        private int rootIndex = -1;

        public PolygonAABBTree(Polygon2d poly, bool autoBuild = true)
        {
            Polygon = poly;
            if (autoBuild)
                Build();
        }

        public void Build()
        {
            nodes.Clear();
            rootIndex = -1;
            if (Polygon == null || Polygon.VertexCount < 2)
                return;

            List<int> segments = Enumerable.Range(0, Polygon.VertexCount).ToList();
            rootIndex = build_recursive(segments);
        }

        public bool FindNearestHitSegment(Ray2d ray, out int segmentIndex, out double rayT, out Vector2d hitPoint)
        {
            segmentIndex = -1;
            rayT = double.MaxValue;
            hitPoint = Vector2d.Zero;

            if (rootIndex < 0 || Polygon == null || Polygon.VertexCount < 2)
                return false;

            double bestT = double.MaxValue;
            int bestSeg = -1;
            Vector2d bestPoint = Vector2d.Zero;
            find_nearest_hit(rootIndex, ray, ref bestT, ref bestSeg, ref bestPoint);

            if (bestSeg >= 0)
            {
                segmentIndex = bestSeg;
                rayT = bestT;
                hitPoint = bestPoint;
                return true;
            }

            return false;
        }

        public bool FindNearestHitSegment(Ray2d ray, out int segmentIndex, out double rayT)
        {
            Vector2d hitPoint;
            return FindNearestHitSegment(ray, out segmentIndex, out rayT, out hitPoint);
        }

        public bool FindNearestHitSegment(Ray2d ray)
        {
            int segmentIndex;
            double rayT;
            Vector2d hitPoint;
            return FindNearestHitSegment(ray, out segmentIndex, out rayT, out hitPoint);
        }

        private int build_recursive(List<int> segmentIds)
        {
            Node node = new Node();
            node.Box = new AxisAlignedBox2d(false);
            for (int i = 0; i < segmentIds.Count; ++i)
            {
                int segId = segmentIds[i];
                AxisAlignedBox2d segBox = get_segment_box(segId);
                node.Box.Contain(ref segBox);
            }

            if (segmentIds.Count == 1)
            {
                node.SegmentIndex = segmentIds[0];
                nodes.Add(node);
                return nodes.Count - 1;
            }

            if (segmentIds.Count == 0)
            {
                nodes.Add(node);
                return nodes.Count - 1;
            }

            Vector2d diag = node.Box.Diagonal;
            int splitAxis = (diag.x >= diag.y) ? 0 : 1;
            double splitValue = 0.5 * (node.Box.Min[splitAxis] + node.Box.Max[splitAxis]);

            List<int> left = new List<int>();
            List<int> right = new List<int>();
            for (int i = 0; i < segmentIds.Count; ++i)
            {
                AxisAlignedBox2d segBox = get_segment_box(segmentIds[i]);
                double center = 0.5 * (segBox.Min[splitAxis] + segBox.Max[splitAxis]);
                if (center < splitValue)
                    left.Add(segmentIds[i]);
                else
                    right.Add(segmentIds[i]);
            }

            if (left.Count == 0 || right.Count == 0)
            {
                int mid = segmentIds.Count / 2;
                left = segmentIds.Take(mid).ToList();
                right = segmentIds.Skip(mid).ToList();
            }

            int leftChild = build_recursive(left);
            int rightChild = build_recursive(right);
            node.LeftChild = leftChild;
            node.RightChild = rightChild;

            nodes.Add(node);
            return nodes.Count - 1;
        }

        private void find_nearest_hit(int nodeIndex, Ray2d ray, ref double bestT, ref int bestSeg, ref Vector2d bestPoint)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Count)
                return;

            Node node = nodes[nodeIndex];
            double boxT;
            if (IntrRay2AxisAlignedBox2.FindRayIntersectT(ref ray, ref node.Box, out boxT) == false)
                return;
            if (boxT > bestT)
                return;

            if (node.IsLeaf)
            {
                Segment2d seg = get_segment(node.SegmentIndex);
                double segT;
                Vector2d hitPoint;
                if (IntrRay2Segment2.Intersects(ref ray, ref seg, out segT, out hitPoint))
                {
                    if (segT >= 0.0 && segT < bestT)
                    {
                        bestT = segT;
                        bestSeg = node.SegmentIndex;
                        bestPoint = hitPoint;
                    }
                }
                return;
            }

            int firstChild = node.LeftChild;
            int secondChild = node.RightChild;
            double firstT = double.MaxValue;
            double secondT = double.MaxValue;
            bool hitFirst = false;
            bool hitSecond = false;

            if (firstChild >= 0)
            {
                hitFirst = IntrRay2AxisAlignedBox2.FindRayIntersectT(ref ray, ref nodes[firstChild].Box, out firstT);
            }
            if (secondChild >= 0)
            {
                hitSecond = IntrRay2AxisAlignedBox2.FindRayIntersectT(ref ray, ref nodes[secondChild].Box, out secondT);
            }

            if (hitFirst && hitSecond)
            {
                if (firstT <= secondT)
                {
                    if (firstT < bestT)
                        find_nearest_hit(firstChild, ray, ref bestT, ref bestSeg, ref bestPoint);
                    if (secondT < bestT)
                        find_nearest_hit(secondChild, ray, ref bestT, ref bestSeg, ref bestPoint);
                }
                else
                {
                    if (secondT < bestT)
                        find_nearest_hit(secondChild, ray, ref bestT, ref bestSeg, ref bestPoint);
                    if (firstT < bestT)
                        find_nearest_hit(firstChild, ray, ref bestT, ref bestSeg, ref bestPoint);
                }
            }
            else if (hitFirst)
            {
                if (firstT < bestT)
                    find_nearest_hit(firstChild, ray, ref bestT, ref bestSeg, ref bestPoint);
            }
            else if (hitSecond)
            {
                if (secondT < bestT)
                    find_nearest_hit(secondChild, ray, ref bestT, ref bestSeg, ref bestPoint);
            }
        }

        private AxisAlignedBox2d get_segment_box(int segmentIndex)
        {
            Vector2d p0 = Polygon[segmentIndex];
            Vector2d p1 = Polygon[(segmentIndex + 1) % Polygon.VertexCount];
            return new AxisAlignedBox2d(p0, p1);
        }

        private Segment2d get_segment(int segmentIndex)
        {
            Vector2d p0 = Polygon[segmentIndex];
            Vector2d p1 = Polygon[(segmentIndex + 1) % Polygon.VertexCount];
            return new Segment2d(p0, p1);
        }
    }
}
