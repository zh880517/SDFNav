using System.Collections.Generic;
using System.IO;

namespace SDFNav
{
    [System.Serializable]
    public class SDFScene
    {
        public SDFData Data = new SDFData();
        public List<DynamicObstacle> Obstacles = new List<DynamicObstacle>();

        public void Write(BinaryWriter writer)
        {
            Data.Write(writer);
            writer.Write((short)Obstacles.Count);
            foreach (var ob in Obstacles)
            {
                ob.Write(writer);
            }
        }
        public void Read(BinaryReader reader)
        {
            Data.Read(reader);
            int len = reader.ReadInt16();
            Obstacles.Capacity = len;
            for (int i=0; i<len; ++i)
            {
                DynamicObstacle ob = new DynamicObstacle();
                ob.Read(reader);
                Obstacles.Add(ob);
            }
        }
    }
}