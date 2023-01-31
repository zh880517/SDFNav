using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace SDFNav.Editor
{
    public static class DebugMenuItem
    {
        [MenuItem("Tools/生成SDF")]
        static void Test()
        {
            var mesh = NavMeshExportUtil.NavToMesh();
            var subMeshs = NavMeshExportUtil.SplitSubMesh(mesh);
            var maxSubMesh = NavMeshExportUtil.SelectMaxAreaSubMesh(subMeshs);
            var edge = EdgeEditorUtil.SubMeshToEdgeByOffset(maxSubMesh);
            DebugDrawWindow.DrawEdge(edge);

            var sdfData = SDFExportUtil.EdgeToSDF(edge);
            DebugDrawWindow.DrawSDF(sdfData);
            var root = GameObject.Find("ObstacleCollider");
            if (root != null)
            {
                SDFScene scene = new SDFScene { Data = sdfData };
                var boxs = root.GetComponentsInChildren<BoxCollider>();
                foreach (var box in boxs)
                {
                    Vector3 center = box.transform.position;
                    float rot = box.transform.rotation.eulerAngles.y;
                    center += Quaternion.Euler(0, rot, 0) * box.center;
                    var ob = DynamicObstacleExportUtil.BoxToDynamicObstacle(sdfData, 
                        new Vector2(center.x, center.z), 
                        new Vector2(box.size.x, box.size.z), rot);
                    if (ob != null)
                    {
                        scene.Obstacles.Add(ob);
                    }
                }
                var texture = ToTexture(scene);
                var bytes = texture.EncodeToPNG();
                string path = "Assets/sdfscene.png";
                System.IO.File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path);
            }

            using (FileStream file = new FileStream("Assets/ExportData/SDF.bytes", FileMode.Create))
            {
                file.SetLength(0);
                using(BinaryWriter writer = new BinaryWriter(file))
                {
                    sdfData.Write(writer);
                }
            }
            //if (sdfData != null)
            //{
            //    var texture = SDFExportUtil.ToTexture(sdfData);
            //    var bytes = texture.EncodeToPNG();
            //    string path = "Assets/sdf.png";
            //    System.IO.File.WriteAllBytes(path, bytes);
            //    AssetDatabase.ImportAsset(path);
            //}
            /*
            var savemesh = ToMesh(maxSubMesh);
            AssetDatabase.CreateAsset(savemesh, "Assets/Nav1.mesh");
            var sdfData = SDFExportUtil.SubMeshToSDF(maxSubMesh);
            if (sdfData != null)
            {
                var texture = SDFExportUtil.ToTexture(sdfData);
                var bytes = texture.EncodeToPNG();
                string path = "Assets/sdf.png";
                System.IO.File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path);
            }
            */
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

        public static Texture2D ToTexture(SDFScene scene)
        {
            SDFData sdf = scene.Data;
            Texture2D texture = new Texture2D(sdf.Width, sdf.Height);
            for (int i = 0; i < sdf.Width; ++i)
            {
                for (int j = 0; j < sdf.Height; ++j)
                {
                    short val = sdf[i, j];
                    foreach (var ob  in scene.Obstacles)
                    {
                        val = ob.SDF(i, j, val);
                    }
                    float pencent = ((float)val) / short.MaxValue;
                    if (val <= 0)
                    {
                        texture.SetPixel(i, j, new Color(1, 0, 0, -pencent));
                    }
                    else
                    {
                        texture.SetPixel(i, j, new Color(0, 1, 0, pencent));
                    }
                }
            }
            texture.Apply();
            return texture;
        }
    }

}