using System.Collections.Generic;
using UnityEngine;
namespace HeightMap
{

    public class EditorMesh
    {
        public List<Vector3> Vertices = new List<Vector3>();
        public List<int> Triangles = new List<int>();
        const float SqrMinDistance = 0.001f * 0.001f;
        public int AddVertice(Vector3 vertice)
        {
            for (int i = 0; i < Vertices.Count; ++i)
            {
                if (Vector3.SqrMagnitude(vertice - Vertices[i]) <= SqrMinDistance)
                {
                    return i;
                }
            }
            Vertices.Add(vertice);
            return Vertices.Count - 1;
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Triangles.Add(AddVertice(a));
            Triangles.Add(AddVertice(b));
            Triangles.Add(AddVertice(c));
        }

        public Bounds CaclBound()
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var p in Vertices)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            Vector3 size = max - min;
            Vector3 center = min + size * 0.5f;
            return new Bounds(center, size);
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = Vertices.ToArray();
            mesh.triangles = Triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
    }

}