using BenchmarkDotNet.Attributes;
using g4;

namespace Geometry4Sharp.Benchmarks;

/// <summary>
/// Run on the current and modified DLL builds. This change is deliberately
/// not controlled by a runtime flag; compare separate binaries/configurations.
/// </summary>
[MemoryDiagnoser]
public class NtMeshMetadataStorageBenchmarks
{
    [Params(10, 100, 1000)]
    public int MeshCount { get; set; }

    [Benchmark]
    public NTMesh3[] CreateEmptyMeshes()
    {
        var meshes = new NTMesh3[MeshCount];
        for (int i = 0; i < meshes.Length; ++i)
            meshes[i] = new NTMesh3(false, false, false, false);
        return meshes;
    }

    [Benchmark]
    public NTMesh3[] CreateOneTriangleMeshes()
    {
        var meshes = new NTMesh3[MeshCount];
        for (int i = 0; i < meshes.Length; ++i)
        {
            var mesh = new NTMesh3(false, false, false, false);
            int a = mesh.AppendVertex(new Vector3d(0, 0, 0));
            int b = mesh.AppendVertex(new Vector3d(1, 0, 0));
            int c = mesh.AppendVertex(new Vector3d(0, 1, 0));
            mesh.AppendTriangle(a, b, c);
            meshes[i] = mesh;
        }
        return meshes;
    }
}
