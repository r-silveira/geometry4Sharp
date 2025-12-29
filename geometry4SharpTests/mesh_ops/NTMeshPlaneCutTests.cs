using g4;
using gs;
using Shouldly;
using System.Globalization;

namespace geometry4SharpTests.mesh_ops
{
    public static class DMeshExtensionIO
    {
        public static void WriteOFF(this NTMesh3 buffer, string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                var triangles = buffer.Triangles().ToList();

                writer.WriteLine("OFF");
                writer.WriteLine(3 * triangles.Count + " " + triangles.Count + " 0");

                foreach (var triangle in triangles)
                {
                    var i0 = triangle.a;
                    var i1 = triangle.b;
                    var i2 = triangle.c;

                    var v0 = buffer.GetVertex(i0);
                    var v1 = buffer.GetVertex(i1);
                    var v2 = buffer.GetVertex(i2);


                    var x0 = v0.x; var y0 = v0.y; var z0 = v0.z;
                    writer.WriteLine(x0.ToString("F7") + " " + y0.ToString("F7") + " " + z0.ToString("F7"));

                    var x1 = v1.x; var y1 = v1.y; var z1 = v1.z;
                    writer.WriteLine(x1.ToString("F7") + " " + y1.ToString("F7") + " " + z1.ToString("F7"));

                    var x2 = v2.x; var y2 = v2.y; var z2 = v2.z;
                    writer.WriteLine(x2.ToString("F7") + " " + y2.ToString("F7") + " " + z2.ToString("F7"));
                }

                for (int i = 0; i < triangles.Count; i++)
                {
                    writer.WriteLine(3 + " " + (3 * i) + " " + (3 * i + 1) + " " + (3 * i + 2));
                }
            }
        }

        public static void ReadOFF(this NTMesh3 buffer, string filename)
        {
            var lines = File.ReadAllLines(filename);
            int lineIndex = 0;

            // Skip comments and whitespace until the "OFF" header
            while (lines[lineIndex].StartsWith("#") || string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                lineIndex++;
            }

            if (lines[lineIndex++] != "OFF")
            {
                throw new InvalidDataException("Invalid OFF file format");
            }

            // Read number of vertices and faces
            var counts = lines[lineIndex++].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int numVertices = int.Parse(counts[0]);
            int numFaces = int.Parse(counts[1]);

            // Read vertices
            var vertices = new Vector3d[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                var parts = lines[lineIndex++].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                double x = double.Parse(parts[0], CultureInfo.InvariantCulture);
                double y = double.Parse(parts[1], CultureInfo.InvariantCulture);
                double z = double.Parse(parts[2], CultureInfo.InvariantCulture);

                vertices[i] = new Vector3d(x, y, z);
            }

            // Add vertices to the mesh
            var vertexIndices = new int[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                vertexIndices[i] = buffer.AppendVertex(vertices[i]);
            }

            // Read faces and add triangles to the mesh
            for (int i = 0; i < numFaces; i++)
            {
                var parts = lines[lineIndex++].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int numVerticesInFace = int.Parse(parts[0]);

                if (numVerticesInFace != 3)
                {
                    throw new InvalidDataException("Only triangular faces are supported");
                }

                int v0 = int.Parse(parts[1]);
                int v1 = int.Parse(parts[2]);
                int v2 = int.Parse(parts[3]);

                buffer.AppendTriangle(vertexIndices[v0], vertexIndices[v1], vertexIndices[v2]);
            }
        }
    }

    public class NTMeshPlaneCutTests
    {
        #region Test Data

        private static NTMesh3 CreateCube()
        {
            var mesh = new NTMesh3();

            var v0 = mesh.AppendVertex(new Vector3d(-1.0, 1.0, -1.0));
            var v1 = mesh.AppendVertex(new Vector3d(-1.0, 1.0, 1.0));
            var v2 = mesh.AppendVertex(new Vector3d(1.0, 1.0, -1.0));
            var v3 = mesh.AppendVertex(new Vector3d(1.0, 1.0, 1.0));

            var v4 = mesh.AppendVertex(new Vector3d(-1.0, -1.0, -1.0));
            var v5 = mesh.AppendVertex(new Vector3d(-1.0, -1.0, 1.0));
            var v6 = mesh.AppendVertex(new Vector3d(1.0, -1.0, -1.0));
            var v7 = mesh.AppendVertex(new Vector3d(1.0, -1.0, 1.0));

            mesh.AppendTriangle(v3, v2, v0);
            mesh.AppendTriangle(v0, v1, v3);
            mesh.AppendTriangle(v4, v6, v7);
            mesh.AppendTriangle(v7, v5, v4);

            mesh.AppendTriangle(v6, v2, v3);
            mesh.AppendTriangle(v3, v7, v6);
            mesh.AppendTriangle(v1, v0, v4);
            mesh.AppendTriangle(v4, v5, v1);

            mesh.AppendTriangle(v5, v7, v3);
            mesh.AppendTriangle(v3, v1, v5);
            mesh.AppendTriangle(v2, v6, v4);
            mesh.AppendTriangle(v4, v0, v2);

            return mesh;
        }

        #endregion

        [Theory]
        [InlineData(0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 12.0)]                            // Splitting cube in half (y-axis)
        [InlineData(0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 24.0)]                            // Plane is coplanar with top face (same normal as face)
        [InlineData(0.0, 1.0001, 0.0, 0.0, 1.0, 0.0, 24.0)]                         // Cube is entirely behind the plane
        [InlineData(0.0, 0.9999999, 0.0, 0.0, 1.0, 0.0, 20.0)]                      // "Only" top face is in front of the plane
        [InlineData(0.0, -1.0, 0.0, 0.0, 1.0, 0.0, 4.0)]                            // Plane is coplanar with bottom face (opposite normal as face)
        [InlineData(0.0, -1.0001, 0.0, 0.0, 1.0, 0.0, 0.0)]                         // Cube is entirely in front of the plane
        [InlineData(0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 12.0)]                            // Splitting cube in half (x-axis)
        [InlineData(0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 12.0)]                            // Splitting cube in half (z-axis)
        [InlineData(0.1, 0.1, 0.1, 0.57735027, 0.57735027, 0.57735027, 13.8)]       // Splitting cube with displaced/rotated plane
        public void NTMeshPlaneCut_Cut_Cube(double cx, double cy, double cz, 
            double nx, double ny, double nz, double expectedTotalArea)
        {
            const double EPS = 1e-3;
            var mesh = CreateCube();

            var planeCenter = new Vector3d(cx, cy, cz);
            var planeNormal = new Vector3d(nx, ny, nz).Normalized;

            var planeCut = new NTMeshPlaneCut(mesh, planeCenter, planeNormal);
            planeCut.Cut();

            var totalArea = mesh
                .TriangleIndices()
                .Sum(mesh.GetTriArea);

            totalArea.ShouldBe(expectedTotalArea, EPS);
        }
    }
}