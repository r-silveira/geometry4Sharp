using g4;

namespace Geometry4Sharp.Benchmarks;

internal static class FixtureMeshes
{
    /// <summary>
    /// One 12-triangle BIM-like solid. The BVH allocation footprint is the same
    /// for all three shapes; the traversal geometry is intentionally different.
    /// </summary>
    public static NTMesh3 CreateElement(string geometry)
    {
        var mesh = new NTMesh3(false, false, false, false);
        switch (geometry)
        {
            case "Column": AppendBox(mesh, 0, 0, 0, 0.25, 0.25, 3.5); break;
            case "Room": AppendBox(mesh, 0, 0, 0, 4.0, 3.0, 2.7); break;
            case "Slab": AppendBox(mesh, 0, 0, 0, 8.0, 6.0, 0.15); break;
            default: throw new ArgumentOutOfRangeException(nameof(geometry), geometry, "Unknown geometry");
        }
        return mesh;
    }

    // Non-overlapping, independent boxes, representative of structural elements.
    // Each box consists of 8 vertices / 12 triangles.
    public static NTMesh3 CreateBoxes(int count) => CreateBoxes(count, 0.0);

    public static NTMesh3 CreateBoxes(int count, double offsetX)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        var mesh = new NTMesh3(false, false, false, false);
        int side = (int)Math.Ceiling(Math.Sqrt(count));
        for (int i = 0; i < count; i++)
        {
            int x = i % side;
            int y = i / side;
            // Cycle column, room and slab proportions, keeping distinct locations.
            double sx = i % 3 == 0 ? 0.25 : i % 3 == 1 ? 1.0 : 2.0;
            double sy = i % 3 == 0 ? 0.25 : i % 3 == 1 ? 1.0 : 1.6;
            double sz = i % 3 == 0 ? 3.5 : i % 3 == 1 ? 2.7 : 0.15;
            AppendBox(mesh, x * 6 + offsetX, y * 6, 0, sx, sy, sz);
        }
        return mesh;
    }

    private static void AppendBox(NTMesh3 mesh, double x, double y, double z,
        double sx, double sy, double sz)
    {
        int[] v =
        {
            mesh.AppendVertex(new Vector3d(x, y, z)),
            mesh.AppendVertex(new Vector3d(x + sx, y, z)),
            mesh.AppendVertex(new Vector3d(x + sx, y + sy, z)),
            mesh.AppendVertex(new Vector3d(x, y + sy, z)),
            mesh.AppendVertex(new Vector3d(x, y, z + sz)),
            mesh.AppendVertex(new Vector3d(x + sx, y, z + sz)),
            mesh.AppendVertex(new Vector3d(x + sx, y + sy, z + sz)),
            mesh.AppendVertex(new Vector3d(x, y + sy, z + sz)),
        };
        int[,] tris =
        {
            {0, 2, 1}, {0, 3, 2}, // bottom
            {4, 5, 6}, {4, 6, 7}, // top
            {0, 4, 7}, {0, 7, 3}, // x-
            {1, 2, 6}, {1, 6, 5}, // x+
            {0, 1, 5}, {0, 5, 4}, // y-
            {3, 7, 6}, {3, 6, 2}, // y+
        };
        for (int i = 0; i < 12; ++i)
            mesh.AppendTriangle(v[tris[i, 0]], v[tris[i, 1]], v[tris[i, 2]]);
    }

    public static Ray3d[] CreateRays(int boxCount, int samples = 128)
    {
        int side = (int)Math.Ceiling(Math.Sqrt(boxCount));
        var rays = new Ray3d[samples];
        for (int i = 0; i < samples; i++)
        {
            int boxId = i % boxCount;
            int x = boxId % side;
            int y = boxId / side;
            // Alternate hits and misses so the benchmark exercises BVH pruning.
            bool miss = (i % 4) == 0;
            double ox = x * 6 + (miss ? 3.0 : 0.12);
            double oy = y * 6 + 0.08;
            rays[i] = new Ray3d(new Vector3d(ox, oy, 5), new Vector3d(0, 0, -1));
        }
        return rays;
    }
}
