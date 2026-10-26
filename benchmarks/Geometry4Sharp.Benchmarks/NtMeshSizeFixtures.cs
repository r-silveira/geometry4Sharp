using System;
using g4;

namespace Geometry4Sharp.Benchmarks;

/// <summary>
/// Deterministic BIM-like NTMesh3 fixtures with exactly the requested triangle count.
/// Uses disjoint boxes (column / room / slab proportions) to exercise the topology
/// buffers, not just the vertex coordinate storage. A final partial box handles
/// counts that are not multiples of twelve.
/// </summary>
internal static class NtMeshSizeFixtures
{
    private static readonly Index3i[] BoxTriangles =
    {
        new(0, 2, 1), new(0, 3, 2),
        new(4, 5, 6), new(4, 6, 7),
        new(0, 4, 7), new(0, 7, 3),
        new(1, 2, 6), new(1, 6, 5),
        new(0, 1, 5), new(0, 5, 4),
        new(3, 7, 6), new(3, 6, 2),
    };

    public static NTMesh3 Create(int triangleCount, int blockSize = 2048)
    {
        if (triangleCount < 0)
            throw new ArgumentOutOfRangeException(nameof(triangleCount));

        // Match the existing metadata benchmark: no normals/colors/groups.
        var mesh = blockSize == 2048
            ? new NTMesh3(false, false, false, false)
            : new NTMesh3(blockSize, false, false, false, false);
        int boxes = (triangleCount + 11) / 12;
        if (boxes == 0)
            return mesh;

        int side = (int)Math.Ceiling(Math.Sqrt(boxes));
        int trianglesLeft = triangleCount;
        for (int b = 0; b < boxes; ++b)
        {
            int gridX = b % side;
            int gridY = b / side;
            double sx = b % 3 == 0 ? 0.25 : b % 3 == 1 ? 4.0 : 8.0;
            double sy = b % 3 == 0 ? 0.25 : b % 3 == 1 ? 3.0 : 6.0;
            double sz = b % 3 == 0 ? 3.5 : b % 3 == 1 ? 2.7 : 0.15;
            double x = gridX * 12.0, y = gridY * 12.0;

            int toAppend = Math.Min(trianglesLeft, BoxTriangles.Length);
            int vertexCount = 0;
            for (int j = 0; j < toAppend; ++j)
            {
                Index3i tri = BoxTriangles[j];
                vertexCount = Math.Max(vertexCount,
                    Math.Max(tri.a, Math.Max(tri.b, tri.c)) + 1);
            }

            // The 1-triangle fixture uses exactly 3 vertices, matching the
            // earlier metadata benchmark. Complete boxes use all 8 vertices.
            var v = new int[vertexCount];
            for (int j = 0; j < vertexCount; ++j)
            {
                Vector3d point = j switch
                {
                    0 => new Vector3d(x, y, 0),
                    1 => new Vector3d(x + sx, y, 0),
                    2 => new Vector3d(x + sx, y + sy, 0),
                    3 => new Vector3d(x, y + sy, 0),
                    4 => new Vector3d(x, y, sz),
                    5 => new Vector3d(x + sx, y, sz),
                    6 => new Vector3d(x + sx, y + sy, sz),
                    7 => new Vector3d(x, y + sy, sz),
                    _ => throw new InvalidOperationException("Invalid box corner")
                };
                v[j] = mesh.AppendVertex(point);
            }

            for (int j = 0; j < toAppend; ++j)
            {
                Index3i t = BoxTriangles[j];
                int tid = mesh.AppendTriangle(v[t.a], v[t.b], v[t.c]);
                if (tid == NTMesh3.InvalidID)
                    throw new InvalidOperationException($"Could not append triangle {j} of box {b}.");
            }
            trianglesLeft -= toAppend;
        }

        if (mesh.TriangleCount != triangleCount)
            throw new InvalidOperationException(
                $"Expected {triangleCount} triangles, found {mesh.TriangleCount}.");

        return mesh;
    }
}
