using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace SDFNav.Editor
{
    public static class NavMeshExportUtil
    {
        private static void ReplaceIndice(int[] indices, int value, int newIdx)
        {
            for (int i=0; i<indices.Length; ++i)
            {
                if (indices[i] == value)
                    indices[i] = newIdx;
            }
        }

        public static MeshData NavToMesh(float reachHeight = 0.5f)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            triangulation = DeDuplicate(triangulation);
            int triangleCount = triangulation.indices.Length / 3;
            List<TriangleIndice> triangles = new List<TriangleIndice>(triangleCount);
            for (int i=0; i<triangleCount; ++i)
            {
                int startIndex = i * 3;
                var triangle = new TriangleIndice
                {
                    A = triangulation.indices[startIndex],
                    B = triangulation.indices[startIndex + 1],
                    C = triangulation.indices[startIndex + 2],
                };
                Vector3 a = triangulation.vertices[triangle.A];
                Vector3 b = triangulation.vertices[triangle.B];
                Vector3 c = triangulation.vertices[triangle.C];
                Vector3 center = GetTriangleCenter(a, b, c);
                //过滤掉空中的三角形
                if (center.y > reachHeight)
                    continue;
                triangles.Add(triangle);
            }
            var mesh = new MeshData
            {
                Vertices = triangulation.vertices,
                Triangles = triangles.ToArray(),
            };
            return mesh;
        }
        //去除重复的点，重新构建Mesh
        public static NavMeshTriangulation DeDuplicate(NavMeshTriangulation triangulation)
        {
            HashSet<int> duplicate = new HashSet<int>();
            int[] indices = (int[])triangulation.indices.Clone();
            for (int i = 0; i < triangulation.vertices.Length - 1; ++i)
            {
                if (duplicate.Contains(i))
                    continue;
                Vector3 p = triangulation.vertices[i];
                for (int j = 1; j < triangulation.vertices.Length; ++j)
                {
                    if (i == j || duplicate.Contains(j))
                        continue;
                    Vector3 diff = p - triangulation.vertices[j];
                    if (diff.sqrMagnitude < 0.001)
                    {
                        duplicate.Add(j);
                        //将重复的点索引替换掉
                        ReplaceIndice(indices, j, i);
                    }
                }
            }
            NavMeshTriangulation newmesh = new NavMeshTriangulation();
            //去除重复的点
            List<Vector3> vertices = new List<Vector3>(triangulation.vertices);
            for (int i=vertices.Count-1; i>=0; --i)
            {
                if (duplicate.Contains(i))
                {
                    for (int j=0;j< indices.Length; ++j)
                    {
                        int idx = indices[j];
                        if (idx > i)
                            indices[j] = idx - 1;
                    }
                    vertices.RemoveAt(i);
                }
            }
            newmesh.vertices = vertices.ToArray();
            newmesh.indices = indices;
            newmesh.areas = triangulation.areas;
            return newmesh;
        }

        public static List<SubMeshData> SplitSubMesh(MeshData mesh)
        {
            List<SubMeshData> subMeshs = new List<SubMeshData>();
            //过滤的三角形索引
            HashSet<int> filterTriangles = new HashSet<int>();
            for (int i=0; i< mesh.Triangles.Length; ++i)
            {
                if (filterTriangles.Contains(i))
                    continue;
                filterTriangles.Add(i);
                var subMesh = new SubMeshData { Mesh = mesh };
                subMeshs.Add(subMesh);
                subMesh.TriangleIndices.Add(i);
                BuildSubMesh(subMesh, filterTriangles);
            }
            return subMeshs;
        }

        public static void BuildSubMesh(SubMeshData subMesh, HashSet<int> filterTriangles)
        {
            int index = 0;
            var mesh = subMesh.Mesh;
            while(index < subMesh.TriangleIndices.Count)
            {
                var triangle = mesh.Triangles[subMesh.TriangleIndices[index]];
                for (int j = 0; j < mesh.Triangles.Length; ++j)
                {
                    if (filterTriangles.Contains(j))
                        continue;
                    var testTriangle = mesh.Triangles[j];
                    if (IsTriangleConnect(triangle, testTriangle))
                    {
                        filterTriangles.Add(j);
                        //添加到三角形列表中，在上层循环中继续判断和这个三角形相邻的三角形
                        subMesh.TriangleIndices.Add(j);
                    }
                }
                ++index;
            }
        }

        public static Vector3 GetTriangleCenter(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = Vector3.LerpUnclamped(a, b, 0.5f);
            return Vector3.LerpUnclamped(ab, c, 0.5f);
        }
        
        public static SubMeshData SelectMaxAreaSubMesh(List<SubMeshData> subMeshs)
        {
            float maxArea = 0;
            SubMeshData subMesh = null;
            foreach (var s in subMeshs)
            {
                float area = CalcSubMeshArea(s);
                if (area > maxArea)
                {
                    maxArea = area;
                    subMesh = s;
                }
            }
            return subMesh;
        }

        public static float CalcSubMeshArea(SubMeshData subMesh)
        {
            var mesh = subMesh.Mesh;
            float area = 0;
            foreach (var idx in subMesh.TriangleIndices)
            {
                var t = mesh.Triangles[idx];
                
                float a = Vector3.Distance(mesh.Vertices[t.A], mesh.Vertices[t.B]);
                float b = Vector3.Distance(mesh.Vertices[t.C], mesh.Vertices[t.B]);
                float c = Vector3.Distance(mesh.Vertices[t.A], mesh.Vertices[t.C]);

                float p = (a + b + c) / 2;//半周长
                area += Mathf.Sqrt(p * (p - a) * (p - b) * (p - c));//海伦公式计算三角形面积
            }
            return area;
        }

        public static EdgeData SubMeshToEdgeByOffset(SubMeshData subMesh)
        {
            var mesh = subMesh.Mesh;
            EdgeData edgeData = new EdgeData { Vertices = mesh.Vertices.Select(it => new Vector2(it.x, it.z)).ToArray() };
            List<SegmentIndice> segments = new List<SegmentIndice>();
            for (int i = 0; i < subMesh.TriangleIndices.Count; ++i)
            {
                var t = mesh.Triangles[subMesh.TriangleIndices[i]];
                if (!segments.Exists(it=> (t.A == it.From && t.B == it.To) || t.A == it.To && t.B == it.From))
                {
                    segments.Add(new SegmentIndice { From = t.A, To = t.B });
                }
                if (!segments.Exists(it => (t.B == it.From && t.C == it.To) || t.B == it.To && t.C == it.From))
                {
                    segments.Add(new SegmentIndice { From = t.B, To = t.C });
                }
                if (!segments.Exists(it => (t.C == it.From && t.A == it.To) || t.C == it.To && t.A == it.From))
                {
                    segments.Add(new SegmentIndice { From = t.C, To = t.A });
                }
            }

            foreach (var seg in segments)
            {
                Vector2 from = edgeData.Vertices[seg.From];
                Vector2 to = edgeData.Vertices[seg.To];
                Vector2 normal = to - from;
                Vector2 center = from + normal * 0.5f;

                normal = new Vector2(-normal.y, normal.x);
                normal.Normalize();
                if (!IsPointInSubMesh(center + normal * 0.05f, subMesh))
                {
                    edgeData.Segments.Add(seg);
                }
            }
            return edgeData;
        }
        public static bool IsPointInSubMesh(Vector2 p, SubMeshData subMesh)
        {
            foreach (var idx in subMesh.TriangleIndices )
            {
                var t = subMesh.Mesh.Triangles[idx];
                var a = subMesh.Mesh.Vertices[t.A];
                var b = subMesh.Mesh.Vertices[t.B];
                var c = subMesh.Mesh.Vertices[t.C];
                if (SDFUtil.IsInTriangle(p, new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z)))
                    return true;
            }
            return false;
        }

        public static bool IsTriangleConnect(TriangleIndice a, TriangleIndice b)
        {
            int count = 0;
            if (b.IsVertice(a.A))
                ++count;
            if (b.IsVertice(a.B))
                ++count;
            if (b.IsVertice(a.C))
                ++count;
            return count >= 2;
        }

    }
}