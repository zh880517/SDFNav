using UnityEditor;
using UnityEngine;
namespace SDFNav.Editor
{
    public static class DebugDrawUtil
    {
        private static Vector3 ToV3(this Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }
        public static void DrawEdgeOnScene(EdgeData edgeData)
        {
            foreach (var seg in edgeData.Segments)
            {
                Handles.DrawLine(edgeData.Vertices[seg.From].ToV3(), edgeData.Vertices[seg.To].ToV3());
            }
        }
    }
}