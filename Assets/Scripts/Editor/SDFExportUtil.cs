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

        public static float SDF(Vector2 p, List<Triangle> triangles)
        {
            //此处计算错误，需要转化成多边形处理
            float sdf = triangles[0].SDF(p);
            for (int i=1; i<triangles.Count; ++i)
            {
                sdf = Mathf.Min(triangles[i].SDF(p), sdf);
            }
            return sdf;
        }

        public static Rect CalcBounds(List<Triangle> triangles, float expand)
        {
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            foreach (var t in triangles)
            {
                xMin = Mathf.Min(xMin, t.A.x);
                xMin = Mathf.Min(xMin, t.B.x);
                xMin = Mathf.Min(xMin, t.C.x);

                xMax = Mathf.Max(xMax, t.A.x);
                xMax = Mathf.Max(xMax, t.B.x);
                xMax = Mathf.Max(xMax, t.C.x);

                yMin = Mathf.Min(yMin, t.A.y);
                yMin = Mathf.Min(yMin, t.B.y);
                yMin = Mathf.Min(yMin, t.C.y);

                yMax = Mathf.Max(yMax, t.A.y);
                yMax = Mathf.Max(yMax, t.B.y);
                yMax = Mathf.Max(yMax, t.C.y);
            }

            return Rect.MinMaxRect(xMin - expand, yMin - expand, xMax + expand, yMax + expand);
        }

        public static SDFData SubMeshToSDF(SubMeshData subMesh, float grain = 0.25f)
        {
            List<Triangle> triangles = SubMeshToTriangles(subMesh);
            var rect = CalcBounds(triangles, 1);
            Vector2 size = rect.size / grain;
            if (size.x < 1 || size.y < 1)
                return null;
            Vector2 min = rect.min;
            int width = Mathf.CeilToInt(size.x);
            int height = Mathf.CeilToInt(size.y);
            float maxDistance = Mathf.Max(size.x, size.y);
            float scale = maxDistance / short.MaxValue;
            short[] data = new short[width * height];
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; ++j)
                {
                    Vector2 pos = min + new Vector2(i * grain, j * grain);
                    float val = SDF(pos, triangles);
                    val = (val / maxDistance) * short.MaxValue;
                    data[i + width * j] = (short)val;
                }
            }
            SDFData sdfData = new SDFData();
            sdfData.Init(width, height, grain, scale, min, data);
            return sdfData;
        }

        public static Texture2D ToTexture(SDFData sdf)
        {
            Texture2D texture = new Texture2D(sdf.Width, sdf.Height);
            for (int i = 0; i < sdf.Width; ++i)
            {
                for (int j = 0; j < sdf.Height; ++j)
                {
                    short val = sdf[i, j];
                    if (val <= 0)
                    {
                        texture.SetPixel(i, j, Color.red);
                    }
                    else
                    {
                        texture.SetPixel(i, j, Color.green);
                    }
                }
            }
            return texture;
        }
    }
}
