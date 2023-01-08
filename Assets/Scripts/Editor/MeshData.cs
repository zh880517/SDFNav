using System.Collections.Generic;
using UnityEngine;

namespace SDFNav.Editor
{
    public struct TriangleIndice
    {
        public int A;
        public int B;
        public int C;

        public bool IsVertice(int index)
        {
            return A == index || B == index || C == index;
        }
    }
    public class MeshData
    {
        public Vector3[] Vertices;
        public TriangleIndice[] Triangles;
    }

    public class SubMeshData
    {
        public MeshData Mesh;
        public List<int> TriangleIndices = new List<int>();
    }
}
