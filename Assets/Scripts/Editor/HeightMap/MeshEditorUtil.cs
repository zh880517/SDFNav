using UnityEditor;
using UnityEngine;
namespace HeightMap
{
    public static class MeshEditorUtil
    {
        public struct Triangle2D
        {
            public Vector2 A;
            public Vector2 B;
            public Vector2 C;
            public Plane Plane3D;
            public Rect Bound;

            public bool IsInTriangle(Vector2 point)
            {
                Vector2 e0 = B - A, e1 = C - B, e2 = A - C;
                Vector2 v0 = point - A, v1 = point - B, v2 = point - C;
                float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);

                float y = Mathf.Min(s * (v0.x * e0.y - v0.y * e0.x), s * (v1.x * e1.y - v1.y * e1.x));
                y = Mathf.Min(y, s * (v2.x * e2.y - v2.y * e2.x));
                return y > 0;
            }
        }

        public static Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 normal = Vector3.Cross(ab, ac);
            return normal;
        }
        public static void AddMeshToEditorMesh(EditorMesh editorMesh, MeshRenderer renderer)
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (!meshFilter || !meshFilter.sharedMesh)
                return;
            Mesh mesh = meshFilter.sharedMesh;
            Matrix4x4 matrix = renderer.transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = matrix.MultiplyPoint(vertices[triangles[i]]);
                Vector3 b = matrix.MultiplyPoint(vertices[triangles[i + 1]]);
                Vector3 c = matrix.MultiplyPoint(vertices[triangles[i + 2]]);
                Vector3 normal = TriangleNormal(a, b, c);
                //只保留朝上的三角面
                if (Vector3.Dot(normal, Vector3.up) > 0)
                {
                    editorMesh.AddTriangle(a, b, c);
                }
            }
        }

        private static Vector2 ToV2(Vector3 p)
        {
            return new Vector2(p.x, p.z);
        }

        public static Triangle2D ToTriangle2D(Vector3 a, Vector3 b, Vector3 c)
        {
            Triangle2D triangle = new Triangle2D
            {
                A = ToV2(a), B = ToV2(b), C = ToV2(c),
                Plane3D = new Plane(a, b, c),
            };
            Vector2 min = Vector2.Min(Vector2.Min(triangle.A, triangle.B), triangle.C);
            Vector2 max = Vector2.Max(Vector2.Max(triangle.A, triangle.B), triangle.C);
            triangle.Bound = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return triangle;
        }

        public static bool TrySampleHeight(Triangle2D triangle, Vector2 pt, out float height)
        {
            height = float.MinValue;
            if (!triangle.Bound.Contains(pt) || !triangle.IsInTriangle(pt))
                return false;
            Ray ray = new Ray(new Vector3(pt.x, 100, pt.y), Vector3.down);
            if (!triangle.Plane3D.Raycast(ray, out float enter))
                return false;
            height = ray.origin.y - enter;
            return true;
        }

        public static void SampleFromTriangle(EditorHeightMap heightMap, Triangle2D triangle2D)
        {
            for (int x = 0; x < heightMap.Width; ++x)
            {
                for (int y = 0; y < heightMap.Height; ++y)
                {
                    Vector2 pt = new Vector2(x * heightMap.Grain, y * heightMap.Grain) + heightMap.Origin;
                    if (TrySampleHeight(triangle2D, pt, out float height))
                    {
                        if (height >= heightMap.MinHeight && height <= heightMap.MaxHeight)
                        {
                            int idx = x + y * heightMap.Width;
                            float v = heightMap.Data[idx];
                            if (v < height)
                            {
                                heightMap.Data[idx] = height;
                            }
                        }
                    }
                }
            }
        }

        public static void SampleFromMesh(EditorHeightMap heightMap, Mesh mesh, Matrix4x4 transform)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = transform.MultiplyPoint(vertices[triangles[i]]);
                Vector3 b = transform.MultiplyPoint(vertices[triangles[i + 1]]);
                Vector3 c = transform.MultiplyPoint(vertices[triangles[i + 2]]);
                Vector3 normal = TriangleNormal(a, b, c);
                //只保留朝上的三角面
                if (Vector3.Dot(normal, Vector3.up) > 0)
                {
                    Triangle2D triangle2D = ToTriangle2D(a, b, c);
                    SampleFromTriangle(heightMap, triangle2D);
                }
            }
        }

        public static void SampleFromEditorMesh(EditorHeightMap heightMap, EditorMesh editorMesh, float minHeight, float maxHeight)
        {
            for (int i=0;i<editorMesh.Triangles.Count; i+=3)
            {
                Vector3 a = editorMesh.Vertices[editorMesh.Triangles[i]];
                Vector3 b = editorMesh.Vertices[editorMesh.Triangles[i + 1]];
                Vector3 c = editorMesh.Vertices[editorMesh.Triangles[i + 2]];
                Triangle2D triangle2D = ToTriangle2D(a, b, c);
                for (int x=0; x<heightMap.Width; ++x)
                {
                    for (int y=0; y<heightMap.Height; ++y)
                    {
                        Vector2 pt = new Vector2(x * heightMap.Grain, y * heightMap.Grain) + heightMap.Origin;
                        if (TrySampleHeight(triangle2D, pt, out float height))
                        {
                            if (height >= minHeight && height <= maxHeight)
                            {
                                int idx = x + y * heightMap.Width;
                                float v = heightMap.Data[idx];
                                if (v < height)
                                {
                                    heightMap.Data[idx] = height;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SaveToMesh(GameObject root)
        {
            var renders = root.GetComponentsInChildren<MeshRenderer>();
            EditorMesh editorMesh = new EditorMesh();
            foreach (var render in renders)
            {
                AddMeshToEditorMesh(editorMesh, render);
            }
            SaveAsset(editorMesh.ToMesh(), "Assets/EditorMesh.mesh");
            var bounds = editorMesh.CaclBound();
            var min = bounds.min;
            var max = bounds.max;
            var size = bounds.size;
            EditorHeightMap editorHeight = EditorHeightMap.Create(new Vector2(min.x, min.z), new Vector2(size.x, size.z), 0.25f, min.y);
            editorHeight.MinHeight = min.y;
            editorHeight.MaxHeight = max.y;
            SampleFromEditorMesh(editorHeight, editorMesh, min.y, bounds.max.y);
            SaveAsset(editorHeight.ToMesh(), "Assets/HeightMapMesh.mesh");
        }

        public static void SaveAsset(Object asset, string path)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        [MenuItem("GameObject/测试")]
        static void Test()
        {
            SaveToMesh(Selection.activeGameObject);
        }
    }
}