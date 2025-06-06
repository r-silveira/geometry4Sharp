﻿// Copyright (c) Ryan Schmidt (rms@gradientspace.com) - All Rights Reserved
// Distributed under the Boost Software License, Version 1.0. http://www.boost.org/LICENSE_1_0.txt
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using g4;

namespace gs
{
	/// <summary>
	/// Remove "occluded" triangles, ie triangles on the "inside" of the mesh. 
    /// This is a fuzzy definition, current implementation is basically computing
    /// something akin to ambient occlusion, and if face is fully occluded, then
    /// we classify it as inside and remove it.
	/// </summary>
	public class NTRemoveOccludedTriangles
	{
		public NTMesh3 Mesh;
        public NTMeshAABBTree3 Spatial;

        // indices of removed triangles. List will be empty if nothing removed
        public List<int> RemovedT = null;

        // Mesh.RemoveTriange() can return false, if that happens, this will be true
        public bool RemoveFailed = false;

        // if true, then we discard tris if any vertex is occluded.
        // Otherwise we discard based on tri centroids
        public bool PerVertex = false;

        // we nudge points out by this amount to try to counteract numerical issues
        public double NormalOffset = MathUtil.ZeroTolerance;

        // use this as winding isovalue for WindingNumber mode
        public double WindingIsoValue = 0.5;

        public enum CalculationMode
        {
            RayParity = 0,
            AnalyticWindingNumber = 1,
            FastWindingNumber = 2,
            SimpleOcclusionTest = 3
        }
        public CalculationMode InsideMode = CalculationMode.FastWindingNumber;


        /// <summary>
        /// Set this to be able to cancel running remesher
        /// </summary>
        public ProgressCancel Progress = null;

        /// <summary>
        /// if this returns true, abort computation. 
        /// </summary>
        protected virtual bool Cancelled() {
            return (Progress == null) ? false : Progress.Cancelled();
        }



        public NTRemoveOccludedTriangles(NTMesh3 mesh)
		{
			Mesh = mesh;
		}

        public NTRemoveOccludedTriangles(NTMesh3 mesh, NTMeshAABBTree3 spatial)
        {
            Mesh = mesh;
            Spatial = spatial;
        }


        public virtual bool Apply()
        {
            var testAgainstMesh = Mesh;
            NTMeshAABBTree3 spatial = (Spatial != null && testAgainstMesh == Mesh) ? 
                Spatial : new NTMeshAABBTree3(testAgainstMesh, true);
            if (InsideMode == CalculationMode.AnalyticWindingNumber)
                spatial.WindingNumber(Vector3d.Zero);
            else if (InsideMode == CalculationMode.FastWindingNumber )
                spatial.FastWindingNumber(Vector3d.Zero);

            if (Cancelled())
                return false;

            // ray directions
            List<Vector3d> ray_dirs = null; int NR = 0;
            if (InsideMode == CalculationMode.SimpleOcclusionTest) {
                ray_dirs = new List<Vector3d>();
                ray_dirs.Add(Vector3d.AxisX); ray_dirs.Add(-Vector3d.AxisX);
                ray_dirs.Add(Vector3d.AxisY); ray_dirs.Add(-Vector3d.AxisY);
                ray_dirs.Add(Vector3d.AxisZ); ray_dirs.Add(-Vector3d.AxisZ);
                NR = ray_dirs.Count;
            }

            Func<Vector3d, Vector3d, bool> isOccludedF = (pt, normal) => {

                if (InsideMode == CalculationMode.RayParity) {
                    return spatial.IsInside(pt, normal);
                } else 
                if (InsideMode == CalculationMode.AnalyticWindingNumber) {
                    return spatial.WindingNumber(pt) > WindingIsoValue;
                } else if (InsideMode == CalculationMode.FastWindingNumber) {
                    return spatial.FastWindingNumber(pt) > WindingIsoValue;
                } else {
                    for (int k = 0; k < NR; ++k) {
                        int hit_tid = spatial.FindNearestHitTriangle(new Ray3d(pt, ray_dirs[k]));
                        if (hit_tid == DMesh3.InvalidID)
                            return false;
                    }
                    return true;
                }
            };

            bool cancel = false;

            BitArray vertices = null;
            if ( PerVertex ) {
                vertices = new BitArray(Mesh.MaxVertexID);

                MeshNormals normals = null;
                if (Mesh.HasVertexNormals == false) {
                    normals = new MeshNormals(Mesh);
                    normals.Compute();
                }

                gParallel.ForEach(Mesh.VertexIndices(), (vid) => {
                    if (cancel) return;
                    if (vid % 10 == 0) cancel = Cancelled();

                    Vector3d c = Mesh.GetVertex(vid);
                    Vector3d n = (normals == null) ? Mesh.GetVertexNormal(vid) : normals[vid];
                    c += n * NormalOffset;
                    vertices[vid] = isOccludedF(c, n);
                });
            }
            if (Cancelled())
                return false;

            RemovedT = new List<int>();
            SpinLock removeLock = new SpinLock();

            gParallel.ForEach(Mesh.TriangleIndices(), (tid) =>
            {
                if (cancel) return;
                if (tid % 10 == 0) cancel = Cancelled();

                bool inside = false;
                if (PerVertex)
                {
                    Index3i tri = Mesh.GetTriangle(tid);
                    inside = vertices[tri.a] || vertices[tri.b] || vertices[tri.c];

                }
                else
                {
                    Vector3d n = Mesh.GetTriNormal(tid);
                    var c = Mesh.GetTriCentroid(tid);


                    if (n.LengthSquared < 0.99)
                    {
                        n = new Vector3d(0.331960519038825, 0.462531727525156, 0.822111072077288);
                    }

                    c += n * NormalOffset;
                    inside = isOccludedF(c, n);
                }

                if (inside)
                {
                    bool taken = false;
                    removeLock.Enter(ref taken);
                    RemovedT.Add(tid);
                    removeLock.Exit();
                }
            });

            if (Cancelled())
                return false;

            if (RemovedT.Count > 0)
            {
                var editor = new NTMeshEditor(Mesh);
                bool bOK = editor.RemoveTriangles(RemovedT, true);
                RemoveFailed = (bOK == false);
            }

            return true;
		}
	}
}
