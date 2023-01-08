using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace SDFNav.Editor
{
    public static class DebugMenuItem
    {
        [MenuItem("Tools/test")]
        static void Test()
        {
            var mesh = NavMeshExporter.NavToMesh();
            var subMeshs = NavMeshExporter.SplitSubMesh(mesh);
            int maxCout = 0;
            int idx = -1;
            for (int i = 0; i < subMeshs.Count; ++i)
            {
                int count = subMeshs[i].TriangleIndices.Count;
                if (count > maxCout)
                {
                    maxCout = count;
                    idx = i;
                }
            }
            var savemesh = ToMesh(subMeshs[idx]);
            AssetDatabase.CreateAsset(savemesh, "Assets/Nav1.mesh");
        }

        [MenuItem("Tools/NavMesh生成Mesh")]
        static void SaveNavMeshToMesh()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            triangulation = NavMeshExporter.DeDuplicate(triangulation);
            var mesh = ToMesh(triangulation);
            AssetDatabase.CreateAsset(mesh, "Assets/Nav.mesh");
        }

        public static Mesh ToMesh(NavMeshTriangulation triangulation)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = triangulation.vertices;
            mesh.triangles = triangulation.indices;
            mesh.RecalculateNormals();
            return mesh;
        }

        public static Mesh ToMesh(SubMeshData subMesh)
        {
            int[] indices = new int[subMesh.TriangleIndices.Count*3];
            for (int i=0; i<subMesh.TriangleIndices.Count; ++i)
            {
                var t = subMesh.Mesh.Triangles[subMesh.TriangleIndices[i]];
                indices[i * 3] = t.A;
                indices[i * 3 + 1] = t.B;
                indices[i * 3 + 2] = t.C;
            }
            Mesh mesh = new Mesh();
            mesh.vertices = subMesh.Mesh.Vertices;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            MeshUtility.Optimize(mesh);
            return mesh;
        }
    }

}