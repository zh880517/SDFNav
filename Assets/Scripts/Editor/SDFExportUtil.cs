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

        public static SDFData EdgeToSDF(EdgeData edgeData, float grain = 0.25f)
        {
            var rect = EdgeEditorUtil.CalcBounds(edgeData, 1);
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
                    var result = EdgeEditorUtil.SDF(pos, edgeData);
                    float val = (result.SDF / maxDistance) * short.MaxValue;
                    data[i + width * j] = (short)val;
                }
            }
            UnityEditor.EditorUtility.ClearProgressBar();
            SDFData sdfData = new SDFData();
            sdfData.Init(width, height, grain, scale, min, data);
            return sdfData;
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
                        texture.SetPixel(i, j, new Color(1, 0, 0, 0));
                    }
                    else
                    {
                        texture.SetPixel(i, j, new Color(0, 1, 0, 1));
                    }
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
