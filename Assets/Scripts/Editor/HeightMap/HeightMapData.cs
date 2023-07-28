using UnityEngine;
namespace HeightMap
{
    [System.Serializable]
    public class HeightMapData
    {
        public int Width;
        public int Height;
        public float Grain;
        public Vector2 Origin;
        public float Min;
        public float Max;
        public byte[] Data;

        public float Sample(Vector2 pos)
        {
            pos = (pos - Origin) / Grain;
            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int idx = x + y * Width;
            float rx = pos.x - x;
            float ry = pos.y - y;
            
            //2 3
            //0 1
            float v0 = Data[idx];
            float v1 = Data[idx + 1];
            float v2 = Data[idx + Width];
            float v3 = Data[idx + Width + 1];
            float v = (v0 * (1 - rx) + v1 * rx) * (1 - ry) + (v2 * (1 - rx) + v3 * rx) * ry;
            return v *(Max - Min) + Min;
        }
        public void Write(System.IO.BinaryWriter writer)
        {
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Grain);
            writer.Write(Origin.x);
            writer.Write(Min);
            writer.Write(Max);
            writer.Write(Data.Length);
            writer.Write(Data);
        }

        public void Read(System.IO.BinaryReader reader)
        {
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            Grain = reader.ReadSingle();
            Origin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            Min = reader.ReadSingle();
            Max = reader.ReadSingle();
            int len = reader.ReadInt32();
            Data = reader.ReadBytes(len);
        }
    }
}