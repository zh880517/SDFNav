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
            var mesh = NavMeshExportUtil.NavToMesh();
            var subMeshs = NavMeshExportUtil.SplitSubMesh(mesh);
            var maxSubMesh = NavMeshExportUtil.SelectMaxAreaSubMesh(subMeshs);
            var savemesh = ToMesh(maxSubMesh);
            AssetDatabase.CreateAsset(savemesh, "Assets/Nav1.mesh");
        }

        [MenuItem("Tools/NavMesh生成Mesh")]
        static void SaveNavMeshToMesh()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            triangulation = NavMeshExportUtil.DeDuplicate(triangulation);
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