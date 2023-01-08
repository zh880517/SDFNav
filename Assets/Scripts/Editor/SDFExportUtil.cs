using System.Collections.Generic;
using UnityEngine;

namespace SDFNav.Editor
{
    public static class SDFExportUtil
    {
        public class Triangle
        {
            public Vector2 A;
            public Vector2 B;
            public Vector2 C;

            public float SDF(Vector2 point)
            {
                return SDFUtil.TriangleSDF(point, A, B, C);
            }
        }

        public static Vector2 ToV2(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static List<Triangle> SubMeshToTriangles(SubMeshData subMesh)
        {
            List<Triangle> triangles = new List<Triangle>(subMesh.TriangleIndices.Count);
            var mesh = subMesh.Mesh;
            foreach (var idx in subMesh.TriangleIndices)
            {
                var t = mesh.Triangles[idx];
                Triangle triangle = new Triangle 
                { 
                    A = ToV2(mesh.Vertices[t.A]),
                    B = ToV2(mesh.Vertices[t.B]),
                    C = ToV2(mesh.Vertices[t.C]),
                };
                triangles.Add(triangle);
            }
            return triangles;
        }
    }
}
