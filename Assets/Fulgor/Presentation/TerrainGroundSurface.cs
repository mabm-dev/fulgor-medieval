using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>CPU ground sampler. The widest connected surface excludes detached trees and props.</summary>
    public sealed class TerrainGroundSurface
    {
        private readonly Vector3[] vertices;
        private readonly List<int> ground = new();

        public TerrainGroundSurface(Mesh mesh)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            vertices = mesh.vertices;
            var indices = mesh.triangles;
            var parent = new int[vertices.Length];
            var welded = new Dictionary<Vector3Int,int>();
            for (int i = 0; i < vertices.Length; i++) {
                parent[i] = i; var v = vertices[i];
                var key = new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));
                if (welded.TryGetValue(key,out int previous)) Join(parent,i,previous); else welded.Add(key,i);
            }
            for (int i = 0; i < indices.Length; i += 3) { Join(parent,indices[i],indices[i+1]); Join(parent,indices[i],indices[i+2]); }
            var bounds = new Dictionary<int,Bounds>();
            foreach (int i in indices) {
                int root = Find(parent,i);
                if (!bounds.TryGetValue(root,out var b)) b = new Bounds(vertices[i],Vector3.zero);
                b.Encapsulate(vertices[i]); bounds[root] = b;
            }
            int chosen = -1; float largest = -1;
            foreach (var pair in bounds) {
                float area = pair.Value.size.x*pair.Value.size.z;
                if (area > largest) { largest = area; chosen = pair.Key; }
            }
            for (int i = 0; i < indices.Length; i += 3)
                if (Find(parent,indices[i]) == chosen && Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]).y > .0000001f)
                { ground.Add(indices[i]); ground.Add(indices[i+1]); ground.Add(indices[i+2]); }
        }

        private static int Find(int[] parent,int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
        private static void Join(int[] parent,int a,int b) { a = Find(parent,a); b = Find(parent,b); if (a != b) parent[a] = b; }

        /// <summary>Samples the ground and optionally bridges the small gaps of the blockout grid.</summary>
        public bool TrySample(Transform tile,Vector3 world,out Vector3 point,out Vector3 normal,out float distance,float maxGap = .08f)
        {
            point = default; normal = Vector3.up; distance = float.PositiveInfinity;
            var local = tile.InverseTransformPoint(world); var p = new Vector2(local.x,local.z);
            float bestY = float.NegativeInfinity; Vector3 best = default, bestNormal = Vector3.up;
            for (int i = 0; i < ground.Count; i += 3)
            {
                var a = vertices[ground[i]]; var b = vertices[ground[i+1]]; var c = vertices[ground[i+2]];
                var av = new Vector2(a.x,a.z); var bv = new Vector2(b.x,b.z); var cv = new Vector2(c.x,c.z);
                float det = Cross(bv-av,cv-av);
                if (Mathf.Abs(det) < .00000001f) continue;
                float wb = Cross(p-av,cv-av)/det, wc = Cross(bv-av,p-av)/det, wa = 1-wb-wc;
                Vector3 sample;
                float gap;
                if (wa >= -.00001f && wb >= -.00001f && wc >= -.00001f) { sample = a*wa+b*wb+c*wc; gap = 0; }
                else {
                    sample = Closest(a,b,p); float d = FlatDistance(sample,local);
                    var other = Closest(b,c,p); float next = FlatDistance(other,local); if (next < d) { sample=other; d=next; }
                    other = Closest(c,a,p); next=FlatDistance(other,local); if (next < d) sample=other;
                    var worldClosest = tile.TransformPoint(sample);
                    gap = Vector2.Distance(new Vector2(worldClosest.x,worldClosest.z),new Vector2(world.x,world.z));
                }
                if (gap > maxGap || gap > distance+.00001f || (Mathf.Abs(gap-distance)<.00001f && sample.y <= bestY)) continue;
                distance=gap; bestY=sample.y; best=sample; bestNormal=Vector3.Cross(b-a,c-a).normalized;
            }
            if (float.IsPositiveInfinity(distance)) return false;
            point = tile.TransformPoint(best); point.x=world.x; point.z=world.z;
            normal = tile.localToWorldMatrix.inverse.transpose.MultiplyVector(bestNormal).normalized;
            return true;
        }
        private static float Cross(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;
        private static float FlatDistance(Vector3 a,Vector3 b) => (a.x-b.x)*(a.x-b.x)+(a.z-b.z)*(a.z-b.z);
        private static Vector3 Closest(Vector3 a,Vector3 b,Vector2 p)
        {
            var av=new Vector2(a.x,a.z); var edge=new Vector2(b.x-a.x,b.z-a.z);
            float t=edge.sqrMagnitude < .00000001f ? 0 : Mathf.Clamp01(Vector2.Dot(p-av,edge)/edge.sqrMagnitude);
            return Vector3.Lerp(a,b,t);
        }
    }
}
