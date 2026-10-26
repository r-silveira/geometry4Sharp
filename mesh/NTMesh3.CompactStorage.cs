using System;

namespace g4
{
    /// <summary>
    /// Additional opt-in constructor for NTMesh3 instances whose size is expected
    /// to remain small. NTMesh3's existing constructors and default storage sizes
    /// are unchanged. All topology features remain supported: this only changes
    /// DVector segment sizes, not the geometric/topological representation.
    /// </summary>
    public partial class NTMesh3
    {
        /// <summary>
        /// Creates a mesh whose primary storage uses fixed-size DVector blocks of
        /// <paramref name="storageBlockSize"/> elements. Use a power of two.
        /// Example: new NTMesh3(256, false, false, false, false).
        /// </summary>
        public NTMesh3(int storageBlockSize,
            bool bWantNormals = true, bool bWantColors = false,
            bool bWantTriGroups = false, bool bWantVtxGroups = false)
        {
            if (storageBlockSize <= 0 || (storageBlockSize & (storageBlockSize - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(storageBlockSize),
                    "Storage block size must be a positive power of two.");

            vertices = new DVector<double>(storageBlockSize);
            if (bWantNormals)
                normals = new DVector<float>(storageBlockSize);
            if (bWantColors)
                colors = new DVector<float>(storageBlockSize);

            vertex_edges = new SmallListSet(storageBlockSize);
            vertices_refcount = new RefCountVector(storageBlockSize);

            triangles = new DVector<int>(storageBlockSize);
            triangle_edges = new DVector<int>(storageBlockSize);
            triangles_refcount = new RefCountVector(storageBlockSize);

            if (bWantTriGroups)
                triangle_groups = new DVector<int>(storageBlockSize);
            if (bWantVtxGroups)
                vertex_groups = new DVector<int>(storageBlockSize);

            max_group_id = 0;
            max_vertex_group_id = 0;

            edges = new DVector<int>(storageBlockSize);
            edges_refcount = new RefCountVector(storageBlockSize);
            edge_triangles = new SmallListSet(storageBlockSize);
        }
    }
}
