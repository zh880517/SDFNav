using UnityEngine;

namespace SDFNav.Editor
{
    public struct EdgeSDFResult
    {
        public float SDF;
        public SegmentIndice Segment;
        public SegmentIndice ConnectSegement;//距离点相同的线段
        public Vector2 Point;
    }
    public static class SDFExportUtil
    {
        public static float Cross(Vector2 lhs, Vector2 rhs)
        {
            return lhs.x * rhs.y - lhs.y * rhs.x;
        }

        public static float PointOnSegmentSide(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ap = p - a;
            Vector2 ab = b - a;
            return Mathf.Sign(Cross(ap, ab));
        }


        public static float SqrMagnitudeToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 ap = point - from;
            Vector2 ab = to - from;
            float h = Mathf.Clamp01(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab));
            return (ap - h * ab).sqrMagnitude;
        }

        public static SDFData EdgeToSDF(EdgeData edgeData, float grain = 0.25f)
        {
            var rect = CalcBounds(edgeData, 1);
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
                UnityEditor.EditorUtility.DisplayProgressBar("生成", $"生成SDF 行 {i + 1}", i/(float)width);
                for (int j = 0; j < height; ++j)
                {
                    Vector2 pos = min + new Vector2(i * grain, j * grain);
                    var result = SDF(pos, edgeData);
                    float val = (result.SDF / maxDistance) * short.MaxValue;
                    data[i + width * j] = (short)val;
                }
            }
            UnityEditor.EditorUtility.ClearProgressBar();
            SDFData sdfData = new SDFData();
            sdfData.Init(width, height, grain, scale, min, data);
            return sdfData;
        }

        public static EdgeSDFResult SDF(Vector2 p, EdgeData edgeData)
        {
            EdgeSDFResult result = new EdgeSDFResult{Point = p };
            float sqrMagnitude = float.MaxValue;
            for (int i=0; i< edgeData.Segments.Count; ++i)
            {
                var seg = edgeData.Segments[i];
                var from = edgeData.Vertices[seg.From];
                var to = edgeData.Vertices[seg.To];
                float sqrM = SqrMagnitudeToSegment(p, from, to);
                //如果点在线上就直接返回
                if (Mathf.Abs(sqrM) < 0.0001f)
                {
                    result.Segment = seg;
                    result.SDF = 0;
                    return result;
                }
                float diff = sqrM - sqrMagnitude;
                if (Mathf.Abs(diff) < 0.0001f)
                {
                    result.ConnectSegement = seg;
                    continue;
                }
                if (diff < 0)
                {
                    result.Segment = seg;
                    result.ConnectSegement = seg;
                    sqrMagnitude = sqrM;
                }
            }
            if (!result.Segment.IsEquals(result.ConnectSegement))
            {
                Vector2 a1 = edgeData.Vertices[result.Segment.From];
                Vector2 b1 = edgeData.Vertices[result.Segment.To];
                Vector2 a2 = edgeData.Vertices[result.ConnectSegement.From];
                Vector2 b2 = edgeData.Vertices[result.ConnectSegement.To];
                float side1 = PointOnSegmentSide(p, a1, b1);
                float side2 = PointOnSegmentSide(p, a2, b2);
                if (side1 != side2)
                {
                    result.SDF = -Mathf.Sqrt(sqrMagnitude);
                    return result;
                }
            }
            {
                Vector2 from = edgeData.Vertices[result.Segment.From];
                Vector2 to = edgeData.Vertices[result.Segment.To];
                Vector2 ap = p - from;
                Vector2 ab = to - from;
                float cross = Cross(ap, ab);
                if (cross < 0)
                    result.SDF = -Mathf.Sqrt(sqrMagnitude);
                else
                    result.SDF = Mathf.Sqrt(sqrMagnitude);
                return result;
            }
        }

        public static Rect CalcBounds(EdgeData edgeData, float expand)
        {
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            foreach (var seg in edgeData.Segments)
            {
                var from = edgeData.Vertices[seg.From];
                var to = edgeData.Vertices[seg.To];
                xMin = Mathf.Min(xMin, from.x);
                xMin = Mathf.Min(xMin, to.x);

                xMax = Mathf.Max(xMax, from.x);
                xMax = Mathf.Max(xMax, to.x);

                yMin = Mathf.Min(yMin, from.y);
                yMin = Mathf.Min(yMin, to.y);

                yMax = Mathf.Max(yMax, from.y);
                yMax = Mathf.Max(yMax, to.y);
            }
            return Rect.MinMaxRect(xMin - expand, yMin - expand, xMax + expand, yMax + expand);
        }

        public static Texture2D ToTexture(SDFData sdf)
        {
            Texture2D texture = new Texture2D(sdf.Width, sdf.Height, TextureFormat.Alpha8, false);
            for (int i = 0; i < sdf.Width; ++i)
            {
                for (int j = 0; j < sdf.Height; ++j)
                {
                    short val = sdf[i, j];
                    if (val <= 0)
                    {
                        texture.SetPixel(i, j, new Color(1, 1, 1, 0));
                    }
                    else
                    {
                        texture.SetPixel(i, j, new Color(1, 1, 1, 1));
                    }
                }
            }
            return texture;
        }
    }
}
