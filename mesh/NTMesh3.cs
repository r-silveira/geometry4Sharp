using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace g4
{

	//
	// NTMesh3 is a variant of DMesh3 that supports non-manifold mesh topology. 
	// See DMesh3 comments for most details. 
	// Main change is that edges buffer only stores 2-tuple vertex pairs.
	// Each edge can be connected to arbitrary number of triangle, which are
	// stored in edge_triangles
	//
	// per-vertex UVs have been removed (perhaps temporarily)
	//
	// Currently poke-face and split-edge are supported, but not collapse or flip.
	// 
	public partial class NTMesh3 : IDeformableMesh
	{
		public const int InvalidID = -1;
		public const int NonManifoldID = -2;

		public static readonly Vector3d InvalidVertex = new Vector3d(Double.MaxValue, 0, 0);
		public static readonly Index3i InvalidTriangle = new Index3i(InvalidID, InvalidID, InvalidID);
		public static readonly Index2i InvalidEdge = new Index2i(InvalidID, InvalidID);

		RefCountVector vertices_refcount;
		DVector<double> vertices;
		DVector<float> normals;
		DVector<float> colors;

		SmallListSet vertex_edges;

		RefCountVector triangles_refcount;
		DVector<int> triangles;
		DVector<int> triangle_edges;
		DVector<int> triangle_groups;

		RefCountVector edges_refcount;
		DVector<int> edges;
		SmallListSet edge_triangles;

		int timestamp = 0;
		int shape_timestamp = 0;

		int max_group_id = 0;


		public NTMesh3(bool bWantNormals = true, bool bWantColors = false, bool bWantTriGroups = false)
		{
			allocate(bWantNormals, bWantColors, bWantTriGroups);
		}
		public NTMesh3(MeshComponents flags) : 
			this( (flags & MeshComponents.VertexNormals) != 0,  (flags & MeshComponents.VertexColors) != 0,
				  (flags & MeshComponents.FaceGroups) != 0 )
		{
		}

		private void allocate(bool bWantNormals, bool bWantColors, bool bWantTriGroups)
		{
			vertices = new DVector<double>();
			if (bWantNormals)
				normals = new DVector<float>();
			if (bWantColors)
				colors = new DVector<float>();

			vertex_edges = new SmallListSet();

			vertices_refcount = new RefCountVector();

			triangles = new DVector<int>();
			triangle_edges = new DVector<int>();
			triangles_refcount = new RefCountVector();
			if (bWantTriGroups)
				triangle_groups = new DVector<int>();
			max_group_id = 0;

			edges = new DVector<int>();
			edges_refcount = new RefCountVector();
			edge_triangles = new SmallListSet();
		}



		public NTMesh3(NTMesh3 copy) {
			Copy(copy, true, true);
		}

		public void Copy(NTMesh3 copy, bool bNormals = true, bool bColors = true)
		{
			vertices = new DVector<double>(copy.vertices);
			normals = (bNormals && copy.normals != null) ? new DVector<float>(copy.normals) : null;
			colors = (bColors && copy.colors != null) ? new DVector<float>(copy.colors) : null;

			vertices_refcount = new RefCountVector(copy.vertices_refcount);
			vertex_edges = new SmallListSet(copy.vertex_edges);

			triangles = new DVector<int>(copy.triangles);
			triangle_edges = new DVector<int>(copy.triangle_edges);
			triangles_refcount = new RefCountVector(copy.triangles_refcount);
			if (copy.triangle_groups != null)
				triangle_groups = new DVector<int>(copy.triangle_groups);
			max_group_id = copy.max_group_id;

			edges = new DVector<int>(copy.edges);
			edges_refcount = new RefCountVector(copy.edges_refcount);
			edge_triangles = new SmallListSet(copy.edge_triangles);
			updateTimeStamp(true);
		}



		public NTMesh3(DMesh3 copy)
		{
			allocate(copy.HasVertexNormals, copy.HasVertexColors, copy.HasTriangleGroups);

			int[] mapV = new int[copy.MaxVertexID];
			foreach (int vid in copy.VertexIndices())
				mapV[vid] = AppendVertex(copy.GetVertex(vid));
			foreach ( Index3i tri in copy.Triangles()) {
				AppendTriangle(mapV[tri.a], mapV[tri.b], mapV[tri.c]);
			}
		}



		void updateTimeStamp(bool bShapeChange) {
			timestamp++;
			if (bShapeChange)
				shape_timestamp++;
		}
		public int Timestamp {
			get { return timestamp; }
		}
		public int ShapeTimestamp {
			get { return shape_timestamp; }
		}


		// IMesh impl

		public int VertexCount {
			get { return vertices_refcount.count; }
		}
		public int TriangleCount {
			get { return triangles_refcount.count; }
		}
		public int EdgeCount {
			get { return edges_refcount.count; }
		}

		// these values are (max_used+1), ie so an iteration should be < MaxTriangleID, not <=
		public int MaxVertexID {
			get { return vertices_refcount.max_index; }
		}
		public int MaxTriangleID {
			get { return triangles_refcount.max_index; }
		}
		public int MaxEdgeID {
			get { return edges_refcount.max_index; }
		}
		public int MaxGroupID {
			get { return max_group_id; }
		}

		public bool HasVertexColors { get { return colors != null; } }
		public bool HasVertexNormals { get { return normals != null; } }
		public bool HasVertexUVs { get { return false; } }
		public bool HasTriangleGroups { get { return triangle_groups != null; } }

		public MeshComponents Components {
			get {
				MeshComponents c = 0;
				if (normals != null) c |= MeshComponents.VertexNormals;
				if (colors != null) c |= MeshComponents.VertexColors;
				if (triangle_groups != null) c |= MeshComponents.FaceGroups;
				return c;
			}
		}

		// info

		public bool IsVertex(int vID) {
			return vertices_refcount.isValid(vID);
		}
		public bool IsTriangle(int tID) {
			return triangles_refcount.isValid(tID);
		}
		public bool IsEdge(int eID) {
			return edges_refcount.isValid(eID);
		}


		// getters


		public Vector3d GetVertex(int vID) {
			debug_check_is_vertex(vID);
			int i = 3 * vID;
			return new Vector3d(vertices[i], vertices[i + 1], vertices[i + 2]);
		}
		public Vector3f GetVertexf(int vID) {
			debug_check_is_vertex(vID);
			int i = 3 * vID;
			return new Vector3f((float)vertices[i], (float)vertices[i + 1], (float)vertices[i + 2]);
		}

		public void SetVertex(int vID, Vector3d vNewPos) {
			Debug.Assert(vNewPos.IsFinite);     // this will really catch a lot of bugs...
			debug_check_is_vertex(vID);

			int i = 3*vID;
			vertices[i] = vNewPos.x; vertices[i+1] = vNewPos.y; vertices[i+2] = vNewPos.z;
			updateTimeStamp(true);
		}

		public Vector3f GetVertexNormal(int vID) {
			if (normals == null) {
				return Vector3f.AxisY;
			} else {
				debug_check_is_vertex(vID);
				int i = 3 * vID;
				return new Vector3f(normals[i], normals[i + 1], normals[i + 2]);
			}
		}

		public Vector2f GetVertexUV(int i) {
			return Vector2f.Zero;
		}

		public void SetVertexNormal(int vID, Vector3f vNewNormal) {
			if ( HasVertexNormals ) {
				debug_check_is_vertex(vID);
				int i = 3*vID;
				normals[i] = vNewNormal.x; normals[i+1] = vNewNormal.y; normals[i+2] = vNewNormal.z;
				updateTimeStamp(false);
			}
		}

		public Vector3f GetVertexColor(int vID) {
			if (colors == null) { 
				return Vector3f.One;
			} else {
				debug_check_is_vertex(vID);
				int i = 3 * vID;
				return new Vector3f(colors[i], colors[i + 1], colors[i + 2]);
			}
		}

		public void SetVertexColor(int vID, Vector3f vNewColor) {
			if ( HasVertexColors ) {
				debug_check_is_vertex(vID);
				int i = 3*vID;
				colors[i] = vNewColor.x; colors[i+1] = vNewColor.y; colors[i+2] = vNewColor.z;
				updateTimeStamp(false);
			}
		}


		public bool GetVertex(int vID, ref NewVertexInfo vinfo, bool bWantNormals, bool bWantColors, bool bWantUVs)
		{
			if (vertices_refcount.isValid(vID) == false)
				return false;
			vinfo.v.Set(vertices[3 * vID], vertices[3 * vID + 1], vertices[3 * vID + 2]);
			vinfo.bHaveN = vinfo.bHaveUV = vinfo.bHaveC = false;
			if (HasVertexColors && bWantNormals) {
				vinfo.bHaveN = true;
				vinfo.n.Set(normals[3 * vID], normals[3 * vID + 1], normals[3 * vID + 2]);
			}
			if (HasVertexColors && bWantColors) {
				vinfo.bHaveC = true;
				vinfo.c.Set(colors[3 * vID], colors[3 * vID + 1], colors[3 * vID + 2]);
			}
			return true;
		}


		public int GetVtxEdgeCount(int vID) {
			return vertices_refcount.isValid(vID) ? vertex_edges.Count(vID) : -1;
		}


		public int GetMaxVtxEdgeCount() {
			int max = 0;
			foreach (int vid in vertices_refcount)
				max = Math.Max(max, vertex_edges.Count(vid));
			return max;
		}

		public NewVertexInfo GetVertexAll(int i) {
			NewVertexInfo vi = new NewVertexInfo();
			vi.v = GetVertex(i);
			if ( HasVertexNormals ) {
				vi.bHaveN = true;
				vi.n = GetVertexNormal(i);
			} else
				vi.bHaveN = false;
			if ( HasVertexColors ) {
				vi.bHaveC = true;
				vi.c = GetVertexColor(i);
			} else
				vi.bHaveC = false;
			vi.bHaveUV = false;
			return vi;
		}
		



		public Index3i GetTriangle(int tID) {
			debug_check_is_triangle(tID);
			int i = 3 * tID;
			return new Index3i(triangles[i], triangles[i + 1], triangles[i + 2]);
		}

		public Index3i GetTriEdges(int tID) {
			debug_check_is_triangle(tID);
			int i = 3 * tID;
			return new Index3i(triangle_edges[i], triangle_edges[i + 1], triangle_edges[i + 2]);
		}

		public int GetTriEdge(int tid, int j) {
			debug_check_is_triangle(tid);
			return triangle_edges[3*tid+j];
		}


		public IEnumerable<int> TriTrianglesItr(int tID) {
			if (triangles_refcount.isValid(tID)) {
				int tei = 3 * tID;
				for (int j = 0; j < 3; ++j) {
					int eid = triangle_edges[tei + j];
					foreach ( int nbr_t in edge_triangles.ValueItr(eid) ) {
						if (nbr_t != tID)
							yield return nbr_t;
					}
				}
			}
		}

		/// <summary>
		/// efficiently compute centroid of triangle
		/// </summary>
		public Vector3d GetTriCentroid(int tID)
		{
			int ai = 3 * triangles[3 * tID],
				bi = 3 * triangles[3 * tID + 1],
				ci = 3 * triangles[3 * tID + 2];
			double f = (1.0 / 3.0);
			return new Vector3d(
				(vertices[ai] + vertices[bi] + vertices[ci]) * f,
				(vertices[ai + 1] + vertices[bi + 1] + vertices[ci + 1]) * f,
				(vertices[ai + 2] + vertices[bi + 2] + vertices[ci + 2]) * f);
		}


		public int GetTriangleGroup(int tID) { 
			return (triangle_groups == null) ? -1 
				: ( triangles_refcount.isValid(tID) ? triangle_groups[tID] : 0 );
		}

		public void SetTriangleGroup(int tid, int group_id) {
			if ( triangle_groups != null ) {
				debug_check_is_triangle(tid);
				triangle_groups[tid] = group_id;
				max_group_id = Math.Max(max_group_id, group_id+1);
				updateTimeStamp(false);
			}
		}

		public int AllocateTriangleGroup() {
			return max_group_id++;
		}


		public void GetTriVertices(int tID, ref Vector3d v0, ref Vector3d v1, ref Vector3d v2) {
			int ai = 3 * triangles[3 * tID];
			v0.x = vertices[ai]; v0.y = vertices[ai + 1]; v0.z = vertices[ai + 2];
			int bi = 3 * triangles[3 * tID + 1];
			v1.x = vertices[bi]; v1.y = vertices[bi + 1]; v1.z = vertices[bi + 2];
			int ci = 3 * triangles[3 * tID + 2];
			v2.x = vertices[ci]; v2.y = vertices[ci + 1]; v2.z = vertices[ci + 2];
		}

		public Vector3d GetTriVertex(int tid, int j) {
			int a = triangles[3 * tid + j];
			return new Vector3d(vertices[3 * a], vertices[3 * a + 1], vertices[3 * a + 2]);
		}


		public Vector3d GetTriNormal(int tID)
		{
			Vector3d v0 = Vector3d.Zero, v1 = Vector3d.Zero, v2 = Vector3d.Zero;
			GetTriVertices(tID, ref v0, ref v1, ref v2);
			return MathUtil.Normal(ref v0, ref v1, ref v2);
		}

		public double GetTriArea(int tID)
		{
			Vector3d v0 = Vector3d.Zero, v1 = Vector3d.Zero, v2 = Vector3d.Zero;
			GetTriVertices(tID, ref v0, ref v1, ref v2);
			return MathUtil.Area(ref v0, ref v1, ref v2);
		}

		/// <summary>
		/// Compute triangle normal, area, and centroid all at once. Re-uses vertex
		/// lookups and computes normal & area simultaneously. *However* does not produce
		/// the same normal/area as separate calls, because of this.
		/// </summary>
		public void GetTriInfo(int tID, out Vector3d normal, out double fArea, out Vector3d vCentroid)
		{
			Vector3d v0 = Vector3d.Zero, v1 = Vector3d.Zero, v2 = Vector3d.Zero;
			GetTriVertices(tID, ref v0, ref v1, ref v2);
			vCentroid = (1.0 / 3.0) * (v0 + v1 + v2);
			normal = MathUtil.FastNormalArea(ref v0, ref v1, ref v2, out fArea);
		}




		public AxisAlignedBox3d GetTriBounds(int tID)
		{
			int vi = 3 * triangles[3 * tID];
			double x = vertices[vi], y = vertices[vi + 1], z = vertices[vi + 2];
			double minx = x, maxx = x, miny = y, maxy = y, minz = z, maxz = z;
			for (int i = 1; i < 3; ++i) {
				vi = 3 * triangles[3 * tID + i];
				x = vertices[vi]; y = vertices[vi + 1]; z = vertices[vi + 2];
				if (x < minx) minx = x; else if (x > maxx) maxx = x;
				if (y < miny) miny = y; else if (y > maxy) maxy = y;
				if (z < minz) minz = z; else if (z > maxz) maxz = z;
			}
			return new AxisAlignedBox3d(minx, miny, minz, maxx, maxy, maxz);
		}


		public Frame3f GetTriFrame(int tID, int nEdge = 0)
		{
			int ti = 3 * tID;
			int a = triangles[ti + (nEdge % 3)];
			int b = triangles[ti + ((nEdge+1) % 3)];
			int c = triangles[ti + ((nEdge+2) % 3)];
			Vector3d v0 = new Vector3d(vertices[3 * a], vertices[3 * a + 1], vertices[3 * a + 2]);
			Vector3d v1 = new Vector3d(vertices[3 * b], vertices[3 * b + 1], vertices[3 * b + 2]);
			Vector3d v2 = new Vector3d(vertices[3 * c], vertices[3 * c + 1], vertices[3 * c + 2]);
			Vector3f edge = (Vector3f)(v1 - v0).Normalized;
			Vector3f normal = (Vector3f)MathUtil.Normal(ref v0, ref v1, ref v2);
			Vector3f other = edge.Cross(normal);
			Vector3f center = (Vector3f)(v0 + v1 + v2) / 3;
			return new Frame3f(center, edge, other, normal);
		}





		public Index2i GetEdgeV(int eID) {
			debug_check_is_edge(eID);
			int i = 2 * eID;
			return new Index2i(edges[i], edges[i + 1]);
		}
		public bool GetEdgeV(int eID, ref Vector3d a, ref Vector3d b) {
			debug_check_is_edge(eID);
			int iv0 = 3 * edges[2 * eID];
			a.x = vertices[iv0]; a.y = vertices[iv0 + 1]; a.z = vertices[iv0 + 2];
			int iv1 = 3 * edges[2 * eID + 1];
			b.x = vertices[iv1]; b.y = vertices[iv1 + 1]; b.z = vertices[iv1 + 2];
			return true;
		}


		public IEnumerable<int> EdgeTrianglesItr(int eID)
		{
			return edge_triangles.ValueItr(eID);
		}

		public int EdgeTrianglesCount(int eID)
		{
			return edge_triangles.Count(eID);
		}

		public Vector3d GetEdgePoint(int eID, double t)
		{
			if (edges_refcount.isValid(eID))
			{
				int iv0 = 3 * edges[2 * eID];
				int iv1 = 3 * edges[2 * eID + 1];
				double mt = 1.0 - t;
				return new Vector3d(
					mt * vertices[iv0] + t * vertices[iv1],
					mt * vertices[iv0 + 1] + t * vertices[iv1 + 1],
					mt * vertices[iv0 + 2] + t * vertices[iv1 + 2]);
			}
			Util.gDevAssert(false);
			return Vector3d.Zero;
		}


		// return same indices as GetEdgeV, but oriented based on attached triangle
		public Index2i GetOrientedBoundaryEdgeV(int eID)
		{
			if ( edges_refcount.isValid(eID) && edge_is_boundary(eID) ) {
				var eIndices = GetEdgeV(eID);
				int a = eIndices.a, b = eIndices.b;

				int ti = edge_triangles.First(eID);
				//Index3i tri = new Index3i(triangles[ti], triangles[ti + 1], triangles[ti + 2]);
				var tri = GetTriangle(ti);
				int ai = IndexUtil.find_edge_index_in_tri(a, b, ref tri);
				return new Index2i(tri[ai], tri[(ai + 1) % 3]);
			}
			Util.gDevAssert(false);
			return InvalidEdge;
		}


		// mesh-building

		/// <summary>
		/// Pre-allocates memory for a specified number of vertices to improve performance.
		/// </summary>
		/// <remarks>
		/// Call this method before adding a large number of vertices to avoid multiple
		/// internal array re-allocations, which can be a performance bottleneck.
		/// </remarks>
		/// <param name="vertexCount">The number of vertices to reserve capacity for.</param>
		public void ReserveVertices(int vertexCount)
		{
			vertices.resize(vertexCount * 3);
			if (HasVertexNormals)
			{
				normals.resize(vertexCount * 3);
			}
			if (HasVertexColors)
			{
				colors.resize(vertexCount * 3);
			}
		}

		/// <summary>
		/// Pre-allocates memory for a specified number of triangles to improve performance.
		/// </summary>
		/// <remarks>
		/// Call this method before adding a large number of triangles to avoid multiple
		/// internal array re-allocations.
		/// </remarks>
		/// <param name="triangleCount">The number of triangles to reserve capacity for.</param>
		public void ReserveTriangles(int triangleCount)
		{
			triangles.resize(triangleCount * 3);
			triangle_edges.resize(triangleCount * 3);

			if (HasTriangleGroups)
			{
				triangle_groups.resize(triangleCount);
			}
			int estimatedEdges = triangleCount + (triangleCount / 2);
			edges.resize(estimatedEdges * 2);
		}

		public int AppendVertex(Vector3d v) {
			return AppendVertex(new NewVertexInfo() {
				v = v, bHaveC = false, bHaveUV = false, bHaveN = false
			});
		}

		/// <summary>
		/// copy vertex fromVID from existing source mesh, returns new vid
		/// </summary>
		public int AppendVertex(NTMesh3 from, int fromVID)
		{
			return AppendVertex(from.GetVertex(fromVID));
		}

		public int AppendVertex(NewVertexInfo info)
		{
			int vid = vertices_refcount.allocate();
			int i = 3*vid;
			vertices.insert(info.v[2], i + 2);
			vertices.insert(info.v[1], i + 1);
			vertices.insert(info.v[0], i);

			if ( normals != null ) {
				Vector3f n = (info.bHaveN) ? info.n : Vector3f.AxisY;
				normals.insert(n[2], i + 2);
				normals.insert(n[1], i + 1);
				normals.insert(n[0], i);
			}

			if ( colors != null ) {
				Vector3f c = (info.bHaveC) ? info.c : Vector3f.One;
				colors.insert(c[2], i + 2);
				colors.insert(c[1], i + 1);
				colors.insert(c[0], i);
			}

			allocate_vertex_edges_list(vid);

			updateTimeStamp(true);
			return vid;
		}
		

		public int AppendTriangle(int v0, int v1, int v2, int gid = -1) {
			return AppendTriangle(new Index3i(v0, v1, v2), gid);
		}
		public int AppendTriangle(Index3i tv, int gid = -1) {
			if (IsVertex(tv[0]) == false || IsVertex(tv[1]) == false || IsVertex(tv[2]) == false) {
				Util.gDevAssert(false);
				return InvalidID;
			}
			if (tv[0] == tv[1] || tv[0] == tv[2] || tv[1] == tv[2]) {
				Util.gDevAssert(false);
				return InvalidID;
			}

			// look up edges. 
			int e0 = find_edge(tv[0], tv[1]);
			int e1 = find_edge(tv[1], tv[2]);
			int e2 = find_edge(tv[2], tv[0]);

			// now safe to insert triangle
			int tid = triangles_refcount.allocate();
			int i = 3*tid;
			triangles.insert(tv[2], i + 2);
			triangles.insert(tv[1], i + 1);
			triangles.insert(tv[0], i);
			if (triangle_groups != null) {
				triangle_groups.insert(gid, tid);
				max_group_id = Math.Max(max_group_id, gid+1);
			}

			// increment ref counts and update/create edges
			vertices_refcount.increment(tv[0]);
			vertices_refcount.increment(tv[1]);
			vertices_refcount.increment(tv[2]);

			add_tri_edge(tid, tv[0], tv[1], 0, e0);
			add_tri_edge(tid, tv[1], tv[2], 1, e1);
			add_tri_edge(tid, tv[2], tv[0], 2, e2);

			updateTimeStamp(true);
			return tid;
		}
		// helper fn for above, just makes code cleaner
		void add_tri_edge(int tid, int v0, int v1, int j, int eid)
		{
			if (eid != InvalidID) {
				edge_triangles.Insert(eid, tid);
				triangle_edges.insert(eid, 3 * tid + j);
			} else {
				eid = add_edge(v0, v1, tid);
				triangle_edges.insert(eid, 3 * tid + j);
			}
		}


		/// <summary>
		/// Remove vertex vID, and all connected triangles if bRemoveAllTriangles = true
		/// (if false, them throws exception if there are still any triangles!)
		/// if bPreserveManifold, checks that we will not create a bowtie vertex first
		/// </summary>
		public MeshResult RemoveVertex(int vID, bool bRemoveAllTriangles = true, bool bPreserveManifold = false)
		{
			if (vertices_refcount.isValid(vID) == false)
				return MeshResult.Failed_NotAVertex;

			if (bRemoveAllTriangles)
			{

				// if any one-ring vtx is a boundary vtx and one of its outer-ring edges is an
				// interior edge then we will create a bowtie if we remove that triangle
				if (bPreserveManifold)
				{
					foreach (int tid in VtxTrianglesItr(vID))
					{
						Index3i tri = GetTriangle(tid);
						int j = IndexUtil.find_tri_index(vID, ref tri);
						int oa = tri[(j + 1) % 3], ob = tri[(j + 2) % 3];
						int eid = find_edge(oa, ob);
						if (IsBoundaryEdge(eid))
							continue;
						if (IsBoundaryVertex(oa) || IsBoundaryVertex(ob))
							return MeshResult.Failed_WouldCreateBowtie;
					}
				}

				List<int> tris = new List<int>();
				GetVtxTriangles(vID, tris);
				foreach (int tID in tris)
				{
					MeshResult result = RemoveTriangle(tID, false, bPreserveManifold);
					if (result != MeshResult.Ok)
						return result;
				}
			}

			if (vertices_refcount.refCount(vID) != 1)
				throw new NotImplementedException("NTMesh3.RemoveVertex: vertex is still referenced");

			vertices_refcount.decrement(vID);
			Debug.Assert(vertices_refcount.isValid(vID) == false);
			vertex_edges.Clear(vID);

			updateTimeStamp(true);
			return MeshResult.Ok;
		}



		public void EnableVertexNormals(Vector3f initial_normal)
		{
			if (HasVertexNormals)
				return;
			normals = new DVector<float>();
			int NV = MaxVertexID;
			normals.resize(3*NV);
			for (int i = 0; i < NV; ++i) {
				int vi = 3 * i;
				normals[vi] = initial_normal.x;
				normals[vi + 1] = initial_normal.y;
				normals[vi + 2] = initial_normal.z;
			}
		}
		public void DiscardVertexNormals() {
			normals = null;
		}

		public void EnableVertexColors(Vector3f initial_color)
		{
			if (HasVertexColors)
				return;
			colors = new DVector<float>();
			int NV = MaxVertexID;
			colors.resize(3*NV);
			for (int i = 0; i < NV; ++i) {
				int vi = 3 * i;
				colors[vi] = initial_color.x;
				colors[vi + 1] = initial_color.y;
				colors[vi + 2] = initial_color.z;
			}
		}
		public void DiscardVertexColors() {
			colors= null;
		}


		public void EnableTriangleGroups(int initial_group = 0)
		{
			if (HasTriangleGroups)
				return;
			triangle_groups = new DVector<int>();
			int NT = MaxTriangleID;
			triangle_groups.resize(NT);
			for (int i = 0; i < NT; ++i)
				triangle_groups[i] = initial_group;
			max_group_id = 0;
		}
		public void DiscardTriangleGroups() {
			triangle_groups = null;
			max_group_id = 0;
		}









		// iterators

		public IEnumerable<int> VertexIndices() {
			foreach (int vid in vertices_refcount)
				yield return vid;
		}
		public IEnumerable<int> TriangleIndices() {
			foreach (int tid in triangles_refcount)
				yield return tid;
		}
		public IEnumerable<int> EdgeIndices() {
			foreach (int eid in edges_refcount)
				yield return eid;
		}


		public IEnumerable<int> BoundaryEdgeIndices() {
			foreach ( int eid in edges_refcount ) {
				if ( edge_triangles.Count(eid) == 1 )
					yield return eid;
			}
		}


		public IEnumerable<Vector3d> Vertices() {
			foreach (int vid in vertices_refcount) {
				int i = 3 * vid;
				yield return new Vector3d(vertices[i], vertices[i + 1], vertices[i + 2]);
			}
		}
		public IEnumerable<Index3i> Triangles() {
			foreach (int tid in triangles_refcount) {
				int i = 3 * tid;
				yield return new Index3i(triangles[i], triangles[i + 1], triangles[i + 2]);
			}
		}


		// queries

		// linear search through edges of vA
		public int FindEdge(int vA, int vB) {
			debug_check_is_vertex(vA);
			debug_check_is_vertex(vB);
			return find_edge(vA, vB);
		}

		// faster than FindEdge
		public int FindEdgeFromTri(int vA, int vB, int t) {
			return find_edge_from_tri(vA, vB, t);
		}

		// [RMS] not just 2 in NTMesh...
   //     public Index2i GetEdgeOpposingV(int eID)
   //     {
			//// ** it is important that verts returned maintain [c,d] order!!
			//int i = 4*eID;
   //         int a = edges[i], b = edges[i + 1];
   //         int t0 = edges[i + 2], t1 = edges[i + 3];
			//int c = IndexUtil.find_tri_other_vtx(a, b, triangles, t0);
   //         if (t1 != InvalidID) {
			//	int d = IndexUtil.find_tri_other_vtx(a, b, triangles, t1);
   //             return new Index2i(c, d);
   //         } else
   //             return new Index2i(c, InvalidID);
   //     }



		public IEnumerable<int> VtxVerticesItr(int vID) {
			if ( vertices_refcount.isValid(vID) ) {
				foreach ( int eid in vertex_edges.ValueItr(vID) )
					yield return edge_other_v(eid, vID);
			}
		}


		public IEnumerable<int> VtxEdgesItr(int vID) {
			if ( vertices_refcount.isValid(vID) ) {
				return vertex_edges.ValueItr(vID);
			}
			return Enumerable.Empty<int>();
		}


		/// <summary>
		/// Returns count of boundary edges at vertex
		/// </summary>
		public List<int> VtxBoundaryEdges(int vID)
		{
			if ( vertices_refcount.isValid(vID) ) {
				var result = new List<int>();
				foreach (int eid in vertex_edges.ValueItr(vID)) {
					if (edge_triangles.Count(eid) == 1)
						result.Add(eid);
				}
				return result;
			}
			return new List<int>();
		}

		/// <summary>
		/// Returns count of boundary edges at vertex
		/// </summary>
		public int CountVtxBoundaryEdges(int vID)
		{
			if ( vertices_refcount.isValid(vID) ) {
				int count = 0;
				foreach (int eid in vertex_edges.ValueItr(vID)) {
					if (edge_triangles.Count(eid) == 1)
						count++;
				}
				return count;
			}
			Debug.Assert(false);
			return -1;
		}

		/// <summary>
		/// e needs to be large enough (ie call VtxBoundaryEdges, or as large as max one-ring)
		/// returns count, ie number of elements of e that were filled
		/// </summary>
		public int VtxAllBoundaryEdges(int vID, int[] e)
		{
			if (vertices_refcount.isValid(vID)) {
				int count = 0;
				foreach (int eid in vertex_edges.ValueItr(vID)) {
					if (edge_triangles.Count(eid) == 1)
						e[count++] = eid;
				}
				return count;
			}
			Debug.Assert(false);
			return -1;
		}



		public MeshResult GetVtxTriangles(int vID, List<int> vTriangles)
		{
			if (!IsVertex(vID))
				return MeshResult.Failed_NotAVertex;

			foreach (int eid in vertex_edges.ValueItr(vID)) {
				foreach (int tid in edge_triangles.ValueItr(eid)) {
					if (vTriangles.Contains(tid) == false)
						vTriangles.Add(tid);
				}
			}
			return MeshResult.Ok;
		}


		/// <summary>
		/// return # of triangles attached to vID, or -1 if invalid vertex
		/// </summary>
		public int GetVtxTriangleCount(int vID, bool bBruteForce = false)
		{
			List<int> vTriangles = new List<int>();
			if (GetVtxTriangles(vID, vTriangles) != MeshResult.Ok)
				return -1;
			return vTriangles.Count;

		}


		public IEnumerable<int> VtxTrianglesItr(int vID) {
			if ( IsVertex(vID) ) {
				List<int> tris = new List<int>();
				GetVtxTriangles(vID, tris);
				foreach (int tid in tris)
					yield return tid;
			}
		}



		protected bool tri_has_v(int tID, int vID) {
			int i = 3*tID;
			return triangles[i] == vID 
				|| triangles[i + 1] == vID
				|| triangles[i + 2] == vID;
		}

		protected bool tri_is_boundary(int tID) {
			int i = 3*tID;
			return edge_is_boundary(triangle_edges[i])
				|| edge_is_boundary(triangle_edges[i + 1])
				|| edge_is_boundary(triangle_edges[i + 2]);
		}

		protected bool tri_has_neighbour_t(int tCheck, int tNbr) {
			int i = 3*tCheck;
			return edge_has_t(triangle_edges[i], tNbr)
				|| edge_has_t(triangle_edges[i + 1], tNbr)
				|| edge_has_t(triangle_edges[i + 2], tNbr);
		}

		protected bool tri_has_sequential_v(int tID, int vA, int vB)
		{
			int i = 3*tID;
			int v0 = triangles[i], v1 = triangles[i + 1], v2 = triangles[i + 2];
			if (v0 == vA && v1 == vB) return true;
			if (v1 == vA && v2 == vB) return true;
			if (v2 == vA && v0 == vB) return true;
			return false;
		}

		//! returns edge ID
		protected int find_tri_neighbour_edge(int tID, int vA, int vB)
		{
			int i = 3*tID;
			int tv0 = triangles[i], tv1 = triangles[i+1];
			if ( IndexUtil.same_pair_unordered(tv0, tv1, vA, vB) ) return triangle_edges[3*tID];
			int tv2 = triangles[i+2];
			if ( IndexUtil.same_pair_unordered(tv1, tv2, vA, vB) ) return triangle_edges[3*tID+1];
			if ( IndexUtil.same_pair_unordered(tv2, tv0, vA, vB) ) return triangle_edges[3*tID+2];
			return InvalidID;	
		}

		// returns 0/1/2
		protected int find_tri_neighbour_index(int tID, int vA, int vB)
		{
			int i = 3*tID;
			int tv0 = triangles[i], tv1 = triangles[i+1];
			if ( IndexUtil.same_pair_unordered(tv0, tv1, vA, vB) ) return 0;
			int tv2 = triangles[i+2];
			if ( IndexUtil.same_pair_unordered(tv1, tv2, vA, vB) ) return 1;
			if ( IndexUtil.same_pair_unordered(tv2, tv0, vA, vB) ) return 2;
			return InvalidID;	
		}


		public bool IsNonManifoldEdge(int eid)
		{
			return edge_triangles.Count(eid) > 2;
		}
		public bool IsBoundaryEdge(int eid) {
			return edge_triangles.Count(eid) == 1;
		}


		protected bool edge_is_boundary(int eid) {
			return edge_triangles.Count(eid) == 1;
		}
		protected bool edge_has_v(int eid, int vid) {
			int i = 2*eid;
			return (edges[i] == vid) || (edges[i + 1] == vid);
		}
		protected bool edge_has_t(int eid, int tid) {
			return edge_triangles.Contains(eid, tid);
		}
		public int edge_other_v(int eID, int vID)
		{
			int i = 2*eID;
			int ev0 = edges[i], ev1 = edges[i + 1];
			return (ev0 == vID) ? ev1 : ((ev1 == vID) ? ev0 : InvalidID);
		}
		//protected int edge_other_t(int eID, int tid) {
		//    int i = 4 * eID;
		//    int et0 = edges[i + 2], et1 = edges[i + 3];
		//    return (et0 == tid) ? et1 : ((et1 == tid) ? et0 : InvalidID);
		//}


		// ugh need to deprecate this...weird API!
		public bool vertex_is_boundary(int vID) {
			return IsBoundaryVertex(vID);
		}
		public bool IsBoundaryVertex(int vID) {
			foreach (int eid in vertex_edges.ValueItr(vID)) {
				if (edge_triangles.Count(eid) == 1)
					return true;
			}
			return false;
		}


		public bool IsBoundaryTriangle(int tID)
		{
			debug_check_is_triangle(tID);
			int i = 3 * tID;
			return IsBoundaryEdge(triangle_edges[i]) || IsBoundaryEdge(triangle_edges[i + 1]) || IsBoundaryEdge(triangle_edges[i + 2]);
		}



		int find_edge(int vA, int vB)
		{
			// [RMS] edge vertices must be sorted (min,max),
			//   that means we only need one index-check in inner loop.
			//   commented out code is robust to incorrect ordering, but slower.
			int vO = Math.Max(vA, vB);
			int vI = Math.Min(vA, vB);
			foreach (int eid in vertex_edges.ValueItr(vI)) {
				if (edges[2 * eid + 1] == vO)
					//if (edge_has_v(eid, vO))
					return eid;
			}
			return InvalidID;

			// this is slower, likely because it creates new func<> every time. can we do w/o that?
			//return vertex_edges.Find(vI, (eid) => { return edges[4 * eid + 1] == vO; }, InvalidID);
		}

		int find_edge_from_tri(int vA, int vB, int tID)
		{
			int i = 3 * tID;
			int t0 = triangles[i], t1 = triangles[i + 1];
			if (IndexUtil.same_pair_unordered(vA, vB, t0, t1))
				return triangle_edges[i];
			int t2 = triangles[i + 2];
			if (IndexUtil.same_pair_unordered(vA, vB, t1, t2))
				return triangle_edges[i+1];
			if (IndexUtil.same_pair_unordered(vA, vB, t2, t0))
				return triangle_edges[i+2];
			return InvalidID;
		}

		/// <summary>
		/// Find triangle made up of any permutation of vertices [a,b,c]
		/// </summary>
		public int FindTriangle(int a, int b, int c)
		{
			int eid = find_edge(a, b);
			if (eid == InvalidID)
				return InvalidID;

			foreach (var tid in EdgeTrianglesItr(eid))
			{
				var triVertices = GetTriangle(tid);
				if (triVertices.a == c || triVertices.b == c || triVertices.c == c)
				{
					return tid;
				}
			}

			return InvalidID;
		}


		/// <summary>
		/// returns true if vID is a "bowtie" vertex, ie multiple disjoint triangle sets in one-ring
		/// </summary>
		//public bool IsBowtieVertex(int vID)
		//{
		//    if (vertices_refcount.isValid(vID)) {
		//        int nTris = GetVtxTriangleCount(vID);
		//        int vtx_edge_count = GetVtxEdgeCount(vID);
		//        if (!(nTris == vtx_edge_count || nTris == vtx_edge_count - 1))
		//            return true;
		//        return false;
		//    } else
		//        throw new Exception("NTMesh3.IsBowtieVertex: " + vID + " is not a valid vertex");
		//}


		/// <summary>
		/// returns true if vID is a "bowtie" vertex, ie multiple disjoint triangle sets in one-ring
		/// </summary>
		public bool IsBowtieVertex(int vID)
		{
			if (vertices_refcount.isValid(vID))
			{
				int nEdges = vertex_edges.Count(vID);
				if (nEdges == 0)
					return false;

				// find a boundary edge to start at
				int start_eid = -1;
				bool start_at_boundary = false;
				foreach (int eid in vertex_edges.ValueItr(vID))
				{
					if (IsBoundaryEdge(eid))
					{
						start_at_boundary = true;
						start_eid = eid;
						break;
					}
				}
				// if no boundary edge, start at arbitrary edge
				if (start_eid == -1)
					start_eid = vertex_edges.First(vID);
				// initial triangle
				int start_tid = EdgeTrianglesItr(start_eid).First();

				int prev_tid = start_tid;
				int prev_eid = start_eid;

				// walk forward to next edge. if we hit start edge or boundary edge,
				// we are done the walk. count number of edges as we go.
				int count = 1;
				var stack = new List<(int next_eid, int next_tid)>();
				var visited = new HashSet<int>() { };
				while (true)
				{
					visited.Add(prev_tid);
					int i = 3 * prev_tid;
					Index3i tv = new Index3i(triangles[i], triangles[i + 1], triangles[i + 2]);
					Index3i te = new Index3i(triangle_edges[i], triangle_edges[i + 1], triangle_edges[i + 2]);
					int vert_idx = IndexUtil.find_tri_index(vID, ref tv);
					int e1 = te[vert_idx], e2 = te[(vert_idx + 2) % 3];
					
					int next_eid = (e1 == prev_eid) ? e2 : e1;
					var next_eid_tris = EdgeTrianglesItr(next_eid).Except(visited);

					stack.AddRange(next_eid_tris.Select(ntid => (next_eid, ntid)));
					if (stack.Count == 0)
						break;

					var next = stack[0];
					stack.RemoveAt(0);

					prev_eid = next.next_eid;
					prev_tid = next.next_tid;
					count++;
				}

				// if we did not see all edges at vertex, we have a bowtie
				int target_count = (start_at_boundary) ? nEdges - 1 : nEdges;
				bool is_bowtie = (target_count != count);
				return is_bowtie;

			}
			else
				throw new Exception("DMesh3.IsBowtieVertex: " + vID + " is not a valid vertex");
		}


		// compute vertex bounding box
		public AxisAlignedBox3d GetBounds()
		{
			double x = 0, y = 0, z = 0;
			foreach ( int vi in vertices_refcount ) {
				x = vertices[3*vi]; y = vertices[3*vi + 1]; z = vertices[3*vi + 2];
				break;
			}
			double minx = x, maxx = x, miny = y, maxy = y, minz = z, maxz = z;
			foreach ( int vi in vertices_refcount ) {
				x = vertices[3*vi]; y = vertices[3*vi + 1]; z = vertices[3*vi + 2];
				if (x < minx) minx = x; else if (x > maxx) maxx = x;
				if (y < miny) miny = y; else if (y > maxy) maxy = y;
				if (z < minz) minz = z; else if (z > maxz) maxz = z;
			}
			return new AxisAlignedBox3d(minx, miny, minz, maxx, maxy, maxz);
		}

		AxisAlignedBox3d cached_bounds;
		int cached_bounds_timestamp = -1;

		//! cached bounding box, lazily re-computed on access if mesh has changed
		public AxisAlignedBox3d CachedBounds
		{
			get {
				if (cached_bounds_timestamp != Timestamp) {
					cached_bounds = GetBounds();
					cached_bounds_timestamp = Timestamp;
				}
				return cached_bounds;
			}
		}




		bool cached_is_closed = false;
		int cached_is_closed_timstamp = -1;

		bool cached_has_non_manifold_edge = false;
		int cached_has_non_manifold_edge_timestamp = -1;

		bool cached_has_self_intersections = false;
		int cached_has_self_intersections_timestamp = -1;

		int cached_number_of_components = 0;
		int cached_number_of_components_timestamp = -1;

		public bool IsClosed() {
			if (TriangleCount == 0)
				return false;
			// [RMS] under possibly-mistaken belief that foreach() has some overhead...
			if (MaxEdgeID / EdgeCount > 5) {
				foreach (int eid in edges_refcount)
					if (edge_is_boundary(eid))
						return false;
			} else {
				int N = MaxEdgeID;
				for (int i = 0; i < N; ++i)
					if (edges_refcount.isValid(i) && edge_is_boundary(i))
						return false;
			}
			return true;            
		}

		public bool HasNonManifoldEdge()
		{
			for (int i = 0; i < EdgeCount; i++)
			{
				if (IsEdge(i) && IsNonManifoldEdge(i))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasBowtieVertex()
		{
			for (int i = 0; i < VertexCount; i++)
			{
				if (IsVertex(i) && IsBowtieVertex(i))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasSelfIntersections()
		{
			var tree = new NTMeshAABBTree3(this, true);
			return tree.FindAllSelfIntersectionsTriangles().TrianglePairs.Count > 0;
		}

		public int NumberOfComponents()
		{
			var meshConnectedComponents = new NTMeshConnectedComponents(this);
			meshConnectedComponents.FindConnectedT();
			return meshConnectedComponents.Count;
		}

		public bool CachedIsClosed {
			get {
				if (cached_is_closed_timstamp != Timestamp) {
					cached_is_closed = IsClosed();
					cached_is_closed_timstamp = Timestamp;
				}
				return cached_is_closed;
			}
		}

		public bool CachedHasNonManifoldEdge {
			get {
				if (cached_has_non_manifold_edge_timestamp != Timestamp) {
					cached_has_non_manifold_edge = HasNonManifoldEdge();
					cached_has_non_manifold_edge_timestamp = Timestamp;
				}
				return cached_has_non_manifold_edge;
			}
		}

		public bool CachedHasSelfIntersections {
			get {
				if (cached_has_self_intersections_timestamp != Timestamp) {
					cached_has_self_intersections = HasSelfIntersections();
					cached_has_self_intersections_timestamp = Timestamp;
				}
				return cached_has_self_intersections;
			}
		}

		public int CachedNumberOfComponents {
			get {
				if (cached_number_of_components_timestamp != Timestamp) {
					cached_number_of_components = NumberOfComponents();
					cached_number_of_components_timestamp = Timestamp;
				}
				return cached_number_of_components;
			}
		}


		public bool IsCompact {
			get { return vertices_refcount.is_dense && edges_refcount.is_dense && triangles_refcount.is_dense; }
		}
		public bool IsCompactV {
			get { return vertices_refcount.is_dense; }
		}



		














	   // internal

		void set_triangle(int tid, int v0, int v1, int v2)
		{
			int i = 3*tid;
			triangles[i] = v0;
			triangles[i + 1] = v1;
			triangles[i + 2] = v2;
		}
		void set_triangle_edges(int tid, int e0, int e1, int e2)
		{
			int i = 3*tid;
			triangle_edges[i] = e0;
			triangle_edges[i + 1] = e1;
			triangle_edges[i + 2] = e2;
		}

		int add_edge(int vA, int vB, int tA, int tB = InvalidID)
		{
			if (vB < vA) {
				int t = vB; vB = vA; vA = t;
			}
			int eid = edges_refcount.allocate();
			allocate_edge_triangles_list(eid);

			int i = 2*eid;
			edges.insert(vA, i);
			edges.insert(vB, i + 1);

			if ( tA != InvalidID )
				edge_triangles.Insert(eid, tA);
			if ( tB != InvalidID )
				edge_triangles.Insert(eid, tB);

			vertex_edges.Insert(vA, eid);
			vertex_edges.Insert(vB, eid);
			return eid;
		}

		int replace_tri_vertex(int tID, int vOld, int vNew) {
			int i = 3*tID;
			if ( triangles[i] == vOld ) { triangles[i] = vNew; return 0; }
			if ( triangles[i+1] == vOld ) { triangles[i+1] = vNew; return 1; }
			if ( triangles[i+2] == vOld ) { triangles[i+2] = vNew; return 2; }
			return -1;
		}

		int add_triangle_only(int a, int b, int c, int e0, int e1, int e2) {
			int tid = triangles_refcount.allocate();
			int i = 3*tid;
			triangles.insert(c, i + 2);
			triangles.insert(b, i + 1);
			triangles.insert(a, i);
			triangle_edges.insert(e2, i+2);
			triangle_edges.insert(e1, i+1);
			triangle_edges.insert(e0, i+0);	
			return tid;
		}



		void allocate_vertex_edges_list(int vid)
		{
			if ( vid < vertex_edges.Size )
				vertex_edges.Clear(vid);
			vertex_edges.AllocateAt(vid);
		}
		List<int> vertex_edges_list(int vid)
		{
			return new List<int>( vertex_edges.ValueItr(vid) );
		}


		void allocate_edge_triangles_list(int eid)
		{
			if (eid < edge_triangles.Size)
				edge_triangles.Clear(eid);
			edge_triangles.AllocateAt(eid);
		}


		void set_edge_vertices(int eID, int a, int b) {
			int i = 2*eID;
			edges[i] = Math.Min(a,b);
			edges[i + 1] = Math.Max(a,b);
		}

		int replace_edge_vertex(int eID, int vOld, int vNew) {
			int i = 2*eID;
			int a = edges[i], b = edges[i+1];
			if ( a == vOld ) {
				edges[i] = Math.Min(b, vNew);
				edges[i+1] = Math.Max(b, vNew);
				return 0;
			} else if ( b == vOld ) {
				edges[i] = Math.Min(a, vNew);
				edges[i+1] = Math.Max(a, vNew);
				return 1;				
			} else
				return -1;
		}


		bool replace_edge_triangle(int eID, int tOld, int tNew) {
			bool found = edge_triangles.Remove(eID, tOld);
			edge_triangles.Insert(eID, tNew);
			return found;
		}

		void add_edge_triangle(int eID, int tID)
		{
			edge_triangles.Insert(eID, tID);
		}
		bool remove_edge_triangle(int eID, int tID)
		{
			return edge_triangles.Remove(eID, tID);
		}

		int replace_triangle_edge(int tID, int eOld, int eNew) {
			int i = 3*tID;
			if ( triangle_edges[i] == eOld ) {
				triangle_edges[i] = eNew;
				return 0;
			} else if ( triangle_edges[i+1] == eOld ) {
				triangle_edges[i+1] = eNew;
				return 1;
			} else if ( triangle_edges[i+2] == eOld ) {
				triangle_edges[i+2] = eNew;
				return 2;
			} else
				return -1;
		}













		/// <summary>
		/// Remove a tID from the mesh. Also removes any unreferenced edges after tri is removed.
		/// If bRemoveIsolatedVertices is false, then if you remove all tris from a vert, that vert is also removed.
		/// If bPreserveManifold, we check that you will not create a bowtie vertex (and return false).
		///   If this check is not done, you have to make sure you don't create a bowtie, because other
		///   code assumes we don't have bowties, and will not handle it properly
		/// </summary>
		public MeshResult RemoveTriangle(int tID, bool bRemoveIsolatedVertices = true)
		{
			if (!triangles_refcount.isValid(tID)) {
				Debug.Assert(false);
				return MeshResult.Failed_NotATriangle;
			}

			Index3i tv = GetTriangle(tID);
			Index3i te = GetTriEdges(tID);

			// Remove triangle from its edges. if edge has no triangles left,
			// then it is removed.
			for (int j = 0; j < 3; ++j) {
				int eid = te[j];
				remove_edge_triangle(eid, tID);
				if ( edge_triangles.Count(eid) == 0 ) { 
					int a = edges[2 * eid];
					vertex_edges.Remove(a, eid);
					int b = edges[2 * eid + 1];
					vertex_edges.Remove(b, eid);
					edges_refcount.decrement(eid);
				}
			}

			// free this triangle
			triangles_refcount.decrement(tID);
			Debug.Assert(triangles_refcount.isValid(tID) == false);

			// Decrement vertex refcounts. If any hit 1 and we got remove-isolated flag,
			// we need to remove that vertex
			for (int j = 0; j < 3; ++j) {
				int vid = tv[j];
				vertices_refcount.decrement(vid);
				if (bRemoveIsolatedVertices && vertices_refcount.refCount(vid) == 1) {
					vertices_refcount.decrement(vid);
					Debug.Assert(vertices_refcount.isValid(vid) == false);
					vertex_edges.Clear(vid);
				}
			}

			updateTimeStamp(true);
			return MeshResult.Ok;
		}







		public struct EdgeSplitInfo
		{
			public bool bIsBoundary;
			public int vNew;
			public int eNewBN;      // new edge [vNew,vB] (original was AB)
			public List<int> NewEdges;   // new edges [vNew,vK] where vK was an 'other' vtx opposite split edge
		}
		public MeshResult SplitEdge(int vA, int vB, out EdgeSplitInfo split)
		{
			int eid = find_edge(vA, vB);
			if (eid == InvalidID) {
				split = new EdgeSplitInfo();
				return MeshResult.Failed_NotAnEdge;
			}
			return SplitEdge(eid, out split);
		}
		public MeshResult SplitEdge(int eab, out EdgeSplitInfo split)
		{
			split = new EdgeSplitInfo();
			if (!IsEdge(eab))
				return MeshResult.Failed_NotAnEdge;

			// look up primary edge & triangle
			int eab_i = 2 * eab;
			int a = edges[eab_i], b = edges[eab_i + 1];

			List<int> triangles = new List<int>(edge_triangles.ValueItr(eab));
			if ( triangles.Count < 1 )
				return MeshResult.Failed_BrokenTopology;
				
			// create new vertex
			Vector3d vNew = 0.5 * (GetVertex(a) + GetVertex(b));
			int f = AppendVertex(vNew);
			if (HasVertexNormals)
				SetVertexNormal(f, (GetVertexNormal(a) + GetVertexNormal(b)).Normalized);
			if (HasVertexColors)
				SetVertexColor(f, 0.5f * (GetVertexColor(a) + GetVertexColor(b)));


			// edge eab becomes eaf
			int eaf = eab; //Edge * eAF = eAB;
			replace_edge_vertex(eaf, b, f);

			// b is no longer connected to a 
			vertex_edges.Remove(b, eab);
			// f is connected to a
			vertex_edges.Insert(f, eaf);

			// add new edge efb
			int efb = add_edge(f, b, -1, -1);
			vertices_refcount.increment(f, (short)triangles.Count );

			split.NewEdges = new List<int>();
			split.eNewBN = efb;

			// ok now we have split the edge but broken all the triangles
			// around edge [a,b], so fix them...

			foreach ( int tid in triangles ) {
				Index3i Ttv = GetTriangle(tid);
				Index3i Tte = GetTriEdges(tid);
				int k = IndexUtil.find_tri_other_vtx(a, b, Ttv);

				// old tri [a,b,k] becomes [a,f,k]
				replace_tri_vertex(tid, b, f);
				int ebk = Tte[IndexUtil.find_edge_index_in_tri(b, k, ref Ttv)];

				// add new tri [f,b,k], with proper orientation
				bool swap = IndexUtil.is_ordered(a, b, ref Ttv);
				int tNew = (swap) ?
					add_triangle_only(f, b, k, InvalidID, InvalidID, InvalidID) :
					add_triangle_only(b, f, k, InvalidID, InvalidID, InvalidID);
				//int tNew = add_triangle_only(f, b, k, InvalidID, InvalidID, InvalidID);
				if (triangle_groups != null) 
					triangle_groups.insert(triangle_groups[tid], tNew);

				// edge [b,k] is no longer adjacent to tid, now it's to tNew
				replace_edge_triangle(ebk, tid, tNew);

				// attach new tri to edge [f,b]
				add_edge_triangle(efb, tNew);

				// create new edge [k,f], which is adjacent to tid and tNew
				int ekf = add_edge(k, f, tid, tNew);
				split.NewEdges.Add(ekf);

				// triangle tid is no longer adjacent to [b,k], replace with [k,f]
				replace_triangle_edge(tid, ebk, ekf);

				// set edges for new tri
				//set_triangle_edges(tNew, efb, ebk, ekf);
				if (swap)
					set_triangle_edges(tNew, efb, ebk, ekf);
				else
					set_triangle_edges(tNew, efb, ekf, ebk);

				// update vertex refcounts
				vertices_refcount.increment(k);
				vertices_refcount.increment(f);
			}

			split.bIsBoundary = (triangles.Count == 1);
			split.vNew = f;

			updateTimeStamp(true);
			return MeshResult.Ok;
		}




















		public struct PokeTriangleInfo
		{
			public int new_vid;
			public int new_t1, new_t2;
			public Index3i new_edges;
		}
		public virtual MeshResult PokeTriangle(int tid, out PokeTriangleInfo result)
		{
			return PokeTriangle(tid, Vector3d.One / 3.0, out result);
		}
		public virtual MeshResult PokeTriangle(int tid, Vector3d baryCoordinates, out PokeTriangleInfo result)
		{
			result = new PokeTriangleInfo();

			if (!IsTriangle(tid))
				return MeshResult.Failed_NotATriangle;

			Index3i tv = GetTriangle(tid);
			Index3i te = GetTriEdges(tid);

			// create new vertex with interpolated vertex attribs
			Vector3d midPt = (GetVertex(tv.a) + GetVertex(tv.b) + GetVertex(tv.c)) / 3.0;
			int center = AppendVertex(midPt);

			// add in new edges to center vtx, do not connect to triangles yet
			int eaC = add_edge(tv.a, center, -1, -1);
			int ebC = add_edge(tv.b, center, -1, -1);
			int ecC = add_edge(tv.c, center, -1, -1);
			vertices_refcount.increment(tv.a);
			vertices_refcount.increment(tv.b);
			vertices_refcount.increment(tv.c);
			vertices_refcount.increment(center, 3);

			// old triangle becomes tri along first edge
			set_triangle(tid, tv.a, tv.b, center);
			set_triangle_edges(tid, te.a, ebC, eaC);

			// add two new triangles
			int t1 = add_triangle_only(tv.b, tv.c, center, te.b, ecC, ebC);
			int t2 = add_triangle_only(tv.c, tv.a, center, te.c, eaC, ecC);

			// second and third edges of original tri have new neighbours
			replace_edge_triangle(te.b, tid, t1);
			replace_edge_triangle(te.c, tid, t2);

			// set the triangles for the new edges we created above
			add_edge_triangle(eaC, tid); add_edge_triangle(eaC, t2);
			add_edge_triangle(ebC, tid); add_edge_triangle(ebC, t1);
			add_edge_triangle(ecC, t1); add_edge_triangle(ecC, t2);
			//set_edge_triangles(eaC, tid, t2);
			//set_edge_triangles(ebC, tid, t1);
			//set_edge_triangles(ecC, t1, t2);

			// transfer groups
			if (HasTriangleGroups) {
				int g = triangle_groups[tid];
				triangle_groups.insert(g, t1);
				triangle_groups.insert(g, t2);
			}

			result.new_vid = center;
			result.new_t1 = t1;
			result.new_t2 = t2;
			result.new_edges = new Index3i(eaC, ebC, ecC);

			updateTimeStamp(true);
			return MeshResult.Ok;
		}






		public DMesh3 Deconstruct()
		{
			DMesh3 m = new DMesh3();
			foreach ( Index3i tri in Triangles() ) {
				m.AppendTriangle(m.AppendVertex(GetVertex(tri.a)), m.AppendVertex(GetVertex(tri.b)), m.AppendVertex(GetVertex(tri.c)));
			}
			return m;
		}






		[Conditional("DEBUG")]
		public void debug_check_is_vertex(int v)
		{
			if (!IsVertex(v))
				throw new Exception("NTMesh3.debug_is_vertex - not a vertex!");
		}

		[Conditional("DEBUG")]
		public void debug_check_is_triangle(int t)
		{
			if (!IsTriangle(t))
				throw new Exception("NTMesh3.debug_is_triangle - not a triangle!");
		}

		[Conditional("DEBUG")]
		public void debug_check_is_edge(int e)
		{
			if (!IsEdge(e))
				throw new Exception("NTMesh3.debug_is_edge - not an edge!");
		}



		/// <summary>
		// This function checks that the mesh is well-formed, ie all internal data
		// structures are consistent
		/// </summary>
		public bool CheckValidity(FailMode eFailMode = FailMode.Throw)
		{
			int[] triToVtxRefs = new int[this.MaxVertexID];

			bool is_ok = true;
			Action<bool> CheckOrFailF = (b) => { is_ok = is_ok && b; };
			if (eFailMode == FailMode.DebugAssert) {
				CheckOrFailF = (b) => { Debug.Assert(b); is_ok = is_ok && b; };
			} else if (eFailMode == FailMode.gDevAssert) {
				CheckOrFailF = (b) => { Util.gDevAssert(b); is_ok = is_ok && b; };
			} else if (eFailMode == FailMode.Throw) {
				CheckOrFailF = (b) => { if (b == false) throw new Exception("NTMesh3.CheckValidity: check failed"); };
			}

			if (normals != null)
				CheckOrFailF(normals.size == vertices.size);
			if (colors != null)
				CheckOrFailF(colors.size == vertices.size);
			if (triangle_groups != null)
				CheckOrFailF(triangle_groups.size == triangles.size / 3);

			foreach (int tID in TriangleIndices()) {
				CheckOrFailF(IsTriangle(tID));
				CheckOrFailF(triangles_refcount.refCount(tID) == 1);

				// vertices must exist
				Index3i tv = GetTriangle(tID);
				for (int j = 0; j < 3; ++j) {
					CheckOrFailF(IsVertex(tv[j]));
					triToVtxRefs[tv[j]] += 1;
				}

				// edges must exist and reference this tri
				Index3i e = new Index3i();
				for (int j = 0; j < 3; ++j) {
					int a = tv[j], b = tv[(j + 1) % 3];
					e[j] = FindEdge(a, b);
					CheckOrFailF(e[j] != InvalidID);
					CheckOrFailF(edge_has_t(e[j], tID));
					CheckOrFailF(e[j] == FindEdgeFromTri(a, b, tID));
				}
				CheckOrFailF(e[0] != e[1] && e[0] != e[2] && e[1] != e[2]);

				// tri nbrs must exist and reference this tri, or same edge must be boundary edge
				Index3i te = GetTriEdges(tID);
				for (int j = 0; j < 3; ++j) {
					int eid = te[j];
					CheckOrFailF(IsEdge(eid));
					if (edge_is_boundary(eid)) {
						CheckOrFailF(tri_is_boundary(tID));
						continue;
					}

					bool saw_tid = false;
					foreach (int tOther in EdgeTrianglesItr(eid)) {
						if (tOther != tID)
							CheckOrFailF(tri_has_neighbour_t(tOther, tID) == true);
						else
							saw_tid = true;
					}
					CheckOrFailF(saw_tid);

					// edge must have same two verts as tri for same index
					int a = tv[j], b = tv[(j + 1) % 3];
					Index2i ev = GetEdgeV(te[j]);
					CheckOrFailF(IndexUtil.same_pair_unordered(a, b, ev[0], ev[1]));
				}
			}


			// edge verts/tris must exist
			foreach (int eID in EdgeIndices()) {
				CheckOrFailF(IsEdge(eID));
				CheckOrFailF(edges_refcount.refCount(eID) == 1);
				Index2i ev = GetEdgeV(eID);
				CheckOrFailF(IsVertex(ev[0]));
				CheckOrFailF(IsVertex(ev[1]));
				CheckOrFailF(ev[0] < ev[1]);
				foreach (int tid in EdgeTrianglesItr(eID))
					CheckOrFailF(IsTriangle(tid));
			}

			// verify compact check
			bool is_compact = vertices_refcount.is_dense;
			if (is_compact) {
				for (int vid = 0; vid < vertices.Length / 3; ++vid) {
					CheckOrFailF(vertices_refcount.isValid(vid));
				}
			}

			// vertex edges must exist and reference this vert
			foreach (int vID in VertexIndices()) {
				CheckOrFailF(IsVertex(vID));

				Vector3d v = GetVertex(vID);
				CheckOrFailF(double.IsNaN(v.LengthSquared) == false);
				CheckOrFailF(double.IsInfinity(v.LengthSquared) == false);

				foreach (int edgeid in vertex_edges.ValueItr(vID)) {
					CheckOrFailF(IsEdge(edgeid));
					CheckOrFailF(edge_has_v(edgeid, vID));

					int otherV = edge_other_v(edgeid, vID);
					int e2 = find_edge(vID, otherV);
					CheckOrFailF(e2 != InvalidID);
					CheckOrFailF(e2 == edgeid);
					e2 = find_edge(otherV, vID);
					CheckOrFailF(e2 != InvalidID);
					CheckOrFailF(e2 == edgeid);
				}

				foreach (int nbr_vid in VtxVerticesItr(vID)) {
					CheckOrFailF(IsVertex(nbr_vid));
					int edge = find_edge(vID, nbr_vid);
					CheckOrFailF(IsEdge(edge));
				}

				List<int> vTris = new List<int>();
				GetVtxTriangles(vID, vTris);

				CheckOrFailF(vertices_refcount.refCount(vID) == vTris.Count + 1);
				CheckOrFailF(triToVtxRefs[vID] == vTris.Count);
				foreach (int tID in vTris) {
					CheckOrFailF(tri_has_v(tID, vID));
				}

				// check that edges around vert only references tris above, and reference all of them!
				List<int> vRemoveTris = new List<int>(vTris);
				foreach (int edgeid in vertex_edges.ValueItr(vID)) {
					foreach ( int tid in EdgeTrianglesItr(edgeid) ) { 
						CheckOrFailF(vTris.Contains(tid));
						vRemoveTris.Remove(tid);
					}
				}
				CheckOrFailF(vRemoveTris.Count == 0);
			}

			return is_ok;
		}


		public MeshResult ReverseTriOrientation(int tID)
		{
			if (!IsTriangle(tID))
				return MeshResult.Failed_NotATriangle;
			internal_reverse_tri_orientation(tID);
			updateTimeStamp(true);
			return MeshResult.Ok;
		}

		void internal_reverse_tri_orientation(int tID)
		{
			Index3i t = GetTriangle(tID);
			set_triangle(tID, t[1], t[0], t[2]);
			Index3i te = GetTriEdges(tID);
			set_triangle_edges(tID, te[0], te[2], te[1]);
		}


		/// <summary>
		/// Remove a tID from the mesh. Also removes any unreferenced edges after tri is removed.
		/// If bRemoveIsolatedVertices is false, then if you remove all tris from a vert, that vert is also removed.
		/// If bPreserveManifold, we check that you will not create a bowtie vertex (and return false).
		///   If this check is not done, you have to make sure you don't create a bowtie, because other
		///   code assumes we don't have bowties, and will not handle it properly
		/// </summary>
		public MeshResult RemoveTriangle(int tID, bool bRemoveIsolatedVertices = true, bool bPreserveManifold = false)
		{
			if (!triangles_refcount.isValid(tID))
			{
				Debug.Assert(false);
				return MeshResult.Failed_NotATriangle;
			}

			Index3i tv = GetTriangle(tID);
			Index3i te = GetTriEdges(tID);

			// if any tri vtx is a boundary vtx connected to two interior edges, then
			// we cannot remove this triangle because it would create a bowtie vertex!
			// (that vtx already has 2 boundary edges, and we would add two more)
			if (bPreserveManifold)
			{
				for (int j = 0; j < 3; ++j)
				{
					if (IsBoundaryVertex(tv[j]))
					{
						if (IsBoundaryEdge(te[j]) == false && IsBoundaryEdge(te[(j + 2) % 3]) == false)
							return MeshResult.Failed_WouldCreateBowtie;
					}
				}
			}

			// Remove triangle from its edges. if edge has no triangles left,
			// then it is removed.
			for (int j = 0; j < 3; ++j)
			{
				int eid = te[j];
				remove_edge_triangle(eid, tID);
				
				if (EdgeTrianglesCount(eid) == 0)
				{
					var vIndices = GetEdgeV(eid);
					int a = vIndices.a; // edges[4 * eid];
					vertex_edges.Remove(a, eid);

					int b = vIndices.b; // edges[4 * eid + 1];
					vertex_edges.Remove(b, eid);

					edges_refcount.decrement(eid);
				}
			}

			// free this triangle
			triangles_refcount.decrement(tID);
			Debug.Assert(triangles_refcount.isValid(tID) == false);

			// Decrement vertex refcounts. If any hit 1 and we got remove-isolated flag,
			// we need to remove that vertex
			for (int j = 0; j < 3; ++j)
			{
				int vid = tv[j];
				vertices_refcount.decrement(vid);
				if (bRemoveIsolatedVertices && vertices_refcount.refCount(vid) == 1)
				{
					vertices_refcount.decrement(vid);
					Debug.Assert(vertices_refcount.isValid(vid) == false);
					vertex_edges.Clear(vid);
				}
			}

			updateTimeStamp(true);
			return MeshResult.Ok;
		}

		public virtual MeshResult SetTriangle(int tID, Index3i newv, bool bRemoveIsolatedVertices = true)
		{
			Index3i tv = GetTriangle(tID);
			Index3i te = GetTriEdges(tID);
			if (tv.a == newv.a && tv.b == newv.b)
				te.a = -1;
			if (tv.b == newv.b && tv.c == newv.c)
				te.b = -1;
			if (tv.c == newv.c && tv.a == newv.a)
				te.c = -1;

			if (!triangles_refcount.isValid(tID))
			{
				Debug.Assert(false);
				return MeshResult.Failed_NotATriangle;
			}
			if (IsVertex(newv[0]) == false || IsVertex(newv[1]) == false || IsVertex(newv[2]) == false)
			{
				Util.gDevAssert(false);
				return MeshResult.Failed_NotAVertex;
			}
			if (newv[0] == newv[1] || newv[0] == newv[2] || newv[1] == newv[2])
			{
				Util.gDevAssert(false);
				return MeshResult.Failed_BrokenTopology;
			}
			// look up edges. if any already have two triangles, this would 
			// create non-manifold geometry and so we do not allow it
			int e0 = find_edge(newv[0], newv[1]);
			int e1 = find_edge(newv[1], newv[2]);
			int e2 = find_edge(newv[2], newv[0]);
			if ((te.a != -1 && e0 != InvalidID && IsBoundaryEdge(e0) == false)
				 || (te.b != -1 && e1 != InvalidID && IsBoundaryEdge(e1) == false)
				 || (te.c != -1 && e2 != InvalidID && IsBoundaryEdge(e2) == false))
			{
				return MeshResult.Failed_BrokenTopology;
			}


			// [TODO] check that we are not going to create invalid stuff...

			// Remove triangle from its edges. if edge has no triangles left, then it is removed.
			for (int j = 0; j < 3; ++j)
			{
				int eid = te[j];
				if (eid == -1)      // we don't need to modify this edge
					continue;
				remove_edge_triangle(eid, tID);
				if (edges[4 * eid + 2] == InvalidID)
				{
					int a = edges[4 * eid];
					vertex_edges.Remove(a, eid);

					int b = edges[4 * eid + 1];
					vertex_edges.Remove(b, eid);

					edges_refcount.decrement(eid);
				}
			}

			// Decrement vertex refcounts. If any hit 1 and we got remove-isolated flag,
			// we need to remove that vertex
			for (int j = 0; j < 3; ++j)
			{
				int vid = tv[j];
				if (vid == newv[j])     // we don't need to modify this vertex
					continue;
				vertices_refcount.decrement(vid);
				if (bRemoveIsolatedVertices && vertices_refcount.refCount(vid) == 1)
				{
					vertices_refcount.decrement(vid);
					Debug.Assert(vertices_refcount.isValid(vid) == false);
					vertex_edges.Clear(vid);
				}
			}


			// ok now re-insert with new vertices
			int i = 3 * tID;
			for (int j = 0; j < 3; ++j)
			{
				if (newv[j] != tv[j])
				{
					triangles[i + j] = newv[j];
					vertices_refcount.increment(newv[j]);
				}
			}

			if (te.a != -1)
				add_tri_edge(tID, newv[0], newv[1], 0, e0);
			if (te.b != -1)
				add_tri_edge(tID, newv[1], newv[2], 1, e1);
			if (te.c != -1)
				add_tri_edge(tID, newv[2], newv[0], 2, e2);

			updateTimeStamp(true);
			return MeshResult.Ok;
		}


		void set_edge_triangles(int eID, int t0, int t1)
		{
			edge_triangles.Clear(eID);
			edge_triangles.Insert(eID, t0);
			edge_triangles.Insert(eID, t1);
		}


		public struct MergeEdgesInfo
		{
			public int eKept;
			public int eRemoved;

			public Vector2i vKept;
			public Vector2i vRemoved;           // either may be InvalidID if it was same as vKept

			public Vector2i eRemovedExtra;      // bonus collapsed edge, or InvalidID
			public Vector2i eKeptExtra;         // edge paired w/ eRemovedExtra
		}
		public MeshResult MergeEdges(int eKeep, int eDiscard, out MergeEdgesInfo merge_info)
		{
			merge_info = new MergeEdgesInfo();
			if (IsEdge(eKeep) == false || IsEdge(eDiscard) == false)
				return MeshResult.Failed_NotAnEdge;

			var edgevtx_keep = GetEdgeV(eKeep);
			var edgevtx_discard = GetEdgeV(eDiscard);

			var edgetris_keep = EdgeTrianglesItr(eKeep);
			var edgetris_discard = EdgeTrianglesItr(eDiscard);

			if (edgetris_keep.Count() != 1 || edgetris_discard.Count() != 1)
				return MeshResult.Failed_NotABoundaryEdge;

			int a = edgevtx_keep.a, b = edgevtx_keep.b;
			int tab = edgetris_keep.First();
			int eab = eKeep;
			int c = edgevtx_discard.a, d = edgevtx_discard.b;
			int tcd = edgetris_discard.First();
			int ecd = eDiscard;

			// Need to correctly orient a,b and c,d and then check that 
			// we will not join triangles with incompatible winding order
			// I can't see how to do this purely topologically. 
			// So relying on closest-pairs testing.
			IndexUtil.orient_tri_edge(ref a, ref b, GetTriangle(tab));
			//int tcd_otherv = IndexUtil.orient_tri_edge_and_find_other_vtx(ref c, ref d, GetTriangle(tcd));
			IndexUtil.orient_tri_edge(ref c, ref d, GetTriangle(tcd));

			// [GT] Another check explicitly calculating the orientation
			// of the edges to increase robustness
			//Vector3d vecAB = (GetVertex(b) - GetVertex(a)).Normalized;
			//Vector3d vecCD = (GetVertex(d) - GetVertex(c)).Normalized;
			//if (vecAB.Dot(vecCD) > 0)
			//    return MeshResult.Failed_SameOrientation;

			int x = c; c = d; d = x;   // joinable bdry edges have opposing orientations, so flip to get ac and b/d correspondences
			Vector3d Va = GetVertex(a), Vb = GetVertex(b), Vc = GetVertex(c), Vd = GetVertex(d);
			if ((Va.DistanceSquared(Vc) + Vb.DistanceSquared(Vd)) >
				(Va.DistanceSquared(Vd) + Vb.DistanceSquared(Vc)))
				return MeshResult.Failed_SameOrientation;

			// alternative that detects normal flip of triangle tcd. This is a more 
			// robust geometric test, but fails if tri is degenerate...also more expensive
			//int tcd_otherv = IndexUtil.find_tri_other_vtx(c, d, triangles, tcd);
			//Vector3d otherv = GetVertex(tcd_otherv);
			//Vector3d Ncd = MathUtil.FastNormalDirection(ref Vc, ref Vd, ref otherv);
			//Vector3d Nab = MathUtil.FastNormalDirection(ref Va, ref Vb, ref otherv);
			//if (Ncd.Dot(Nab) < 0)
			//    return MeshResult.Failed_SameOrientation;

			merge_info.eKept = eab;
			merge_info.eRemoved = ecd;

			// if a/c or b/d are connected by an existing edge, we can't merge
			if (a != c && find_edge(a, c) != NTMesh3.InvalidID)
				return MeshResult.Failed_InvalidNeighbourhood;
			if (b != d && find_edge(b, d) != NTMesh3.InvalidID)
				return MeshResult.Failed_InvalidNeighbourhood;

			// if vertices at either end already share a common neighbour vertex, and we 
			// do the merge, that would create duplicate edges. This is something like the
			// 'link condition' in edge collapses. 
			// Note that we have to catch cases where both edges to the shared vertex are
			// boundary edges, in that case we will also merge this edge later on
			if (a != c)
			{
				int ea = 0, ec = 0, other_v = (b == d) ? b : -1;
				foreach (int cnbr in VtxVerticesItr(c))
				{
					if (cnbr != other_v && (ea = find_edge(a, cnbr)) != NTMesh3.InvalidID)
					{
						ec = find_edge(c, cnbr);
						if (IsBoundaryEdge(ea) == false || IsBoundaryEdge(ec) == false)
							return MeshResult.Failed_InvalidNeighbourhood;
					}
				}
			}
			if (b != d)
			{
				int eb = 0, ed = 0, other_v = (a == c) ? a : -1;
				foreach (int dnbr in VtxVerticesItr(d))
				{
					if (dnbr != other_v && (eb = find_edge(b, dnbr)) != NTMesh3.InvalidID)
					{
						ed = find_edge(d, dnbr);
						if (IsBoundaryEdge(eb) == false || IsBoundaryEdge(ed) == false)
							return MeshResult.Failed_InvalidNeighbourhood;
					}
				}
			}


			// [TODO] this acts on each interior tri twice. could avoid using vtx-tri iterator?
			if (a != c)
			{
				// replace c w/ a in edges and tris connected to c, and move edges to a
				foreach (int eid in vertex_edges.ValueItr(c))
				{
					if (eid == eDiscard)
						continue;
					
					replace_edge_vertex(eid, c, a);
					short rc = 0;

					var eid_tris = EdgeTrianglesItr(eid);
					foreach (var eid_t in eid_tris)
					{
						if (replace_tri_vertex(eid_t, c, a) >= 0)
							rc++;
					}

					//if (replace_tri_vertex(edges[4 * eid + 2], c, a) >= 0)
					//    rc++;
					//if (edges[4 * eid + 3] != InvalidID)
					//{
					//    if (replace_tri_vertex(edges[4 * eid + 3], c, a) >= 0)
					//        rc++;
					//}
					vertex_edges.Insert(a, eid);
					if (rc > 0)
					{
						vertices_refcount.increment(a, rc);
						vertices_refcount.decrement(c, rc);
					}
				}
				vertex_edges.Clear(c);
				vertices_refcount.decrement(c);
				merge_info.vRemoved[0] = c;
			}
			else
			{
				vertex_edges.Remove(a, ecd);
				merge_info.vRemoved[0] = InvalidID;
			}
			merge_info.vKept[0] = a;

			if (d != b)
			{
				// replace d w/ b in edges and tris connected to d, and move edges to b
				foreach (int eid in vertex_edges.ValueItr(d))
				{
					if (eid == eDiscard)
						continue;
					replace_edge_vertex(eid, d, b);
					short rc = 0;
					var eid_tris = EdgeTrianglesItr(eid);
					foreach (var eid_t in eid_tris)
					{
						if (replace_tri_vertex(eid_t, d, b) >= 0)
							rc++;
					}

					//if (replace_tri_vertex(edges[4 * eid + 2], d, b) >= 0)
					//    rc++;
					//if (edges[4 * eid + 3] != InvalidID)
					//{
					//    if (replace_tri_vertex(edges[4 * eid + 3], d, b) >= 0)
					//        rc++;
					//}

					vertex_edges.Insert(b, eid);
					if (rc > 0)
					{
						vertices_refcount.increment(b, rc);
						vertices_refcount.decrement(d, rc);
					}

				}
				vertex_edges.Clear(d);
				vertices_refcount.decrement(d);
				merge_info.vRemoved[1] = d;
			}
			else
			{
				vertex_edges.Remove(b, ecd);
				merge_info.vRemoved[1] = InvalidID;
			}
			merge_info.vKept[1] = b;

			// replace edge cd with edge ab in triangle tcd
			replace_triangle_edge(tcd, ecd, eab);
			edges_refcount.decrement(ecd);

			// update edge-tri adjacency
			set_edge_triangles(eab, tab, tcd);

			// Once we merge ab to cd, there may be additional edges (now) connected
			// to either a or b that are connected to the same vertex on their 'other' side.
			// So we now have two boundary edges connecting the same two vertices - disaster!
			// We need to find and merge these edges. 
			// Q: I don't think it is possible to have multiple such edge-pairs at a or b
			//    But I am not certain...is a bit tricky to handle because we modify edges_v...
			merge_info.eRemovedExtra = new Vector2i(InvalidID, InvalidID);
			merge_info.eKeptExtra = merge_info.eRemovedExtra;
			for (int vi = 0; vi < 2; ++vi)
			{
				int v1 = a, v2 = c;   // vertices of merged edge
				if (vi == 1)
				{
					v1 = b; v2 = d;
				}
				if (v1 == v2)
					continue;
				List<int> edges_v = vertex_edges_list(v1);
				int Nedges = edges_v.Count;
				bool found = false;
				// in this loop, we compare 'other' vert_1 and vert_2 of edges around v1.
				// problem case is when vert_1 == vert_2  (ie two edges w/ same other vtx).
				//restart_merge_loop:
				for (int i = 0; i < Nedges && found == false; ++i)
				{
					int edge_1 = edges_v[i];
					if (IsBoundaryEdge(edge_1) == false)
						continue;
					int vert_1 = edge_other_v(edge_1, v1);
					for (int j = i + 1; j < Nedges; ++j)
					{
						int edge_2 = edges_v[j];
						int vert_2 = edge_other_v(edge_2, v1);
						if (vert_1 == vert_2 && IsBoundaryEdge(edge_2))
						{ // if ! boundary here, we are in deep trouble...
						  // replace edge_2 w/ edge_1 in tri, update edge and vtx-edge-nbr lists
							int tri_1 = EdgeTrianglesItr(edge_1).First(); //  edges[4 * edge_1 + 2];
							int tri_2 = EdgeTrianglesItr(edge_2).First(); // edges[4 * edge_2 + 2];
							replace_triangle_edge(tri_2, edge_2, edge_1);
							set_edge_triangles(edge_1, tri_1, tri_2);
							vertex_edges.Remove(v1, edge_2);
							vertex_edges.Remove(vert_1, edge_2);
							edges_refcount.decrement(edge_2);
							merge_info.eRemovedExtra[vi] = edge_2;
							merge_info.eKeptExtra[vi] = edge_1;

							//edges_v = vertex_edges_list(v1);      // this code allows us to continue checking, ie in case we had
							//Nedges = edges_v.Count;               // multiple such edges. but I don't think it's possible.
							//goto restart_merge_loop;
							found = true;			  // exit outer i loop
							break;					  // exit inner j loop
						}
					}
				}
			}

			updateTimeStamp(true);
			return MeshResult.Ok;
		}

		public struct EdgeFlipInfo
		{
			public int eID;
			public int v0, v1;
			public int ov0, ov1;
			public int t0, t1;
		}
		public MeshResult FlipEdge(int vA, int vB, out EdgeFlipInfo flip)
		{
			int eid = find_edge(vA, vB);
			if (eid == InvalidID)
			{
				flip = new EdgeFlipInfo();
				return MeshResult.Failed_NotAnEdge;
			}
			return FlipEdge(eid, out flip);
		}
		public MeshResult FlipEdge(int eab, out EdgeFlipInfo flip)
		{
			flip = new EdgeFlipInfo();
			if (!IsEdge(eab))
				return MeshResult.Failed_NotAnEdge;
			if (IsBoundaryEdge(eab))
				return MeshResult.Failed_IsBoundaryEdge;

			// [GT] TODO: implement for non-manifold edges
			var edgeTris = EdgeTrianglesItr(eab);
			if (edgeTris.Count() > 2)
				return MeshResult.Failed_InvalidNeighbourhood;

			// find oriented edge [a,b], tris t0,t1, and other verts c in t0, d in t1
			var edgeVtx = GetEdgeV(eab);
			int a = edgeVtx.a, b = edgeVtx.b;

			int t0 = edgeTris.First();
			int t1 = edgeTris.Last();
			int[] T0tv = GetTriangle(t0).array;
			int[] T1tv = GetTriangle(t1).array;
			int c = IndexUtil.orient_tri_edge_and_find_other_vtx(ref a, ref b, T0tv);
			int d = IndexUtil.find_tri_other_vtx(a, b, T1tv);
			if (c == InvalidID || d == InvalidID)
			{
				return MeshResult.Failed_BrokenTopology;
			}

			int flipped = find_edge(c, d);
			if (flipped != InvalidID)
				return MeshResult.Failed_FlippedEdgeExists;

			// find edges bc, ca, ad, db
			int ebc = find_tri_neighbour_edge(t0, b, c);
			int eca = find_tri_neighbour_edge(t0, c, a);
			int ead = find_tri_neighbour_edge(t1, a, d);
			int edb = find_tri_neighbour_edge(t1, d, b);

			// update triangles
			set_triangle(t0, c, d, b);
			set_triangle(t1, d, c, a);

			// update edge AB, which becomes flipped edge CD
			set_edge_vertices(eab, c, d);
			set_edge_triangles(eab, t0, t1);
			int ecd = eab;

			// update the two other edges whose triangle nbrs have changed
			if (!replace_edge_triangle(eca, t0, t1))
				throw new ArgumentException("NTMesh3.FlipEdge: first replace_edge_triangle failed");
			if (!replace_edge_triangle(edb, t1, t0))
				throw new ArgumentException("NTMesh3.FlipEdge: second replace_edge_triangle failed");

			// update triangle nbr lists (these are edges)
			set_triangle_edges(t0, ecd, edb, ebc);
			set_triangle_edges(t1, ecd, eca, ead);

			// remove old eab from verts a and b, and decrement ref counts
			if (vertex_edges.Remove(a, eab) == false)
				throw new ArgumentException("NTMesh3.FlipEdge: first edge list remove failed");
			if (vertex_edges.Remove(b, eab) == false)
				throw new ArgumentException("NTMesh3.FlipEdge: second edge list remove failed");
			vertices_refcount.decrement(a);
			vertices_refcount.decrement(b);
			if (IsVertex(a) == false || IsVertex(b) == false)
				throw new ArgumentException("NTMesh3.FlipEdge: either a or b is not a vertex?");

			// add new edge ecd to verts c and d, and increment ref counts
			vertex_edges.Insert(c, ecd);
			vertex_edges.Insert(d, ecd);
			vertices_refcount.increment(c);
			vertices_refcount.increment(d);

			// success! collect up results
			flip.eID = eab;
			flip.v0 = a; flip.v1 = b;
			flip.ov0 = c; flip.ov1 = d;
			flip.t0 = t0; flip.t1 = t1;

			updateTimeStamp(true);
			return MeshResult.Ok;
		}


	}
}

