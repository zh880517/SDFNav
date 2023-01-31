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

        public static SDFData EdgeToSDF(EdgeData edgeData, float grain = 0.5f)
        {
            var rect = EdgeEditorUtil.CalcBounds(edgeData, 1);
            Vector2 size = rect.size / grain;
            if (size.x < 1 || size.y < 1)
                return null;
            Vector2 min = rect.min;
            int width = Mathf.CeilToInt(size.x);
            int height = Mathf.CeilToInt(size.y);
            float[] originalData = new float[width * height];
            float sdAbsMax = float.MinValue;
            for (int i = 0; i < width; ++i)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("生成", $"生成SDF 行 {i + 1}", i/(float)width);
                for (int j = 0; j < height; ++j)
                {
                    Vector2 pos = min + new Vector2(i * grain, j * grain);
                    var result = EdgeEditorUtil.SDF(pos, edgeData);
                    float val = result.SDF;
                    originalData[i + width * j] = val;
                    sdAbsMax = Mathf.Max(Mathf.Abs(val), sdAbsMax);
                }
            }
            float scale = sdAbsMax / short.MaxValue;
            short[] data = new short[originalData.Length];
            for (int i=0; i<originalData.Length; ++i)
            {
                data[i] = (short)(originalData[i] / scale);
            }
            
            UnityEditor.EditorUtility.ClearProgressBar();
            SDFData sdfData = new SDFData();
            sdfData.Init(width, height, grain, scale, min, data);
            return sdfData;
        }

        public static Texture2D ToTextureWithDistance(SDFData sdf)
        {
            Texture2D texture = new Texture2D(sdf.Width, sdf.Height);
            for (int i = 0; i < sdf.Width; ++i)
            {
                for (int j = 0; j < sdf.Height; ++j)
                {
                    short val = sdf[i, j];
                    float pencent = ((float)val)/ short.MaxValue;
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
