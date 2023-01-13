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
                Vector3 from = edgeData.Vertices[seg.From].ToV3();
                Vector3 to = edgeData.Vertices[seg.To].ToV3();
                Handles.DrawLine(from, to);
                Vector3 diff = to - from;
                using(new Handles.DrawingScope(Color.green))
                {
                    Handles.ArrowHandleCap(0, from, Quaternion.LookRotation(diff.normalized), 1f, EventType.Repaint);
                }
            }
        }

        public static void DrawSDFResult(EdgeSDFResult result, EdgeData edgeData)
        {
            using (new Handles.DrawingScope(result.SDF < 0 ? Color.red : Color.blue))
            {
                Vector3 from = edgeData.Vertices[result.Segment.From].ToV3();
                Vector3 to = edgeData.Vertices[result.Segment.To].ToV3();

                Handles.DrawLine(from, to);
                Handles.DrawLine(result.Point.ToV3(), from);
                Handles.DrawLine(result.Point.ToV3(), to);
                if (!result.Segment.IsEquals(result.ConnectSegement))
                {
                    Vector3 from1 = edgeData.Vertices[result.ConnectSegement.From].ToV3();
                    Vector3 to1 = edgeData.Vertices[result.ConnectSegement.To].ToV3();
                    Handles.DrawLine(from1, to1);
                }
            }
        }
    }
}